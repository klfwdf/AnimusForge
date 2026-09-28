using System.Net;
using System.Net.Sockets;
using System.Text;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static JObject Payload(string model = "validation-model")
{
    return new JObject
    {
        ["model"] = model,
        ["messages"] = new JArray(new JObject
        {
            ["role"] = "user",
            ["content"] = "connection test"
        }),
        ["stream"] = false
    };
}

static LlmProviderSnapshot Provider(string endpoint, int timeout = 2000)
{
    return new LlmProviderSnapshot("validation", endpoint, "validation-model", timeout, 32);
}

await using (ReplayServer success = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"choices\":[{\"message\":{\"content\":\"validation ok\"}}]}")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    ConfiguredChatValidationExchange exchange = await gateway.SendValidationAsync(Provider(success.Url), Payload(), CancellationToken.None);
    AssertTrue(exchange.StatusCode == 200 && exchange.Result.Status == LlmResultStatus.Succeeded, "validation success status mismatch");
    AssertTrue(exchange.ResponseBody.Contains("validation ok", StringComparison.Ordinal), "validation response body was not captured");
    AssertTrue(success.Requests.Count == 1 && success.Requests[0].Authorization == "Bearer secret", "validation credential was not sent at boundary");
    AssertTrue(!success.Requests[0].Body.Contains("secret", StringComparison.Ordinal), "validation credential leaked into JSON body");
}

await using (ReplayServer prepared = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"choices\":[{\"message\":{\"content\":\"prepared ok\"}}]}")))
{
    const string preparedJson = "{\"model\":\"anthropic-model\",\"system\":\"already converted\",\"messages\":[{\"role\":\"user\",\"content\":\"raw body\"}]}";
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    ConfiguredChatValidationExchange exchange = await gateway.SendValidationJsonAsync(Provider(prepared.Url), preparedJson, CancellationToken.None);
    AssertTrue(exchange.Result.Status == LlmResultStatus.Succeeded, "prepared JSON validation did not succeed");
    AssertTrue(prepared.Requests.Count == 1 && prepared.Requests[0].Body == preparedJson, "prepared provider JSON was transformed a second time");
}

await using (ReplayServer failure = await ReplayServer.StartAsync((_, _) =>
    (503, "temporary validation failure")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    ConfiguredChatValidationExchange exchange = await gateway.SendValidationAsync(Provider(failure.Url), Payload(), CancellationToken.None);
    AssertTrue(exchange.StatusCode == 503 && exchange.Result.Status == LlmResultStatus.RetryableFailure, "validation HTTP failure classification mismatch");
}

await using (ReplayServer slow = await ReplayServer.StartAsync(async (_, _) =>
{
    await Task.Delay(5000);
    return (200, "{\"choices\":[{\"message\":{\"content\":\"late\"}}]}");
}))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    using CancellationTokenSource cancellation = new CancellationTokenSource(100);
    ConfiguredChatValidationExchange exchange = await gateway.SendValidationAsync(Provider(slow.Url, 5000), Payload(), cancellation.Token);
    AssertTrue(exchange.Result.Status == LlmResultStatus.Cancelled && exchange.Result.ErrorCode == "cancelled", "validation caller cancellation was not isolated");
}

await using (ReplayServer timeout = await ReplayServer.StartAsync(async (_, _) =>
{
    await Task.Delay(5000);
    return (200, "{\"choices\":[{\"message\":{\"content\":\"late\"}}]}");
}))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    ConfiguredChatValidationExchange exchange = await gateway.SendValidationAsync(Provider(timeout.Url, 100), Payload(), CancellationToken.None);
    AssertTrue(exchange.Result.Status == LlmResultStatus.Cancelled, "validation timeout was not isolated");
}

Console.WriteLine("PASS configuredChatValidationReplay success=1 preparedJsonPreserved=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1");

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
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/v1/chat/completions";
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
                Requests.Add(new ReplayRequest(authorization[(authorization.IndexOf(':') + 1)..].Trim(), body));
            }
            int index = Interlocked.Increment(ref _requestIndex);
            (int statusCode, string responseBody) = await _handler(index, body);
            byte[] response = Encoding.UTF8.GetBytes(responseBody ?? string.Empty);
            byte[] prefix = Encoding.UTF8.GetBytes("HTTP/1.1 " + statusCode + "\r\nContent-Type: application/json\r\nContent-Length: " + response.Length + "\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(prefix);
            await stream.WriteAsync(response);
        }
    }

    private static int FindHeaderEnd(byte[] buffer, int count)
    {
        for (int i = 3; i < count; i++)
        {
            if (buffer[i - 3] == 13 && buffer[i - 2] == 10 && buffer[i - 1] == 13 && buffer[i] == 10) return i - 3;
        }
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
