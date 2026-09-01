using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

static void AssertTrue(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string implementationPath = Path.Combine(projectRoot, "bin", "Debug", "single_module_stage", "AnimusForge", "bin", "Win64_Shipping_Client", "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 implementation is missing");

AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { Path.GetDirectoryName(implementationPath), Path.Combine(projectRoot, ".tmp", "build_check", "1.4") })
    {
        if (!Directory.Exists(root)) continue;
        foreach (string candidate in Directory.GetFiles(root, name + ".dll", SearchOption.AllDirectories))
        {
            try { return Assembly.LoadFrom(candidate); } catch { }
        }
    }
    return null;
};

Assembly af = Assembly.LoadFrom(implementationPath);
Type T(string name) => af.GetType(name, true);
Type interactionStatusType = T("AnimusForge.Refactor.Contracts.InteractionStatus");
Type interactionChannelType = T("AnimusForge.Refactor.Contracts.InteractionChannel");
Type identityType = T("AnimusForge.Refactor.Contracts.InteractionIdentity");
Type traceType = T("AnimusForge.Refactor.Contracts.TraceContext");
Type snapshotType = T("AnimusForge.Refactor.Contracts.GameInteractionSnapshot");
Type actionRequestType = T("AnimusForge.Refactor.Contracts.ActionRequest");
Type actionPlanType = T("AnimusForge.Refactor.Contracts.ActionPlan");
Type capabilitySetType = T("AnimusForge.Refactor.Contracts.CapabilitySet");
Type factRecordType = T("AnimusForge.Refactor.Contracts.FactRecord");
Type economyActionType = T("AnimusForge.Refactor.Contracts.EconomyRewardDebtAction");
Type economyKindType = T("AnimusForge.Refactor.Contracts.EconomyRewardDebtActionKind");
Type economyPlanType = T("AnimusForge.Refactor.Contracts.EconomyRewardDebtReplayPlan");
Type economyResultType = T("AnimusForge.Refactor.Contracts.EconomyRewardDebtReplayResult");
Type economyStatusType = T("AnimusForge.Refactor.Contracts.EconomyRewardDebtReplayStatus");
Type plannerInterfaceType = T("AnimusForge.Refactor.Contracts.IEconomyRewardDebtReplayPlanner");
Type portInterfaceType = T("AnimusForge.Refactor.Contracts.IEconomyRewardDebtMainThreadPort");
Type executorType = T("AnimusForge.Refactor.Adapters.LegacyNativeActionPlanExecutor");
Type executorReceiptType = T("AnimusForge.Refactor.Contracts.IActionPlanExecutionReceipt");
Type executorOutcomeType = T("AnimusForge.Refactor.Contracts.IActionPlanExecutionOutcomeReceipt");
Type executorEffectType = T("AnimusForge.Refactor.Contracts.IActionPlanExecutionEffectReceipt");

object New(Type type, params object[] args) => Activator.CreateInstance(type, args);
Array Empty(Type element) => Array.CreateInstance(element, 0);
Array One(Type element, object value) { Array a=Array.CreateInstance(element,1); a.SetValue(value,0); return a; }
Type Generic(Type d, params Type[] a) => d.MakeGenericType(a);

Delegate DelegateFor(Type delegateType, Func<object[], object> handler)
{
    MethodInfo invoke = delegateType.GetMethod("Invoke");
    ParameterExpression[] parameters = invoke.GetParameters().Select(x => Expression.Parameter(x.ParameterType, x.Name)).ToArray();
    NewArrayExpression args = Expression.NewArrayInit(typeof(object), parameters.Select(x => Expression.Convert(x, typeof(object))));
    MethodInfo call = typeof(Helpers).GetMethod(nameof(Helpers.Invoke), BindingFlags.Public | BindingFlags.Static);
    Expression body = Expression.Convert(Expression.Call(call, Expression.Constant(handler), args), invoke.ReturnType);
    return Expression.Lambda(delegateType, body, parameters).Compile();
}

