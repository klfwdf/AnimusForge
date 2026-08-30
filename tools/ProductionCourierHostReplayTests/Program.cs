using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
Type contracts = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionContracts", false);
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
Type actionPlanType = animusForge.GetType("AnimusForge.Refactor.Contracts.ActionPlan", true);
Type resultType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionResult", true);
Type interactionResultStatusType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionStatus", true);
Type providerType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmProviderSnapshot", true);
Type runtimeConfigType = animusForge.GetType("AnimusForge.Refactor.Contracts.RuntimeConfigSnapshot", true);
Type gatewayInterfaceType = animusForge.GetType("AnimusForge.Refactor.Contracts.ILlmGateway", true);
Type memoryInterfaceType = animusForge.GetType("AnimusForge.Refactor.Contracts.IInteractionMemory", true);
Type commitResultType = animusForge.GetType("AnimusForge.Refactor.Runtime.InteractionCommitResult", true);
Type facadeType = animusForge.GetType("AnimusForge.Refactor.Adapters.LegacyChannelInteractionFacade", true);
Type portsType = animusForge.GetType("AnimusForge.Refactor.Adapters.LegacyInteractionPipelinePorts", true);
Type courierType = animusForge.GetType("AnimusForge.CourierDeliveryBehavior", true);
Type hostType = animusForge.GetType("AnimusForge.Refactor.Runtime.DetachedInteractionHost", true);
Type hostResultType = animusForge.GetType("AnimusForge.Refactor.Runtime.DetachedInteractionHostResult", true);
Type llmRequestType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmGenerateRequest", true);
Type llmResultType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmGenerateResult", true);
Type llmStatusType = animusForge.GetType("AnimusForge.Refactor.Contracts.LlmResultStatus", true);
Type stageType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionStage", true);

object New(Type type, params object[] args) => Activator.CreateInstance(type, args);
Array EmptyArray(Type elementType) => Array.CreateInstance(elementType, 0);
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
    Expression body;
    if (invoke.ReturnType == typeof(void))
    {
        body = Expression.Block(call, Expression.Empty());
    }
    else
    {
        body = Expression.Convert(call, invoke.ReturnType);
    }
    return Expression.Lambda(delegateType, body, parameters).Compile();
}

object MakeProxy(Type interfaceType, Func<MethodInfo, object[], object> handler)
{
    object proxy = DispatchProxy.Create(interfaceType, typeof(ReplayProxy));
    ((ReplayProxy)proxy).Handler = handler;
    return proxy;
}

object InvokeStatic(Type owner, string name, params object[] arguments)
{
    MethodInfo method = owner.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(candidate => candidate.Name == name)
        .Where(candidate => candidate.GetParameters().Length == arguments.Length)
        .Single(candidate => candidate.GetParameters().Select(parameter => parameter.ParameterType)
            .Zip(arguments, (parameterType, argument) => argument == null || parameterType.IsInstanceOfType(argument))
            .All(match => match));
    return method.Invoke(null, arguments);
}

object replyEnvelope = InvokeStatic(courierType, "CaptureCourierReplyRefactorEnvelopeForExternal", "replay-courier-reply", "courier reply input");
object inboundEnvelope = InvokeStatic(courierType, "CaptureCourierInboundRefactorEnvelopeForExternal", "replay-courier-inbound", "courier inbound seed");
object activeEnvelope = replyEnvelope;
object replySnapshot = envelopeType.GetProperty("Snapshot").GetValue(replyEnvelope);
object replyTrace = snapshotType.GetProperty("Trace").GetValue(replySnapshot);
long currentRuntimeGeneration = (long)traceType.GetProperty("RuntimeGeneration").GetValue(replyTrace);
object allowedTags = new[] { "ACTION:DUEL" };
MethodInfo portsFactory = courierType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreateCourierDetachedPortsForExternal" && method.GetParameters().Length == 3);
object ports = portsFactory.Invoke(null, new object[] { allowedTags, true, 64 });
AssertTrue(ports != null, "production Courier ports factory returned null");
string portError = string.Empty;

int gatewayCalls = 0;
object gateway = MakeProxy(gatewayInterfaceType, (method, arguments) =>
{
    try
    {
        if (method.Name != "GenerateAsync")
        {
            throw new InvalidOperationException("unexpected gateway method: " + method.Name);
        }
        gatewayCalls++;
        object request = arguments[0];
        object stage = llmRequestType.GetProperty("Stage").GetValue(request);
        string raw = stage.ToString() == "Postprocess" ? "production-postprocess-raw" : "production-main-raw";
        object llmResult = New(llmResultType, Enum.Parse(llmStatusType, "Succeeded"), raw, 3, 4, "", null);
        return TaskFromResult(llmResultType, llmResult);
    }
    catch (Exception exception)
    {
        portError = "gateway: " + exception;
        throw;
    }
});

