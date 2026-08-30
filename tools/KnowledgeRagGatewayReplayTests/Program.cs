using System.Net;
using System.Net.Sockets;
using System.Text;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

static void AssertTrue(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static LlmGenerateRequest Request(string endpoint, InteractionStage stage = InteractionStage.MainReply, int timeout = 2000)
{
    return new LlmGenerateRequest(
        new TraceContext("knowledge-rag-replay", 2, 2, "knowledge", "1.4"),
        new LlmProviderSnapshot("knowledge-rag", endpoint, "knowledge-model", timeout, 32),
        new PromptPackage(new[] { new PromptMessage("system", "RAG system"), new PromptMessage("user", "生成三条短句") }, 32, "knowledge-model"),
        stage);
}

await using (ReplayServer server = await ReplayServer.StartAsync((_, _) => (200, "{\"choices\":[{\"message\":{\"content\":\"知识短句\"}}]}")))
{
    LegacyKnowledgeRagGateway gateway = new LegacyKnowledgeRagGateway(_ => "rag-secret", 0.2f);
    LlmGenerateResult result = await gateway.GenerateAsync(Request(server.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.Succeeded && result.RawText == "知识短句", "success failed");
    AssertTrue(server.Requests.Count == 1, "success request count mismatch");
    AssertTrue(server.Requests[0].Authorization == "Bearer rag-secret", "credential header mismatch");
    AssertTrue(server.Requests[0].Body.Contains("knowledge-model", StringComparison.Ordinal), "model missing");
    AssertTrue(!server.Requests[0].Body.Contains("rag-secret", StringComparison.Ordinal), "credential leaked into body");
    AssertTrue(!server.Requests[0].Body.Contains("\"enabled\":true", StringComparison.Ordinal), "RAG thinking was enabled");
}

await using (ReplayServer empty = await ReplayServer.StartAsync((_, _) => (200, "{\"choices\":[{\"message\":{\"content\":\" \"}}]}")))
{
    LlmGenerateResult result = await new LegacyKnowledgeRagGateway(_ => "secret").GenerateAsync(Request(empty.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.EmptyResponse, "empty provider response was not preserved");
}

await using (ReplayServer failure = await ReplayServer.StartAsync((_, _) => (503, "provider unavailable")))
{
    LlmGenerateResult result = await new LegacyKnowledgeRagGateway(_ => "secret").GenerateAsync(Request(failure.Url), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.RetryableFailure && result.ErrorCode == "http_503", "provider failure mapping mismatch");
}

await using (ReplayServer slow = await ReplayServer.StartAsync(async (_, _) =>
{
    await Task.Delay(5000);
    return (200, "{\"choices\":[{\"message\":{\"content\":\"late\"}}]}");
}))
{
    using CancellationTokenSource cancellation = new CancellationTokenSource(100);
    LlmGenerateResult result = await new LegacyKnowledgeRagGateway(_ => "secret").GenerateAsync(Request(slow.Url, timeout: 5000), cancellation.Token);
    AssertTrue(result.Status == LlmResultStatus.Cancelled && result.ErrorCode == "cancelled", "caller cancellation was not preserved");
}

await using (ReplayServer nonMain = await ReplayServer.StartAsync((_, _) => (500, "must not be called")))
{
    LlmGenerateResult result = await new LegacyKnowledgeRagGateway(_ => "secret").GenerateAsync(Request(nonMain.Url, InteractionStage.Postprocess), CancellationToken.None);
    AssertTrue(result.Status == LlmResultStatus.NonRetryableFailure && result.ErrorCode == "knowledge_stage_not_supported", "non-main exclusion mismatch");
    AssertTrue(nonMain.Requests.Count == 0, "non-main stage reached provider");
}

Console.WriteLine("PASS knowledgeRagGatewayReplay success=1 empty=1 providerFailure=1 cancellation=1 nonMainExclusion=1 credentialBoundary=1");

internal sealed class ReplayServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<int, string, Task<(int StatusCode, string Body)>> _handler;
    private readonly CancellationTokenSource _stop = new CancellationTokenSource();
    private readonly Task _acceptLoop;
    private int _requestIndex;
    private ReplayServer(TcpListener listener, Func<int, string, Task<(int StatusCode, string Body)>> handler)
    {
        _listener = listener; _handler = handler;
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/v1/chat/completions";
        _acceptLoop = AcceptLoopAsync();
    }
    public string Url { get; }
    public List<ReplayRequest> Requests { get; } = new List<ReplayRequest>();
    public static Task<ReplayServer> StartAsync(Func<int, string, (int StatusCode, string Body)> handler)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        return Task.FromResult(new ReplayServer(listener, (index, body) => Task.FromResult(handler(index, body))));
    }
    public static Task<ReplayServer> StartAsync(Func<int, string, Task<(int StatusCode, string Body)>> handler)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        return Task.FromResult(new ReplayServer(listener, handler));
    }
    private async Task AcceptLoopAsync()
    {
        try { while (!_stop.IsCancellationRequested) _ = HandleAsync(await _listener.AcceptTcpClientAsync(_stop.Token)); }
        catch (OperationCanceledException) { } catch (ObjectDisposedException) { }
    }
    private async Task HandleAsync(TcpClient client)
    {
        using (client) using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[32768]; int count = 0; int headerEnd = -1;
            while (count < buffer.Length && headerEnd < 0)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(count, buffer.Length - count)); if (read == 0) return;
                count += read; headerEnd = FindHeaderEnd(buffer, count);
            }
            if (headerEnd < 0) return;
            string headers = Encoding.ASCII.GetString(buffer, 0, headerEnd);
            int length = ReadContentLength(headers); int bodyStart = headerEnd + 4;
            while (count - bodyStart < length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(count, buffer.Length - count)); if (read == 0) return; count += read;
            }
            string body = Encoding.UTF8.GetString(buffer, bodyStart, length);
            string authLine = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(x => x.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase)) ?? "";
            lock (Requests) Requests.Add(new ReplayRequest(authLine.Substring(authLine.IndexOf(':') + 1).Trim(), body));
            (int status, string responseBody) = await _handler(Interlocked.Increment(ref _requestIndex), body);
            byte[] payload = Encoding.UTF8.GetBytes(responseBody ?? "");
            byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 " + status + " Error\r\nContent-Type: application/json\r\nContent-Length: " + payload.Length + "\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(response); await stream.WriteAsync(payload);
        }
    }
    private static int FindHeaderEnd(byte[] buffer, int count)
    {
        for (int i = 3; i < count; i++) if (buffer[i - 3] == 13 && buffer[i - 2] == 10 && buffer[i - 1] == 13 && buffer[i] == 10) return i - 3;
        return -1;
    }
    private static int ReadContentLength(string headers)
    {
        string line = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).First(x => x.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
        return int.Parse(line.Substring(line.IndexOf(':') + 1).Trim());
    }
    public async ValueTask DisposeAsync()
    {
        _stop.Cancel(); _listener.Stop(); try { await _acceptLoop; } catch { }
    }
}

internal sealed record ReplayRequest(string Authorization, string Body);