object Proxy(Type interfaceType, Func<MethodInfo, object[], object> handler)
{
    object proxy = DispatchProxy.Create(interfaceType, typeof(ProxyImpl));
    ((ProxyImpl)proxy).Handler = handler;
    return proxy;
}

object Snapshot(string subject = "production-subject")
{
    object identity = New(identityType, "production-economy-session", Enum.Parse(interactionChannelType, "NativeConversation"), subject);
    object trace = New(traceType, "production-economy-trace", 4L, 9L, "single-player", "1.4");
    return New(snapshotType, identity, trace, "input", "town", 12, 8, Empty(T("AnimusForge.Refactor.Contracts.InteractionCandidate")), Empty(typeof(string)), new Dictionary<string,string>());
}

object Action(string tag, string target, Dictionary<string,string> parameters)
    => New(actionRequestType, tag, target, parameters);

object AllCapabilities() => New(capabilitySetType, (object)new[]
{
    "economy.reward.give_asset", "economy.reward.give_gold", "economy.debt.create", "economy.debt.resolve", "economy.settlement.transfer"
});

object MakeEconomyAction()
{
    object kind = Enum.Parse(economyKindType, "GiveGold");
    return New(economyActionType, kind, "ACTION:GIVE_GOLD", "production-subject", "GOLD", "25", "25", "", "", "", "economy.reward.give_gold", "", "");
}

object MakeEconomyPlan(int count = 1)
{
    Array actions = Array.CreateInstance(economyActionType, count);
    for (int index = 0; index < count; index++) actions.SetValue(MakeEconomyAction(), index);
    return New(economyPlanType, actions, Empty(typeof(string)));
}

object MakeFact() => New(factRecordType, "economy.confirmed", "production-subject", "gold applied");
object MakeResult(string status = "Applied", int appliedCount = 1, string errorCode = "")
    => New(economyResultType, Enum.Parse(economyStatusType, status), appliedCount, One(factRecordType, MakeFact()), errorCode);

object BuildExecutor(
    Counter counter,
    int economyActionCount = 1,
    string replayStatus = "Applied",
    int appliedCount = 1,
    string replayError = "",
    string legacyStatus = "Executed",
    bool legacyThrows = false)
{

    object planner = Proxy(plannerInterfaceType, (method, args) =>
    {
        if (method.Name == "Plan") { counter.PlannerCalls++; return MakeEconomyPlan(economyActionCount); }
        return null;
    });
    object port = Proxy(portInterfaceType, (method, args) =>
    {
        if (method.Name == "Replay") { counter.PortCalls++; return MakeResult(replayStatus, appliedCount, replayError); }
        return null;
    });
    Type actionDelegateType = Generic(typeof(Func<,,>), actionPlanType, snapshotType, interactionStatusType);
    Delegate execute = DelegateFor(actionDelegateType, args =>
    {
        counter.LegacyCalls++;
        if (legacyThrows) throw new InvalidOperationException("production legacy owner fixture");
        return Enum.Parse(interactionStatusType, legacyStatus);
    });
    Type ctor = executorType.GetConstructors().Single(candidate => candidate.GetParameters().Length == 6).GetParameters()[0].ParameterType;
    AssertTrue(ctor == actionDelegateType, "production executor delegate type mismatch");
    object[] constructorArgs = { execute, 64, null, planner, port, AllCapabilities() };
    return New(executorType, constructorArgs);
}

