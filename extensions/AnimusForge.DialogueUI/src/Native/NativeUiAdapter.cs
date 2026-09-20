using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Conversation;
using TaleWorlds.TwoDimension;

namespace AnimusForge.DialogueUI.Native;

public static class NativeUiAdapter
{
    private static readonly Dictionary<AnimusForgeNativeConversationOverlayVM, NativeOverlayVM> Wrappers = new();
    private static NativeLayout _native;
    private static OverlayLayout _overlay;
    private static bool _installed;
    private static readonly ConditionalWeakTable<AnimusForgeNativeConversationOverlayVM, OwnerPresentationState> OwnerStates = new();
    private static long _uiTick;
    private static ExitRequest _pendingExit;

    public static void Install(Harmony harmony)
    {
        _installed = false;
        var hit = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "IsMouseOverTopRightButtons");
        var close = AccessTools.Method(typeof(AnimusForgeNativeConversationOverlay), "Close");
        var finalize = AccessTools.Method(typeof(MissionConversationVM), nameof(MissionConversationVM.OnFinalize));
        var datasource = AccessTools.Field(typeof(AnimusForgeNativeConversationOverlay), "_dataSource");
        if (hit == null || hit.ReturnType != typeof(bool) || hit.GetParameters().Length != 0 ||
            close == null || finalize == null || datasource?.FieldType != typeof(AnimusForgeNativeConversationOverlayVM))
            throw new MissingMemberException("Native DialogueUI presentation/lifecycle contract is unavailable.");
        // Fail installation as a unit; the host disables this module on any patch exception.
        harmony.Patch(hit, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(MouseHitPrefix)));
        harmony.Patch(close, postfix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(OverlayClosed)));
        harmony.Patch(finalize, prefix: new HarmonyMethod(typeof(NativeUiAdapter), nameof(NativeFinalizing)));
        _installed = true;
    }

    public static bool TryWrap(IViewModel original, out ViewModel wrapper)
    {
        wrapper = null;
        if (!_installed || Mission.Current == null || original is not AnimusForgeNativeConversationOverlayVM af ||
            _native == null || !ReferenceEquals(_native.Mission, Mission.Current) || !Attached(_native.Root)) return false;
        if (!Wrappers.TryGetValue(af, out var presentation))
        {
            OwnerPresentationState state = OwnerStates.GetValue(af, _ => new OwnerPresentationState());
            presentation = new NativeOverlayVM(af, () => RequestLeave(af), () => state.DefaultAiHandled = true);
            Wrappers.Add(af, presentation);
        }
        wrapper = presentation;
        return true;
    }

    public static void OnMovieLoaded(string movieName, Widget root, IViewModel datasource)
    {
        if (!_installed || Mission.Current == null || root == null) return;
        try
        {
            if (movieName == "SPConversation" && datasource is MissionConversationVM native)
            {
                if (_native != null && ReferenceEquals(_native.Root, root)) return;
                _native?.Dispose();
                _native = null;
                _native = NativeLayout.TryCreate(root, native);
            }
            else if (movieName == "AnimusForgeNativeConversationOverlay")
            {
                var original = datasource as AnimusForgeNativeConversationOverlayVM;
                if (datasource is NativeOverlayVM wrapped) original = wrapped.Original;
                // This marker proves that this is our successfully loaded resource, not AF's fallback.
                if (original == null || root.FindChild("AFDialogueToolbar", true) == null) return;
                _overlay = null;
                _overlay = new OverlayLayout(root, original);
            }
        }
        catch (Exception ex) { DialogueUiRuntime.Log("Native UI adaptation failed: " + ex.GetType().Name + ": " + ex.Message); }
    }

    public static void Tick()
    {
        _uiTick++;
        if (_native != null)
        {
            if (!ReferenceEquals(Mission.Current, _native.Mission) || !Attached(_native.Root))
            { _native.Dispose(); _native = null; }
            else _native.Tick();
        }
        if (_overlay != null && (!ReferenceEquals(Mission.Current, _overlay.Mission) || !Attached(_overlay.Root)))
        { ReleaseOverlay(_overlay.Original); }
        else _overlay?.Tick();
        ProcessPendingExit();
    }

    public static void OnMovieReleased(Widget root)
    {
        if (_native != null && ReferenceEquals(_native.Root, root)) { _native.Dispose(); _native = null; }
        if (_overlay != null && ReferenceEquals(_overlay.Root, root)) ReleaseOverlay(_overlay.Original);
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
        _pendingExit = null;
        _native?.Dispose(); _native = null; _overlay = null;
        foreach (var wrapper in Wrappers.Values) wrapper.OnFinalize();
        Wrappers.Clear();
    }

    private static void ReleaseOverlay(AnimusForgeNativeConversationOverlayVM original)
    {
        if (_pendingExit != null && ReferenceEquals(_pendingExit.Original, original)) _pendingExit = null;
        if (_overlay != null && ReferenceEquals(_overlay.Original, original)) _overlay = null;
        if (original != null && Wrappers.TryGetValue(original, out var wrapper))
        { wrapper.OnFinalize(); Wrappers.Remove(original); }
    }
    private static void OverlayClosed(AnimusForgeNativeConversationOverlayVM ____dataSource) => ReleaseOverlay(____dataSource);
    private static void NativeFinalizing(MissionConversationVM __instance)
    {
        if (_native != null && ReferenceEquals(_native.Source, __instance)) { _native.Dispose(); _native = null; }
    }
    private static bool MouseHitPrefix(AnimusForgeNativeConversationOverlayVM ____dataSource, ref bool __result)
    {
        if (_overlay == null || !ReferenceEquals(_overlay.Original, ____dataSource) ||
            !ReferenceEquals(_overlay.Mission, Mission.Current)) return true;
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

    private sealed class OwnerPresentationState { internal bool DefaultAiHandled; }

    private sealed class ExitRequest
    {
        internal readonly AnimusForgeNativeConversationOverlayVM Original;
        internal readonly MissionConversationVM NativeSource;
        internal readonly Mission Mission;
        internal readonly ConversationManager Manager;
        internal readonly long AfterTick;
        internal ExitRequest(OverlayLayout owner)
        {
            Original = owner.Original; NativeSource = owner.NativeSource; Mission = owner.Mission;
            Manager = owner.Manager; AfterTick = _uiTick + 1;
        }
    }
    private static bool IsCurrentNativeOwner(Mission mission, MissionConversationVM source, ConversationManager manager)
    {
        return _installed && mission != null && ReferenceEquals(Mission.Current, mission) &&
            _native != null && ReferenceEquals(_native.Mission, mission) && ReferenceEquals(_native.Source, source) &&
            Attached(_native.Root) && ReferenceEquals(Campaign.Current?.ConversationManager, manager) &&
            manager != null && manager.IsConversationInProgress;
    }
    private static bool RequestLeave(AnimusForgeNativeConversationOverlayVM original)
    {
        OverlayLayout owner = _overlay;
        if (_pendingExit != null || owner == null || !ReferenceEquals(owner.Original, original) ||
            !Attached(owner.Root) || !IsCurrentNativeOwner(owner.Mission, owner.NativeSource, owner.Manager)) return false;
        OwnerStates.GetValue(original, _ => new OwnerPresentationState()).DefaultAiHandled = true;
        _pendingExit = new ExitRequest(owner);
        return true;
    }
    private static void ProcessPendingExit()
    {
        ExitRequest request = _pendingExit;
        if (request == null || _uiTick < request.AfterTick) return;
        _pendingExit = null;
        if (_overlay == null || !ReferenceEquals(_overlay.Original, request.Original) || !Attached(_overlay.Root) ||
            !IsCurrentNativeOwner(request.Mission, request.NativeSource, request.Manager))
        {
            if (Wrappers.TryGetValue(request.Original, out var stale)) stale.ClearLeavePending();
            return;
        }
        bool wasBusy = !request.Original.IsInputEnabled;
        try
        {
            // This runs on the NEXT application/UI tick, never inside the widget click handler.
            // Retire AF's existing presentation generation first so late replies cannot paint it.
            AnimusForgeNativeConversationOverlay.CloseActive();
            // Closing an owner can invoke lifecycle callbacks; never end a replacement session.
            if (!IsCurrentNativeOwner(request.Mission, request.NativeSource, request.Manager)) return;
            request.Manager.EndConversation();
            DialogueUiRuntime.Log(wasBusy
                ? "Left current mission conversation; AF presentation retired, in-flight network work was not cancelled."
                : "Left current mission conversation through the native EndConversation lifecycle.");
        }
        catch (Exception ex)
        {
            // Do not retry EndConversation automatically: its one-shot callbacks may already have run.
            DialogueUiRuntime.Log("Native conversation exit did not complete: " + ex.GetType().Name + ": " + ex.Message);
            if (Wrappers.TryGetValue(request.Original, out var failed)) failed.ClearLeavePending();
        }
    }

    private sealed class OverlayLayout
    {
        internal readonly Widget Root;
        internal readonly Mission Mission;
        internal readonly AnimusForgeNativeConversationOverlayVM Original;
        internal readonly MissionConversationVM NativeSource;
        internal readonly ConversationManager Manager;
        private readonly OwnerPresentationState _state;
        private readonly long _initializeAfterTick;
        private readonly List<Widget> _buttons = new();
        private readonly Widget _paintSlot;
        private Widget _paint;
        internal OverlayLayout(Widget root, AnimusForgeNativeConversationOverlayVM original)
        {
            Root = root; Original = original; Mission = Mission.Current;
            NativeSource = _native?.Source; Manager = Campaign.Current?.ConversationManager;
            _state = OwnerStates.GetValue(original, _ => new OwnerPresentationState());
            _initializeAfterTick = _uiTick + 1;
            foreach (string id in new[] { "AFDialogueSwitch", "AFDialogueLeave", "AFDialogueHistory", "AFDialogueGift", "AFDialogueMore", "AFDialoguePersona", "AFDialogueTagTest", "AFDialogueSubmit" })
            {
                Widget button = root.FindChild(id, true);
                if (button != null)
                {
                    _buttons.Add(button);
                    try
                    {
                        if ((id == "AFDialogueHistory" || id == "AFDialogueGift" || id == "AFDialogueMore") && button is ButtonWidget round)
                        {
                            DialogueUiButtons.Style(round);
                            StyleRoundLabel(round);
                        }
                        if (id == "AFDialogueSubmit" && button is ButtonWidget submit) StyleSubmitSeal(submit);
                    }
                    catch (Exception ex)
                    {
                        DialogueUiRuntime.Log($"Failed styling button '{id}': {ex.Message}");
                    }
                }
            }
            // Illustrator's existing command is retained; never call an alternative generation path.
            _paintSlot = root.FindChild("AFDialoguePaintSlot", true);
            TryAdoptIllustratorButton();
        }

        private void TryAdoptIllustratorButton()
        {
            if (_paint != null || _paintSlot == null) return;
            Widget paint = Root.FindChild("AnimusForgeConversationIllustrateButton", true);
            if (paint == null) return;
            _paint = paint;
            _paintSlot.IsVisible = paint.IsVisible;
            try
            {
                paint.ParentWidget = _paintSlot;
                ResetBox(paint);
                paint.WidthSizePolicy = SizePolicy.Fixed; paint.SuggestedWidth = 64;
                paint.HeightSizePolicy = SizePolicy.Fixed; paint.SuggestedHeight = 64;
                paint.HorizontalAlignment = HorizontalAlignment.Center;
                if (paint is ButtonWidget button)
                {
                    DialogueUiButtons.Style(button);
                    button.DoNotPassEventsToChildren = true;
                }
                foreach (Widget item in Descendants(paint))
                    if (item is TextWidget text)
                    {
                        text.Brush = text.Brush?.Clone();
                        text.Text = "绘图";
                        if (text.Brush != null) { text.Brush.FontSize = 18; text.Brush.FontColor = Color.FromUint(0xFFFFF4DB); text.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center; text.Brush.TextVerticalAlignment = TextVerticalAlignment.Center; }
                        text.WidthSizePolicy = SizePolicy.StretchToParent;
                        text.HeightSizePolicy = SizePolicy.StretchToParent;
                    }
                if (!_buttons.Contains(paint)) _buttons.Add(paint);
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Log("Failed to adapt Illustrator button: " + ex.Message);
            }
        }

        private static void StyleRoundLabel(ButtonWidget button)
        {
            foreach (Widget child in Descendants(button))
                if (child is TextWidget text && text.Brush != null)
                {
                    text.Brush = text.Brush.Clone();
                    text.Brush.FontColor = Color.FromUint(0xFFFFF4DB);
                }
        }
        private static void StyleSubmitSeal(ButtonWidget button)
        {
            if (button == null) return;
            try
            {
                var seal = DialogueUiSprites.Get("afdui_wax_seal");
                if (seal != null)
                {
                    Brush brush = button.Brush?.Clone()
                        ?? button.Context.GetBrush("Popup.Done.Button.NineGrid")?.Clone()
                        ?? button.Context.GetBrush("ButtonBrush2")?.Clone();
                    if (brush != null)
                    {
                        brush.Name = "AFDialogue.SubmitSeal";
                        brush.TransitionDuration = 0.08f;
                        SetBrushStateSprite(brush, "Default", seal, 1f);
                        SetBrushStateSprite(brush, "Hovered", seal, 1f);
                        SetBrushStateSprite(brush, "Pressed", seal, 0.78f);
                        SetBrushStateSprite(brush, "Selected", seal, 1f);
                        SetBrushStateSprite(brush, "Disabled", seal, 0.40f);
                        button.Brush = brush;
                    }
                }
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Log("Failed to style submit seal: " + ex.Message);
            }
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
            StyleRoundLabel(button);
        }

        private static void SetBrushStateSprite(Brush brush, string stateName, Sprite sprite, float alpha)
        {
            if (brush == null || sprite == null) return;
            Style style = brush.GetStyle(stateName);
            if (style?.DefaultLayer != null)
            {
                style.DefaultLayer.Sprite = sprite;
                style.DefaultLayer.AlphaFactor = alpha;
            }
            else if (stateName == "Default" && brush.DefaultStyleLayer != null)
            {
                brush.DefaultStyleLayer.Sprite = sprite;
                brush.DefaultStyleLayer.AlphaFactor = alpha;
            }
        }

        internal void Tick()
        {
            if (!_state.DefaultAiHandled && _uiTick >= _initializeAfterTick && ReferenceEquals(_overlay, this) &&
                Attached(Root) && Root.IsRecursivelyVisible() && IsCurrentNativeOwner(Mission, NativeSource, Manager))
            {
                // The stamp belongs to AF's owner VM, not this resource/wrapper. Refreshing a movie
                // cannot overwrite a player's subsequent choice of the original answer list.
                _state.DefaultAiHandled = true;
                if (!Original.IsCustomAnswerVisible) Original.SwitchTalk();
            }
            if (_paint == null) TryAdoptIllustratorButton();
            if (_paintSlot != null) _paintSlot.IsVisible = _paint != null && _paint.IsVisible;
        }
        internal bool HitTest()
        {
            if (!Root.IsRecursivelyVisible()) return false;
            Vec2 mouse = Input.MousePositionPixel;
            // The widget references are captured once. Layout computes these physical rectangles.
            for (int i = 0; i < _buttons.Count; i++)
            {
                Widget w = _buttons[i];
                if (!w.IsEnabled || !w.IsRecursivelyVisible()) continue;
                var p = w.GlobalPosition; var s = w.Size;
                if (mouse.x >= p.X && mouse.x <= p.X + s.X && mouse.y >= p.Y && mouse.y <= p.Y + s.Y) return true;
            }
            return false;
        }
    }

    private sealed class NativeLayout : IDisposable
    {
        internal readonly Widget Root;
        internal readonly MissionConversationVM Source;
        internal readonly Mission Mission;
        private readonly List<WidgetSnapshot> _changes = new();
        private readonly HashSet<Widget> _captured = new();
        private readonly HashSet<Widget> _pendingOptions = new();
        private readonly ListPanel _answers;
        private readonly bool _captureOnly;
        private Widget _panel;
        private ImageIdentifierWidget _portrait;
        private CharacterObject _character;
        private Agent _speakerAgent;
        private CharacterImageIdentifierVM _portraitVm;
        private bool _disposed;
        private NativeLayout(Widget root, MissionConversationVM source, ListPanel answers, bool captureOnly = false)
        { Root = root; Source = source; Mission = Mission.Current; _answers = answers; _captureOnly = captureOnly; }

        internal static NativeLayout TryCreate(Widget root, MissionConversationVM source)
        {
            if (root.Id == "AFDialogueNativePanel" || root.FindChild("AFDialogueNativePanel", true) != null)
            {
                DialogueUiRuntime.LogOnce("native-panel-already-owned", "Existing AFDialogueNativePanel retained; duplicate presentation adapter not installed.");
                return null;
            }
            var answers = root.FindChild("AnswerList", true) as ListPanel;
            Widget dialogue = root.FindChild("DialogueContainer", true);
            Widget options = root.FindChild("AnswerListContainer", true);
            Widget name = root.FindChild("CharacterNameContainer", true);
            Widget next = root.FindChild("ContinueButton", true);
            if (root is not ConversationScreenButtonWidget || answers == null || dialogue == null || options == null || name == null || next == null)
            { DialogueUiRuntime.Log("SPConversation controls unavailable; original layout retained."); return null; }
            // Keep the vanilla SPConversation widget tree intact. Its navigation scopes
            // retain parent/target relationships that the gamepad navigation manager
            // traverses outside this module. Reparenting those widgets at runtime can
            // leave a stale scope node behind and crash the next navigation update.
            // The AI overlay only needs the live MissionConversationVM identity for
            // ownership/lifecycle checks, so capture it without touching the tree.
            DialogueUiRuntime.Log("SPConversation captured; vanilla layout retained for navigation safety.");
            return new NativeLayout(root, source, answers, captureOnly: true);
        }
        private void Remember(Widget widget) { if (widget != null && _captured.Add(widget)) _changes.Add(new WidgetSnapshot(widget)); }
        private void Move(Widget widget, Widget parent)
        { Remember(widget); widget.ParentWidget = parent; ResetBox(widget); }

        private void Build(Widget dialogue, Widget options, Widget name, Widget next)
        {
            _panel = Box(Root, "AFDialogueNativePanel");
            _panel.HeightSizePolicy = SizePolicy.Fixed; _panel.SuggestedHeight = 340;
            _panel.VerticalAlignment = VerticalAlignment.Bottom;
            _panel.MarginLeft = 60; _panel.MarginRight = 60; _panel.MarginBottom = 24;
            _panel.SetSiblingIndex(0);

            Widget left = Box(_panel, "AFDialogueIdentity");
            left.WidthSizePolicy = SizePolicy.Fixed; left.SuggestedWidth = 225;
            left.MarginLeft = 20; left.MarginTop = 18; left.MarginBottom = 18;
            left.Sprite = DialogueUiSprites.Get("afdui_parchment_panel");
            Widget center = Box(_panel, "AFDialogueSpeech");
            center.MarginLeft = 260; center.MarginRight = 625; center.MarginTop = 18; center.MarginBottom = 18;
            center.Sprite = DialogueUiSprites.Get("afdui_parchment_panel");
            Widget right = Box(_panel, "AFDialogueOptions");
            right.WidthSizePolicy = SizePolicy.Fixed; right.SuggestedWidth = 590;
            right.HorizontalAlignment = HorizontalAlignment.Right;
            right.MarginRight = 20; right.MarginTop = 18; right.MarginBottom = 18;
            right.Sprite = DialogueUiSprites.Get("afdui_parchment_panel");
            Widget speechContent = Box(center, "AFDialogueSpeechContent");
            speechContent.MarginLeft = speechContent.MarginRight = 22;
            speechContent.MarginTop = 112; speechContent.MarginBottom = 20;
            Widget optionsContent = Box(right, "AFDialogueOptionsContent");
            optionsContent.MarginLeft = optionsContent.MarginRight = 22;
            optionsContent.MarginTop = 64; optionsContent.MarginBottom = 20;

            _portrait = new ImageIdentifierWidget(Root.Context)
            {
                Id = "AFDialoguePortrait", WidthSizePolicy = SizePolicy.Fixed, HeightSizePolicy = SizePolicy.Fixed,
                SuggestedWidth = 142, SuggestedHeight = 176, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top, MarginTop = 16, HideWhenNull = true, DoNotAcceptEvents = true
            };
            left.AddChild(_portrait);
            Move(name, left); name.HeightSizePolicy = SizePolicy.CoverChildren; name.VerticalAlignment = VerticalAlignment.Bottom;
            name.MarginBottom = 48; name.Sprite = null;
            StyleTree(name, 23, true);
            foreach (Widget item in Descendants(name))
            {
                Remember(item);
                item.MarginLeft = 0; item.MarginRight = 0;
                item.WidthSizePolicy = SizePolicy.StretchToParent;
                if (item is TextWidget text) { text.HeightSizePolicy = SizePolicy.CoverChildren; text.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center; }
            }
            Widget banner = Root.FindChild("ConversedHeroBanner", true)?.ParentWidget;
            if (banner != null)
            {
                Move(banner, left); banner.WidthSizePolicy = SizePolicy.Fixed; banner.HeightSizePolicy = SizePolicy.Fixed;
                banner.SuggestedWidth = 46; banner.SuggestedHeight = 46;
                banner.HorizontalAlignment = HorizontalAlignment.Center; banner.VerticalAlignment = VerticalAlignment.Bottom;
                banner.MarginBottom = 4;
            }

            Widget speechInner = AddScroll(speechContent, "AFDialogueSpeechScroll");
            Move(dialogue, speechInner); dialogue.HeightSizePolicy = SizePolicy.CoverChildren;
            foreach (Widget item in Descendants(dialogue))
            {
                Remember(item);
                if (item is DimensionSyncWidget) { _changes[_changes.Count - 1].RestoreVisibility = true; item.IsVisible = false; continue; }
                item.WidthSizePolicy = SizePolicy.StretchToParent; item.HeightSizePolicy = SizePolicy.CoverChildren;
                item.HorizontalAlignment = HorizontalAlignment.Left; item.VerticalAlignment = VerticalAlignment.Top;
                item.MarginLeft = item.MarginRight = 0; item.MarginTop = item.MarginBottom = 0;
                item.MinHeight = 0; item.AlphaFactor = 1; item.Sprite = null;
            }
            StyleTree(dialogue, 25, true);

            Widget optionsInner = AddScroll(optionsContent, "AFDialogueOptionsScroll");
            Move(options, optionsInner); options.HeightSizePolicy = SizePolicy.CoverChildren;
            Remember(_answers); _answers.VerticalAlignment = VerticalAlignment.Top;
            _answers.ItemAddEventHandlers.Add(OptionAdded);
            _answers.ItemRemoveEventHandlers.Add(OptionRemoved);
            for (int i = 0; i < _answers.ChildCount; i++) StyleOption(_answers.GetChild(i));

            Move(next, optionsContent); next.WidthSizePolicy = SizePolicy.StretchToParent; next.HeightSizePolicy = SizePolicy.Fixed;
            next.SuggestedHeight = 80; next.VerticalAlignment = VerticalAlignment.Center;
            foreach (Widget item in Descendants(next))
            {
                Remember(item); ResetBox(item);
                item.WidthSizePolicy = SizePolicy.StretchToParent; item.HeightSizePolicy = SizePolicy.StretchToParent;
                item.Sprite = null;
            }
            StyleTree(next, 26, true);
            Widget persuasion = Root.FindChild("PersuationExtensionListPanel", true);
            if (persuasion != null)
            {
                Move(persuasion, Root); persuasion.WidthSizePolicy = SizePolicy.StretchToParent;
                persuasion.HeightSizePolicy = SizePolicy.CoverChildren; persuasion.VerticalAlignment = VerticalAlignment.Bottom;
                persuasion.MarginLeft = 320; persuasion.MarginRight = 680; persuasion.MarginBottom = 372;
            }
            RefreshPortrait();
            DialogueUiRuntime.Log("Mission SPConversation restyled; original answer, persuasion and continue owners retained.");
        }
        private void OptionAdded(Widget parent, Widget child)
        {
            // Generated prefabs add the child BEFORE CreateWidgets/SetAttributes. Style on the
            // next owner tick, after construction, rather than scanning unfinished trees here.
            if (!_disposed && child != null) _pendingOptions.Add(child);
        }
        private void OptionRemoved(Widget parent, Widget child)
        {
            if (_disposed || child == null) return;
            _pendingOptions.Remove(child);
            var retired = new HashSet<Widget>(Descendants(child, true));
            _changes.RemoveAll(state => retired.Contains(state.Widget));
            _captured.ExceptWith(retired);
        }
        internal void Tick()
        {
            if (_pendingOptions.Count != 0)
            {
                foreach (Widget option in _pendingOptions)
                    if (ReferenceEquals(option.ParentWidget, _answers)) StyleOption(option);
                _pendingOptions.Clear();
            }
            RefreshPortrait();
        }
        private void StyleOption(Widget option)
        {
            if (option == null) return;
            // Keep the original ConversationOptionListPanel and every command/tooltip binding.
            Remember(option); option.WidthSizePolicy = SizePolicy.StretchToParent;
            option.MarginLeft = option.MarginRight = 0;
            if (option is ConversationOptionListPanel nativeOption && nativeOption.OptionButtonWidget?.ParentWidget != null)
            {
                Widget textContainer = nativeOption.OptionButtonWidget.ParentWidget;
                Remember(textContainer);
                // The original template hard-codes 750 here; let the text wrap in our right column.
                textContainer.WidthSizePolicy = SizePolicy.StretchToParent;
            }
            // Preserve the vanilla state-dependent colours against its normal/special backgrounds.
            StyleTree(option, 22, false);
        }
        private void StyleTree(Widget root, int fontSize, bool dark)
        {
            foreach (Widget item in Descendants(root, true))
            {
                if (item is TextWidget text)
                {
                    Remember(text); text.Brush = text.Brush?.Clone();
                    if (text.Brush != null)
                    {
                        text.Brush.FontSize = fontSize;
                        if (dark) text.Brush.FontColor = Color.FromUint(0xFF382919);
                    }
                }
                else if (item is RichTextWidget rich)
                {
                    Remember(rich); rich.Brush = rich.Brush?.Clone();
                    if (rich.Brush != null)
                    {
                        rich.Brush.FontSize = fontSize;
                        if (dark) rich.Brush.FontColor = Color.FromUint(0xFF382919);
                    }
                }
            }
        }
        internal void RefreshPortrait()
        {
            var conversation = Campaign.Current?.ConversationManager;
            Agent speaker = conversation?.SpeakerAgent as Agent;
            CharacterObject character = conversation?.SpeakerAgent?.Character as CharacterObject
                ?? conversation?.OneToOneConversationCharacter;
            if (ReferenceEquals(character, _character) && ReferenceEquals(speaker, _speakerAgent)) return;
            _character = character;
            _speakerAgent = speaker;
            _portraitVm?.OnFinalize(); _portraitVm = null;
            _portrait.ImageId = string.Empty;
            if (character == null) return;
            try
            {
                CharacterCode code = speaker?.SpawnEquipment == null
                    ? CharacterCode.CreateFrom(character)
                    : CharacterCode.CreateFrom(speaker.SpawnEquipment.CalculateEquipmentCode(), speaker.BodyPropertiesValue,
                        speaker.IsFemale, character.IsHero, speaker.ClothingColor1, speaker.ClothingColor2,
                        character.DefaultFormationClass, character.Race);
                _portraitVm = new CharacterImageIdentifierVM(code);
                _portrait.TextureProviderName = _portraitVm.TextureProviderName;
                _portrait.AdditionalArgs = _portraitVm.AdditionalArgs;
                _portrait.ImageId = _portraitVm.Id;
            }
            catch (Exception ex) { DialogueUiRuntime.Log("Native portrait unavailable: " + ex.GetType().Name); }
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _answers.ItemAddEventHandlers.Remove(OptionAdded);
            _answers.ItemRemoveEventHandlers.Remove(OptionRemoved);
            // Restore children before removing our empty layout containers.
            for (int i = _changes.Count - 1; i >= 0; i--)
                try { _changes[i].Restore(); } catch { /* An engine-destroyed root needs no restoration. */ }
            try { _portrait?.OnClearTextureProvider(); } catch { }
            _portraitVm?.OnFinalize(); _portraitVm = null;
            if (_panel != null) _panel.ParentWidget = null;
            _changes.Clear(); _captured.Clear(); _pendingOptions.Clear();
        }
    }

    private static Widget Box(Widget parent, string id)
    {
        var box = new Widget(parent.Context)
        {
            Id = id, WidthSizePolicy = SizePolicy.StretchToParent, HeightSizePolicy = SizePolicy.StretchToParent,
            DoNotAcceptEvents = true, DoNotPassEventsToChildren = false
        };
        parent.AddChild(box); return box;
    }
    private static Widget AddScroll(Widget parent, string id)
    {
        var scroll = new ScrollablePanel(parent.Context)
        {
            Id = id, WidthSizePolicy = SizePolicy.StretchToParent, HeightSizePolicy = SizePolicy.StretchToParent,
            AutoHideScrollBars = true, AutoAdjustScrollbarHandleSize = true, MouseScrollAxis = AlignmentAxis.Vertical
        };
        parent.AddChild(scroll);
        Widget clip = Box(scroll, id + "Clip"); clip.ClipContents = true; clip.MarginRight = 8;
        Widget inner = Box(clip, id + "Inner"); inner.HeightSizePolicy = SizePolicy.CoverChildren;
        var bar = new ScrollbarWidget(parent.Context)
        {
            WidthSizePolicy = SizePolicy.Fixed, SuggestedWidth = 6, HeightSizePolicy = SizePolicy.StretchToParent,
            HorizontalAlignment = HorizontalAlignment.Right, AlignmentAxis = AlignmentAxis.Vertical,
            MinValue = 0, MaxValue = 100
        };
        scroll.AddChild(bar);
        var handle = new Widget(parent.Context)
        {
            WidthSizePolicy = SizePolicy.StretchToParent, HeightSizePolicy = SizePolicy.Fixed, SuggestedHeight = 32,
            Sprite = parent.Context.SpriteData.GetSprite("BlankWhiteSquare_9"), Color = Color.FromUint(0xFF75532E)
        };
        bar.AddChild(handle); bar.Handle = handle;
        scroll.ClipRect = clip; scroll.InnerPanel = inner; scroll.VerticalScrollbar = bar;
        return inner;
    }
    private static void ResetBox(Widget w)
    {
        w.MarginLeft = w.MarginRight = w.MarginTop = w.MarginBottom = 0;
        w.PositionXOffset = w.PositionYOffset = 0;
        w.HorizontalAlignment = HorizontalAlignment.Left; w.VerticalAlignment = VerticalAlignment.Top;
        w.WidthSizePolicy = SizePolicy.StretchToParent; w.HeightSizePolicy = SizePolicy.StretchToParent;
    }
    private static IEnumerable<Widget> Descendants(Widget parent, bool includeRoot = false)
    {
        if (includeRoot) yield return parent;
        for (int i = 0; i < parent.ChildCount; i++)
        {
            Widget child = parent.GetChild(i); yield return child;
            foreach (Widget descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed class WidgetSnapshot
    {
        internal Widget Widget => _w;
        internal bool RestoreVisibility;
        private readonly Widget _w, _parent;
        private readonly int _index;
        private readonly SizePolicy _width, _height;
        private readonly HorizontalAlignment _horizontal;
        private readonly VerticalAlignment _vertical;
        private readonly float _sw, _sh, _ml, _mr, _mt, _mb, _x, _y, _min, _alpha;
        private readonly bool _visible;
        private readonly Sprite _sprite;
        private readonly Brush _brush;
        internal WidgetSnapshot(Widget w)
        {
            _w = w; _parent = w.ParentWidget; _index = _parent?.GetChildIndex(w) ?? 0;
            _width = w.WidthSizePolicy; _height = w.HeightSizePolicy; _horizontal = w.HorizontalAlignment; _vertical = w.VerticalAlignment;
            _sw = w.SuggestedWidth; _sh = w.SuggestedHeight; _ml = w.MarginLeft; _mr = w.MarginRight; _mt = w.MarginTop; _mb = w.MarginBottom;
            _x = w.PositionXOffset; _y = w.PositionYOffset; _min = w.MinHeight; _alpha = w.AlphaFactor;
            _sprite = w.Sprite; _visible = w.IsVisible; _brush = (w as BrushWidget)?.Brush;
        }
        internal void Restore()
        {
            if (!ReferenceEquals(_w.ParentWidget, _parent)) _w.ParentWidget = _parent;
            if (_parent != null) _w.SetSiblingIndex(_index);
            _w.WidthSizePolicy = _width; _w.HeightSizePolicy = _height; _w.HorizontalAlignment = _horizontal; _w.VerticalAlignment = _vertical;
            _w.SuggestedWidth = _sw; _w.SuggestedHeight = _sh; _w.MarginLeft = _ml; _w.MarginRight = _mr; _w.MarginTop = _mt; _w.MarginBottom = _mb;
            _w.PositionXOffset = _x; _w.PositionYOffset = _y; _w.MinHeight = _min; _w.AlphaFactor = _alpha; _w.Sprite = _sprite;
            if (RestoreVisibility) _w.IsVisible = _visible;
            if (_w is BrushWidget brush) brush.Brush = _brush;
        }
    }
}
