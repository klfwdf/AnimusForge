using System;
using System.Reflection;

internal static class SceneTauntConflictLifecycleOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = assembly.GetType("AnimusForge.Refactor.Modules.SceneTauntConflictLifecycleOwner", true);
        object owner = Activator.CreateInstance(type, true);
        bool Call(string name) => (bool)type.GetMethod(name, All).Invoke(owner, null);
        bool Flag(string name) => (bool)type.GetProperty(name, All).GetValue(owner);
        void End(bool preserve) => type.GetMethod("End", All).Invoke(owner, new object[] { preserve });
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Taunt lifecycle owner: " + label);
        }

        Check(!Call("TryEscalate"), "idle escalation admitted");
        Check(Call("TryBeginUnarmed") && Flag("Active") && !Flag("Armed"),
            "unarmed initialization failed");
        Check(!Call("TryBeginUnarmed") && !Call("TryBeginArmedCarryover"),
            "duplicate initialization admitted");
        Check(Call("TryEscalate") && Flag("ArmedOccurred") && !Call("TryEscalate"),
            "armed escalation was not exactly once");
        End(true);
        Check(!Flag("Active") && !Flag("Armed") && Flag("ArmedOccurred"),
            "armed defeat state was not preserved after end");
        Check(Call("TryBeginArmedCarryover") && Flag("Armed") && Flag("ArmedOccurred"),
            "carryover re-entry failed");
        End(false);
        Check(!Flag("Active") && !Flag("ArmedOccurred") && !Call("TryEscalate"),
            "ordinary cleanup left stale conflict active");
        Type mission = assembly.GetType("AnimusForge.SceneTauntMissionBehavior", true);
        Check(mission.GetField("_conflictLifecycle", All)?.FieldType == type,
            "Mission behavior does not hold production lifecycle owner");
        Console.WriteLine("PASS SceneTauntConflictLifecycleOwnerReplay current-DLL init/duplicate/escalate/end/re-entry and Mission owner field; Mission callbacks/live=NOT_RUN");
    }
}