object mixedAction = Action("ACTION:GIVE_GOLD", "25", new Dictionary<string,string>());
object duelAction = Action("ACTION:DUEL", "npc", new Dictionary<string,string>());
Array actionArray = Array.CreateInstance(actionRequestType, 2);
actionArray.SetValue(mixedAction, 0);
actionArray.SetValue(duelAction, 1);
Array economyActionArray = Array.CreateInstance(actionRequestType, 1);
economyActionArray.SetValue(mixedAction, 0);
object mixedPlan = New(actionPlanType, actionArray, "[ACTION:GIVE_GOLD:25] [ACTION:DUEL:npc]");
object economyPlan = New(actionPlanType, economyActionArray, "[ACTION:GIVE_GOLD:25]");
Array duelActionArray = Array.CreateInstance(actionRequestType, 1);
duelActionArray.SetValue(duelAction, 0);
object duelPlan = New(actionPlanType, duelActionArray, "[ACTION:DUEL:npc]");
Array twoEconomyActionArray = Array.CreateInstance(actionRequestType, 2);
twoEconomyActionArray.SetValue(mixedAction, 0);
twoEconomyActionArray.SetValue(Action("ACTION:GIVE_GOLD", "26", new Dictionary<string,string>()), 1);
object twoEconomyPlan = New(actionPlanType, twoEconomyActionArray, "[ACTION:GIVE_GOLD:25] [ACTION:GIVE_GOLD:26]");
object snapshot = Snapshot();

Counter mixedCounter = new Counter();
object mixedExecutor = BuildExecutor(mixedCounter);
object mixedStatus = executorType.GetMethod("ValidateAndExecute").Invoke(mixedExecutor, new[] { mixedPlan, snapshot });
AssertTrue(mixedStatus.ToString() == "NonRetryableFailure"
    && executorEffectType.GetProperty("EffectState").GetValue(mixedExecutor).ToString() == "UnknownAfterStart"
    && (int)executorOutcomeType.GetProperty("AppliedActionCount").GetValue(mixedExecutor) == 1
    && (string)executorOutcomeType.GetProperty("ExecutionErrorCode").GetValue(mixedExecutor) == "duel.outcome_pending"
    && mixedCounter.LegacyCalls == 1 && mixedCounter.PlannerCalls == 1 && mixedCounter.PortCalls == 1,
    "production mixed Duel route was promoted to terminal gameplay success");
IReadOnlyList<object> mixedFacts = (IReadOnlyList<object>)executorReceiptType.GetProperty("ConfirmedFacts").GetValue(mixedExecutor);
AssertTrue(mixedFacts.Count == 1, "production receipt mismatch");

Counter onlyCounter = new Counter();
object onlyExecutor = BuildExecutor(onlyCounter);
object onlyStatus = executorType.GetMethod("ValidateAndExecute").Invoke(onlyExecutor, new[] { economyPlan, snapshot });
AssertTrue(onlyStatus.ToString() == "Executed" && onlyCounter.LegacyCalls == 0 && onlyCounter.PlannerCalls == 1 && onlyCounter.PortCalls == 1, "production economy-only route mismatch");

Counter partialCounter = new Counter();
object partialExecutor = BuildExecutor(
    partialCounter, economyActionCount: 2, replayStatus: "PartiallyApplied",
    appliedCount: 1, replayError: "economy.partial_replay");
object partialStatus = executorType.GetMethod("ValidateAndExecute").Invoke(partialExecutor, new[] { twoEconomyPlan, snapshot });
AssertTrue(partialStatus.ToString() == "NonRetryableFailure"
    && (int)executorOutcomeType.GetProperty("AppliedActionCount").GetValue(partialExecutor) == 1
    && (string)executorOutcomeType.GetProperty("ExecutionErrorCode").GetValue(partialExecutor) == "economy.partial_replay"
    && ((IReadOnlyList<object>)executorReceiptType.GetProperty("ConfirmedFacts").GetValue(partialExecutor)).Count == 1
    && partialCounter.LegacyCalls == 0 && partialCounter.PortCalls == 1,
    "production known-partial outcome mismatch");

Counter mixedRejectedCounter = new Counter();
object mixedRejectedExecutor = BuildExecutor(mixedRejectedCounter, legacyStatus: "RejectedByValidation");
object mixedRejectedStatus = executorType.GetMethod("ValidateAndExecute").Invoke(mixedRejectedExecutor, new[] { mixedPlan, snapshot });
AssertTrue(mixedRejectedStatus.ToString() == "NonRetryableFailure"
    && (int)executorOutcomeType.GetProperty("AppliedActionCount").GetValue(mixedRejectedExecutor) == 1
    && (string)executorOutcomeType.GetProperty("ExecutionErrorCode").GetValue(mixedRejectedExecutor) == "economy.applied_before_legacy_rejection"
    && ((IReadOnlyList<object>)executorReceiptType.GetProperty("ConfirmedFacts").GetValue(mixedRejectedExecutor)).Count == 1,
    "production Economy-before-legacy-rejection outcome mismatch");

