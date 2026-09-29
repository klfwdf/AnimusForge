using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.ScreenSystem;
using AnimusForge.DialogueUI.Native;

namespace AnimusForge.DialogueUI.Scene;

// Presents the host's own scene action menu (ShoutBehavior.TriggerShout) as the Pen radial wheel.
// The host still builds every entry, decides eligibility and runs each choice; this only replaces
// the native multi-selection popup, only for that menu, only when a session panel style is active.
internal static class SceneWheel
{
    private static WheelLayer _open;
    private static bool _installed;

    internal static bool IsOpen => _open != null;

    internal static void Install(Harmony harmony)
    {
        var show = AccessTools.Method(typeof(MBInformationManager), nameof(MBInformationManager.ShowMultiSelectionInquiry));
        if (show == null) throw new MissingMethodException("MBInformationManager.ShowMultiSelectionInquiry");
        harmony.Patch(show, prefix: new HarmonyMethod(typeof(SceneWheel), nameof(ShowPrefix)));
        _installed = true;
    }

    // Runs for every multi-selection popup; the checks below are a few field reads and a short scan.
    private static bool ShowPrefix(MultiSelectionInquiryData __0, bool __runOriginal)
    {
        // Cheap structural check first; the MCM read only happens for the host's scene menu.
        if (!__runOriginal || !_installed || !IsHostSceneMenu(__0) || !SceneSessionPanel.IsAvailable) return true;
        try
        {
            if (!DialogueUiSprites.EnsureSceneLoaded()) return true;
            _open?.Close(invokeCancel: false);
            _open = WheelLayer.TryOpen(__0);
            return _open == null;
        }
        catch (Exception ex)
        {
            DialogueUiRuntime.LogOnce("scene-wheel-open", "Scene wheel unavailable; host menu shown: " + ex);
            _open = null;
            return true;
        }
    }

    private static bool IsHostSceneMenu(MultiSelectionInquiryData data)
    {
        if (data?.AffirmativeAction == null || data.InquiryElements == null || data.MaxSelectableOptionCount != 1
            || Mission.Current == null || Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
            return false;
        bool normal = false, give = false;
        foreach (var element in data.InquiryElements)
        {
            string id = element?.Identifier as string;
            normal |= id == "normal";
            give |= id == "give";
        }
        if (!normal || !give) return false;
        for (Type type = data.AffirmativeAction.Method.DeclaringType; type != null; type = type.DeclaringType)
            if (type == typeof(ShoutBehavior)) return true;
        return false;
    }

    internal static void Tick() => _open?.Tick();

    internal static void Released(WheelLayer layer)
    {
        if (ReferenceEquals(_open, layer)) _open = null;
    }

    internal static void Shutdown()
    {
        _open?.Close(invokeCancel: false);
        _open = null;
        _installed = false;
    }
}

internal sealed class WheelLayer
{
    private readonly ScreenBase _screen;
    private readonly GauntletLayer _layer;
    private readonly SceneWheelVM _vm;
    private readonly MultiSelectionInquiryData _data;
    private readonly Mission _mission;
    private readonly LiveSpeakerPortrait _portrait = new LiveSpeakerPortrait();
    private readonly SceneStyler _styler = new SceneStyler();
    private CharacterTableauWidget _tableau;
    private Widget _hit;
    private Widget _plate;
    private InquiryElement _pendingChoice;
    private bool _pendingCancel;
    private bool _closed;

    private WheelLayer(ScreenBase screen, MultiSelectionInquiryData data)
    {
        _screen = screen;
        _data = data;
        _mission = Mission.Current;
        _vm = new SceneWheelVM(data, element => _pendingChoice = element, () => _pendingCancel = true);
        _layer = new GauntletLayer("AFSceneWheel", 1100, false);
    }

    internal static WheelLayer TryOpen(MultiSelectionInquiryData data)
    {
        ScreenBase screen = ScreenManager.TopScreen;
        if (screen == null) return null;
        var wheel = new WheelLayer(screen, data);
        if (!wheel.Open()) { wheel.Close(invokeCancel: false); return null; }
        return wheel;
    }