Delegate currentGeneration = HandlerDelegate(typeof(Func<long>), _ => currentRuntimeGeneration);
Delegate capture = HandlerDelegate(Generic(typeof(Func<,>), typeof(string), envelopeType), _ => activeEnvelope);
object facade = New(facadeType, ports, gateway, currentGeneration, capture);

List<string> appendedRoles = new List<string>();
object memory = MakeProxy(memoryInterfaceType, (method, arguments) =>
{
    if (method.Name == "Read")
    {
        Type promptListType = method.ReturnType;
        Type promptElementType = promptListType.GetGenericArguments()[0];
        return EmptyArray(promptElementType);
    }
    if (method.Name == "Append")
    {
        object message = arguments[1];
        appendedRoles.Add((string)promptMessageType.GetProperty("Role").GetValue(message));
    }
    return null;
});

object provider = New(providerType, "fixture-provider", "fixture://provider", "fixture-model", 5000, 128);
object providers = Activator.CreateInstance(Generic(typeof(Dictionary<,>), typeof(string), providerType));
providers.GetType().GetMethod("Add").Invoke(providers, new[] { "fixture-provider", provider });
object enabledModules = new Dictionary<string, bool> { ["fixture-module"] = true };
object configuration = New(runtimeConfigType, "fixture-profile", 1L, enabledModules, providers);

Type facadeGenerateDelegateType = Generic(typeof(Func<,,,,,>), envelopeType, runtimeConfigType, typeof(string), typeof(string), typeof(CancellationToken), Generic(typeof(Task<>), resultType));
Delegate generate = HandlerDelegate(facadeGenerateDelegateType, arguments => facadeType.GetMethod("GenerateAsync").Invoke(facade, arguments));
Type facadeCommitDelegateType = Generic(typeof(Func<,,,,,>), envelopeType, resultType, animusForge.GetType("AnimusForge.Refactor.Contracts.IActionPlanExecutor", true), memoryInterfaceType, typeof(bool), commitResultType);
Delegate commit = HandlerDelegate(facadeCommitDelegateType, arguments => facadeType.GetMethod("Commit").Invoke(facade, arguments));
object host = New(hostType, capture, generate, commit);

MethodInfo hostExecute = hostType.GetMethod("ExecuteAsync");
ParameterInfo[] hostParameters = hostExecute.GetParameters();
int commitDispatches = 0;
Delegate actionExecutorFactory = HandlerDelegate(hostParameters[4].ParameterType, _ => null);
Delegate memoryFactory = HandlerDelegate(hostParameters[5].ParameterType, _ => memory);
Delegate dispatchCommit = HandlerDelegate(hostParameters[6].ParameterType, arguments =>
{
    commitDispatches++;
    object commitCallback = arguments[1];
    object commitResult = commitCallback.GetType().GetMethod("Invoke").Invoke(commitCallback, (object[])null);
    return TaskFromResult(commitResultType, commitResult);
});
Delegate fallback = HandlerDelegate(hostParameters[7].ParameterType, _ => Task.FromResult("legacy fallback must not run"));
Delegate afterCommit = HandlerDelegate(hostParameters[9].ParameterType, _ => null);
object[] hostArguments =
{
    "production detached input",
    configuration,
    "fixture-module",
    "fixture-provider",
    actionExecutorFactory,
    memoryFactory,
    dispatchCommit,
    fallback,
    CancellationToken.None,
    afterCommit,
    true
};
Task hostTask = (Task)hostExecute.Invoke(host, hostArguments);
await hostTask.ConfigureAwait(false);
object hostResult = hostTask.GetType().GetProperty("Result").GetValue(hostTask, null);
string status = hostResultType.GetProperty("Status").GetValue(hostResult).ToString();
string visibleReply = (string)hostResultType.GetProperty("VisibleReply").GetValue(hostResult);
object commitResultValue = hostResultType.GetProperty("Commit").GetValue(hostResult);
string hostError = (string)hostResultType.GetProperty("ErrorCode").GetValue(hostResult);
bool usedFallback = (bool)hostResultType.GetProperty("UsedLegacyFallback").GetValue(hostResult);
AssertTrue(commitResultValue != null, "production detached host returned no commit: status=" + status + " error=" + hostError + " fallback=" + usedFallback + " gatewayCalls=" + gatewayCalls + " detail=" + portError);
string commitStatus = commitResultValue.GetType().GetProperty("Status").GetValue(commitResultValue).ToString();

