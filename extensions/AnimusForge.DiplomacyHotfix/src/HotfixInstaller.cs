using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace AnimusForge.DiplomacyHotfix;

public static class HotfixInstaller
{
    public const string Owner = "AnimusForge.DiplomacyHotfix.v0_1";
    private static readonly Harmony Patcher = new Harmony(Owner);

    public static string Install(Assembly assembly, Action<string> log)
    {
        string maintenance = InstallMaintenance(assembly, log);
        string requests = InstallRequests(assembly, log);
        string status = "maintenance=" + maintenance + ";requests=" + requests;
        log(status);
        return status;
    }

    private static string InstallMaintenance(Assembly assembly, Action<string> log)
    {
        MethodInfo target = AccessTools.DeclaredMethod(assembly.GetType("AnimusForge.WorldDiplomacyOrchestration"), "MaintainPlayerEvents");
        if (target == null) return "unsupported_method";
        if (IsOwned(target)) return "applied";
        Type round = assembly.GetType("AnimusForge.WorldDiplomacyRound");
        if (round == null) return "unsupported_round";
        if (PatchProcessor.GetOriginalInstructions(target).Any(i => i.opcode == OpCodes.Newarr && Equals(i.operand, round)))
            return "already_fixed";
        try
        {
            Patcher.Patch(target, transpiler: new HarmonyMethod(typeof(MaintenancePatch), "Transpiler"));
            return "applied";
        }
        catch (Exception ex)
        {
            Patcher.Unpatch(target, HarmonyPatchType.All, Owner);
            log("maintenance_patch_rejected:" + ex);
            return "unsupported_shape";
        }
    }

    private static string InstallRequests(Assembly assembly, Action<string> log)
    {
        Type client = assembly.GetType("AnimusForge.WorldDiplomacyLlmClient");
        Type result = assembly.GetType("AnimusForge.WorldDiplomacyApiCallResult");
        Type source = assembly.GetType("AnimusForge.WorldDiplomacyBehavior+LlmDispatchSource");
        if (client == null || result == null || source == null) return "unsupported_types";
        if (AccessTools.DeclaredMethod(client, "TryPrepareCallWithRetries") != null) return "already_fixed";
        MethodInfo dispatch = AccessTools.DeclaredMethod(source, "StartRequest");
        MethodInfo retry = AccessTools.DeclaredMethod(client, "CallMessagesWithRetriesAsync");
        MethodInfo send = AccessTools.DeclaredMethod(client, "SendAndReadAsync");
        var targets = new[] { dispatch, retry, send };
        if (targets.Any(m => m == null)) return "unsupported_methods";
        if (targets.All(IsOwned)) return "applied";
        try
        {
            FrozenRequestPatch.Configure(client, result);
            Patcher.Patch(send, prefix: new HarmonyMethod(typeof(FrozenRequestPatch), "SendPrefix"));
            Patcher.Patch(retry, transpiler: new HarmonyMethod(typeof(FrozenRequestPatch), "ClientTranspiler"));
            Patcher.Patch(dispatch, transpiler: new HarmonyMethod(typeof(FrozenRequestPatch), "DispatchTranspiler"));
            return "applied";
        }
        catch (Exception ex)
        {
            foreach (var target in targets) Patcher.Unpatch(target, HarmonyPatchType.All, Owner);
            log("request_patch_rejected:" + ex);
            return "unsupported_shape";
        }
    }

    private static bool IsOwned(MethodBase method) => Harmony.GetPatchInfo(method)?.Owners.Contains(Owner) == true;
}