    private bool Open()
    {
        var movie = _layer.LoadMovie("AFSceneWheel", _vm)?.Movie;
        if (movie?.RootWidget == null) return false;
        _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
        try { _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory")); } catch { }
        _screen.AddLayer(_layer);
        _layer.IsFocusLayer = true;
        ScreenManager.TrySetFocus(_layer);
        _styler.Attach(movie.RootWidget);
        _hit = movie.RootWidget.FindChild("AFBareWheelHit", true);
        _plate = movie.RootWidget.FindChild("AFWheelPlate", true);
        _tableau = movie.RootWidget.FindChild("AFWheelPortrait", true) as CharacterTableauWidget;
        Agent target = ShoutBehavior.GetScenePresentationWheelTargetForExternal();
        if (_tableau != null && target?.Character is CharacterObject character)
        {
            PortraitCamera.Register(_tableau);
            _tableau.IsVisible = _portrait.Apply(_tableau, target, character);
            // The wheel portrait is the conversation portrait at half size: same crop ratios, so
            // PortraitCamera framing still holds; only the texture offset halves and the render
            // scale doubles to keep the same pixel density.
            _tableau.PositionYOffset = PortraitFraming.TextureOffsetY * 0.5f;
            _tableau.CustomRenderScale = PortraitFraming.RenderQuality * 2f;
        }
        return true;
    }

    internal void Tick()
    {
        if (_closed) return;
        // Deferred so the choice never runs inside the Gauntlet click dispatch that produced it.
        if (_pendingChoice != null)
        {
            InquiryElement choice = _pendingChoice;
            Close(invokeCancel: false);
            try { _data.AffirmativeAction(new List<InquiryElement> { choice }); }
            catch (Exception ex) { DialogueUiRuntime.Log("Scene wheel choice failed: " + ex); }
            return;
        }
        if (_pendingCancel || !ReferenceEquals(Mission.Current, _mission) || Agent.Main == null || !Agent.Main.IsActive()
            || _layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape) || _layer.Input.IsKeyReleased(InputKey.RightMouseButton))
        {
            Close(invokeCancel: true);
            return;
        }
        // Sector hover from geometry: one rect test and one atan2 per frame while the wheel is open.
        string sector = SectorUnderMouse();
        _vm.SetHovered(sector);
        if (_vm.TakeSectorClick()) _vm.Activate(sector);
        _styler.Tick(_vm.LayoutVersion);
    }

    private string SectorUnderMouse()
    {
        if (_hit == null || !_hit.IsRecursivelyVisible()) return null;
        var mouse = Input.MousePositionPixel;
        if (Inside(_plate, mouse.x, mouse.y)) return null;
        var p = _hit.GlobalPosition; var s = _hit.Size;
        if (s.X <= 1f || s.Y <= 1f) return null;
        float hx = s.X * 0.5f, hy = s.Y * 0.5f;
        return WheelSectors.At((mouse.x - (p.X + hx)) / hx, (mouse.y - (p.Y + hy)) / hy);
    }

    private static bool Inside(Widget widget, float x, float y)
    {
        if (widget == null || !widget.IsRecursivelyVisible()) return false;
        var p = widget.GlobalPosition; var s = widget.Size;
        return x >= p.X && x <= p.X + s.X && y >= p.Y && y <= p.Y + s.Y;
    }

    internal void Close(bool invokeCancel)
    {
        if (_closed) return;
        _closed = true;
        try { _layer.IsFocusLayer = false; ScreenManager.TryLoseFocus(_layer); } catch { }
        try { _screen.RemoveLayer(_layer); } catch (Exception ex) { DialogueUiRuntime.Log("Scene wheel layer removal: " + ex.Message); }
        if (_tableau != null) PortraitCamera.Unregister(_tableau);
        _tableau = null;
        _vm.OnFinalize();
        SceneWheel.Released(this);
        if (invokeCancel)
        {
            try { _data.NegativeAction?.Invoke(new List<InquiryElement>()); }
            catch (Exception ex) { DialogueUiRuntime.Log("Scene wheel cancel failed: " + ex); }
        }
    }
}
