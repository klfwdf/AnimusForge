using System.Net;
using System.Net.Sockets;
using System.Text;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static PromptPackage BuildPrompt(string model = "replay-model")
{
    return new PromptPackage(
        new[]
        {
            new PromptMessage("system", "replay system"),
            new PromptMessage("user", "reply with a short answer")
        },
        96,
        model);
}

static LlmGenerateRequest BuildRequest(string endpoint, string model = "replay-model", int timeout = 2000)
{
    return new LlmGenerateRequest(
        new TraceContext("replay-trace", 1, 1, "replay", "1.4"),
        new LlmProviderSnapshot("replay", endpoint, model, timeout, 96),
        BuildPrompt(model));
}

await using (ReplayServer success = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"choices\":[{\"message\":{\"content\":\"replay ok\"}}]}")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret", disableThinking: true);
    LlmGenerateResult result = await gateway.GenerateAsync(BuildRequest(success.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.Succeeded && result.RawText == "replay ok", "success replay did not extract assistant text");
    AssertTrue(success.Requests.Count == 1, "success replay sent an unexpected request count");
    AssertTrue(success.Requests[0].Authorization == "Bearer secret", "credential did not stay at the send boundary");
    AssertTrue(success.Requests[0].Body.Contains("replay-model", StringComparison.Ordinal), "model was not sent");
}

await using (ReplayServer streaming = await ReplayServer.StartAsync((_, _) =>
    (200, "data: {\"choices\":[{\"delta\":{\"content\":\"stream \"}}]}\n\n"
        + "data: {\"choices\":[{\"delta\":{\"content\":\"reply\"}}]}\n\n"
        + "data: [DONE]\n\n")))
{
    List<string> deltas = new List<string>();
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret", disableThinking: true);
    ConfiguredChatGenerationExchange exchange = await gateway.GenerateExchangeAsync(
        BuildRequest(streaming.Url),
        streamResponse: true,
        onDelta: deltas.Add,
        CancellationToken.None);
    AssertTrue(exchange.Result.Status == LlmResultStatus.Succeeded, "streaming replay did not succeed");
    AssertTrue(exchange.Result.RawText == "stream reply", "streaming replay final text was duplicated or incomplete: " + exchange.Result.RawText);
    AssertTrue(deltas.SequenceEqual(new[] { "stream ", "reply" }), "streaming delta sequence mismatch: " + string.Join("|", deltas));
    AssertTrue(exchange.RawStreamSample.Contains("[DONE]", StringComparison.Ordinal) == false, "stream sample should not include terminal marker");
    AssertTrue(streaming.Requests.Count == 1 && streaming.Requests[0].Body.Contains("\"stream\":true", StringComparison.Ordinal), "streaming request did not set stream=true");
}

