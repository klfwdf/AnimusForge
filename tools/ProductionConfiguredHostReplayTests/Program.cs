using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string referenceDirectory = Path.Combine(projectRoot, ".tmp", "build_check", "1.4");
string implementationPath = Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 AnimusForge.dll is missing");

AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { AppContext.BaseDirectory, stageDirectory, referenceDirectory })
    {
        if (!Directory.Exists(root))
        {
            continue;
        }
        foreach (string candidate in Directory.GetFiles(root, name + ".dll", SearchOption.AllDirectories))
        {
            try
            {
                return Assembly.LoadFrom(candidate);
            }
            catch
            {
            }
        }
    }
    return null;
};

Assembly animusForge = Assembly.LoadFrom(implementationPath);
Type identityType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionIdentity", true);
Type channelType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionChannel", true);
Type traceType = animusForge.GetType("AnimusForge.Refactor.Contracts.TraceContext", true);
Type candidateType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionCandidate", true);
Type snapshotType = animusForge.GetType("AnimusForge.Refactor.Contracts.GameInteractionSnapshot", true);
Type envelopeType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionEnvelope", true);
Type promptMessageType = animusForge.GetType("AnimusForge.Refactor.Contracts.PromptMessage", true);
Type promptPackageType = animusForge.GetType("AnimusForge.Refactor.Contracts.PromptPackage", true);
Type ruleSelectionType = animusForge.GetType("AnimusForge.Refactor.Contracts.RuleSelection", true);
Type capabilitySetType = animusForge.GetType("AnimusForge.Refactor.Contracts.CapabilitySet", true);
Type postprocessContextType = animusForge.GetType("AnimusForge.Refactor.Contracts.PostprocessContext", true);
Type actionRequestType = animusForge.GetType("AnimusForge.Refactor.Contracts.ActionRequest", true);
Type actionPlanType = animusForge.GetType("AnimusForge.Refactor.Contracts.ActionPlan", true);
Type resultType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionResult", true);
Type runtimeConfigType = animusForge.GetType("AnimusForge.Refactor.Contracts.RuntimeConfigSnapshot", true);
Type providerType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmProviderSnapshot", true);
Type llmResultType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmGenerateResult", true);
Type facadeType = animusForge.GetType("AnimusForge.Refactor.Adapters.LegacyChannelInteractionFacade", true);
Type portsType = animusForge.GetType("AnimusForge.Refactor.Adapters.LegacyInteractionPipelinePorts", true);
Type gatewayType = animusForge.GetType("AnimusForge.Refactor.Adapters.LegacyConfiguredChatGateway", true);
Type hostType = animusForge.GetType("AnimusForge.Refactor.Runtime.DetachedInteractionHost", true);
Type hostResultType = animusForge.GetType("AnimusForge.Refactor.Runtime.DetachedInteractionHostResult", true);
Type commitResultType = animusForge.GetType("AnimusForge.Refactor.Runtime.InteractionCommitResult", true);
Type memoryInterfaceType = animusForge.GetType("AnimusForge.Refactor.Contracts.IInteractionMemory", true);
Type actionExecutorInterfaceType = animusForge.GetType("AnimusForge.Refactor.Contracts.IActionPlanExecutor", true);
Type llmStatusType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmResultStatus", true);

object New(Type type, params object[] args) => Activator.CreateInstance(type, args);
Array EmptyArray(Type elementType) => Array.CreateInstance(elementType, 0);
Array OneArray(Type elementType, object value)
{
    Array values = Array.CreateInstance(elementType, 1);
    values.SetValue(value, 0);
    return values;
}
Type Generic(Type definition, params Type[] arguments) => definition.MakeGenericType(arguments);

object TaskFromResult(Type valueType, object value)
{
    MethodInfo method = typeof(Task).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(candidate => candidate.Name == nameof(Task.FromResult) && candidate.IsGenericMethodDefinition);
    return method.MakeGenericMethod(valueType).Invoke(null, new[] { value });
}

Delegate HandlerDelegate(Type delegateType, Func<object[], object> handler)
{
    MethodInfo invoke = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);
    ParameterExpression[] parameters = invoke.GetParameters()
        .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
        .ToArray();
    NewArrayExpression arguments = Expression.NewArrayInit(
        typeof(object),
        parameters.Select(parameter => Expression.Convert(parameter, typeof(object))));
    MethodInfo dispatch = typeof(DelegateHelpers).GetMethod(nameof(DelegateHelpers.InvokeHandler), BindingFlags.Public | BindingFlags.Static);
    MethodCallExpression call = Expression.Call(dispatch, Expression.Constant(handler), arguments);
    Expression body = invoke.ReturnType == typeof(void)
        ? Expression.Block(call, Expression.Empty())
        : Expression.Convert(call, invoke.ReturnType);
    return Expression.Lambda(delegateType, body, parameters).Compile();
}

