using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;

namespace AnimusForge.DialogueUI.Native;

public static class NativeUiAdapter
{
    private static readonly Dictionary<AnimusForgeNativeConversationOverlayVM, NativeOverlayVM> Wrappers = new();
    private static NativeSession _native;
    private static OverlayLayout _overlay;
    private static bool _installed;
    private static long _tick;

    public static void Install(Harmony harmony)
    {
        _installed = false;
        var hit = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "IsMouseOverTopRightButtons");
        var close = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "Close");
        var finalize = AccessTools.Method(typeof(MissionConversationVM), nameof(MissionConversationVM.OnFinalize));
        var dataSource = AccessTools.Field(typeof(AnimusForgeNativeConversationOverlay), "_dataSource");
        if (hit == null || close == null || finalize == null || dataSource?.FieldType != typeof(AnimusForgeNativeConversationOverlayVM))
            throw new MissingMemberException("DialogueUI native lifecycle contract is unavailable.");
        harmony.Patch(hit, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(MouseHitPrefix)));
        harmony.Patch(close, postfix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(OverlayClosed)));
        harmony.Patch(finalize, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(NativeFinalizing)));
        _installed = true;
    }

    public static bool TryWrap(IViewModel original, out ViewModel wrapper)
    {
        wrapper = null;
        if (!_installed || Mission.Current == null || original is not AnimusForgeNativeConversationOverlayVM af) return false;
        if (!Wrappers.TryGetValue(af, out var vm))
        {
            vm = new NativeOverlayVM(af);
            Wrappers.Add(af, vm);
        }
        // The sub-module contract is AI-first.  Apply this while the overlay VM is
        // being created so the native answer list is suppressed before its first
        // visible frame; waiting for the application tick leaves the vanilla list
        // on screen and makes the toolbar appear to be missing.
        if (!af.IsCustomAnswerVisible)
            af.SwitchTalk();
        wrapper = vm;
        return true;
    }

    public static void OnMovieLoaded(string movieName, Widget root, IViewModel datasource)
    {
        if (!_installed || Mission.Current == null || root == null) return;
        try
        {
            if ((movieName == "SPConversation" || movieName == "AFDialogueConversation") && datasource is MissionConversationVM source)
            {
                if (_native != null && ReferenceEquals(_native.Root, root)) return;
                _native?.Dispose();
                _native = new NativeSession(root, source);
                return;
            }
            if (movieName != "AnimusForgeNativeConversationOverlay") return;
            var original = datasource is NativeOverlayVM wrapped ? wrapped.Original : datasource as AnimusForgeNativeConversationOverlayVM;
            if (original == null) return;
            _overlay?.Dispose();
            _overlay = new OverlayLayout(root, original);
        }
        catch (Exception ex) { DialogueUiRuntime.Log("Dialogue overlay setup failed: " + ex.GetType().Name + ": " + ex.Message); }
    }

    public static void Tick()
    {
        _tick++;
        if (_native != null && (!ReferenceEquals(Mission.Current, _native.Mission) || !Attached(_native.Root)))
        { _native.Dispose(); _native = null; }
        _native?.Tick(_tick);
        _overlay?.Tick(_tick);
        if (_overlay != null && (!ReferenceEquals(Mission.Current, _overlay.Mission) || !Attached(_overlay.Root)))
        { _overlay.Dispose(); _overlay = null; }
    }

    public static void OnMovieReleased(Widget root)
    {
        if (_native != null && ReferenceEquals(_native.Root, root)) { _native.Dispose(); _native = null; }
        if (_overlay != null && ReferenceEquals(_overlay.Root, root)) { _overlay.Dispose(); _overlay = null; }
    }

    public static void ReleaseRoot(Widget root) => OnMovieReleased(root);

    public static void Release(IViewModel original)
    {
        if (original is NativeOverlayVM wrapped) ReleaseOverlay(wrapped.Original);
        else if (original is AnimusForgeNativeConversationOverlayVM af) ReleaseOverlay(af);
    }

    public static void Shutdown()
    {
        _installed = false;
        _native?.Dispose(); _native = null;
        _overlay?.Dispose(); _overlay = null;
        foreach (var vm in Wrappers.Values) vm.OnFinalize();
        Wrappers.Clear();
    }

    private static void ReleaseOverlay(AnimusForgeNativeConversationOverlayVM original)
    {
        if (_overlay != null && ReferenceEquals(_overlay.Original, original)) { _overlay.Dispose(); _overlay = null; }
        if (original != null && Wrappers.TryGetValue(original, out var vm)) { vm.OnFinalize(); Wrappers.Remove(original); }
    }

    private static void OverlayClosed(AnimusForgeNativeConversationOverlayVM ____dataSource) => ReleaseOverlay(____dataSource);
    private static void NativeFinalizing(MissionConversationVM __instance)
    {
        if (_native != null && ReferenceEquals(_native.Source, __instance)) { _native.Dispose(); _native = null; }
    }

    private static bool MouseHitPrefix(AnimusForgeNativeConversationOverlayVM ____dataSource, ref bool __result)
    {
        if (_overlay == null || !ReferenceEquals(_overlay.Original, ____dataSource) || !ReferenceEquals(_overlay.Mission, Mission.Current))
            return true;
        __result = _overlay.HitTest();
        return false;
    }

    private static bool Attached(Widget widget)
    {
        Widget node = widget;
        for (int i = 0; node != null && i < 32; i++, node = node.ParentWidget)
            if (ReferenceEquals(node, widget.Context.Root)) return true;
        return false;
    }

    private sealed class NativeSession : IDisposable
    {
        internal readonly Widget Root;
        internal readonly MissionConversationVM Source;
        internal readonly Mission Mission;
        private CharacterTableauWidget _tableau;
        private CharacterObject _character;
        private Agent _speaker;
        private bool _disposed;
        private long _lastRefreshTick = -6;

        internal NativeSession(Widget root, MissionConversationVM source)
        {
            Root = root;
            Source = source;
            Mission = Mission.Current;
            _tableau = root.FindChild("AFDialogueLiveSpeakerPortrait", true) as CharacterTableauWidget;
            if (_tableau != null)
            {
                _tableau.StanceIndex = (int)CharacterViewModel.StanceTypes.EmphasizeFace;
                RefreshSpeaker();
            }
        }

        internal void Tick(long tick)
        {
            if (_disposed || _tableau == null || tick - _lastRefreshTick < 6) return;
            _lastRefreshTick = tick;
            RefreshSpeaker(force: true);
        }

        private void RefreshSpeaker(bool force = false)
        {
            var manager = Campaign.Current?.ConversationManager;
            var speaker = manager?.SpeakerAgent as Agent;
            var character = speaker?.Character as CharacterObject
                ?? manager?.OneToOneConversationCharacter;
            if (!force && ReferenceEquals(character, _character) && ReferenceEquals(speaker, _speaker)) return;
            _character = character;
            _speaker = speaker;
            if (_tableau == null) return;
            if (character == null)
            {
                _tableau.IsVisible = false;
                return;
            }
            _tableau.IsVisible = LiveSpeakerPortrait.Apply(_tableau, speaker, character);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_tableau != null) _tableau.IsVisible = false;
            _tableau = null;
        }
    }

    private sealed class OverlayLayout : IDisposable
    {
        internal readonly Widget Root;
        internal readonly Mission Mission;
        internal readonly AnimusForgeNativeConversationOverlayVM Original;
        private readonly NativeOverlayVM _vm;
        private readonly List<Widget> _buttons = new();
        private bool _defaultModeApplied;
        private bool _disposed;

        internal OverlayLayout(Widget root, AnimusForgeNativeConversationOverlayVM original)
        {
            Root = root; Original = original; Mission = Mission.Current;
            _vm = Wrappers.TryGetValue(original, out var vm) ? vm : null;
            foreach (string id in new[] { "AFDialogueHistory", "AFDialogueGift", "AFDialogueMore", "AFDialogueSwitch", "AFDialogueLeave", "AFDialogueSubmit", "AFDialoguePersona", "AFDialogueTagTest" })
            {
                var button = root.FindChild(id, true);
                if (button != null)
                {
                    _buttons.Add(button);
                    if (button is ButtonWidget b)
                    {
                        try { DialogueUiButtons.Style(b); }
                        catch (Exception ex) { DialogueUiRuntime.Log("Button styling skipped for " + id + ": " + ex.GetType().Name); }
                    }
                }
            }
            var submit = root.FindChild("AFDialogueSubmit", true) as ButtonWidget;
            if (submit != null)
            {
                try { DialogueUiButtons.StyleWaxSeal(submit); }
                catch (Exception ex) { DialogueUiRuntime.Log("Submit styling skipped: " + ex.GetType().Name); }
            }
        }

        internal void Tick(long tick)
        {
            if (_disposed) return;
            _vm?.RefreshConversation();
            if (!_defaultModeApplied && tick > 1)
            {
                _defaultModeApplied = true;
                if (!Original.IsCustomAnswerVisible) Original.SwitchTalk();
            }
            // The owning AFDialogueConversation tree updates the sole live tableau.
            // This overlay remains an input and toolbar layer, so it never creates a
            // second portrait over the native conversation movie.
        }

        internal bool HitTest()
        {
            if (!Root.IsRecursivelyVisible()) return false;
            var mouse = Input.MousePositionPixel;
            foreach (var widget in _buttons)
            {
                if (!widget.IsEnabled || !widget.IsRecursivelyVisible()) continue;
                var p = widget.GlobalPosition; var s = widget.Size;
                if (mouse.x >= p.X && mouse.x <= p.X + s.X && mouse.y >= p.Y && mouse.y <= p.Y + s.Y) return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
