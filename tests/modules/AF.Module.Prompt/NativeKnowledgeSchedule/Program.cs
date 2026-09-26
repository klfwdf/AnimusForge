using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TaleWorlds.CampaignSystem
{
    public class Hero { }
    public class CharacterObject
    {
        public Hero HeroObject { get { AnimusForge.Probe.MainOnly(); return new Hero(); } }
    }
}
namespace AnimusForge
{
    internal static class Probe
    {
        internal static int MainThread, Captures, Routes, KnowledgeRuns, Applies, Completes, WrongThread;
        internal static bool BlockKnowledge, ThrowKnowledge;
        internal static ManualResetEventSlim KnowledgeEntered = new ManualResetEventSlim(), KnowledgeRelease = new ManualResetEventSlim(true);
        internal static readonly ConcurrentQueue<Action> MainQueue = new ConcurrentQueue<Action>();
        internal static void MainOnly() { if (Environment.CurrentManagedThreadId != MainThread) { WrongThread++; throw new Exception("game read on worker"); } }
        internal static void WorkerOnly() { if (Environment.CurrentManagedThreadId == MainThread) { WrongThread++; throw new Exception("retrieval on game thread"); } }
        internal static void Reset() { Captures = Routes = KnowledgeRuns = Applies = Completes = WrongThread = 0; BlockKnowledge = ThrowKnowledge = false; KnowledgeEntered.Reset(); KnowledgeRelease.Set(); while (MainQueue.TryDequeue(out _)) { } SaveRuntimeGuard.Generation = 1; }
    }
    internal sealed class NativeConversationAdmission { internal bool Current = true; }
    internal sealed class PromptBuildPhases { internal PromptBuildRequest Request = new PromptBuildRequest(); }
    internal sealed class PromptBuildRequest { internal PromptRuntimeTargetBinding Target; internal PromptRuleEligibility Eligibility = new PromptRuleEligibility(); }
    internal readonly struct PromptRuntimeTargetBinding { }
    internal sealed class PromptRuleEligibility { }
    internal sealed class PromptKnowledgeWorkInput { }
    internal sealed class PromptKnowledgeWorkResult { }
    internal static class SaveRuntimeGuard { internal static long Generation = 1; internal static bool IsStale(long g, string reason) => g != Generation; }
    internal static class FreezeWatchdog { internal static void Mark(string scope, string detail, bool immediate = false) { } }
    internal static class Logger { internal static void Log(string channel, string text) { } }
    internal static class AIConfigHandler
    {
        internal static IDisposable BeginGuardrailRuntimeScope() => new Scope();
        private sealed class Scope : IDisposable { public void Dispose() { } }
        internal static void ApplyGuardrailRuntimeTarget(PromptRuntimeTargetBinding target, PromptRuleEligibility eligibility) { }
        internal static void ClearGuardrailRuntimeTarget() { }
    }
    public sealed class MyBehavior
    {
        internal static MyBehavior Instance = new MyBehavior();
        public sealed class ShoutPromptContext { }
        public sealed class WeeklyPromptSnapshot { }
        internal PromptBuildPhases BeginSharedPromptBuild(TaleWorlds.CampaignSystem.Hero hero, string input, string fact, string culture, bool anyHero,
            TaleWorlds.CampaignSystem.CharacterObject character, string kingdom, int agent, bool suppressDynamicRuleAndLore,
            bool usePrefetchedLoreContext, string prefetchedLoreContext, IEnumerable<string> excludedRuleIds,
            IEnumerable<string> preprocessExcludedRuleIds, IEnumerable<string> forcedPreprocessRuleIds, object preprocessMentionedEntities)
        { Probe.MainOnly(); return new PromptBuildPhases(); }
        internal void RunSharedPromptRouting(PromptBuildPhases phases) { Probe.WorkerOnly(); Probe.Routes++; }
        internal void CaptureSharedKnowledgeSnapshot(PromptBuildPhases phases, TaleWorlds.CampaignSystem.Hero hero) { Probe.MainOnly(); Probe.Captures++; }
        internal static PromptKnowledgeWorkInput CreateSharedKnowledgeWorkInput(PromptBuildPhases phases) { Probe.MainOnly(); return new PromptKnowledgeWorkInput(); }
        internal static PromptKnowledgeWorkResult RunSharedKnowledgeRetrieval(PromptKnowledgeWorkInput input)
        {
            Probe.WorkerOnly(); Probe.KnowledgeRuns++;
            if (Probe.BlockKnowledge) { Probe.KnowledgeEntered.Set(); Probe.KnowledgeRelease.Wait(); }
            if (Probe.ThrowKnowledge) throw new InvalidOperationException("knowledge failure");
            return new PromptKnowledgeWorkResult();
        }
        internal static void ApplySharedKnowledgeRetrieval(PromptBuildPhases phases, PromptKnowledgeWorkResult result) { Probe.MainOnly(); Probe.Applies++; }
        internal ShoutPromptContext CompleteSharedPromptBuild(PromptBuildPhases phases, TaleWorlds.CampaignSystem.Hero hero,
            TaleWorlds.CampaignSystem.CharacterObject character, WeeklyPromptSnapshot weekly) { Probe.MainOnly(); Probe.Completes++; return new ShoutPromptContext(); }
    }
    public partial class ShoutBehavior
    {
        private static MyBehavior.ShoutPromptContext CreateEmptyNativeConversationPromptContext() => new MyBehavior.ShoutPromptContext();
        private static bool IsNativeConversationAdmissionCurrent(NativeConversationAdmission admission, out string reason) { reason = null; return admission.Current; }
        private static Task<T> RunNativeConversationMainThreadFuncAsync<T>(string label, string target, int agent, Func<T> action, T fallback)
        {
            if (Environment.CurrentManagedThreadId == Probe.MainThread) return Task.FromResult(action());
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Probe.MainQueue.Enqueue(() => { try { completion.TrySetResult(action()); } catch (Exception ex) { completion.TrySetException(ex); } });
            return completion.Task;
        }
        private static Task<MyBehavior.ShoutPromptContext> RunNativeConversationBackgroundPreprocessAsync(string target, int agent, long generation, Func<MyBehavior.ShoutPromptContext> action)
            => Task.Run(() => { try { return action(); } catch { return new MyBehavior.ShoutPromptContext(); } });
        private static Task<MyBehavior.ShoutPromptContext> AwaitNativeConversationBackgroundPreprocessAsync(Task<MyBehavior.ShoutPromptContext> task, string target, int agent, long generation) => task;
        internal Task<MyBehavior.ShoutPromptContext> Run(NativeConversationAdmission admission, long generation)
            => BuildNativePromptContextScheduledAsync(admission, "npc", 1, generation, new TaleWorlds.CampaignSystem.Hero(),
                new TaleWorlds.CampaignSystem.CharacterObject(), "hello", "fact", "culture", true, null, null);
    }
    internal static class Program
    {
        private static int _checks;
        private static void Check(bool okay, string reason) { _checks++; if (!okay) throw new Exception("FAIL " + reason); }
        private static void PumpUntil(Func<bool> done)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!done())
            {
                while (Probe.MainQueue.TryDequeue(out Action action)) action();
                if (DateTime.UtcNow > deadline) throw new Exception("fixture timeout");
                Thread.Sleep(1);
            }
        }
        private static void Case(string scenario)
        {
            Probe.Reset();
            var admission = new NativeConversationAdmission();
            if (scenario != "normal" && scenario != "error") { Probe.BlockKnowledge = true; Probe.KnowledgeRelease.Reset(); }
            Probe.ThrowKnowledge = scenario == "error";
            Task<MyBehavior.ShoutPromptContext> task = new ShoutBehavior().Run(admission, 1);
            if (Probe.BlockKnowledge)
            {
                PumpUntil(() => Probe.KnowledgeEntered.IsSet);
                if (scenario == "late_admission") admission.Current = false;
                if (scenario == "late_generation") SaveRuntimeGuard.Generation++;
                Probe.KnowledgeRelease.Set();
            }
            PumpUntil(() => task.IsCompleted);
            var result = task.GetAwaiter().GetResult();
            Check(Probe.WrongThread == 0 && Probe.Captures == 1 && Probe.Routes == 1 && Probe.KnowledgeRuns == 1, scenario + " physical thread and phase counts");
            if (scenario == "normal") Check(result != null && Probe.Applies == 1 && Probe.Completes == 1, "normal final publish once");
            else Check(result == null && Probe.Applies == 0 && Probe.Completes == 0, scenario + " rejected before game publication");
        }
        private static int Main()
        {
            Probe.MainThread = Environment.CurrentManagedThreadId;
            try { foreach (string scenario in new[] { "normal", "late_admission", "late_generation", "error" }) Case(scenario);
                Console.WriteLine("PASS prompt-j06-native-knowledge checks=" + _checks + " scenarios=4 source=production-schedule game=stubbed"); return 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
            finally { Probe.KnowledgeRelease.Set(); }
        }
    }
}
