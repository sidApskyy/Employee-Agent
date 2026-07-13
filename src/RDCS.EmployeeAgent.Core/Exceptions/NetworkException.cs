namespace RDCS.EmployeeAgent.Core.Exceptions;

public class NetworkException : AgentException
{
    public NetworkException(string message) : base(message) { }
    public NetworkException(string message, Exception innerException) : base(message, innerException) { }
}
