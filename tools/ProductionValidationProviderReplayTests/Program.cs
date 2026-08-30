using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

static void AssertTrue(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}

string stageDirectory = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "..",
    "bin", "Debug", "single_module_stage", "AnimusForge",
    "bin", "Win64_Shipping_Client"));
string projectRoot = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string referenceDirectory = Path.Combine(projectRoot, ".tmp", "build_check", "1.4");
string implementationPath = Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 stage implementation is missing; run unified Debug stage first");

AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string directory in new[] { AppContext.BaseDirectory, stageDirectory, referenceDirectory })
    {
        if (!Directory.Exists(directory)) continue;
        foreach (string candidate in Directory.GetFiles(directory, name + ".dll", SearchOption.AllDirectories))
        {
            try { return Assembly.LoadFrom(candidate); } catch { }
        }
    }
    return null;
};

Assembly implementation = Assembly.LoadFrom(implementationPath);
Type snapshotType = implementation.GetType("AnimusForge.Refactor.Contracts.LlmProviderSnapshot", true);
Type gatewayType = implementation.GetType("AnimusForge.Refactor.Adapters.LegacyConfiguredChatGateway", true);
Type exchangeType = implementation.GetType("AnimusForge.Refactor.Adapters.ConfiguredChatValidationExchange", true);
Type resultStatusType = implementation.GetType("AnimusForge.Refactor.Contracts.LlmResultStatus", true);
Type settingsType = implementation.GetType("AnimusForge.DuelSettings", true);
ConstructorInfo snapshotCtor = snapshotType.GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(int), typeof(int) });
ConstructorInfo gatewayCtor = gatewayType.GetConstructors().Single();
MethodInfo sendMethod = gatewayType.GetMethod("SendValidationAsync", BindingFlags.Public | BindingFlags.Instance);
AssertTrue(snapshotCtor != null && sendMethod != null, "production validation gateway reflection surface is incomplete");

ParameterExpression credentialParameter = Expression.Parameter(snapshotType, "provider");
Type credentialResolverType = typeof(Func<,>).MakeGenericType(snapshotType, typeof(string));
Delegate credentialResolver = Expression.Lambda(
    credentialResolverType,
    Expression.Constant("provider-secret"),
    credentialParameter).Compile();
object gateway = gatewayCtor.Invoke(new object[] { credentialResolver, null, true, false, false, false, false, null });

static JObject OpenAiPayload()
{
    return new JObject
    {
        ["model"] = "validation-model",
        ["messages"] = new JArray
        {
            new JObject { ["role"] = "system", ["content"] = "system text" },
            new JObject { ["role"] = "user", ["content"] = "user text" }
        },
        ["max_tokens"] = 64,
        ["stream"] = false
    };
}

static object ReadTaskResult(Task task) => task.GetType().GetProperty("Result").GetValue(task, null);
static string StatusName(object result, Type statusType) => result.GetType().GetProperty("Status").GetValue(result, null).ToString();
static string ReadString(object value, string property) => value.GetType().GetProperty(property)?.GetValue(value, null)?.ToString() ?? string.Empty;

await using (ReplayServer openAi = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"choices\":[{\"message\":{\"content\":\"openai validation ok\"}}]}")))
{
    object provider = snapshotCtor.Invoke(new object[] { "openai", openAi.Url + "/v1/chat/completions", "validation-model", 2000, 64 });
    Task task = (Task)sendMethod.Invoke(gateway, new object[] { provider, OpenAiPayload(), CancellationToken.None });
    await task.ConfigureAwait(false);
    object exchange = ReadTaskResult(task);
    object result = exchangeType.GetProperty("Result").GetValue(exchange, null);
    AssertTrue(StatusName(result, resultStatusType) == "Succeeded", "production OpenAI validation did not succeed");
    AssertTrue(ReadString(exchange, "ResponseBody").Contains("openai validation ok", StringComparison.Ordinal), "production OpenAI response was not captured");
    AssertTrue(openAi.Requests.Count == 1 && openAi.Requests[0].Headers.Contains("Authorization: Bearer provider-secret", StringComparison.OrdinalIgnoreCase), "production OpenAI credential header mismatch");
    AssertTrue(!openAi.Requests[0].Body.Contains("provider-secret", StringComparison.Ordinal), "production OpenAI credential leaked into body");
}

await using (ReplayServer anthropic = await ReplayServer.StartAsync((_, _) =>
    (200, "{\"content\":[{\"type\":\"text\",\"text\":\"anthropic validation ok\"}]}")))
{
    object provider = snapshotCtor.Invoke(new object[] { "anthropic", anthropic.Url + "/anthropic", "claude-validation", 2000, 2048 });
    JObject payload = OpenAiPayload();
    payload["model"] = "claude-validation";
    payload["max_tokens"] = 2048;
    Task task = (Task)sendMethod.Invoke(gateway, new object[] { provider, payload, CancellationToken.None });
    await task.ConfigureAwait(false);
    object exchange = ReadTaskResult(task);
    object result = exchangeType.GetProperty("Result").GetValue(exchange, null);
    AssertTrue(StatusName(result, resultStatusType) == "Succeeded", "production Anthropic validation did not succeed");
    AssertTrue(ReadString(exchange, "ResponseBody").Contains("anthropic validation ok", StringComparison.Ordinal), "production Anthropic response was not captured");
    AssertTrue(anthropic.Requests.Count == 1, "production Anthropic request count mismatch");
    string body = anthropic.Requests[0].Body;
    AssertTrue(body.Contains("\"system\":\"system text\"", StringComparison.Ordinal), "Anthropic system prompt was not converted");
    AssertTrue(!body.Contains("\"role\":\"system\"", StringComparison.Ordinal), "Anthropic system prompt remained in messages");
    AssertTrue(anthropic.Requests[0].Headers.Contains("x-api-key: provider-secret", StringComparison.OrdinalIgnoreCase), "Anthropic x-api-key header missing");
}

MethodInfo applyThinking = settingsType.GetMethod("ApplyThinkingControls", BindingFlags.Public | BindingFlags.Static);
AssertTrue(applyThinking != null, "production thinking compatibility API is missing");
JObject yjPayload = new JObject { ["model"] = "gemini-3.7-flash-high", ["messages"] = new JArray() };
object[] thinkingArgs = { yjPayload, "https://yjapi.manqiaotechnology.com/v1/chat/completions", "gemini-3.7-flash-high", false, "none", null };
applyThinking.Invoke(null, thinkingArgs);
AssertTrue(yjPayload["thinking"] == null && yjPayload["reasoning_effort"] == null, "YJ Gemini plain compatibility retained unsupported thinking controls");

Console.WriteLine("PASS productionValidationProviderReplay openAi=1 anthropic=1 yjGeminiThinking=1 credentialBoundary=1");

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
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
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
                lock (Requests)
                {
                    Requests.Add(new ReplayRequest(headers, body));
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

internal sealed record ReplayRequest(string Headers, string Body);
