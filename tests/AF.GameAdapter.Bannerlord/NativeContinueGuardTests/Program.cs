using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AnimusForge;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapConversation;

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL " + name);
        _checks++;
        Console.WriteLine("PASS " + name);
    }
    private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object instance, string field, object value) => AccessTools.Field(instance.GetType(), field).SetValue(instance, value);
    private static void InProgress(ConversationManager manager, bool value) => Set(manager, "<IsConversationInProgress>k__BackingField", value);
    private static int Main()
    {
        try { Run(); Console.WriteLine("PASS " + _checks + " native DLL / real Harmony continue guard checks; Campaign/overlay shell and backend are fixtures, no live game."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void Run()
    {
        var campaign = Empty<Campaign>();
        var manager = Empty<ConversationManager>();
        AccessTools.Field(typeof(Campaign), "<Current>k__BackingField").SetValue(null, campaign);
        Set(campaign, "<ConversationManager>k__BackingField", manager);
        InProgress(manager, true);
        var vm = Empty<MissionConversationVM>();
        Set(vm, "_conversationManager", manager);
        Set(vm, "_isProcessingOption", true);
        var map = Empty<MapConversationVM>();
        int mapCallbacks = 0;
        Set(map, "_onContinue", (Action)(() => mapCallbacks++));
        var overlay = new AnimusForgeNativeConversationOverlay();
        overlay.Activate(true);
        Check(AnimusForgeNativeConversationOverlay.IsAiModeBlockingNativeContinue(), "AI mode active with no Mission or backend request");
        ContinueConversationSafePatch.EnsurePatched();
        var targets = new[] {
            AccessTools.Method(typeof(ConversationManager), "ContinueConversation", Type.EmptyTypes),
            AccessTools.Method(typeof(MissionConversationVM), "ExecuteContinue", Type.EmptyTypes),
            AccessTools.Method(typeof(MapConversationVM), "ExecuteContinue", Type.EmptyTypes)
        };
        foreach (var target in targets)
            Check(Harmony.GetPatchInfo(target).Prefixes.Count(p => p.owner == "AnimusForge.continueconversation.safety") == 1, "installed real native prefix " + target.DeclaringType.Name);
        int diagnostics = Logger.Installed;
        for (int i = 0; i < 100; i++) ContinueConversationSafePatch.EnsurePatched();
        Check(Logger.Installed == diagnostics, "repeated ensure does not install/log again");

        foreach (string state in new[] { "idle", "generating", "streaming", "audio", "completed", "history", "give-show", "temporary-hidden", "restored" })
        {
            ShoutBehavior.Busy = state == "generating";
            map.ExecuteContinue();
            Check(mapCallbacks == 0, state + " map button callback blocked");
            vm.ExecuteContinue(); // A released ContinueKey in either native view reaches this same command.
            Check((bool)AccessTools.Field(typeof(MissionConversationVM), "_isProcessingOption").GetValue(vm), state + " native mission command skipped before option side effect");
            manager.ContinueConversation(); // Uninitialized native internals would fail if the original ran.
            Check(ConversationExceptionGuard.Preemptions == 0, state + " manager blocked before stale-recovery/final native execution");
        }
        ShoutBehavior.Busy = false;
        Logger.ThrowVerbose = true;
        map.ExecuteContinue(); manager.ContinueConversation();
        Check(mapCallbacks == 0, "logging exception cannot release continuation");
        Logger.ThrowVerbose = false;
        overlay.Activate(false);
        map.ExecuteContinue();
        Check(mapCallbacks == 1, "ordinary map executes actual native callback");
        Check(ContinueConversationSafePatch.Prefix(manager, targets[0]), "ordinary manager follows existing safety guard");
        ConversationExceptionGuard.Stale = true;
        Check(!ContinueConversationSafePatch.Prefix(manager, targets[0]), "ordinary stale conversation recovery remains effective");
        ConversationExceptionGuard.Stale = false;
        ShoutBehavior.Busy = true;
        map.ExecuteContinue();
        Check(mapCallbacks == 1, "NPC opening backend guard retained before AI mode entry");
        ShoutBehavior.Busy = false;
        overlay.Activate(true);
        overlay.Close();
        map.ExecuteContinue();
        Check(mapCallbacks == 2, "explicit close releases continuation");
        overlay.Activate(true);
        InProgress(manager, false);
        Check(!AnimusForgeNativeConversationOverlay.IsAiModeBlockingNativeContinue(), "ended conversation ignores stale overlay");
        InProgress(manager, true);
        Set(campaign, "<ConversationManager>k__BackingField", Empty<ConversationManager>());
        Check(!AnimusForgeNativeConversationOverlay.IsAiModeBlockingNativeContinue(), "replaced campaign manager ignores stale overlay");
        Set(campaign, "<ConversationManager>k__BackingField", manager);
        SaveRuntimeGuard.AdvanceGeneration("fixture_load");
        Check(!AnimusForgeNativeConversationOverlay.IsAiModeBlockingNativeContinue(), "save reload retires old mode guard");
        var fresh = new AnimusForgeNativeConversationOverlay(); fresh.Activate(true);
        Check(AnimusForgeNativeConversationOverlay.IsAiModeBlockingNativeContinue(), "new overlay after reload protects conversation");
        Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(ConversationManager), "EndConversation"))?.Prefixes.Any(p => p.owner == "AnimusForge.continueconversation.safety") != true, "explicit EndConversation remains outside continue patch");
        fresh.Close();
        var error = new InvalidOperationException("fixture");
        Check(ReferenceEquals(ContinueConversationSafePatch.Finalizer(error, manager, targets[0]), error), "existing exception finalizer retained");
    }
}

namespace AnimusForge
{
    // Only UI/host scaffolding is fake; the guard partial, save generation and patch class are production.
    public sealed partial class AnimusForgeNativeConversationOverlay
    {
        private static AnimusForgeNativeConversationOverlay _activeOverlay;
        private bool _isClosed;
        private readonly OverlayVm _dataSource = new OverlayVm();
        internal void Activate(bool ai) { _activeOverlay = this; _isClosed = false; _dataSource.IsCustomAnswerVisible = ai; }
        internal void Close() { _isClosed = true; }
        private sealed class OverlayVm { internal bool IsCustomAnswerVisible; }
    }
    internal static class ShoutBehavior
    {
        internal static bool Busy;
        internal static bool IsNativeConversationBackendBusy() => Busy;
    }
    internal static class ConversationExceptionGuard
    {
        internal static int Preemptions;
        internal static bool Stale;
        internal static bool TryPreemptStaleConversation(object instance, string context, MethodBase method) { Preemptions++; return Stale; }
        internal static Exception Filter(Exception exception, object instance, string context, MethodBase method) => exception;
    }
    internal static class Logger
    {
        internal static int Installed;
        internal static bool ThrowVerbose;
        internal static void Log(string category, string text) { if (text.StartsWith("Continue guard installed:")) Installed++; }
        internal static void LogVerbose(string category, string key, Func<string> text, double interval)
        { if (ThrowVerbose) throw new Exception("fixture logger failure"); }
    }
}