object MakeProxy(Type interfaceType, Func<MethodInfo, object[], object> handler)
{
    object proxy = DispatchProxy.Create(interfaceType, typeof(ReplayProxy));
    ((ReplayProxy)proxy).Handler = handler;
    return proxy;
}

object MakePrompt(string role, string content, string model)
{
    object message = New(promptMessageType, role, content);
    return New(promptPackageType, OneArray(promptMessageType, message), 128, model);
}

object MakeEnvelope(string channel, string sessionId, string playerText)
{
    object identity = New(identityType, sessionId, Enum.Parse(channelType, channel), sessionId + "-subject");
    object trace = New(traceType, sessionId + "-trace", 4L, 9L, "single-player", "1.4");
    object candidate = New(candidateType, sessionId + "-subject", "Replay NPC", 7, true);
    object snapshot = New(
        snapshotType,
        identity,
        trace,
        playerText,
        "replay-town",
        12,
        8,
        OneArray(candidateType, candidate),
        new[] { sessionId + "-subject" },
        new Dictionary<string, string> { ["fixture"] = "production-configured-host" });
    return New(envelopeType, snapshot, EmptyArray(promptMessageType), null, null);
}

object capabilities = New(capabilitySetType, (object)new[]
{
    "llm.generate", "prompt.compose", "postprocess.compose", "action.parse"
});
object selection = New(ruleSelectionType, (object)new[] { "fixture.rule" }, (object)Array.Empty<string>());
object context = New(postprocessContextType, (object)new[] { "fixture.rule" }, (object)Array.Empty<string>(), capabilities);
object emptyPlan = New(actionPlanType, EmptyArray(actionRequestType), "fixture-postprocess");

Type[] portParameters = portsType.GetConstructors().Single().GetParameters()
    .Select(parameter => parameter.ParameterType).ToArray();
Delegate selectRules = HandlerDelegate(portParameters[0], _ => selection);
Delegate composePrompt = HandlerDelegate(portParameters[1], _ => MakePrompt("user", "provider main", "fixture-main"));
Delegate buildContext = HandlerDelegate(portParameters[2], _ => context);
Delegate parseActions = HandlerDelegate(portParameters[3], _ => emptyPlan);
Delegate normalize = HandlerDelegate(portParameters[4], arguments => arguments[0]?.ToString() ?? string.Empty);
Delegate composePostprocess = HandlerDelegate(portParameters[6], _ => MakePrompt("user", "provider postprocess", "fixture-postprocess"));
object ports = New(
    portsType,
    selectRules,
    composePrompt,
    buildContext,
    parseActions,
    normalize,
    capabilities,
    composePostprocess);

Type credentialDelegateType = Generic(typeof(Func<,>), providerType, typeof(string));
Delegate credentialResolver = HandlerDelegate(credentialDelegateType, _ => "fixture-secret");
object MakeGateway()
{
    ConstructorInfo constructor = gatewayType.GetConstructors().Single();
    object[] arguments = new object[constructor.GetParameters().Length];
    arguments[0] = credentialResolver;
    for (int i = 1; i < arguments.Length; i++)
    {
        Type parameterType = constructor.GetParameters()[i].ParameterType;
        arguments[i] = parameterType == typeof(bool) ? (object)false : null;
    }
    return constructor.Invoke(arguments);
}

object MakeConfiguration(string endpoint, long generation = 1L)
{
    object provider = New(providerType, "fixture-provider", endpoint, "fixture-model", 5000, 128);
    object providers = Activator.CreateInstance(Generic(typeof(Dictionary<,>), typeof(string), providerType));
    providers.GetType().GetMethod("Add").Invoke(providers, new[] { "fixture-provider", provider });
    object enabledModules = new Dictionary<string, bool> { ["fixture-module"] = true };
    return New(runtimeConfigType, "fixture-profile", generation, enabledModules, providers);
}

List<string> historyRoles = new List<string>();
object memory = MakeProxy(memoryInterfaceType, (method, arguments) =>
{
    if (method.Name == "Read")
    {
        Type listType = method.ReturnType;
        return EmptyArray(listType.GetGenericArguments()[0]);
    }
    if (method.Name == "Append" && arguments.Length > 1 && arguments[1] != null)
    {
        PropertyInfo role = promptMessageType.GetProperty("Role");
        historyRoles.Add(role?.GetValue(arguments[1])?.ToString() ?? string.Empty);
    }
    return null;
});

