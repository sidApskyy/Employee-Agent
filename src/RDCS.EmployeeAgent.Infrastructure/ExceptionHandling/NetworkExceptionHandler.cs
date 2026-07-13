using RDCS.EmployeeAgent.Core.Exceptions;

namespace RDCS.EmployeeAgent.Infrastructure.ExceptionHandling;

public static class NetworkExceptionHandler
{
    public static NetworkException HandleNetworkException(Exception exception)
    {
        if (exception is HttpRequestException httpEx)
        {
            return new NetworkException("Network request failed", httpEx);
        }

        if (exception is TimeoutException timeoutEx)
        {
            return new NetworkException("Request timed out", timeoutEx);
        }

        if (exception is System.Net.Sockets.SocketException socketEx)
        {
            return new NetworkException("Socket error occurred", socketEx);
        }

        return new NetworkException("An unexpected network error occurred", exception);
    }
}
