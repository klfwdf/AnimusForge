using System;
using System.Collections.Generic;
using System.IO;
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

string stageDirectory = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "..",
    "bin", "Debug", "single_module_stage", "AnimusForge",
    "bin", "Win64_Shipping_Client"));
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
Type gatewayType = animusForge.GetType("AnimusForge.Refactor.Adapters.LegacyWorldDiplomacyLlmGateway", true);
Type stageType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionStage", true);

object settings = settingsType.GetMethod("GetSettings", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
PropertyInfo eventUrl = settingsType.GetProperty("EventAndRebellionApiUrl");
PropertyInfo eventKey = settingsType.GetProperty("EventAndRebellionApiKey");
PropertyInfo eventModel = settingsType.GetProperty("EventAndRebellionModelName");
AssertTrue(eventUrl != null && eventKey != null && eventModel != null, "EventAndRebellion settings properties are unavailable");

void Configure(string endpoint)
{
    eventUrl.SetValue(settings, endpoint, null);
    eventKey.SetValue(settings, "world-replay-secret", null);
    eventModel.SetValue(settings, "world-replay-model", null);
}

object BuildRequest(string endpoint, int timeoutMilliseconds)
{
    object trace = Activator.CreateInstance(traceType, "world-replay-trace", 1L, 1L, "world-replay", "1.4");
    object provider = Activator.CreateInstance(providerType, "world", endpoint, "world-replay-model", timeoutMilliseconds, 96);
    Array messages = Array.CreateInstance(messageType, 2);
    messages.SetValue(Activator.CreateInstance(messageType, "system", "world replay system"), 0);
    messages.SetValue(Activator.CreateInstance(messageType, "user", "world replay user"), 1);
    object prompt = Activator.CreateInstance(promptType, messages, 96, "world-replay-model");
    object mainReply = Enum.Parse(stageType, "MainReply");
    return Activator.CreateInstance(requestType, trace, provider, prompt, mainReply);
}

async Task<(string Status, string ErrorCode)> InvokeAsync(string endpoint, int timeoutMilliseconds, CancellationToken token)
{
    object gateway = Activator.CreateInstance(gatewayType);
    MethodInfo method = gatewayType.GetMethod("GenerateAsync");
    Task task = (Task)method.Invoke(gateway, new[] { BuildRequest(endpoint, timeoutMilliseconds), token });
    await task.ConfigureAwait(false);
    object result = task.GetType().GetProperty("Result").GetValue(task, null);
    Type resultType = result.GetType();
    return (
        resultType.GetProperty("Status").GetValue(result, null).ToString(),
        (string)resultType.GetProperty("ErrorCode").GetValue(result, null));
}

using (ReplayServer cancelled = ReplayServer.Start(ReplayResponse.Delay(5000, 200)))
{
    Configure(cancelled.Url);
    using CancellationTokenSource token = new CancellationTokenSource(150);
    (string status, string errorCode) = await InvokeAsync(cancelled.Url, 5000, token.Token);
    AssertTrue(status == "Cancelled" && errorCode == "cancelled", "World Diplomacy caller cancellation was converted to timeout/failure");
}

using (ReplayServer retryCancelled = ReplayServer.Start(ReplayResponse.Status(500, 200)))
{
    Configure(retryCancelled.Url);
    using CancellationTokenSource token = new CancellationTokenSource(150);
    (string status, string errorCode) = await InvokeAsync(retryCancelled.Url, 5000, token.Token);
    AssertTrue(status == "Cancelled" && errorCode == "cancelled", "World Diplomacy cancellation did not interrupt retry backoff");
    AssertTrue(retryCancelled.RequestCount == 1, "World Diplomacy cancellation issued a second retry after caller cancellation");
}

using (ReplayServer timedOut = ReplayServer.Start(ReplayResponse.Delay(5000, 200)))
{
    Configure(timedOut.Url);
    (string status, string errorCode) = await InvokeAsync(timedOut.Url, 1000, CancellationToken.None);
    AssertTrue(status == "RetryableFailure" && errorCode == "world_diplomacy_domain_failure", "World Diplomacy hard timeout semantics changed");
}

Console.WriteLine("PASS worldDiplomacyGatewayReplay callerCancellation=1 retryDelayCancellation=1 timeoutIsolation=1 noCredentialLeak=1");

internal sealed class ReplayResponse
{
    public int StatusCode { get; private init; }
    public int DelayMilliseconds { get; private init; }
    public string Body { get; private init; }
    public static ReplayResponse Delay(int delayMilliseconds, int statusCode) => new() { DelayMilliseconds = delayMilliseconds, StatusCode = statusCode, Body = "{}" };
    public static ReplayResponse Status(int statusCode, int delayMilliseconds) => new() { StatusCode = statusCode, DelayMilliseconds = delayMilliseconds, Body = "{}" };
}

internal sealed class ReplayServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly ReplayResponse _response;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _acceptLoop;
    private int _requestCount;

    private ReplayServer(TcpListener listener, ReplayResponse response)
    {
        _listener = listener;
        _response = response;
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/v1/chat/completions";
        _acceptLoop = AcceptLoopAsync();
    }

    public string Url { get; }
    public int RequestCount => Volatile.Read(ref _requestCount);

    public static ReplayServer Start(ReplayResponse response)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new ReplayServer(listener, response);
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
        {
            Interlocked.Increment(ref _requestCount);
            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            int read;
            do { read = await stream.ReadAsync(buffer).ConfigureAwait(false); }
            while (read > 0 && Encoding.UTF8.GetString(buffer, 0, read).IndexOf("\r\n\r\n", StringComparison.Ordinal) < 0);
            if (_response.DelayMilliseconds > 0)
            {
                try { await Task.Delay(_response.DelayMilliseconds, _stop.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
            byte[] body = Encoding.UTF8.GetBytes(_response.Body);
            string headers = "HTTP/1.1 " + _response.StatusCode + " Test\r\nContent-Type: application/json\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            await stream.WriteAsync(headerBytes).ConfigureAwait(false);
            await stream.WriteAsync(body).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        try { _acceptLoop.GetAwaiter().GetResult(); } catch { }
    }
}
