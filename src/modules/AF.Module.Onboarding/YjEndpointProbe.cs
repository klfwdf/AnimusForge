using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

// Explicit onboarding click only: three endpoints, two header-only samples each, no credentials.
internal static class YjEndpointProbe
{
    internal const string DefaultUrl = "https://www.shenlanqaq.com/v1";
    internal const string AlternateUrl = "https://yjapi.shenlanqaq.com/v1";
    internal const string AsiaUrl = "https://asia.shenlanqaq.com/v1";
    private static readonly HttpClient Client = new HttpClient(new HttpClientHandler {
        AllowAutoRedirect = false, UseCookies = false
    }) { Timeout = Timeout.InfiniteTimeSpan };

    internal sealed class Result
    {
        internal string Url;
        internal double Milliseconds;
    }

    internal static Task<Result> SelectAsync(CancellationToken cancellation) => SelectAsync(Client, cancellation);

    internal static async Task<Result> SelectAsync(HttpClient client, CancellationToken cancellation, int timeoutMs = 3500)
    {
        var results = await Task.WhenAll(new[] { DefaultUrl, AlternateUrl, AsiaUrl }
            .Select(url => ProbeAsync(client, url, cancellation, timeoutMs))).ConfigureAwait(false);
        cancellation.ThrowIfCancellationRequested();
        return results.Where(r => r != null).OrderBy(r => r.Milliseconds).FirstOrDefault();
    }

    private static async Task<Result> ProbeAsync(HttpClient client, string url, CancellationToken cancellation, int timeoutMs)
    {
        double total = 0;
        for (int i = 0; i < 2; i++)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            using (var request = new HttpRequestMessage(HttpMethod.Get, url + "/models"))
            {
                timeout.CancelAfter(timeoutMs);
                request.Headers.Accept.ParseAdd("application/json");
                var watch = Stopwatch.StartNew();
                try
                {
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                    {
                        string media = response.Content?.Headers.ContentType?.MediaType ?? "";
                        bool apiResponse = media.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                            || media.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
                        bool reachable = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized
                            || response.StatusCode == HttpStatusCode.Forbidden;
                        if (!reachable || !apiResponse) return null;
                        total += watch.Elapsed.TotalMilliseconds;
                    }
                }
                catch (OperationCanceledException) { cancellation.ThrowIfCancellationRequested(); return null; }
                catch (HttpRequestException) { return null; }
            }
        }
        return new Result { Url = url, Milliseconds = total / 2 };
    }
}
