using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.DialogueUI.Scene;

// Owns the Gauntlet layer for the host's persistent scene session. The mission keeps running:
// expanded = this layer holds keyboard/mouse (player stands still), collapsed = full control returns.
// Per frame: two static reads from the host. Rebuilds only on a host version change.
internal static class SceneSessionPanel
{
    private static SessionLayer _layer;
    private static bool _failed;

    // Consulted by the host hook: a session is only offered while this module can show it.
    internal static bool IsAvailable => !_failed && DialogueUiRuntime.Enabled && DialogueUiOptions.UsesSceneSession;

    // True while the expanded panel (or the wheel) owns keyboard input.
    internal static bool IsCapturingInput => SceneWheel.IsOpen || (_layer != null && _layer.IsInteractive);

    // Vanilla mission hotkeys (encyclopedia N, inventory I, party P, quests J, clan L, kingdom K,
    // character C, banner B) are read in MissionSingleplayerViewHandler.OnMissionScreenTick through
    // SceneLayer.Input. Typing into the panel must never open them, so that tick is skipped while
    // the panel owns input (its base MissionView.OnMissionScreenTick is empty on 1.3 and 1.4).
    internal static void Install(Harmony harmony)
    {
        Type handler = AccessTools.TypeByName("SandBox.View.Missions.MissionSingleplayerViewHandler");
        var tick = handler == null ? null : AccessTools.Method(handler, "OnMissionScreenTick", new[] { typeof(float) });
        if (tick == null)
        {
            DialogueUiRuntime.Log("Mission hotkey guard unavailable: MissionSingleplayerViewHandler.OnMissionScreenTick not found.");
            return;
        }
        harmony.Patch(tick, prefix: new HarmonyMethod(typeof(SceneSessionPanel), nameof(MissionHotkeysPrefix)));
    }

    private static bool MissionHotkeysPrefix() => !IsCapturingInput;

    internal static void Tick(float dt)
    {
        bool active = ShoutBehavior.IsScenePresentationActiveForExternal;
        if (_layer != null && (!active || !ReferenceEquals(_layer.Mission, Mission.Current)))
        {
            _layer.Close();
            _layer = null;
        }
        if (active && _layer == null && !_failed)
        {
            _layer = SessionLayer.TryOpen(DialogueUiOptions.PanelStyle);
            if (_layer == null)
            {
                _failed = true;
                DialogueUiRuntime.Log("Scene session panel unavailable; ending session and using the host input.");
                ShoutBehavior.EndScenePresentationForExternal("ui_unavailable");
                return;
            }
        }
        _layer?.Tick(dt);
    }

    internal static void Shutdown()
    {
        _layer?.Close();
        _layer = null;
        _failed = false;
    }
}

internal sealed class SessionLayer
{
    private const int BusyPollFrames = 10;
    internal readonly Mission Mission;
    private readonly ScreenBase _screen;
    private readonly GauntletLayer _layer;
    private readonly SceneSessionVM _vm = new SceneSessionVM();
    private readonly SceneStyler _styler = new SceneStyler();
    private Widget _root;
    private int _version = int.MinValue;
    private int _frames;
    private bool? _interactive;
    private bool _closed;

    private SessionLayer(ScreenBase screen)
    {
        _screen = screen;
        Mission = Mission.Current;
        _layer = new GauntletLayer("AFSceneSession", 900, false);
    }