async Task<(string Status, bool Fallback, string Visible, string CommitStatus, int Requests)> RunHostAsync(
    TestServer server,
    string channel,
    string sessionId,
    string playerText,
    bool useFallback,
    CancellationToken cancellationToken)
{
    object envelope = MakeEnvelope(channel, sessionId, playerText);
    Type captureType = Generic(typeof(Func<,>), typeof(string), envelopeType);
    Delegate capture = HandlerDelegate(captureType, _ => envelope);
    object facade = New(facadeType, ports, MakeGateway(), HandlerDelegate(typeof(Func<long>), _ => 4L), capture);
    object host = New(hostType,
        capture,
        HandlerDelegate(
            Generic(typeof(Func<,,,,,>), envelopeType, runtimeConfigType, typeof(string), typeof(string), typeof(CancellationToken), Generic(typeof(Task<>), resultType)),
            arguments => facadeType.GetMethod("GenerateAsync").Invoke(facade, arguments)),
        HandlerDelegate(
            Generic(typeof(Func<,,,,,>), envelopeType, resultType, actionExecutorInterfaceType, memoryInterfaceType, typeof(bool), commitResultType),
            arguments => facadeType.GetMethod("Commit").Invoke(facade, arguments)));

    MethodInfo execute = hostType.GetMethod("ExecuteAsync");
    ParameterInfo[] parameters = execute.GetParameters();
    Delegate actionFactory = HandlerDelegate(parameters[4].ParameterType, _ => null);
    Delegate memoryFactory = HandlerDelegate(parameters[5].ParameterType, _ => memory);
    Delegate dispatchCommit = HandlerDelegate(parameters[6].ParameterType, arguments =>
    {
        object callback = arguments[1];
        object result = callback.GetType().GetMethod("Invoke").Invoke(callback, null);
        return TaskFromResult(commitResultType, result);
    });
    Delegate fallback = HandlerDelegate(parameters[7].ParameterType, _ => Task.FromResult("legacy-fallback"));
    Delegate afterCommit = HandlerDelegate(parameters[9].ParameterType, _ => null);
    object[] arguments =
    {
        playerText,
        MakeConfiguration(server.Url),
        "fixture-module",
        "fixture-provider",
        actionFactory,
        memoryFactory,
        dispatchCommit,
        fallback,
        cancellationToken,
        afterCommit,
        true
    };
    Task task = (Task)execute.Invoke(host, arguments);
    await task.ConfigureAwait(false);
    object hostResult = task.GetType().GetProperty("Result").GetValue(task, null);
    string status = hostResultType.GetProperty("Status").GetValue(hostResult).ToString();
    bool fallbackUsed = (bool)hostResultType.GetProperty("UsedLegacyFallback").GetValue(hostResult);
    string visible = (string)hostResultType.GetProperty("VisibleReply").GetValue(hostResult);
    object commit = hostResultType.GetProperty("Commit").GetValue(hostResult);
    string commitStatus = commit == null ? string.Empty : commitResultType.GetProperty("Status").GetValue(commit).ToString();
    return (status, fallbackUsed, visible, commitStatus, server.Requests.Count);
}

await using (TestServer success = await TestServer.StartAsync((_, body) =>
    Task.FromResult((200, body.Contains("provider postprocess", StringComparison.Ordinal)
        ? "{\"choices\":[{\"message\":{\"content\":\"provider-postprocess\"}}]}"
        : "{\"choices\":[{\"message\":{\"content\":\"provider-main\"}}]}"))))
{
    foreach ((string channel, string session) in new[]
    {
        ("NativeConversation", "native-production-host"),
        ("SceneShout", "scene-production-host"),
        ("Courier", "courier-production-host")
    })
    {
        historyRoles.Clear();
        (string status, bool fallback, string visible, string commitStatus, int requests) result =
            await RunHostAsync(success, channel, session, channel + " input", false, CancellationToken.None);
        AssertTrue(result.status == "Succeeded", channel + " host status mismatch: " + result.status);
        AssertTrue(!result.fallback, channel + " unexpectedly used fallback");
        AssertTrue(result.visible == "provider-main", channel + " visible reply mismatch: " + result.visible);
        AssertTrue(result.commitStatus == "Succeeded", channel + " commit status mismatch: " + result.commitStatus);
        AssertTrue(result.requests == (channel == "NativeConversation" ? 2 : channel == "SceneShout" ? 4 : 6), channel + " did not send main/postprocess requests");
        AssertTrue(historyRoles.SequenceEqual(new[] { "user", "assistant" }), channel + " history role boundary mismatch: " + string.Join("|", historyRoles));
    }
    AssertTrue(success.Requests.All(request => request.Authorization == "Bearer fixture-secret"), "credential did not remain at send boundary");
    AssertTrue(success.Requests.All(request => request.Body.Contains("\"stream\":false", StringComparison.Ordinal)), "host generation unexpectedly used stream transport");
}

