using System;
using System.Reflection;

internal static class SettlementFollowerMissionOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type generic = assembly.GetType("AnimusForge.Refactor.Modules.SettlementFollowerMissionOwner`2", true);
        Type closed = generic.MakeGenericType(typeof(object), typeof(object));
        object owner = Activator.CreateInstance(closed, true);
        MethodInfo setActive = closed.GetMethod("SetActive", All);
        MethodInfo register = closed.GetMethod("Register", All);
        MethodInfo tracked = closed.GetMethod("IsTracked", All);
        MethodInfo remove = closed.GetMethod("Remove", All);
        MethodInfo clear = closed.GetMethod("Clear", All);
        object mission = new object(), nextMission = new object();
        object follower = new object(), reusedIndexAgent = new object();
        bool IsTracked(object current, int index, object agent) =>
            (bool)tracked.Invoke(owner, new[] { current, (object)index, agent });
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Settlement follower owner: " + label);
        }

        Check((bool)setActive.Invoke(owner, new object[] { mission, true }), "new Mission not recognized");
        Check((bool)register.Invoke(owner, new[] { (object)17, follower }), "follower registration failed");
        Check(IsTracked(mission, 17, follower) && !IsTracked(mission, 17, reusedIndexAgent),
            "Agent index reuse impersonated selected follower");
        Check(!IsTracked(nextMission, 17, follower), "different Mission retained selected follower");
        Check(!(bool)register.Invoke(owner, new[] { (object)17, follower }),
            "duplicate registration reported new follower");
        Check(!(bool)remove.Invoke(owner, new[] { (object)17, reusedIndexAgent })
            && IsTracked(mission, 17, follower), "wrong Agent removed selected follower");
        Check((bool)remove.Invoke(owner, new[] { (object)17, follower })
            && !IsTracked(mission, 17, follower), "removed/dead Agent remained selected");
        register.Invoke(owner, new[] { (object)17, follower });
        Check((bool)setActive.Invoke(owner, new object[] { nextMission, true })
            && !IsTracked(nextMission, 17, follower), "Mission transition retained old Agent");
        register.Invoke(owner, new[] { (object)18, reusedIndexAgent });
        clear.Invoke(owner, null);
        Check(!IsTracked(nextMission, 18, reusedIndexAgent)
            && !(bool)closed.GetProperty("Active", All).GetValue(owner), "end/load cleanup retained follower");
        Type host = assembly.GetType("AnimusForge.SettlementEntryTroopSelectionBehavior", true);
        Check(host.GetField("_followerOwner", All)?.FieldType.GetGenericTypeDefinition() == generic,
            "production SETS host does not hold Agent identity owner");
        Console.WriteLine("PASS SettlementFollowerMissionOwnerReplay current-DLL Agent identity/index reuse/death/Mission change/clear and host field; live Agent=NOT_RUN");
    }
}