AssertTrue(status == "Succeeded", "production detached host did not succeed: " + status);
AssertTrue(visibleReply == "production-main-raw", "production detached host visible reply mismatch: " + visibleReply);
AssertTrue(commitStatus == "Succeeded", "production detached host commit mismatch: " + commitStatus);
AssertTrue(gatewayCalls == 2, "production detached host did not execute main and postprocess stages");
AssertTrue(appendedRoles.SequenceEqual(new[] { "user", "assistant" }), "Courier reply history role order mismatch");

activeEnvelope = inboundEnvelope;
appendedRoles.Clear();
object[] inboundHostArguments =
{
    "courier inbound seed",
    configuration,
    "fixture-module",
    "fixture-provider",
    actionExecutorFactory,
    memoryFactory,
    dispatchCommit,
    fallback,
    CancellationToken.None,
    afterCommit,
    false
};
Task inboundHostTask = (Task)hostExecute.Invoke(host, inboundHostArguments);
await inboundHostTask.ConfigureAwait(false);
object inboundHostResult = inboundHostTask.GetType().GetProperty("Result").GetValue(inboundHostTask, null);
string inboundStatus = hostResultType.GetProperty("Status").GetValue(inboundHostResult).ToString();
object inboundCommit = hostResultType.GetProperty("Commit").GetValue(inboundHostResult);
AssertTrue(inboundStatus == "Succeeded", "production Courier inbound host did not succeed: " + inboundStatus);
AssertTrue(inboundCommit != null && inboundCommit.GetType().GetProperty("Status").GetValue(inboundCommit).ToString() == "Succeeded", "production Courier inbound commit failed");
AssertTrue(gatewayCalls == 4, "production Courier reply/inbound did not execute expected stages");
AssertTrue(appendedRoles.SequenceEqual(new[] { "assistant" }), "Courier inbound seed was written as user history");

int committedBeforeTerminalCases = commitDispatches;
using (CancellationTokenSource cancelled = new CancellationTokenSource())
{
    cancelled.Cancel();
    Task cancelledTask = (Task)hostExecute.Invoke(host, new object[]
    {
        "courier cancelled input", configuration, "fixture-module", "fixture-provider", actionExecutorFactory,
        memoryFactory, dispatchCommit, fallback, cancelled.Token, afterCommit, true
    });
    await cancelledTask.ConfigureAwait(false);
    object cancelledResult = cancelledTask.GetType().GetProperty("Result").GetValue(cancelledTask, null);
    AssertTrue(hostResultType.GetProperty("Status").GetValue(cancelledResult).ToString() == "CancelledAsStale", "Courier cancellation was not terminal");
}

currentRuntimeGeneration++;
Task staleTask = (Task)hostExecute.Invoke(host, new object[]
{
    "courier stale input", configuration, "fixture-module", "fixture-provider", actionExecutorFactory,
    memoryFactory, dispatchCommit, fallback, CancellationToken.None, afterCommit, true
});
await staleTask.ConfigureAwait(false);
object staleResult = staleTask.GetType().GetProperty("Result").GetValue(staleTask, null);
AssertTrue(hostResultType.GetProperty("Status").GetValue(staleResult).ToString() == "CancelledAsStale", "Courier stale generation was not terminal");
currentRuntimeGeneration--;

int fallbackCalls = 0;
Delegate countingFallback = HandlerDelegate(hostParameters[7].ParameterType, _ =>
{
    fallbackCalls++;
    return Task.FromResult("courier legacy fallback");
});
Task fallbackTask = (Task)hostExecute.Invoke(host, new object[]
{
    "courier missing provider", configuration, "fixture-module", "missing-provider", actionExecutorFactory,
    memoryFactory, dispatchCommit, countingFallback, CancellationToken.None, afterCommit, true
});
await fallbackTask.ConfigureAwait(false);
object fallbackResult = fallbackTask.GetType().GetProperty("Result").GetValue(fallbackTask, null);
AssertTrue((bool)hostResultType.GetProperty("UsedLegacyFallback").GetValue(fallbackResult)
    && (string)hostResultType.GetProperty("VisibleReply").GetValue(fallbackResult) == "courier legacy fallback"
    && fallbackCalls == 1, "Courier fallback isolation mismatch");
AssertTrue(commitDispatches == committedBeforeTerminalCases, "Courier terminal/fallback cases dispatched an unexpected commit");

Console.WriteLine("PASS productionCourierHostReplay courierPorts=1 replyMain=1 replyPostprocess=1 replyCommit=1 inboundMain=1 inboundCommit=1 inboundNoUserSeed=1 cancellationBoundary=1 fallbackIsolation=1");

internal class ReplayProxy : DispatchProxy
{
    public Func<MethodInfo, object[], object> Handler { get; set; }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        return Handler(targetMethod, args ?? Array.Empty<object>());
    }
}

internal static class DelegateHelpers
{
    public static object InvokeHandler(Func<object[], object> handler, object[] arguments) => handler(arguments);
}