await using (TestServer failure = await TestServer.StartAsync((_, _) =>
    Task.FromResult((503, "temporary provider failure"))))
{
    historyRoles.Clear();
    (string status, bool fallback, string visible, string commitStatus, int requests) result =
        await RunHostAsync(failure, "SceneShout", "failure-production-host", "failure input", true, CancellationToken.None);
    AssertTrue(result.fallback, "provider failure did not use legacy fallback");
    AssertTrue(result.visible == "legacy-fallback", "fallback visible text mismatch");
    AssertTrue(result.commitStatus == string.Empty, "failed provider unexpectedly committed");
    AssertTrue(result.requests == 1, "provider failure request count mismatch");
    AssertTrue(historyRoles.Count == 0, "provider failure wrote history");
}

await using (TestServer slow = await TestServer.StartAsync(async (_, _) =>
{
    await Task.Delay(5000);
    return (200, "{\"choices\":[{\"message\":{\"content\":\"late\"}}]}");
}))
{
    historyRoles.Clear();
    using CancellationTokenSource cancellation = new CancellationTokenSource(100);
    (string status, bool fallback, string visible, string commitStatus, int requests) result =
        await RunHostAsync(slow, "Courier", "cancel-production-host", "cancel input", true, cancellation.Token);
    AssertTrue(result.status == "CancelledAsStale", "cancelled host status mismatch: " + result.status);
    AssertTrue(!result.fallback, "cancelled host incorrectly used fallback");
    AssertTrue(result.commitStatus == string.Empty, "cancelled host committed");
    AssertTrue(historyRoles.Count == 0, "cancelled host wrote history");
}

Console.WriteLine("PASS productionConfiguredHostReplay native=1 scene=1 courier=1 mainPostprocess=1 commitHistory=1 credentialBoundary=1 providerFallback=1 cancellationBoundary=1");

internal class ReplayProxy : DispatchProxy
{
    public Func<MethodInfo, object[], object> Handler { get; set; }
    protected override object Invoke(MethodInfo targetMethod, object[] args) => Handler(targetMethod, args ?? Array.Empty<object>());
}

internal static class DelegateHelpers
{
    public static object InvokeHandler(Func<object[], object> handler, object[] arguments) => handler(arguments);
}

internal sealed class TestServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<int, string, Task<(int StatusCode, string Body)>> _handler;
    private readonly CancellationTokenSource _stop = new CancellationTokenSource();
    private readonly Task _acceptLoop;
    private int _requestIndex;

    private TestServer(TcpListener listener, Func<int, string, Task<(int StatusCode, string Body)>> handler)
    {
        _listener = listener;
        _handler = handler;
        Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/v1/chat/completions";
        _acceptLoop = AcceptLoopAsync();
    }

    public string Url { get; }
    public List<ReplayRequest> Requests { get; } = new List<ReplayRequest>();

    public static Task<TestServer> StartAsync(Func<int, string, Task<(int StatusCode, string Body)>> handler)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return Task.FromResult(new TestServer(listener, handler));
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_stop.Token).ConfigureAwait(false);
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
            byte[] buffer = new byte[65536];
            int count = 0;
            int headerEnd = -1;
            while (count < buffer.Length && headerEnd < 0)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(count, buffer.Length - count)).ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }
                count += read;
                headerEnd = FindHeaderEnd(buffer, count);
            }
            if (headerEnd < 0)
            {
                return;
            }
            string headers = Encoding.ASCII.GetString(buffer, 0, headerEnd);
            int contentLength = ReadContentLength(headers);
            int bodyStart = headerEnd + 4;
            while (count - bodyStart < contentLength)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(count, buffer.Length - count)).ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }
                count += read;
            }
            string body = Encoding.UTF8.GetString(buffer, bodyStart, contentLength);
            string authorization = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            int separator = authorization.IndexOf(':');
            string auth = separator >= 0 ? authorization.Substring(separator + 1).Trim() : string.Empty;
            lock (Requests)
            {
                Requests.Add(new ReplayRequest(auth, body));
            }
            int index = Interlocked.Increment(ref _requestIndex);
            (int statusCode, string responseBody) = await _handler(index, body).ConfigureAwait(false);
            byte[] payload = Encoding.UTF8.GetBytes(responseBody ?? string.Empty);
            byte[] prefix = Encoding.UTF8.GetBytes(
                "HTTP/1.1 " + statusCode + " " + (statusCode >= 200 && statusCode < 300 ? "OK" : "Error") + "\r\n"
                + "Content-Type: application/json\r\nContent-Length: " + payload.Length + "\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(prefix).ConfigureAwait(false);
            await stream.WriteAsync(payload).ConfigureAwait(false);
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
            await _acceptLoop.ConfigureAwait(false);
        }
        catch
        {
        }
    }
}

internal sealed record ReplayRequest(string Authorization, string Body);
