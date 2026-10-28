using System.Diagnostics;
using AnimusForge;

namespace AnimusForge
{
    internal static class SaveRuntimeGuard { internal static long Generation = 1; internal static long CaptureGeneration() => Generation; }
    public sealed partial class WorldDiplomacyBehavior
    {
        internal static WorldDiplomacyBehavior Instance;
        internal static bool Enabled = true;
        internal bool IsLiveCampaign = true;
        internal sealed class Runtime { internal bool DisabledStateApplied; }
        internal readonly Runtime _runtime = new();
        internal readonly WorldDiplomacyMaterialCaptureQueue _eventMaterialCaptures = new();
        internal readonly WorldDiplomacyOrchestration _orchestration = new();
        internal IWorldDiplomacyOrchestration Orchestration => _orchestration;
        private static bool IsWorldDiplomacyEnabled() => Enabled;
        private static void ProcessComposePopup() { }
        private static void Log(string value) { }
    }
    internal interface IWorldDiplomacyOrchestration
    {
        void HandleDisabledState(); void ProcessCompletedJobs(); void TryScheduleTokenCompression();
        void TryStartNextLlmJob(); void PollNotifications();
    }
    internal sealed class WorldDiplomacyOrchestration : IWorldDiplomacyOrchestration
    {
        public void AdvanceEventRuntime() { }
        public void HandleDisabledState() { }
        public void ProcessCompletedJobs() { }
        public void TryScheduleTokenCompression() { }
        public void TryStartNextLlmJob() { }
        public void PollNotifications() { }
    }
    internal static class DiplomacyModuleServices { internal static readonly WorldDiplomacyModuleAdapter World = new(); }
}

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("FAIL " + message); checks++; }
    private sealed class Work
    {
        internal int Steps, Disposals;
        internal IEnumerator<string> Capture(CancellationToken token)
        {
            try
            {
                for (int i = 0; i < 63; i++) { token.ThrowIfCancellationRequested(); Steps++; yield return null; }
                yield return "complete";
            }
            finally { Disposals++; }
        }
    }
    private static void Main()
    {
        foreach (bool legacy in new[] { false, true })
        {
            var owner = new WorldDiplomacyBehavior(); WorldDiplomacyBehavior.Instance = owner;
            var queue = owner._eventMaterialCaptures;
            var jobs = Enumerable.Range(0, 3).Select(_ => new Work()).ToArray();
            for (int i = 0; i < jobs.Length; i++) queue.Advance(i.ToString(), SaveRuntimeGuard.Generation, jobs[i].Capture, null);
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < 5000 && jobs.Any(w => w.Steps < 63))
            {
                if (legacy) owner.OnEngineTick(); else DiplomacyModuleServices.World.OnEngineTick();
                Thread.Sleep(10);
            }
            Check(jobs.All(w => w.Steps == 63), (legacy ? "legacy" : "module") + " capture stalled");
            for (int i = 0; i < jobs.Length; i++)
                Check(queue.Advance(i.ToString(), SaveRuntimeGuard.Generation, jobs[i].Capture, null) == "complete", "only complete snapshot consumed");
            Check(queue.Count == 0 && jobs.All(w => w.Disposals == 1), "three captures consumed exactly once");
            Console.WriteLine($"{(legacy ? "legacy" : "module")} route: 3 concurrent 63-step captures in {watch.ElapsedMilliseconds} ms");
        }
        var current = new WorldDiplomacyBehavior(); WorldDiplomacyBehavior.Instance = current;
        var pending = new Work(); current._eventMaterialCaptures.Advance("pending", SaveRuntimeGuard.Generation, pending.Capture, null);
        WorldDiplomacyBehavior.Enabled = false; DiplomacyModuleServices.World.OnEngineTick();
        Check(current._eventMaterialCaptures.Count == 0 && pending.Disposals == 1, "disable cancels capture through live entry");
        WorldDiplomacyBehavior.Enabled = true;
        pending = new Work(); current._eventMaterialCaptures.Advance("generation", SaveRuntimeGuard.Generation, pending.Capture, null);
        SaveRuntimeGuard.Generation++; DiplomacyModuleServices.World.OnEngineTick();
        Check(current._eventMaterialCaptures.Count == 0 && pending.Disposals == 1, "generation change cancels stale capture");
        var old = current; pending = new Work(); old._eventMaterialCaptures.Advance("old", SaveRuntimeGuard.Generation, pending.Capture, null);
        WorldDiplomacyBehavior.Instance = new WorldDiplomacyBehavior(); DiplomacyModuleServices.World.OnEngineTick();
        Check(pending.Steps == 1, "owner replacement never pumps old owner");
        old._eventMaterialCaptures.Dispose();
        current = WorldDiplomacyBehavior.Instance;
        pending = new Work(); current._eventMaterialCaptures.Advance("retired", SaveRuntimeGuard.Generation, pending.Capture, null);
        current.IsLiveCampaign = false; DiplomacyModuleServices.World.OnEngineTick();
        Check(pending.Steps == 1, "retired campaign does not advance");
        current._eventMaterialCaptures.Dispose();
        WorldDiplomacyBehavior.Instance = null; DiplomacyModuleServices.World.OnEngineTick();
        Check(true, "missing owner no-op");
        Console.WriteLine($"PASS {checks} real-entry capture routing assertions; game lifetime/settings and unrelated orchestration are test leaves.");
    }
}