    internal static SessionLayer TryOpen(ShoutPanelStyle style)
    {
        ScreenBase screen = ScreenManager.TopScreen;
        if (screen == null || Mission.Current == null || !DialogueUiSprites.EnsureSceneLoaded()) return null;
        var session = new SessionLayer(screen);
        try
        {
            var movie = session._layer.LoadMovie(style == ShoutPanelStyle.SideFolio ? "AFSceneSessionFolio" : "AFSceneSessionScroll", session._vm)?.Movie;
            if (movie?.RootWidget == null) { session.Close(); return null; }
            try { session._layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory")); } catch { }
            screen.AddLayer(session._layer);
            session._root = movie.RootWidget;
            session._styler.Attach(movie.RootWidget);
            return session;
        }
        catch (Exception ex)
        {
            DialogueUiRuntime.Log("Scene session panel failed to load: " + ex);
            session.Close();
            return null;
        }
    }

    internal void Tick(float dt)
    {
        if (_closed) return;
        int version = ShoutBehavior.ScenePresentationVersionForExternal;
        if (version != _version)
        {
            _version = version;
            _vm.Refresh();
        }
        if (++_frames >= BusyPollFrames)
        {
            _frames = 0;
            _vm.SetBusy(ShoutBehavior.IsScenePresentationBusyForExternal, ShoutBehavior.CanInterruptScenePresentationForExternal);
        }
        // Step aside (hidden, no input) while the host's own popups, the wheel, another screen, or a
        // vanilla focus window opened on this same screen (encyclopedia, Esc menu, barter...) is up.
        // Those vanilla layers sit below our order (encyclopedia 310 < 900), so staying focused would
        // draw over them and keep their keys; releasing focus lets ScreenManager hand it to them.
        bool foreign = !ReferenceEquals(ScreenManager.TopScreen, _screen) || ShoutTextInputPopup.IsOpen || SceneWheel.IsOpen
            || InformationManager.IsAnyInquiryActive() || HasForeignFocusLayer();
        _vm.SetHidden(foreign);
        bool interactive = !foreign && _vm.IsExpanded;
        if (_interactive != interactive) ApplyInput(interactive);
        // Something (e.g. the scene layer) took focus back without opening a window: take it again,
        // otherwise typed letters also reach the mission hotkeys. One reference compare per frame.
        else if (interactive && !ReferenceEquals(ScreenManager.FocusedLayer, _layer)) ScreenManager.TrySetFocus(_layer);
        if (interactive && (_layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape)))
        {
            if (!_vm.CloseTradeIfOpen()) ShoutBehavior.EndScenePresentationForExternal("leave_esc");
        }
        _vm.Trade.Tick(dt);
        _vm.RefreshIllustration();
        Action pending = _vm.TakePending();
        if (pending != null)
        {
            try { pending(); }
            catch (Exception ex) { DialogueUiRuntime.Log("Scene session command failed: " + ex.Message); }
        }
        _styler.Tick(_vm.StyleVersion);
    }

    internal bool IsInteractive => !_closed && _interactive == true;

    // Another active focus window on our screen (not ours, not the scene layer). A dozen layers at most;
    // the result is refreshed every few frames so the per-frame cost is one bool read.
    private const int ForeignScanFrames = 3;
    private int _foreignScanFrames;
    private bool _foreignFocus;
    private string _foreignLogged;

    private bool HasForeignFocusLayer()
    {
        if (++_foreignScanFrames < ForeignScanFrames) return _foreignFocus;
        _foreignScanFrames = 0;
        ScreenLayer found = null;
        IReadOnlyList<ScreenLayer> layers = _screen.Layers;
        for (int i = 0; i < layers.Count; i++)
        {
            ScreenLayer layer = layers[i];
            if (layer == null || ReferenceEquals(layer, _layer) || !layer.IsActive || layer is SceneLayer) continue;
            if (layer.IsFocusLayer || ReferenceEquals(ScreenManager.FocusedLayer, layer)) { found = layer; break; }
        }
        _foreignFocus = found != null;
        if (found != null && found.Name != _foreignLogged)
        {
            _foreignLogged = found.Name;
            DialogueUiRuntime.Log("Scene session panel stepped aside for layer: " + found.Name);
        }
        else if (found == null) _foreignLogged = null;
        return _foreignFocus;
    }

    // Transitions only (collapse/expand, popup open/close); never re-applied every frame.
    private void ApplyInput(bool interactive)
    {
        _interactive = interactive;
        if (interactive)
        {
            _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _layer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_layer);
            Widget editor = _root?.FindChild("AFSessionInput", true);
            if (editor != null && _root.EventManager != null) _root.EventManager.FocusedWidget = editor;
        }
        else
        {
            if (_root?.EventManager != null) _root.EventManager.FocusedWidget = null;
            _layer.InputRestrictions.ResetInputRestrictions();
            _layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_layer);
        }
    }

    internal void Close()
    {
        if (_closed) return;
        _closed = true;
        try
        {
            if (_root?.EventManager != null) _root.EventManager.FocusedWidget = null;
            _layer.InputRestrictions.ResetInputRestrictions();
            _layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_layer);
        }
        catch { }
        try { _screen.RemoveLayer(_layer); } catch (Exception ex) { DialogueUiRuntime.Log("Scene session layer removal: " + ex.Message); }
        _vm.OnFinalize();
        _root = null;
    }
}
