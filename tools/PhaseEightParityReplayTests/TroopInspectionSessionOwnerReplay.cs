using System;
using System.Reflection;

internal static class TroopInspectionSessionOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type generic = assembly.GetType("AnimusForge.Refactor.Modules.TroopInspectionSessionOwner`3", true);
        Type closed = generic.MakeGenericType(typeof(object), typeof(object), typeof(object));
        object owner = Activator.CreateInstance(closed, true);
        MethodInfo beginLocal = closed.GetMethod("BeginLocalSelection", All);
        MethodInfo beginExternal = closed.GetMethod("BeginExternalPreparation", All);
        MethodInfo queue = closed.GetMethod("Queue", All);
        MethodInfo ready = closed.GetMethod("IsQueuedOpenReady", All);
        MethodInfo beginQueued = closed.GetMethod("BeginQueuedOpen", All);
        MethodInfo resetSelection = closed.GetMethod("ResetPendingSelection", All);
        MethodInfo beginCleanup = closed.GetMethod("BeginCleanup", All);
        MethodInfo releaseTransient = closed.GetMethod("ReleaseTransient", All);
        MethodInfo resetCampaign = closed.GetMethod("ResetForNewCampaign", All);
        object Get(string name) => closed.GetProperty(name, All).GetValue(owner);
        void Set(string name, object value) => closed.GetProperty(name, All).SetValue(owner, value);
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Inspection session owner: " + label);
        }

        object runtime = new object(), selection = new object(), mission = new object();
        beginLocal.Invoke(owner, null);
        Check((bool)Get("IsOpening") && !(bool)Get("CleanupDone") && Get("Runtime") == null,
            "local selection did not begin cleanly");
        Set("PendingSelection", selection);
        resetSelection.Invoke(owner, null);
        Check(Get("PendingSelection") == null && !(bool)Get("IsOpening"),
            "cancelled selection retained pending/opening state");
        beginExternal.Invoke(owner, null);
        Set("Runtime", runtime);
        queue.Invoke(owner, new object[] { 10f, 0.35f });
        Check(!(bool)ready.Invoke(owner, new object[] { 10.34f, false }), "queued Mission opened early");
        Check(!(bool)ready.Invoke(owner, new object[] { 10.35f, true }), "queued Mission opened over active Mission");
        Check((bool)ready.Invoke(owner, new object[] { 10.35f, false })
            && (bool)beginQueued.Invoke(owner, null) && !(bool)Get("Queued")
            && (bool)Get("IsOpening"), "ready queued Mission did not consume once");
        Check(!(bool)beginQueued.Invoke(owner, null), "queued Mission consumed twice");
        Set("ActiveMission", mission);
        Check(!(bool)beginCleanup.Invoke(owner, null) && Get("ActiveMission") == null,
            "first cleanup did not retire Mission");
        releaseTransient.Invoke(owner, null);
        Check(Get("Runtime") == null && Get("PendingSelection") == null
            && !(bool)Get("Queued"), "cleanup retained temporary runtime");
        Check((bool)beginCleanup.Invoke(owner, null), "repeat cleanup not identified");
        Set("Runtime", runtime);
        queue.Invoke(owner, new object[] { 20f, 0.35f });
        resetCampaign.Invoke(owner, null);
        Check(Get("Runtime") == null && !(bool)Get("Queued") && !(bool)Get("IsOpening")
            && !(bool)Get("CleanupDone") && !(bool)Get("NeedsEngineTick"),
            "new game/load retained stale temporary runtime");
        queue.Invoke(owner, new object[] { 30f, 0.35f });
        Check(!(bool)ready.Invoke(owner, new object[] { 31f, false }) && !(bool)Get("Queued"),
            "missing runtime kept queued-open poll alive");
        Type host = assembly.GetType("AnimusForge.TroopInspectionBehavior", true);
        Check(host.GetField("_sessionOwner", All)?.FieldType.GetGenericTypeDefinition() == generic,
            "production Inspection host does not hold session owner");
        Console.WriteLine("PASS TroopInspectionSessionOwnerReplay current-DLL cancel/queue/deadline/cleanup/reentry/load and host field; live roster/Mission=NOT_RUN");
    }
}
