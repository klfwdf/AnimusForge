using System;
using System.Reflection;
using System.Runtime.Serialization;

// Real host/bridge queries, with synthetic pending state. No Game/Campaign/Mission is created.
internal static class EntryGateRegression
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    internal static void Run(Assembly af, Assembly coup, Action<string> write)
    {
        int count = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new InvalidOperationException("ENTRY_FAIL " + message);
            count++;
            write("ENTRY_PASS " + message);
        };
        Type host = af.GetType("AnimusForge.SettlementEntryTroopSelectionBehavior", true);
        Type bridge = coup.GetType("AnimusForge.CoupSystem.SettlementEntryTroopSelectionBehavior", true);
        Func<bool> pending = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), bridge.GetMethod("HasPendingFlowForCoup", All));
        check(!pending(), "idle registered SETS allows entry");
        foreach (string name in new[] { "_pendingProfileSelection", "_pendingMissionEntry", "_pendingVictoryMenuEntry",
            "_pendingVillageVictoryRewardEntry", "_pendingVillageAftermathEncounterExit", "_pendingSettlementCivilianGatherRequest" })
        {
            FieldInfo field = host.GetField(name, All);
            PropertyInfo property = host.GetProperty(name, All);
            object before = field != null ? field.GetValue(null) : property.GetValue(null);
            Action<object> set = value => { if (field != null) field.SetValue(null, value); else property.SetValue(null, value); };
            try
            {
                set(Activator.CreateInstance(field?.FieldType ?? property.PropertyType, true));
                check(pending(), name + " blocks entry");
            }
            finally { set(before); }
            check(!pending(), name + " cleared releases gate");
        }
        foreach (string name in new[] { "_setsActiveUsableProtection", "_pendingSameKingdomVassalRebellionKingdomId" })
        {
            FieldInfo field = host.GetField(name, All);
            object before = field.GetValue(null);
            try
            {
                field.SetValue(null, field.FieldType == typeof(bool) ? (object)true : "probe-kingdom");
                check(pending(), name + " blocks entry");
            }
            finally { field.SetValue(null, before); }
            check(!pending(), name + " cleared releases gate");
        }
        object followers = host.GetField("_followerOwner", All).GetValue(null);
        PropertyInfo active = followers.GetType().GetProperty("Active", All);
        object previousActive = active.GetValue(followers);
        try { active.SetValue(followers, true); check(pending(), "owner-backed active flag blocks entry"); }
        finally { active.SetValue(followers, previousActive); }
        check(!pending(), "inactive follower owner releases gate");

        FieldInfo query = bridge.GetField("_hasBlockingFlow", All);
        object originalQuery = query.GetValue(null);
        PropertyInfo available = bridge.GetProperty("IsAvailable", All);
        object originalAvailable = available.GetValue(null);
        try
        {
            query.SetValue(null, null);
            check(pending(), "missing cached query fails closed");
            query.SetValue(null, (Func<bool>)(() => throw new InvalidOperationException("injected query failure")));
            check(pending() && !(bool)available.GetValue(null), "query failure disables entry");
        }
        finally { query.SetValue(null, originalQuery); available.SetValue(null, originalAvailable); }

        Type myBehavior = af.GetType("AnimusForge.MyBehavior", true);
        // Its normal constructor owns many unrelated campaign services; bypass only for this fixture.
        object owner = FormatterServices.GetUninitializedObject(myBehavior);
        Type rebellion = coup.GetType("AnimusForge.CoupSystem.CoupRebellionBridge", true);
        object access = rebellion.GetField("_af", All).GetValue(null)
            ?? throw new InvalidOperationException("Rebellion bridge registration failed");
        MethodInfo isBusy = access.GetType().GetMethod("IsBusy", All);
        Func<bool> busy = () => (bool)isBusy.Invoke(access, new[] { owner });
        check((bool)isBusy.Invoke(access, new object[] { null }), "missing rebellion owner fails closed");
        check(!busy(), "idle rebellion owner allows processing");
        object scheduler = myBehavior.GetProperty("AutomaticKingdomRebellions", All).GetValue(owner);
        Type schedulerType = scheduler.GetType();
        Type contextType = schedulerType.GetGenericArguments()[0];
        schedulerType.GetMethod("Enqueue", All).Invoke(scheduler, new[] { Activator.CreateInstance(contextType, true) });
        schedulerType.GetMethod("ActivateIfQueued", All).Invoke(scheduler, null);
        check(busy(), "active automatic rebellion blocks processing");
        schedulerType.GetMethod("Cancel", All).Invoke(scheduler, null);
        check(!busy(), "cancelled automatic flow releases gate");
        object version = schedulerType.GetMethod("BeginNaming", All).Invoke(scheduler, null);
        check(busy(), "naming in progress blocks even without FlowActive");
        schedulerType.GetMethod("CompleteNaming", All).Invoke(scheduler, new[] { version, Activator.CreateInstance(contextType, true) });
        check(busy(), "completed naming awaiting consumption remains blocked");
        schedulerType.GetMethod("TryTakeReady", All).Invoke(scheduler, new object[] { null });
        check(!busy(), "consuming naming result releases gate");
        foreach (string name in new[] { "_devForcedKingdomRebellionInProgress", "_weeklyReportGenerationInProgress" })
        {
            FieldInfo flag = myBehavior.GetField(name, All);
            flag.SetValue(owner, true);
            check(busy(), name + " blocks processing");
            flag.SetValue(owner, false);
            check(!busy(), name + " cleared releases gate");
        }

        Type captivity = coup.GetType("AnimusForge.CoupSystem.CoupCaptivityBehavior", true);
        PropertyInfo protection = captivity.GetProperty("AutomaticReleaseProtectionAvailable", All);
        check(!(bool)protection.GetValue(null), "detention unavailable before campaign behavior construction");
        object detention = Activator.CreateInstance(captivity, true);
        check((bool)protection.GetValue(null), "registered detention patches available after behavior construction");
        FieldInfo saveValid = captivity.GetField("_saveValid", All);
        saveValid.SetValue(detention, false);
        check(!(bool)protection.GetValue(null), "invalid detention save fails closed");
        saveValid.SetValue(detention, true);
        check((bool)protection.GetValue(null), "valid detention state restores availability");
        write("PASS entry-state and detention lifecycle regression assertions=" + count);
        write("ENTRY_SCOPE synthetic pending objects, uninitialized MyBehavior, direct follower Active fixture, normal CoupCaptivityBehavior constructor; no real campaign/save or menu exercised.");
    }
}
