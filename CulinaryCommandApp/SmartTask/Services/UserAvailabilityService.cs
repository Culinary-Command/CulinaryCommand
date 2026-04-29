using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryCommandApp.SmartTask.Services
{
    public sealed class UserAvailabilityService : IUserAvailabilityService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public UserAvailabilityService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<UserAvailability>> GetForUserAsync(
            int userId, int locationId, CancellationToken cancellationToken = default)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await dbContext.UserAvailabilities
                .AsNoTracking()
                .Where(ua => ua.UserId == userId && ua.LocationId == locationId)
                .OrderBy(ua => ua.DayOfWeek)
                .ToListAsync(cancellationToken);
        }

        public async Task UpsertAsync(
            int userId, int locationId, DayOfWeek dayOfWeek, TimeOnly shiftStart, TimeOnly shiftEnd,
            CancellationToken cancellationToken = default)
        {
            if (shiftEnd <= shiftStart)
                throw new InvalidOperationException("Shift end must be after shift start.");

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await dbContext.UserAvailabilities
                .FirstOrDefaultAsync(ua =>
                    ua.UserId == userId &&
                    ua.LocationId == locationId &&
                    ua.DayOfWeek == dayOfWeek,
                    cancellationToken);

            if (existing is null)
            {
                dbContext.UserAvailabilities.Add(new UserAvailability
                {
                    UserId = userId,
                    LocationId = locationId,
                    DayOfWeek = dayOfWeek,
                    ShiftStart = shiftStart,
                    ShiftEnd = shiftEnd
                });
            }
            else
            {
                existing.ShiftStart = shiftStart;
                existing.ShiftEnd = shiftEnd;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(
            int userId, int locationId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await dbContext.UserAvailabilities
                .FirstOrDefaultAsync(ua =>
                    ua.UserId == userId &&
                    ua.LocationId == locationId &&
                    ua.DayOfWeek == dayOfWeek,
                    cancellationToken);

            if (existing is null) return;

            dbContext.UserAvailabilities.Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
