using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

string stageDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "bin", "Debug", "single_module_stage", "AnimusForge", "bin", "Win64_Shipping_Client"));
string implementationPath = Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 AnimusForge.dll is missing");
AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { AppContext.BaseDirectory, stageDirectory })
    {
        if (!Directory.Exists(root)) continue;
        foreach (string candidate in Directory.GetFiles(root, name + ".dll", SearchOption.AllDirectories))
        {
            try { return Assembly.LoadFrom(candidate); } catch { }
        }
    }
    return null;
};
Assembly animusForge = Assembly.LoadFrom(implementationPath);
Type settingsType = animusForge.GetType("AnimusForge.DuelSettings", true);
Type traceType = animusForge.GetType("AnimusForge.Refactor.Contracts.TraceContext", true);
Type providerType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmProviderSnapshot", true);
Type messageType = animusForge.GetType("AnimusForge.Refactor.Contracts.PromptMessage", true);
Type promptType = animusForge.GetType("AnimusForge.Refactor.Contracts.PromptPackage", true);
Type requestType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmGenerateRequest", true);
Type gatewayType = animusForge.GetType("AnimusForge.Refactor.Contracts.LegacyShoutNetworkGateway", true);
Type stageType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionStage", true);
object settings = settingsType.GetMethod("GetSettings", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
PropertyInfo apiUrl = settingsType.GetProperty("ApiUrl");
PropertyInfo apiKey = settingsType.GetProperty("ApiKey");
PropertyInfo modelName = settingsType.GetProperty("ModelName");
PropertyInfo thinking = settingsType.GetProperty("MainApiThinkingEnabled");

void ConfigurePrimary(string endpoint)
{
    apiUrl.SetValue(settings, endpoint, null);
    apiKey.SetValue(settings, "replay-secret", null);
    modelName.SetValue(settings, "deepseek-replay", null);
    thinking.SetValue(settings, true, null);
}

object BuildRequest(string endpoint)
{
    object trace = Activator.CreateInstance(traceType, "sse-replay-trace", 1L, 1L, "sse-replay", "1.4");
    object provider = Activator.CreateInstance(providerType, "primary", endpoint, "deepseek-replay", 5000, 96);
    Array messages = Array.CreateInstance(messageType, 2);
    messages.SetValue(Activator.CreateInstance(messageType, "system", "replay system"), 0);
    messages.SetValue(Activator.CreateInstance(messageType, "user", "replay user"), 1);
    object prompt = Activator.CreateInstance(promptType, messages, 96, "deepseek-replay");
    object mainReply = Enum.Parse(stageType, "MainReply");
    return Activator.CreateInstance(requestType, trace, provider, prompt, mainReply);
}

async Task<(string Status, string ErrorCode, string RawText)> InvokeStreamAsync(string endpoint, Action<string> onDelta, CancellationToken token)
{
    object gateway = Activator.CreateInstance(gatewayType, new object[] { false });
    MethodInfo method = gatewayType.GetMethod("GenerateStreamAsync");
    Task task = (Task)method.Invoke(gateway, new[] { BuildRequest(endpoint), onDelta, token });
    await task.ConfigureAwait(false);
    object result = task.GetType().GetProperty("Result").GetValue(task, null);
    Type resultType = result.GetType();
    return (
        resultType.GetProperty("Status").GetValue(result, null).ToString(),
        (string)resultType.GetProperty("ErrorCode").GetValue(result, null),
        (string)resultType.GetProperty("RawText").GetValue(result, null));
}

string sseBody = "data: {\"choices\":[{\"delta\":{\"content\":\"你好\"}}]}\n\n"
    + "data: {\"choices\":[{\"delta\":{\"content\":\"世界\"}}]}\n\n"
    + "data: [DONE]\n\n";
using (SseReplayServer success = SseReplayServer.Start((_, _) => SseReplayResponse.Complete(sseBody)))
{
    ConfigurePrimary(success.Url);
    List<string> deltas = new List<string>();
    (string status, string errorCode, string rawText) = await InvokeStreamAsync(success.Url, deltas.Add, CancellationToken.None);
    AssertTrue(status == "Succeeded" && rawText == "你好世界", "production SSE final text mismatch");
    AssertTrue(string.Join("", deltas) == rawText, "production SSE delta/final parity mismatch");
    AssertTrue(success.Requests.Count == 1, "production SSE success request count mismatch");
    AssertTrue(success.Requests[0].IndexOf("replay-secret", StringComparison.Ordinal) < 0, "API key leaked into SSE request body");
}
using (SseReplayServer retry = SseReplayServer.Start((index, _) => index == 1
    ? SseReplayResponse.Error(400, "thinking unsupported")
    : SseReplayResponse.Complete(sseBody)))
{
    ConfigurePrimary(retry.Url);
    (string status, string errorCode, string rawText) = await InvokeStreamAsync(retry.Url, _ => { }, CancellationToken.None);
    AssertTrue(status == "Succeeded" && rawText == "你好世界", "production SSE thinking retry did not recover");
    AssertTrue(retry.Requests.Count == 2, "production SSE retry request count mismatch");
    AssertTrue(retry.Requests[0].IndexOf("thinking", StringComparison.Ordinal) >= 0, "production SSE first request lacked thinking controls");
    AssertTrue(retry.Requests[1].IndexOf("thinking", StringComparison.Ordinal) < 0, "production SSE plain retry retained thinking controls");
}
using (SseReplayServer slow = SseReplayServer.Start((_, _) => SseReplayResponse.DelayBeforeBody(5000, sseBody)))
{
    ConfigurePrimary(slow.Url);
    using CancellationTokenSource cancellation = new CancellationTokenSource(150);
    (string status, string errorCode, string rawText) = await InvokeStreamAsync(slow.Url, _ => { }, cancellation.Token);
    AssertTrue(status == "Cancelled" && errorCode == "cancelled", "production SSE cancellation was not isolated");
}
using (SseReplayServer stale = SseReplayServer.Start((_, _) => SseReplayResponse.DelayBeforeHeaders(1000, sseBody)))
{
    ConfigurePrimary(stale.Url);
    Task<(string Status, string ErrorCode, string RawText)> pending = InvokeStreamAsync(stale.Url, _ => { }, CancellationToken.None);
    DateTime deadline = DateTime.UtcNow.AddSeconds(2);
    while (stale.Requests.Count == 0 && DateTime.UtcNow < deadline)
    {
        Thread.Sleep(10);
    }
    Type guardType = animusForge.GetType("AnimusForge.SaveRuntimeGuard", true);
    guardType.GetMethod("AdvanceGeneration", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Invoke(null, new object[] { "sse_replay_stale" });
    (string status, string errorCode, string rawText) = await pending;
    AssertTrue(status == "Cancelled" && errorCode == "stale", "production SSE stale result was not isolated");
}
Console.WriteLine("PASS shoutNetworkSseReplay success=1 thinkingPlainRetry=1 cancellation=1 stale=1 deltaFinalParity=1 actionIsolation=1");

internal sealed class SseReplayResponse
{
    public int StatusCode { get; private set; }
    public string Body { get; private set; }
    public int DelayMilliseconds { get; private set; }
    public bool DelayHeaders { get; private set; }
    public string ContentType { get; private set; } = "text/event-stream";
    public static SseReplayResponse Complete(string body) => new SseReplayResponse { StatusCode = 200, Body = body };
    public static SseReplayResponse Error(int statusCode, string body) => new SseReplayResponse { StatusCode = statusCode, Body = body, ContentType = "application/json" };
    public static SseReplayResponse DelayBeforeBody(int delayMilliseconds, string body) => new SseReplayResponse { StatusCode = 200, Body = body, DelayMilliseconds = delayMilliseconds };
    public static SseReplayResponse DelayBeforeHeaders(int delayMilliseconds, string body) => new SseReplayResponse { StatusCode = 200, Body = body, DelayMilliseconds = delayMilliseconds, DelayHeaders = true };
}

internal sealed class SseReplayServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<int, string, SseReplayResponse> _handler;
    private readonly CancellationTokenSource _stop = new CancellationTokenSource();
    private readonly Task _acceptLoop;
    private int _requestIndex;
    private SseReplayServer(TcpListener listener, Func<int, string, SseReplayResponse> handler)
    {
        _listener = listener;
        _handler = handler;
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/v1/chat/completions";
        _acceptLoop = AcceptLoopAsync();
    }
    public string Url { get; }
    public List<string> Requests { get; } = new List<string>();
    public static SseReplayServer Start(Func<int, string, SseReplayResponse> handler)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new SseReplayServer(listener, handler);
    }
    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _ = HandleAsync(client);
            }
        }
        catch (ObjectDisposedException) { }
        catch (SocketException) { }
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
                int read = await stream.ReadAsync(buffer, count, buffer.Length - count).ConfigureAwait(false);
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
                int read = await stream.ReadAsync(buffer, count, buffer.Length - count).ConfigureAwait(false);
                if (read == 0) return;
                count += read;
            }
            string body = Encoding.UTF8.GetString(buffer, bodyStart, contentLength);
            lock (Requests) Requests.Add(body);
            SseReplayResponse response = _handler(Interlocked.Increment(ref _requestIndex), body);
            byte[] payload = Encoding.UTF8.GetBytes(response.Body ?? string.Empty);
            string prefix = "HTTP/1.1 " + response.StatusCode + " " + (response.StatusCode >= 200 && response.StatusCode < 300 ? "OK" : "Error") + "\r\n"
                + "Content-Type: " + response.ContentType + "\r\nContent-Length: " + payload.Length + "\r\nConnection: close\r\n\r\n";
            if (response.DelayHeaders && response.DelayMilliseconds > 0) await Task.Delay(response.DelayMilliseconds).ConfigureAwait(false);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(prefix)).ConfigureAwait(false);
            if (!response.DelayHeaders && response.DelayMilliseconds > 0) await Task.Delay(response.DelayMilliseconds).ConfigureAwait(false);
            await stream.WriteAsync(payload).ConfigureAwait(false);
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
        string line = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(value => value.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
        return line == null ? 0 : int.Parse(line.Substring(line.IndexOf(':') + 1).Trim());
    }
    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        try { _acceptLoop.GetAwaiter().GetResult(); } catch { }
    }
}
