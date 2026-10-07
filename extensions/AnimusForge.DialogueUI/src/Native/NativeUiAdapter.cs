using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapConversation;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.DialogueUI.Native;

public static class NativeUiAdapter
{
    private static readonly Dictionary<AnimusForgeNativeConversationOverlayVM, NativeOverlayVM> Wrappers = new();
    private static NativeSession _native;
    private static OverlayLayout _overlay;
    private static bool _installed;
    private static long _tick;
    private static FieldInfo _overlayLayerField;
    private static FieldInfo _overlayDataSourceField;
    private static FieldInfo _overlaySubmittingField;
    private static FieldInfo _activeOverlayField;
    private static FieldInfo _temporaryUiField;

    public static void Install(Harmony harmony)
    {
        _installed = false;
        var hit = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "IsMouseOverTopRightButtons");
        var close = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "Close");
        var finalize = AccessTools.Method(typeof(MissionConversationVM), nameof(MissionConversationVM.OnFinalize));
        var dataSource = AccessTools.Field(typeof(AnimusForgeNativeConversationOverlay), "_dataSource");
        var restrictions = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "UpdateButtonsOnlyInputRestrictions");
        var pendingNpcOpening = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "TryStartPendingNpcOpening");
        var focusInput = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "FocusInputIfVisible");
        var restoreOrdinary = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "RestoreNativeConversationInputAfterOrdinaryMode");
        var restoreTemporary = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "RestoreOverlayAfterTemporarySystemUi");
        var showRoot = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "ShowOverlayRoot");
        _overlayLayerField = AccessTools.Field(typeof(AnimusForgeNativeConversationOverlay), "_layer");
        _overlaySubmittingField = AccessTools.Field(typeof(AnimusForgeNativeConversationOverlay), "_isSubmitting");
        _overlayDataSourceField = dataSource;
        _activeOverlayField = AccessTools.Field(typeof(AnimusForgeNativeConversationOverlay), "_activeOverlay");
        _temporaryUiField = AccessTools.Field(typeof(AnimusForgeNativeConversationOverlay), "_temporarySystemUiActive");
        if (hit == null || close == null || finalize == null || dataSource?.FieldType != typeof(AnimusForgeNativeConversationOverlayVM)
            || restrictions == null || pendingNpcOpening == null || focusInput == null || restoreOrdinary == null || restoreTemporary == null || showRoot == null || _activeOverlayField == null || _temporaryUiField == null
            || _overlayLayerField?.FieldType != typeof(GauntletLayer) || _overlaySubmittingField?.FieldType != typeof(bool))
            throw new MissingMemberException("DialogueUI native lifecycle contract is unavailable.");
        harmony.Patch(hit, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(MouseHitPrefix)));
        harmony.Patch(close, postfix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(OverlayClosed)));
        harmony.Patch(finalize, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(NativeFinalizing)));
        harmony.Patch(restrictions, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(UpdateRestrictionsPrefix)));
        harmony.Patch(pendingNpcOpening, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(NpcOpeningPrefix)));
        harmony.Patch(focusInput,
            prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(AuxiliaryFocusPrefix)));
        harmony.Patch(restoreOrdinary, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(AuxiliaryRestorePrefix)));
        harmony.Patch(restoreTemporary, postfix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(OverlayRestored)));
        harmony.Patch(showRoot, postfix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(OverlayRestored)));
        MapPortraitSource.Install(harmony);
        PortraitCamera.Install(harmony);
        try { InlineTradeBridge.Install(harmony); }
        catch (Exception ex) { DialogueUiRuntime.Log("Inline trade unavailable: " + ex.Message); }
        _installed = true;
    }

    public static bool TryWrap(IViewModel original, out ViewModel wrapper)
    {
        wrapper = null;
        if (!_installed || (Mission.Current == null && Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
            || original is not AnimusForgeNativeConversationOverlayVM af) return false;
        if (!Wrappers.TryGetValue(af, out var vm))
        {
            vm = new NativeOverlayVM(af);
            Wrappers.Add(af, vm);
            // Capture/apply once: a resource reload must not override a manual mode switch.
            if (vm.AutoEnterAiMode && !af.IsCustomAnswerVisible)
                af.SwitchTalk();
        }
        wrapper = vm;
        return true;
    }

    // Default entry already happens at movie load. A queued opening must not pull a
    // non-Hero or a player who manually returned to ordinary mode back into AI.
    private static bool NpcOpeningPrefix(AnimusForgeNativeConversationOverlayVM ____dataSource)
        => !_installed || !DialogueUiRuntime.Enabled || ____dataSource == null
            || !Wrappers.ContainsKey(____dataSource) || ____dataSource.IsCustomAnswerVisible;

    public static void OnMovieLoaded(string movieName, Widget root, IViewModel datasource)
    {
        if (!_installed || root == null) return;
        try
        {
            var source = datasource is MapConversationVM map ? map.DialogController : datasource as MissionConversationVM;
            if ((movieName == "SPConversation" || movieName == "AFDialogueConversation" || movieName == "MapConversation") && source != null)
            {
                if (_native != null && ReferenceEquals(_native.Root, root)) return;
                _native?.Dispose();
                _native = new NativeSession(root, source, datasource as MapConversationVM);
                return;
            }
            if (movieName != "AnimusForgeNativeConversationOverlay") return;
            var original = datasource is NativeOverlayVM wrapped ? wrapped.Original : datasource as AnimusForgeNativeConversationOverlayVM;
            if (original == null) return;
            _overlay?.Dispose();
            _overlay = new OverlayLayout(root, original, _native?.IsMapConversation == true || Mission.Current == null);
        }
        catch (Exception ex) { DialogueUiRuntime.Log("Dialogue overlay setup failed: " + ex.GetType().Name + ": " + ex.Message); }
    }

    public static void Tick(float dt)
    {
        _tick++;
        if (_native != null && ((!_native.IsMapConversation && !ReferenceEquals(Mission.Current, _native.Mission)) || !Attached(_native.Root)))
        { _native.Dispose(); _native = null; }
        _native?.Tick(dt);
        if (_overlay != null && ((!_overlay.IsMapConversation && !ReferenceEquals(Mission.Current, _overlay.Mission)) || !Attached(_overlay.Root)))
        { _overlay.Dispose(); _overlay = null; }
        _overlay?.Tick(_tick);
        if (_overlay != null && Wrappers.TryGetValue(_overlay.Original, out var wrapper))
            wrapper.Auxiliary.Tick(dt);
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
        MapPortraitSource.Shutdown();
        PortraitCamera.Shutdown();
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
        if (_overlay == null || !ReferenceEquals(_overlay.Original, ____dataSource)
            || (!_overlay.IsMapConversation && !ReferenceEquals(_overlay.Mission, Mission.Current)))
            return true;
        __result = _overlay.HitTest();
        return false;
    }

    // The host evaluates buttons-only restrictions only at discrete moments (open,
    // submit, restore).  With the pen layout the controls sit at the bottom console,
    // so a cursor outside the column at that instant permanently resets restrictions
    // and leaves the toolbar and input field dead until the next evaluation.  While
    // the custom answer input is visible the overlay must instead claim full input
    // and focus, matching the host's own FocusInputIfVisible path.
    private static bool UpdateRestrictionsPrefix(AnimusForgeNativeConversationOverlay __instance)
    {
        try
        {
            if (!DialogueUiRuntime.Enabled || !_installed
                || !(_overlayDataSourceField.GetValue(__instance) is AnimusForgeNativeConversationOverlayVM ds)
                || !Wrappers.TryGetValue(ds, out var wrapper)) return true;
            if (_temporaryUiField.GetValue(__instance) is true)
            { SetAuxiliaryLayerInput(__instance, false); return false; }
            if (wrapper.Auxiliary.IsOpen)
            {
                SetAuxiliaryLayerInput(__instance, wrapper.Auxiliary.IsVisible);
                return false;
            }
            if (!ds.IsCustomAnswerVisible) return true;
            if (_overlaySubmittingField.GetValue(__instance) is true)
                return true;
            if (_overlayLayerField.GetValue(__instance) is GauntletLayer layer)
            {
                layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                layer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(layer);
            }
            ds.RequestInputFocus();
            return false;
        }
        catch
        {
            return true;
        }
    }

    private static bool AuxiliaryFocusPrefix(AnimusForgeNativeConversationOverlay __instance)
    {
        if (!DialogueUiRuntime.Enabled || !_installed
            || !(_overlayDataSourceField.GetValue(__instance) is AnimusForgeNativeConversationOverlayVM ds)
            || !Wrappers.TryGetValue(ds, out var vm) || !vm.Auxiliary.IsOpen) return true;
        SetAuxiliaryLayerInput(__instance, vm.Auxiliary.IsVisible);
        return false;
    }

    private static bool AuxiliaryRestorePrefix(AnimusForgeNativeConversationOverlayVM ____dataSource, bool ____temporarySystemUiActive)
    {
        if (!DialogueUiRuntime.Enabled || !_installed || ____temporarySystemUiActive
            || ____dataSource == null || !Wrappers.TryGetValue(____dataSource, out var vm) || !vm.Auxiliary.IsVisible) return true;
        NativeConversationAnswerAreaController.SetSuppressed(true);
        return false;
    }

    private static void OverlayRestored(AnimusForgeNativeConversationOverlayVM ____dataSource, bool ____temporarySystemUiActive, bool ____isClosed)
    {
        if (____isClosed || ____temporarySystemUiActive || !_installed || !DialogueUiRuntime.Enabled
            || ____dataSource == null || !Wrappers.TryGetValue(____dataSource, out var vm)) return;
        vm.RefreshAfterSystemUi();
        DialogueUiRuntime.Log("Conversation controls restored: mode=" + (____dataSource.IsCustomAnswerVisible ? "AI" : "ordinary")
            + ", auxiliaryOpen=" + vm.Auxiliary.IsOpen + ", toolbarVisible=" + vm.IsToolbarVisible);
    }

    // Called on panel transitions, not in a frame polling loop. Never steal encyclopedia focus.
    private static void SetAuxiliaryLayerInput(AnimusForgeNativeConversationOverlay host, bool visible)
    {
        if (!(_overlayLayerField.GetValue(host) is GauntletLayer layer)) return;
        visible = visible && !(_temporaryUiField?.GetValue(host) is true);
        if (visible)
        {
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            layer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(layer);
        }
        else
        {
            layer.InputRestrictions.ResetInputRestrictions();
            layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(layer);
        }
    }

    internal static void AuxiliaryStateChanged(NativeOverlayVM vm)
    {
        if (!(_activeOverlayField?.GetValue(null) is AnimusForgeNativeConversationOverlay host)
            || !ReferenceEquals(_overlayDataSourceField.GetValue(host), vm.Original)) return;
        _overlay?.RefreshVisibility();
        if (_overlay?.Root.EventManager != null)
        {
            _overlay.Root.EventManager.FocusedWidget = null;
            if (!vm.Auxiliary.IsOpen && vm.Original.IsCustomAnswerVisible)
                _overlay.Root.EventManager.FocusedWidget = _overlay.InputEditor;
        }
        if (vm.Auxiliary.IsOpen) SetAuxiliaryLayerInput(host, vm.Auxiliary.IsVisible);
        else if (vm.Original.IsCustomAnswerVisible) UpdateRestrictionsPrefix(host);
        else
        {
            NativeConversationAnswerAreaController.SetSuppressed(false);
            NativeConversationAnswerAreaController.ForceRestoreAll();
            SetAuxiliaryLayerInput(host, false);
        }
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
        internal bool IsMapConversation => _mapSource != null;
        private readonly MapConversationVM _mapSource;
        private CharacterTableauWidget _tableau;
        private readonly LiveSpeakerPortrait _portrait = new LiveSpeakerPortrait();
        private bool _disposed;
        private float _refreshElapsed;

        internal NativeSession(Widget root, MissionConversationVM source, MapConversationVM mapSource)
        {
            Root = root;
            Source = source;
            Mission = Mission.Current;
            _mapSource = mapSource;
            _tableau = root.FindChild("AFDialogueLiveSpeakerPortrait", true) as CharacterTableauWidget;
            if (_tableau != null)
            {
                PortraitCamera.Register(_tableau);
                _tableau.StanceIndex = (int)CharacterViewModel.StanceTypes.EmphasizeFace;
                RefreshSpeaker();
            }
        }

        internal void Tick(float dt)
        {
            if (_disposed || _tableau == null) return;
            _refreshElapsed += dt;
            // Bounded 10 Hz appearance checks, independent of frame rate; no catch-up loop.
            if (_refreshElapsed < 0.1f) return;
            _refreshElapsed = 0f;
            RefreshSpeaker();
        }

        private void RefreshSpeaker()
        {
            if (_mapSource != null)
            {
                // The map's IAgent is only a character descriptor, not the rendered individual.
                // Wait for the real tableau appearance; never use troop-default equipment/face.
                _tableau.IsVisible = MapPortraitSource.TryGet(_mapSource.TableauData, out var appearance)
                    && _portrait.Apply(_tableau, null, appearance.Character, appearance);
                return;
            }
            var manager = Campaign.Current?.ConversationManager;
            CharacterObject character = manager?.OneToOneConversationCharacter;
            Agent speaker = null;

            if (manager?.ConversationAgents != null)
            {
                foreach (var a in manager.ConversationAgents)
                {
                    if (a is Agent agent && !agent.IsMainAgent)
                    {
                        speaker = agent;
                        if (character == null) character = agent.Character as CharacterObject;
                        break;
                    }
                }
            }

            if (speaker == null)
            {
                var spk = manager?.SpeakerAgent as Agent;
                var lst = manager?.ListenerAgent as Agent;
                speaker = (spk != null && !spk.IsMainAgent) ? spk : (lst != null && !lst.IsMainAgent) ? lst : spk ?? lst;
                if (character == null && speaker != null)
                    character = speaker.Character as CharacterObject;
            }

            if (character == null)
                character = (speaker?.Character as CharacterObject) ?? manager?.OneToOneConversationCharacter;

            if (_tableau == null) return;
            if (character == null)
            {
                _tableau.IsVisible = false;
                return;
            }
            _tableau.IsVisible = _portrait.Apply(_tableau, speaker, character);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            PortraitCamera.Unregister(_tableau);
            if (_tableau != null) _tableau.IsVisible = false;
            _tableau = null;
        }
    }

    private sealed class OverlayLayout : IDisposable
    {
        internal readonly Widget Root;
        internal readonly Mission Mission;
        internal readonly AnimusForgeNativeConversationOverlayVM Original;
        internal readonly bool IsMapConversation;
        private readonly List<Widget> _buttons = new();
        private readonly NativeOverlayVM _wrapper;
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ButtonWidget, object> _styled = new();
        private int _styledVersion = -1;
        private int _stylePasses;
        internal readonly Widget InputEditor;
        private bool _disposed;

        internal OverlayLayout(Widget root, AnimusForgeNativeConversationOverlayVM original, bool isMapConversation)
        {
            Root = root; Original = original; Mission = Mission.Current;
            IsMapConversation = isMapConversation;
            _column = root.FindChild("AFDialogueRightColumn", true);
            _toolbar = root.FindChild("AFDialogueToolbar", true);
            _input = root.FindChild("AFDialogueInput", true);
            _auxiliary = root.FindChild("AFDialogueAuxiliaryPanel", true);
            Wrappers.TryGetValue(original, out _wrapper);
            InputEditor = root.FindChild("AFDialogueInputEditor", true);
            foreach (string id in new[] { "AFDialogueHistory", "AFDialogueGift", "AnimusForgeConversationIllustrateButton", "AFDialogueSwitch", "AFDialogueLeave" })
            {
                var button = root.FindChild(id, true);
                if (button != null)
                {
                    _buttons.Add(button);
                }
            }
        }

        private readonly Widget _column;
        private readonly Widget _toolbar;
        private readonly Widget _input;
        private readonly Widget _auxiliary;

        internal void RefreshVisibility()
        {
            if (_disposed || _wrapper == null) return;
            // Reconcile cached nodes on transitions; do not force a hidden child panel open.
            if (_toolbar != null) _toolbar.IsVisible = _wrapper.IsToolbarVisible;
            if (_input != null) _input.IsVisible = _wrapper.IsCustomAnswerVisible;
            if (_auxiliary != null) _auxiliary.IsVisible = _wrapper.Auxiliary.IsVisible;
        }

        internal void Tick(long tick)
        {
            if (_disposed) return;
            // List binding can instantiate children on the next UI tick. Two bounded passes
            // after a structural change, no perpetual widget scan or per-frame brush creation.
            if (_wrapper?.Auxiliary.IsVisible == true)
            {
                if (_styledVersion != _wrapper.Auxiliary.LayoutVersion)
                { _styledVersion = _wrapper.Auxiliary.LayoutVersion; _stylePasses = 2; }
                if (_stylePasses > 0) { _stylePasses--; StyleAuxiliary(_auxiliary); }
            }
            // The host asks for input focus by bumping InputFocusVersion (its own prefab binds that to
            // the editor's FocusRequestId). DevMultilineEditableTextWidget only has a one-shot AutoFocus,
            // so after 普通模式 → AI 模式 the editor stayed unfocused and typing went nowhere.
            // Mirror FocusRequestId here: one int compare per frame, refocus only on a new request.
            int focusVersion = Original.InputFocusVersion;
            if (focusVersion != _focusVersion)
            {
                _focusVersion = focusVersion;
                _focusPendingFrames = 30; // the editor may only become visible on the next layout pass
            }
            if (_focusPendingFrames > 0)
            {
                _focusPendingFrames--;
                if (!Original.IsCustomAnswerVisible || _wrapper?.Auxiliary.IsOpen == true || InputEditor == null || Root.EventManager == null)
                    _focusPendingFrames = 0;
                else if (InputEditor.IsRecursivelyVisible())
                {
                    if (Root.EventManager.FocusedWidget != InputEditor) Root.EventManager.FocusedWidget = InputEditor;
                    _focusPendingFrames = 0;
                }
            }
        }

        private int _focusVersion = int.MinValue;
        private int _focusPendingFrames;

        internal bool HitTest()
        {
            if (!Root.IsRecursivelyVisible()) return false;
            if (_wrapper?.Auxiliary.IsVisible == true) return true;
            // AI mode owns body/background clicks even after a reply finishes.
            // The core command guard also covers map/mission ContinueKey and fallback UI.
            if (Original.IsCustomAnswerVisible) return true;
            var mouse = Input.MousePositionPixel;

            // The column is claimed only while the AI input occupies it. In ordinary mode the native
            // answer list renders in this same area; claiming it there made every answer unclickable
            // (the host re-evaluates this hit test each tick). Only the toolbar buttons count then.
            if (Original.IsCustomAnswerVisible && _column != null && _column.IsRecursivelyVisible())
            {
                var cp = _column.GlobalPosition; var cs = _column.Size;
                if (mouse.x >= cp.X && mouse.x <= cp.X + cs.X && mouse.y >= cp.Y && mouse.y <= cp.Y + cs.Y)
                    return true;
            }

            foreach (var widget in _buttons)
            {
                if (!widget.IsEnabled || !widget.IsRecursivelyVisible()) continue;
                var p = widget.GlobalPosition; var s = widget.Size;
                if (mouse.x >= p.X && mouse.x <= p.X + s.X && mouse.y >= p.Y && mouse.y <= p.Y + s.Y) return true;
            }
            return false;
        }

        private void StyleAuxiliary(Widget node)
        {
            if (node == null) return;
            if (node is ButtonWidget button && !_styled.TryGetValue(button, out _))
            {
                if (button.Id == "AFAuxRow") DialogueUiButtons.StyleRow(button);
                else DialogueUiButtons.StyleParchmentTab(button);
                _styled.Add(button, new object());
            }
            for (int i = 0; i < node.ChildCount; i++) StyleAuxiliary(node.GetChild(i));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
