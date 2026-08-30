using System.Net.Http;
using System.Net.Http.Headers;

namespace AnimusForge;

internal sealed class DuelSettings
{
    public static readonly HttpClient GlobalClient = new HttpClient
    {
        Timeout = Timeout.InfiniteTimeSpan
    };
}

public static class LlmApiCompat
{
    public static string BuildModelListApiUrl(string rawApiUrl)
    {
        string value = (rawApiUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri)) return value.TrimEnd('/') + "/models";
        string path = (uri.AbsolutePath ?? string.Empty).TrimEnd('/');
        if (path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            path = path.Substring(0, path.Length - "/chat/completions".Length);
        if (string.IsNullOrWhiteSpace(path)) path = "/v1";
        UriBuilder builder = new UriBuilder(uri) { Path = path + "/models", Query = string.Empty };
        return builder.Uri.ToString();
    }

    public static void ApplyAuthenticationHeaders(HttpRequestMessage request, string endpoint, string apiKey)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", (apiKey ?? string.Empty).Trim());
    }
}
