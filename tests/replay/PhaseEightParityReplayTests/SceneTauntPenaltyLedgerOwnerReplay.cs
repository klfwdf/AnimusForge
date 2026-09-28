using System;
using System.Collections.Generic;
using System.Reflection;

internal static class SceneTauntPenaltyLedgerOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = assembly.GetType("AnimusForge.Refactor.Modules.SceneTauntPenaltyLedgerOwner", true);
        object owner = Activator.CreateInstance(type, true);
        object Call(string method, params object[] args) => type.GetMethod(method, All).Invoke(owner, args);
        float Pending(string id) => (float)Call("GetDeferredCrime", id);
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Taunt penalty owner: " + label);
        }

        Call("RestoreDeferredCrime", new Dictionary<string, float> { [" faction-a "] = 13f });
        Check(Pending("FACTION-A") == 13f, "loaded faction identity drifted");
        Check((float)Call("ReserveNativeCommit", "faction-a", 100f, 100f) == 0f
            && Pending("faction-a") == 13f, "full native cap consumed pool");
        Check((float)Call("ReserveNativeCommit", "FACTION-A", 95f, 100f) == 5f
            && Pending("faction-a") == 8f, "partial native cap lost remainder");
        Call("RestoreFailedNativeCommit", "faction-a", 13f);
        Check(Pending("faction-a") == 13f, "native failure did not roll back reservation");
        Check((float)Call("ClearDeferredCrime", "faction-a") == 13f
            && (float)Call("ClearDeferredCrime", "faction-a") == 0f,
            "execution clear was not idempotent");

        Call("RestoreTrustTenths", new Dictionary<string, int> { ["town-a"] = 4 });
        object[] first = { "TOWN-A", null };
        int firstWhole = (int)type.GetMethod("AwardCriminalKnockdownTrust", All).Invoke(owner, first);
        object[] second = { "town-a", null };
        int secondWhole = (int)type.GetMethod("AwardCriminalKnockdownTrust", All).Invoke(owner, second);
        Check(firstWhole == 1 && (int)first[1] == 7 && secondWhole == 2 && (int)second[1] == 0,
            "fractional trust did not carry and consume exactly once");
        Type behavior = assembly.GetType("AnimusForge.SceneTauntBehavior", true);
        Check(behavior.GetField("_penaltyLedger", All)?.FieldType == type,
            "Campaign behavior does not hold production penalty owner");
        Console.WriteLine("PASS SceneTauntPenaltyLedgerOwnerReplay current-DLL cap/partial/failure/clear/trust and Campaign owner field; native callbacks/live=NOT_RUN");
    }
}
