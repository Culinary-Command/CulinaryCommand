using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using CulinaryCommand.Models;
using CulinaryCommandApp.Services;
using System.ComponentModel.DataAnnotations;

namespace CulinaryCommand.Services
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(User user);
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetUsersByCompanyAsync(int companyId);
        Task UpdateUserAsync(User user);
        Task UpdateUserProfileAsync(int userId, string firstName, string lastName, string email, string phone, string role);
        Task DeleteUserAsync(int id);
        Task AssignLocationsAsync(int userId, List<int> locationIds);
        Task<List<Location>> GetLocationsForUserAsync(int userId);
        Task<List<User>> GetUsersForLocationAsync(int locationId, int? companyId = null);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> ValidateCredentialsAsync(string email, string password);
        Task<User> CreateAdminWithCompanyAndLocationAsync(SignupRequest req);
        Task<User> CreateInvitedUserForLocationAsync(CreateUserForLocationRequest request, int companyId, int createdByUserId);

        // Email Feat
        Task SendInviteEmailAsync(User user);
        Task<User?> GetUserByInviteTokenAsync(string token);
        Task<bool> ActivateUserAsync(string token, string password);
        Task<User> InviteUserAsync(string firstName, string lastName, string email, string role, int companyId, int createdByUserId, List<int> locationIds);
    }

    public class UserService : IUserService
    {
        private readonly PasswordHasher<User> _passwordHasher = new();
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly CognitoProvisioningService _cognito;


        public UserService(AppDbContext context, IEmailSender emailSender, CognitoProvisioningService cognito)
        {
            _context = context;
            _emailSender = emailSender;
            _cognito = cognito;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // check that the user with that email DOESN'T exist
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            { throw new Exception("Email already exists."); }

            user.Password = HashPassword(user.Password!);
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            // >>>>>>> preserve/broken-signup-logic

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.UserLocations)
                    .ThenInclude(ul => ul.Location)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.UserLocations)
                    .ThenInclude(ul => ul.Location)
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersByCompanyAsync(int companyId)
        {
            return await _context.Users
                .Where(u => u.CompanyId == companyId)
                .Include(u => u.UserLocations).ThenInclude(ul => ul.Location)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            var existing = await _context.Users
            .Include(u => u.UserLocations)
            .Include(u => u.ManagerLocations)
            .FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existing == null) return;

            existing.Name = user.Name;
            existing.Email = user.Email;
            existing.Role = user.Role;
            existing.Phone = user.Phone;
            existing.UpdatedAt = DateTime.UtcNow;

            // get the list of IDs from the incoming model
            var newUserLocationIds = user.UserLocations.Select(ul => ul.LocationId).ToList();

            // sync user-locations (general access)
            var toRemoveUL = existing.UserLocations.Where(ul => !newUserLocationIds.Contains(ul.LocationId)).ToList();
            foreach (var ul in toRemoveUL) existing.UserLocations.Remove(ul);

            foreach (var locId in newUserLocationIds)
            {
                if (!existing.UserLocations.Any(ul => ul.LocationId == locId))
                {
                    existing.UserLocations.Add(new UserLocation { UserId = user.Id, LocationId = locId });
                }
            }

            // sync manager-locations (manager access)
            if (string.Equals(existing.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                // remove manager records for locations no longer assigned
                var toRemoveML = existing.ManagerLocations.Where(ml => !newUserLocationIds.Contains(ml.LocationId)).ToList();
                foreach (var ml in toRemoveML) existing.ManagerLocations.Remove(ml);

                // add manager records for new locations
                foreach (var locId in newUserLocationIds)
                {
                    if (!existing.ManagerLocations.Any(ml => ml.LocationId == locId))
                    {
                        existing.ManagerLocations.Add(new ManagerLocation { UserId = user.Id, LocationId = locId });
                    }
                }
            }
            else
            {
                // if role was changed from Manager to something else, wipe manager entries
                existing.ManagerLocations.Clear();
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserProfileAsync(int userId, string firstName, string lastName, string email, string phone, string role)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new Exception("User not found.");

            user.Name = $"{firstName} {lastName}".Trim();
            user.Email = email;
            user.Phone = phone;
            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task AssignLocationsAsync(int userId, List<int> locationIds)
        {
            var existing = _context.UserLocations.Where(ul => ul.UserId == userId);
            _context.UserLocations.RemoveRange(existing);

            foreach (var locId in locationIds)
            {
                _context.UserLocations.Add(new UserLocation
                {
                    UserId = userId,
                    LocationId = locId
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<Location>> GetLocationsForUserAsync(int userId)
        {
            return await _context.UserLocations
                .Where(ul => ul.UserId == userId)
                .Include(ul => ul.Location)
                .Select(ul => ul.Location)
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersForLocationAsync(int locationId, int? companyId = null)
        {
            // Get the user IDs that are assigned to this location
            var userIdsQuery = _context.UserLocations
                .Where(ul => ul.LocationId == locationId)
                .Select(ul => ul.UserId);

            // Optional company filter (safe and fast)
            var usersQuery = _context.Users
                .Where(u => userIdsQuery.Contains(u.Id));

            if (companyId.HasValue)
                usersQuery = usersQuery.Where(u => u.CompanyId == companyId.Value);

            // ✅ THIS is the key: include the user's locations (and Location names)
            return await usersQuery
                .Include(u => u.UserLocations)
                    .ThenInclude(ul => ul.Location)
                .AsNoTracking()
                .ToListAsync();
        }


        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Company)
                .Include(u => u.UserLocations)
                    .ThenInclude(ul => ul.Location)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> ValidateCredentialsAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return null;

            if (VerifyPassword(password, user.Password!))
            {
                return user;
            }
            return VerifyPassword(password, user.Password) ? user : null;
        }

        private string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(new User(), password);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var result = _passwordHasher.VerifyHashedPassword(new User(), storedHash, password);
                return result == PasswordVerificationResult.Success;
            }
            catch
            {
                // Not a valid Identity hash → allow ValidateCredentialsAsync() to try the SHA256 fallback
                return false;
            }
        }

        private string LegacySha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        public async Task<User> CreateAdminWithCompanyAndLocationAsync(SignupRequest req)
        {
            if (req?.Admin == null) throw new Exception("Admin information is required.");
            if (req.Company == null) throw new Exception("Company information is required.");
            if (req.Locations == null || req.Locations.Count == 0) throw new Exception("At least one location is required.");

            ValidateOrThrow(req.Admin);

            var email = req.Admin.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                throw new Exception("A user with this email already exists.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    var company = new Company
                    {
                        Name = req.Company.Name.Trim(),
                        CompanyCode = GenerateCompanyCode(req.Company.Name),
                        Address = req.Company.Address,
                        City = req.Company.City,
                        State = req.Company.State,
                        ZipCode = req.Company.ZipCode,
                        Phone = req.Company.Phone,
                        Email = req.Company.Email,
                        Description = req.Company.Description,
                        LLCName = req.Company.LLCName,
                        TaxId = req.Company.TaxId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();

                    var createdLocations = new List<Location>();

                    foreach (var loc in req.Locations)
                    {
                        var location = new Location
                        {
                            Name = loc.Name.Trim(),
                            Address = loc.Address.Trim(),
                            City = loc.City.Trim(),
                            State = loc.State.Trim(),
                            ZipCode = loc.ZipCode.Trim(),
                            MarginEdgeKey = loc.MarginEdgeKey,
                            CompanyId = company.Id
                        };

                        _context.Locations.Add(location);
                        createdLocations.Add(location);
                    }

                    await _context.SaveChangesAsync();

                    var admin = new User
                    {
                        Name = req.Admin.Name?.Trim(),
                        Email = email,
                        Password = HashPassword(req.Admin.Password),
                        Phone = req.Admin.Phone,
                        Role = "Admin",
                        CompanyId = company.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true,
                        EmailConfirmed = true
                    };

                    _context.Users.Add(admin);
                    await _context.SaveChangesAsync();

                    foreach (var loc in createdLocations)
                    {
                        _context.UserLocations.Add(new UserLocation
                        {
                            UserId = admin.Id,
                            LocationId = loc.Id
                        });
                    }

                    await _context.SaveChangesAsync();

                    // NOTE: if this throws, the DB transaction will roll back.
                    await _cognito.CreateUserWithPasswordAsync(email, admin.Name ?? email, req.Admin.Password);

                    await tx.CommitAsync();
                    return admin;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });
        }

        private static void ValidateOrThrow(object model)
        {
            var ctx = new ValidationContext(model);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(model, ctx, results, validateAllProperties: true))
                throw new ValidationException(string.Join("\n", results.Select(r => r.ErrorMessage)));
        }

        private static string GenerateCompanyCode(string companyName)
        {
            var cleaned = new string(companyName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (cleaned.Length > 8) cleaned = cleaned[..8];
            if (cleaned.Length < 4) cleaned = cleaned.PadRight(4, 'X');

            var suffix = RandomNumberGenerator.GetInt32(1000, 9999);
            return $"{cleaned}{suffix}";
        }


        public async Task<User> CreateInvitedUserForLocationAsync(CreateUserForLocationRequest request, int companyId, int createdByUserId)
        {
            // normalize email
            var email = request.Email.Trim().ToLowerInvariant();

            // ensure no duplicate user in same company
            var exists = await _context.Users
                .AnyAsync(u => u.Email == email && u.CompanyId == companyId);

            if (exists)
            {
                throw new InvalidOperationException("A user with this email already exists for this company.");
            }

            // load company to attach navigation link
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
                throw new InvalidOperationException("Company not found.");

            // generate invite token
            var inviteToken = Guid.NewGuid().ToString("N");

            var user = new User
            {
                Name = request.FirstName.Trim() + " " + request.LastName.Trim(),
                Email = email,
                Role = request.Role,
                CompanyId = companyId,
                Company = company,              // <-- 🔥 THIS attaches the user to the company

                IsActive = false,
                EmailConfirmed = false,
                InviteToken = inviteToken,
                InviteTokenExpires = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            _context.Users.Add(user);

            // also attach via navigation property
            company.Employees ??= new List<User>();
            company.Employees.Add(user);        // <-- 🔥 Ensures Company.Employees is updated in-memory

            await _context.SaveChangesAsync();

            // assign user to the location
            _context.UserLocations.Add(new UserLocation
            {
                UserId = user.Id,
                LocationId = request.LocationId
            });

            // if they are a manager, add ManagerLocation too
            if (string.Equals(request.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                _context.ManagerLocations.Add(new ManagerLocation
                {
                    UserId = user.Id,
                    LocationId = request.LocationId
                });
            }

            await _context.SaveChangesAsync();

            return user;
        }


        public async Task SendInviteEmailAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user.InviteToken))
                throw new InvalidOperationException("User does not have an invite token.");

            var firstName = user.Name?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there";

            await _emailSender.SendInviteEmailAsync(
                user.Email!,
                firstName,
                user.InviteToken
            );
        }

        public async Task<User?> GetUserByInviteTokenAsync(string token)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.InviteToken == token &&
                                        u.InviteTokenExpires > DateTime.UtcNow &&
                                        u.IsActive == false);
        }

        public async Task<bool> ActivateUserAsync(string token, string password)
        {
            var user = await GetUserByInviteTokenAsync(token);
            if (user == null) return false;

            // Create the user in Cognito (so they can log in)
            await _cognito.CreateUserWithPasswordAsync(
                user.Email!,
                user.Name ?? user.Email!,
                password
            );

            // Mark active in DB (your existing logic)
            user.Password = HashPassword(password);
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;
            user.EmailConfirmed = true;
            user.InviteToken = null;
            user.InviteTokenExpires = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User> InviteUserAsync(
                string firstName,
                string lastName,
                string email,
                string role,
                int companyId,
                int createdByUserId,
                List<int> locationIds)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Email is required.");
            if (locationIds == null || locationIds.Count == 0) throw new InvalidOperationException("At least one location must be selected.");

            email = email.Trim().ToLowerInvariant();

            // create invited user for ONE location (your existing method)
            var primaryLocId = locationIds[0];

            var user = await CreateInvitedUserForLocationAsync(new CreateUserForLocationRequest
            {
                FirstName = firstName?.Trim() ?? "",
                LastName = lastName?.Trim() ?? "",
                Email = email,
                Role = role,
                LocationId = primaryLocId
            }, companyId, createdByUserId);

            // now assign any additional locations (since CreateInvitedUserForLocationAsync only attaches one)
            var extraLocs = locationIds.Skip(1).ToList();
            if (extraLocs.Count > 0)
            {
                // add userlocations for extras
                foreach (var locId in extraLocs)
                {
                    _context.UserLocations.Add(new UserLocation
                    {
                        UserId = user.Id,
                        LocationId = locId
                    });

                    // if manager, add manager mapping too
                    if (string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
                    {
                        _context.ManagerLocations.Add(new ManagerLocation
                        {
                            UserId = user.Id,
                            LocationId = locId
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }

            // send invite email
            await SendInviteEmailAsync(user);

            return user;
        }


    }
}