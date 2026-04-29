using CulinaryCommand.Data.Entities;

namespace CulinaryCommandApp.SmartTask.Services
{
    public interface IUserAvailabilityService
    {
        Task<List<UserAvailability>> GetForUserAsync(int userId, int locationId, CancellationToken cancellationToken = default);

        Task UpsertAsync(int userId, int locationId, DayOfWeek dayOfWeek, TimeOnly shiftStart, TimeOnly shiftEnd,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(int userId, int locationId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);
    }
}
