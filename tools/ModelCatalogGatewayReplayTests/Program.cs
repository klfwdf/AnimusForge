using System.Net;
using System.Net.Sockets;
using System.Text;
using AnimusForge.Refactor.Adapters;

static void AssertTrue(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}

await using (ReplayServer probe = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"data\":[{\"id\":\"probe-model\"}]}")))
{
    ModelCatalogExchange result = await new LegacyModelCatalogGateway().ProbeBaseUrlAsync(probe.Url, CancellationToken.None);
    AssertTrue(result.IsSuccessStatusCode && result.StatusCode == 200, "base URL probe status mismatch");
    AssertTrue(result.RequestUrl.EndsWith("/models", StringComparison.OrdinalIgnoreCase), "model catalog URL was not normalized");
    AssertTrue(probe.Requests.Count == 1 && string.IsNullOrEmpty(probe.Requests[0].Authorization), "base URL probe sent credentials");
}

await using (ReplayServer fetch = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"data\":[{\"id\":\"model-a\"},{\"id\":\"model-b\"}]}")))
{
    ModelCatalogExchange result = await new LegacyModelCatalogGateway().FetchModelsAsync(fetch.Url, "secret-key", CancellationToken.None);
    AssertTrue(result.IsSuccessStatusCode && result.ResponseBody.Contains("model-a", StringComparison.Ordinal), "model fetch did not capture provider response");
    AssertTrue(fetch.Requests.Count == 1 && fetch.Requests[0].Authorization == "Bearer secret-key", "model fetch did not authenticate at send boundary");
    AssertTrue(!fetch.Requests[0].Body.Contains("secret-key", StringComparison.Ordinal), "model fetch credential leaked into body");
}

await using (ReplayServer failure = await ReplayServer.StartAsync((_, _) =>
    (401, "unauthorized")))
{
    ModelCatalogExchange result = await new LegacyModelCatalogGateway().FetchModelsAsync(failure.Url, "secret-key", CancellationToken.None);
    AssertTrue(result.StatusCode == 401 && !result.IsSuccessStatusCode, "model fetch HTTP failure was not preserved");
}

await using (ReplayServer slow = await ReplayServer.StartAsync(async (_, _) =>
{
    await Task.Delay(5000);
    return (200, "{\"data\":[]}");
}))
{
    using CancellationTokenSource cancellation = new CancellationTokenSource(100);
    ModelCatalogExchange result = await new LegacyModelCatalogGateway().FetchModelsAsync(slow.Url, "secret-key", cancellation.Token);
    AssertTrue(result.Cancelled && !result.HasStatusCode, "caller cancellation was not isolated");
}

ModelCatalogExchange invalid = await new LegacyModelCatalogGateway().FetchModelsAsync("", "secret-key", CancellationToken.None);
AssertTrue(!invalid.HasStatusCode && !invalid.Cancelled && !string.IsNullOrWhiteSpace(invalid.ErrorMessage), "empty model catalog URL was not rejected");

Console.WriteLine("PASS modelCatalogGatewayReplay probeNoCredential=1 fetchCredentialBoundary=1 httpFailure=1 cancellation=1 invalidConfig=1");

internal sealed class ReplayServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<int, string, Task<(int StatusCode, string Body)>> _handler;
    private readonly CancellationTokenSource _stop = new CancellationTokenSource();
    private readonly Task _acceptLoop;
    private int _requestIndex;

    private ReplayServer(TcpListener listener, Func<int, string, Task<(int, string)>> handler)
    {
        _listener = listener;
        _handler = handler;
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/v1";
        _acceptLoop = AcceptLoopAsync();
    }

    public string Url { get; }
    public List<ReplayRequest> Requests { get; } = new List<ReplayRequest>();

    public static Task<ReplayServer> StartAsync(Func<int, string, (int StatusCode, string Body)> handler)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return Task.FromResult(new ReplayServer(listener, (index, body) => Task.FromResult(handler(index, body))));
    }

    public static Task<ReplayServer> StartAsync(Func<int, string, Task<(int StatusCode, string Body)>> handler)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return Task.FromResult(new ReplayServer(listener, handler));
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_stop.Token);
                _ = HandleAsync(client);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            try
            {
                byte[] buffer = new byte[32768];
                int count = 0;
                int headerEnd = -1;
                while (count < buffer.Length && headerEnd < 0)
                {
                    int read = await stream.ReadAsync(buffer.AsMemory(count, buffer.Length - count));
                    if (read == 0) return;
                    count += read;
                    headerEnd = FindHeaderEnd(buffer, count);
                }
                if (headerEnd < 0) return;
                string headers = Encoding.ASCII.GetString(buffer, 0, headerEnd);
                int contentLength = ReadContentLength(headers);
                int bodyStart = headerEnd + 4;
                while (count - bodyStart < contentLength)
                {
                    int read = await stream.ReadAsync(buffer.AsMemory(count, buffer.Length - count));
                    if (read == 0) return;
                    count += read;
                }
                string body = Encoding.UTF8.GetString(buffer, bodyStart, contentLength);
                string authorization = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
                lock (Requests)
                {
                    Requests.Add(new ReplayRequest(
                        authorization.Contains(':') ? authorization[(authorization.IndexOf(':') + 1)..].Trim() : string.Empty,
                        body));
                }
                (int statusCode, string responseBody) = await _handler(Interlocked.Increment(ref _requestIndex), body);
                byte[] response = Encoding.UTF8.GetBytes(responseBody ?? string.Empty);
                byte[] prefix = Encoding.UTF8.GetBytes("HTTP/1.1 " + statusCode + "\r\nContent-Type: application/json\r\nContent-Length: " + response.Length + "\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(prefix);
                await stream.WriteAsync(response);
            }
            catch (Exception) { }
        }
    }

    private static int FindHeaderEnd(byte[] buffer, int count)
    {
        for (int i = 3; i < count; i++)
            if (buffer[i - 3] == 13 && buffer[i - 2] == 10 && buffer[i - 1] == 13 && buffer[i] == 10) return i - 3;
        return -1;
    }

    private static int ReadContentLength(string headers)
    {
        string line = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(value => value.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
        return line == null ? 0 : int.Parse(line[(line.IndexOf(':') + 1)..].Trim());
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();
        try { await _acceptLoop; } catch { }
    }
}

internal sealed record ReplayRequest(string Authorization, string Body);
