using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using HarmonyLib;

internal static class ActualMaintenanceReplay
{
    private static string ResourceRoot;
    private static bool ResourcePath(string __0, ref string __result)
    {
        __result = Path.Combine(ResourceRoot, "content/modules/AF.Module.Diplomacy/ModuleData", __0);
        return false;
    }
    private sealed class Host : RealProxy
    {
        internal int Day = 12;
        internal Host(Type contract) : base(contract) { }
        public override IMessage Invoke(IMessage message)
        {
            var call = (IMethodCallMessage)message;
            var method = (MethodInfo)call.MethodBase;
            object result = null;
            if (method.Name == "CurrentDay") result = Day;
            else if (method.Name == "CurrentHour") result = Day * 24;
            else if (method.Name == "MaxPendingJobs") result = 24;
            else if (method.Name == "HistoryCompressionTriggerTokens") result = (long)int.MaxValue;
            else if (method.ReturnType == typeof(string)) result = "";
            else if (method.ReturnType != typeof(void) && method.ReturnType.IsValueType) result = Activator.CreateInstance(method.ReturnType);
            object[] values = new object[call.ArgCount];
            var parameters = method.GetParameters();
            for (int i = 0; i < values.Length; i++)
                values[i] = parameters[i].ParameterType.IsByRef && parameters[i].ParameterType.GetElementType().IsValueType
                    ? Activator.CreateInstance(parameters[i].ParameterType.GetElementType()) : call.Args[i];
            return new ReturnMessage(result, values, values.Length, call.LogicalCallContext, call);
        }
    }

    private static object New(Type type, params object[] args) => Activator.CreateInstance(type,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
    private static object Get(object value, string name) => value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(value)
        ?? value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(value);
    private static void Set(object value, string name, object data)
    {
        var property = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null) property.SetValue(value, data);
        else value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(value, data);
    }

    internal static void Run(Assembly af, string root)
    {
        ResourceRoot = root;
        // The native module registry is absent in this process; actual JSON readers remain in use.
        new Harmony("AnimusForge.DiplomacyHotfix.Tests.Resources").Patch(
            AccessTools.Method(af.GetType("AnimusForge.AnimusForgeModulePaths", true), "GetModuleDataFilePath"),
            prefix: new HarmonyMethod(typeof(ActualMaintenanceReplay), nameof(ResourcePath)));
        int assertions = 0;
        foreach (int size in new[] { 2, 0, 1, 3, 8, 9, 17 })
        foreach (int cursor in new[] { 1, 0, 2, 7, int.MaxValue, int.MinValue })
        {
            Type ownerType = af.GetType("AnimusForge.WorldDiplomacyOrchestration", true);
            var host = new Host(af.GetType("AnimusForge.IWorldDiplomacyOrchestrationHost", true));
            object owner = New(ownerType, host.GetTransparentProxy(), New(af.GetType("AnimusForge.WorldDiplomacyRuntimeState", true)));
            object storage = Get(owner, "CurrentStorage");
            Set(storage, "PlayerLifecycleSchemaVersion", 1); Set(storage, "NextPlayerAgendaDay", int.MaxValue);
            Set(owner, "_playerAuthorityDay", host.Day); Set(owner, "_playerRoundCursor", cursor);
            var rounds = (IList)Get(storage, "ConcurrentRounds"); var docs = (IList)Get(storage, "Documents");
            var completed = (IList)Get(storage, "CompletedRounds");
            for (int n = 0; n < size; n++)
            {
                object round = New(af.GetType("AnimusForge.WorldDiplomacyRound", true));
                Set(round, "RoundId", "test_" + n); Set(round, "State", "active"); Set(round, "SchedulingOwner", "player");
                Set(round, "RootDocumentId", "source_" + n);
                var participants = (IList)Get(round, "Participants");
                object participant = New(participants.GetType().GetGenericArguments()[0]);
                Set(participant, "KingdomId", "a"); Set(participant, "State", "withdrawn"); participants.Add(participant);
                rounds.Add(round);
                object doc = New(af.GetType("AnimusForge.WorldDiplomacyDocument", true));
                Set(doc, "DocumentId", "source_" + n); Set(doc, "RoundId", "test_" + n);
                Set(doc, "AuthorKingdomId", "a"); Set(doc, "TargetKingdomId", "p"); Set(doc, "IsReadyForPublication", true);
                docs.Add(doc);
            }
            int count = size + 1, start = cursor < 0 ? 0 : cursor % count, expected = 0;
            for (int i = 0; i < Math.Min(8, count); i++) if ((start + i) % count != 0) expected++;
            ownerType.GetMethod("MaintainPlayerEvents", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
            if (completed.Count != expected || rounds.Count != size - expected)
                throw new Exception("Actual AF maintenance snapshot mismatch: size=" + size + " cursor=" + cursor + " closed=" + completed.Count);
            assertions++;
            int next = (int)Get(owner, "_playerRoundCursor");
            if (next < 0 || next >= count) throw new Exception("Actual AF cursor not bounded");
            assertions++;
        }
        Console.WriteLine("PASS actual AF maintenance execution " + assertions + " assertions (native/host leaves substituted)");
    }
}