await using (ReplayServer thinkingRetry = await ReplayServer.StartAsync((index, _) => index == 1
    ? (400, "thinking unsupported; reject this control")
    : (200, "{\"choices\":[{\"message\":{\"content\":\"plain retry ok\"}}]}")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(
        _ => "secret",
        disableThinking: false,
        retryWithoutThinkingOnBadRequest: true,
        thinkingEnabled: true,
        reasoningEffort: "high");
    LlmGenerateResult result = await gateway.GenerateAsync(BuildRequest(thinkingRetry.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.Succeeded && result.RawText == "plain retry ok", "thinking plain retry did not recover");
    AssertTrue(thinkingRetry.Requests.Count == 2, "thinking retry did not send exactly two requests");
    AssertTrue(thinkingRetry.Requests[0].Body.Contains("thinking", StringComparison.Ordinal), "first request lacked thinking control");
    AssertTrue(!thinkingRetry.Requests[1].Body.Contains("thinking", StringComparison.Ordinal), "plain retry retained thinking control");
}

await using (ReplayServer serverError = await ReplayServer.StartAsync((_, _) =>
    (503, "temporary provider failure")))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    LlmGenerateResult result = await gateway.GenerateAsync(BuildRequest(serverError.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.RetryableFailure && result.ErrorCode == "http_503", "5xx replay was not retryable");
}

await using (ReplayServer slow = await ReplayServer.StartAsync(async (_, _) =>
{
    await Task.Delay(5000);
    return (200, "{\"choices\":[{\"message\":{\"content\":\"late\"}}]}");
}))
{
    LegacyConfiguredChatGateway gateway = new LegacyConfiguredChatGateway(_ => "secret");
    using CancellationTokenSource cancellation = new CancellationTokenSource(100);
    LlmGenerateResult result = await gateway.GenerateAsync(BuildRequest(slow.Url, timeout: 5000), cancellation.Token);
    AssertTrue(result.Status == LlmResultStatus.Cancelled && result.ErrorCode == "cancelled", "cancellation replay was not isolated");
}

Console.WriteLine("PASS configuredGatewayReplay success=1 streaming=1 thinkingPlainRetry=1 retryable5xx=1 cancellation=1 credentialBoundary=1");

internal sealed class ReplayServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<int, string, ValueTask<(int StatusCode, string Body)>> _handler;
    private readonly CancellationTokenSource _stop = new CancellationTokenSource();
    private readonly Task _acceptLoop;
    private int _requestIndex;

    private ReplayServer(
        TcpListener listener,
        Func<int, string, ValueTask<(int StatusCode, string Body)>> handler)
    {
        _listener = listener;
        _handler = handler;
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/v1/chat/completions";
        _acceptLoop = AcceptLoopAsync();
    }

    public string Url { get; }
    public List<ReplayRequest> Requests { get; } = new List<ReplayRequest>();

    public static async Task<ReplayServer> StartAsync(
        Func<int, string, (int StatusCode, string Body)> handler)
    {
        return await StartAsync((index, body) => new ValueTask<(int, string)>(handler(index, body)));
    }

    public static async Task<ReplayServer> StartAsync(
        Func<int, string, Task<(int StatusCode, string Body)>> handler)
    {
        return await StartAsync((index, body) => new ValueTask<(int, string)>(handler(index, body)));
    }

    private static Task<ReplayServer> StartAsync(
        Func<int, string, ValueTask<(int StatusCode, string Body)>> handler)
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
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            byte[] headerBuffer = new byte[16384];
            int count = 0;
            int headerEnd = -1;
            while (count < headerBuffer.Length && headerEnd < 0)
            {
                int read = await stream.ReadAsync(headerBuffer.AsMemory(count, headerBuffer.Length - count));
                if (read == 0)
                {
                    return;
                }
                count += read;
                headerEnd = FindHeaderEnd(headerBuffer, count);
            }
            if (headerEnd < 0)
            {
                return;
            }
            string headers = Encoding.ASCII.GetString(headerBuffer, 0, headerEnd);
            int contentLength = ReadContentLength(headers);
            int bodyStart = headerEnd + 4;
            while (count - bodyStart < contentLength)
            {
                int read = await stream.ReadAsync(headerBuffer.AsMemory(count, headerBuffer.Length - count));
                if (read == 0)
                {
                    return;
                }
                count += read;
            }
            string body = Encoding.UTF8.GetString(headerBuffer, bodyStart, contentLength);
            string authorization = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            ReplayRequest request = new ReplayRequest(authorization.Substring(authorization.IndexOf(':') + 1).Trim(), body);
            lock (Requests)
            {
                Requests.Add(request);
            }
            int index = Interlocked.Increment(ref _requestIndex);
            (int statusCode, string responseBody) = await _handler(index, body);
            byte[] payload = Encoding.UTF8.GetBytes(responseBody ?? string.Empty);
            byte[] prefix = Encoding.UTF8.GetBytes(
                "HTTP/1.1 " + statusCode + " " + (statusCode >= 200 && statusCode < 300 ? "OK" : "Error") + "\r\n"
                + "Content-Type: application/json\r\nContent-Length: " + payload.Length + "\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(prefix);
            await stream.WriteAsync(payload);
        }
    }

    private static int FindHeaderEnd(byte[] buffer, int count)
    {
        for (int i = 3; i < count; i++)
        {
            if (buffer[i - 3] == 13 && buffer[i - 2] == 10 && buffer[i - 1] == 13 && buffer[i] == 10)
            {
                return i - 3;
            }
        }
        return -1;
    }

    private static int ReadContentLength(string headers)
    {
        string line = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(value => value.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
        return line == null ? 0 : int.Parse(line.Substring(line.IndexOf(':') + 1).Trim());
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();
        try
        {
            await _acceptLoop;
        }
        catch
        {
        }
    }
}

internal sealed record ReplayRequest(string Authorization, string Body);
