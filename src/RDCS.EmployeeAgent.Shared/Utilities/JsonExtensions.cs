using System.Text.Json;

namespace RDCS.EmployeeAgent.Shared.Utilities;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string ToJson<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public static T? FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }

    public static async Task<string> ToJsonAsync<T>(this T obj, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => JsonSerializer.Serialize(obj, _options), cancellationToken);
    }

    public static async Task<T?> FromJsonAsync<T>(this string json, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => JsonSerializer.Deserialize<T>(json, _options), cancellationToken);
    }
}
