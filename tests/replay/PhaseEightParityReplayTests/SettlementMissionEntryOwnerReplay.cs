using System;
using System.Reflection;

internal static class SettlementMissionEntryOwnerReplay
{
    private sealed class Ticket
    {
        internal string SettlementId;
        internal bool VillageAftermath;
        internal DateTime CreatedUtc;
    }

    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type generic = assembly.GetType("AnimusForge.Refactor.Modules.SettlementMissionEntryOwner`1", true);
        Type closed = generic.MakeGenericType(typeof(Ticket));
        object owner = Activator.CreateInstance(closed, All, null, new object[]
        {
            (Func<Ticket, string>)(entry => entry.SettlementId),
            (Func<Ticket, bool>)(entry => entry.VillageAftermath),
            (Func<Ticket, DateTime>)(entry => entry.CreatedUtc)
        }, null);
        MethodInfo set = closed.GetMethod("Set", All);
        MethodInfo cancel = closed.GetMethod("CancelVillageAftermath", All);
        MethodInfo consume = closed.GetMethod("TryConsumeForMission", All);
        MethodInfo expire = closed.GetMethod("TryExpire", All);
        object Pending() => closed.GetProperty("Pending", All).GetValue(owner);
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Settlement mission entry owner: " + label);
        }

        var now = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);
        var town = new Ticket { SettlementId = "town_a", CreatedUtc = now };
        set.Invoke(owner, new object[] { town });
        object[] cancelArgs = { "town_a", null };
        Check(!(bool)cancel.Invoke(owner, cancelArgs) && ReferenceEquals(Pending(), town),
            "non-village ticket cancelled by GCCZ seam");
        object[] consumeArgs = { "town_b", null, false };
        Check(!(bool)consume.Invoke(owner, consumeArgs) && (bool)consumeArgs[2]
            && ReferenceEquals(consumeArgs[1], town) && Pending() == null,
            "wrong settlement mission accepted or ticket retained");
        set.Invoke(owner, new object[] { town });
        consumeArgs = new object[] { "town_a", null, false };
        Check((bool)consume.Invoke(owner, consumeArgs) && !(bool)consumeArgs[2]
            && ReferenceEquals(consumeArgs[1], town) && Pending() == null,
            "matching mission did not consume once");

        var village = new Ticket { SettlementId = "village_a", VillageAftermath = true, CreatedUtc = now };
        set.Invoke(owner, new object[] { village });
        cancelArgs = new object[] { "other", null };
        Check(!(bool)cancel.Invoke(owner, cancelArgs) && ReferenceEquals(Pending(), village),
            "unrelated GCCZ cancellation removed ticket");
        cancelArgs = new object[] { "VILLAGE_A", null };
        Check((bool)cancel.Invoke(owner, cancelArgs) && ReferenceEquals(cancelArgs[1], village)
            && Pending() == null, "same settlement village cancellation failed");

        set.Invoke(owner, new object[] { town });
        object[] expireArgs = { now.AddSeconds(30), false, TimeSpan.FromSeconds(30), null };
        Check(!(bool)expire.Invoke(owner, expireArgs) && ReferenceEquals(Pending(), town),
            "entry expired at inclusive deadline");
        expireArgs = new object[] { now.AddSeconds(31), true, TimeSpan.FromSeconds(30), null };
        Check(!(bool)expire.Invoke(owner, expireArgs) && ReferenceEquals(Pending(), town),
            "active mission expired ticket");
        expireArgs = new object[] { now.AddSeconds(31), false, TimeSpan.FromSeconds(30), null };
        Check((bool)expire.Invoke(owner, expireArgs) && ReferenceEquals(expireArgs[3], town)
            && Pending() == null, "stale ticket not expired");
        Type host = assembly.GetType("AnimusForge.SettlementEntryTroopSelectionBehavior", true);
        Check(host.GetField("_missionEntryOwner", All)?.FieldType.GetGenericTypeDefinition() == generic,
            "production SETS host does not own entry lifecycle");
        Console.WriteLine("PASS SettlementMissionEntryOwnerReplay current-DLL one-shot/mismatch/GCCZ cancel/deadline and host field; live Mission=NOT_RUN");
    }
}