Counter unknownCounter = new Counter();
object unknownExecutor = BuildExecutor(
    unknownCounter, replayStatus: "UnknownAfterStart", appliedCount: 0,
    replayError: "economy.domain_replay_exception");
object unknownStatus = executorType.GetMethod("ValidateAndExecute").Invoke(unknownExecutor, new[] { economyPlan, snapshot });
AssertTrue(unknownStatus.ToString() == "NonRetryableFailure"
    && (int)executorOutcomeType.GetProperty("AppliedActionCount").GetValue(unknownExecutor) == 0
    && (string)executorOutcomeType.GetProperty("ExecutionErrorCode").GetValue(unknownExecutor) == "economy.domain_replay_exception"
    && executorEffectType.GetProperty("EffectState").GetValue(unknownExecutor).ToString() == "UnknownAfterStart"
    && ((IReadOnlyList<object>)executorReceiptType.GetProperty("ConfirmedFacts").GetValue(unknownExecutor)).Count == 0
    && unknownCounter.LegacyCalls == 0 && unknownCounter.PortCalls == 1,
    "production unknown Economy outcome mismatch");

Counter legacyThrowCounter = new Counter();
object legacyThrowExecutor = BuildExecutor(
    legacyThrowCounter, economyActionCount: 0, appliedCount: 0, legacyThrows: true);
object legacyThrowStatus = executorType.GetMethod("ValidateAndExecute").Invoke(legacyThrowExecutor, new[] { duelPlan, snapshot });
AssertTrue(legacyThrowStatus.ToString() == "NonRetryableFailure"
    && executorEffectType.GetProperty("EffectState").GetValue(legacyThrowExecutor).ToString() == "UnknownAfterStart"
    && (int)executorOutcomeType.GetProperty("AppliedActionCount").GetValue(legacyThrowExecutor) == 0
    && legacyThrowCounter.LegacyCalls == 1 && legacyThrowCounter.PortCalls == 0,
    "production legacy-only unknown outcome mismatch");

Counter knownThenUnknownCounter = new Counter();
object knownThenUnknownExecutor = BuildExecutor(knownThenUnknownCounter, legacyThrows: true);
object knownThenUnknownStatus = executorType.GetMethod("ValidateAndExecute").Invoke(
    knownThenUnknownExecutor, new[] { mixedPlan, snapshot });
AssertTrue(knownThenUnknownStatus.ToString() == "NonRetryableFailure"
    && executorEffectType.GetProperty("EffectState").GetValue(knownThenUnknownExecutor).ToString() == "UnknownAfterStart"
    && (int)executorOutcomeType.GetProperty("AppliedActionCount").GetValue(knownThenUnknownExecutor) == 1
    && ((IReadOnlyList<object>)executorReceiptType.GetProperty("ConfirmedFacts").GetValue(knownThenUnknownExecutor)).Count == 1,
    "production known Economy plus unknown legacy outcome mismatch");

Console.WriteLine("PASS productionEconomyAwareCommit mixed=1 economyOnly=1 receipt=1 partial=2 unknown=3 productionAssembly=1");

public sealed class Counter
{
    public int LegacyCalls;
    public int PlannerCalls;
    public int PortCalls;
}

public class ProxyImpl : DispatchProxy
{
    public Func<MethodInfo, object[], object> Handler { get; set; }
    protected override object Invoke(MethodInfo targetMethod, object[] args) => Handler?.Invoke(targetMethod, args);
}

public static class Helpers
{
    public static object Invoke(Func<object[], object> handler, object[] args) => handler(args);
}
