namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IExceptionHandler
{
    Task HandleExceptionAsync(Exception exception, CancellationToken cancellationToken = default);
}
