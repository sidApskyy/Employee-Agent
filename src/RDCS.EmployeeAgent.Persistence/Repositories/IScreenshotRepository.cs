using RDCS.EmployeeAgent.Core.Models;

namespace RDCS.EmployeeAgent.Persistence.Repositories;

public interface IScreenshotRepository
{
    Task SaveAsync(Screenshot screenshot, CancellationToken cancellationToken = default);
    Task<Screenshot?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<Screenshot>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<List<Screenshot>> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<List<Screenshot>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task DeleteOlderThanAsync(DateTime date, CancellationToken cancellationToken = default);
}
