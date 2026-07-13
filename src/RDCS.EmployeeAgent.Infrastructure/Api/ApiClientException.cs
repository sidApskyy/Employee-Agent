using RDCS.EmployeeAgent.Core.Exceptions;

namespace RDCS.EmployeeAgent.Infrastructure.Api;

public class ApiClientException : AgentException
{
    public int? StatusCode { get; }
    public string? ResponseContent { get; }

    public ApiClientException(string message) : base(message) { }
    public ApiClientException(string message, Exception innerException) : base(message, innerException) { }
    public ApiClientException(string message, int statusCode, string? responseContent) : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
    public ApiClientException(string message, int statusCode, string? responseContent, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}
