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

object MakeEconomyPlan()
    => New(economyPlanType, One(economyActionType, MakeEconomyAction()), Empty(typeof(string)));

object MakeFact() => New(factRecordType, "economy.confirmed", "production-subject", "gold applied");
object MakeResult()
    => New(economyResultType, Enum.Parse(economyStatusType, "Applied"), 1, One(factRecordType, MakeFact()), "");

object BuildExecutor(Counter counter)
{

    object planner = Proxy(plannerInterfaceType, (method, args) =>
    {
        if (method.Name == "Plan") { counter.PlannerCalls++; return MakeEconomyPlan(); }
        return null;
    });
    object port = Proxy(portInterfaceType, (method, args) =>
    {
        if (method.Name == "Replay") { counter.PortCalls++; return MakeResult(); }
        return null;
    });
    Type actionDelegateType = Generic(typeof(Func<,,>), actionPlanType, snapshotType, interactionStatusType);
    Delegate execute = DelegateFor(actionDelegateType, args =>
    {
        counter.LegacyCalls++;
        return Enum.Parse(interactionStatusType, "Executed");
    });
    Type ctor = executorType.GetConstructors().Single().GetParameters()[0].ParameterType;
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
object snapshot = Snapshot();

Counter mixedCounter = new Counter();
object mixedExecutor = BuildExecutor(mixedCounter);
object mixedStatus = executorType.GetMethod("ValidateAndExecute").Invoke(mixedExecutor, new[] { mixedPlan, snapshot });
AssertTrue(mixedStatus.ToString() == "Executed" && mixedCounter.LegacyCalls == 1 && mixedCounter.PlannerCalls == 1 && mixedCounter.PortCalls == 1, "production mixed route mismatch");
IReadOnlyList<object> mixedFacts = (IReadOnlyList<object>)executorReceiptType.GetProperty("ConfirmedFacts").GetValue(mixedExecutor);
AssertTrue(mixedFacts.Count == 1, "production receipt mismatch");

Counter onlyCounter = new Counter();
object onlyExecutor = BuildExecutor(onlyCounter);
object onlyStatus = executorType.GetMethod("ValidateAndExecute").Invoke(onlyExecutor, new[] { economyPlan, snapshot });
AssertTrue(onlyStatus.ToString() == "Executed" && onlyCounter.LegacyCalls == 0 && onlyCounter.PlannerCalls == 1 && onlyCounter.PortCalls == 1, "production economy-only route mismatch");

Console.WriteLine("PASS productionEconomyAwareCommit mixed=1 economyOnly=1 receipt=1 productionAssembly=1");

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
