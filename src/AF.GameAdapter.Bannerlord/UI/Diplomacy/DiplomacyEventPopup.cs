using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

internal sealed class DiplomacyEventPopup
{
    private static DiplomacyEventPopup _active;
    private static string _pendingEvent, _pendingImage;
    private static long _pendingGeneration;
    private static DiplomacyEventStage _pendingStage;
    private readonly ScreenBase _screen;
    private readonly GauntletLayer _layer;
    private readonly DiplomacyEventPopupVM _vm;
    private readonly Action _onAcknowledged;
    private readonly long _generation;
    private readonly GameStateManager _pauseOwner;
    private GauntletMovieIdentifier _movie;
    private DevPopupInputLease _parentLease;
    private bool _pauseRegistered, _added, _closed, _closeRequested;
    internal static bool IsOpen => _active != null && !_active._closed;
    internal static bool IsPreparing { get; private set; }

    private DiplomacyEventPopup(ScreenBase screen, DiplomacyEventPresentation data, string imageId, Action acknowledged)
    {
        _screen = screen; _onAcknowledged = acknowledged; _generation = data.Generation;
        _pauseOwner = Game.Current?.GameStateManager;
        _layer = new GauntletLayer("DiplomacyEventPopup", 4200, false);
        string chrome = DiplomacyEventSprites.Load("event-chrome");
        if (chrome.Length == 0) throw new InvalidOperationException("Event chrome is unavailable.");
        _vm = new DiplomacyEventPopupVM(data, DiplomacyEventSprites.Load(imageId), chrome, () => _closeRequested = true);
    }

    // Returns false without consuming the caller's announcement when another modal owns input.
    internal static bool TryShow(DiplomacyEventPresentation data, out string selectedImageId,
        Action onAcknowledged = null, IReadOnlyList<string> recentImageIds = null)
    {
        selectedImageId = "";
        IsPreparing = false;
        if (data == null || !SaveRuntimeGuard.IsCurrentGeneration(data.Generation) || IsOpen || ScreenManager.TopScreen == null || InformationManager.IsAnyInquiryActive()
            || CourierLetterReplyPopup.IsOpen || CourierLetterInputPopup.IsOpen || DiplomacyPresentationBridge.IsOpen)
            return false;
        DiplomacyEventPopup popup = null;
        try
        {
            if (_pendingEvent != data.EventId || _pendingGeneration != data.Generation || _pendingStage != data.Stage)
            {
                _pendingEvent = data.EventId; _pendingGeneration = data.Generation; _pendingStage = data.Stage;
                _pendingImage = DiplomacyEventIllustrations.Select(data, recentImageIds);
            }
            string id = _pendingImage;
            if (!DiplomacyEventSprites.Prepare("event-chrome") || !DiplomacyEventSprites.Prepare(id))
            { IsPreparing = true; return false; }
            if (DiplomacyEventSprites.Load("event-chrome").Length == 0) return false;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            popup = new DiplomacyEventPopup(ScreenManager.TopScreen, data, id, onAcknowledged);
            popup.Open();
            _active = popup;
            selectedImageId = id;
            Logger.Log("DiplomacyEventPopup", "Opened event=" + data.EventId + " stage=" + data.Stage + " elapsedMs=" + watch.Elapsed.TotalMilliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            _pendingEvent = null;
            return true;
        }
        catch (Exception ex)
        {
            popup?.Close(false);
            Logger.Log("DiplomacyEventPopup", "Open failed: " + ex);
            return false;
        }
    }

    private void Open()
    {
        _parentLease = new DevPopupInputLease(_screen, ScreenManager.FocusedLayer,
            () => !_screen.IsFinalized && SaveRuntimeGuard.IsCurrentGeneration(_generation));
        _movie = _layer.LoadMovie("DiplomacyEventPopup", _vm);
        _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
        _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
        _screen.AddLayer(_layer); _added = true;
        _layer.IsFocusLayer = true;
        ScreenManager.TrySetFocus(_layer);
        if (_pauseOwner != null) { _pauseOwner.RegisterActiveStateDisableRequest(this); _pauseRegistered = true; }
    }

    internal static void CloseForCampaignEnd()
    {
        _active?.Close(false);
        _pendingEvent = null; _pendingImage = null;
        IsPreparing = false;
        DiplomacyEventSprites.Clear();
    }

    internal static void OnApplicationTick()
    {
        DiplomacyEventPopup popup = _active;
        if (popup == null) { DiplomacyEventPreview.ProcessPending(); return; }
        if (!SaveRuntimeGuard.IsCurrentGeneration(popup._generation) || !ReferenceEquals(ScreenManager.TopScreen, popup._screen)
            || popup._screen.IsFinalized || popup._layer.IsFinalized)
        { popup.Close(false); return; }
        if (ScreenManager.FocusedLayer != popup._layer) return;
        // Defer Escape by one tick so its release cannot also close the restored terminal/map UI.
        if (popup._closeRequested) { popup.Close(true); return; }
        if (popup._layer.Input.IsHotKeyReleased("Exit") || popup._layer.Input.IsKeyReleased(InputKey.Escape))
            popup._closeRequested = true;
    }

    private void Close(bool acknowledge)
    {
        if (_closed) return;
        _closed = true;
        if (ReferenceEquals(_active, this)) _active = null;
        Cleanup(() => { _layer.InputRestrictions.ResetInputRestrictions(); _layer.IsFocusLayer = false; ScreenManager.TryLoseFocus(_layer); });
        Cleanup(() => { if (_movie != null) _layer.ReleaseMovie(_movie); });
        Cleanup(() => { if (_added && !_screen.IsFinalized && _screen.HasLayer(_layer)) _screen.RemoveLayer(_layer); });
        Cleanup(() => { if (_pauseRegistered) _pauseOwner.UnregisterActiveStateDisableRequest(this); });
        Cleanup(() => _vm.OnFinalize());
        Cleanup(() => _parentLease?.Dispose());
        if (acknowledge && SaveRuntimeGuard.IsCurrentGeneration(_generation)) Cleanup(() => _onAcknowledged?.Invoke());
    }

    private static void Cleanup(Action action)
    {
        try { action(); }
        catch (Exception ex) { Logger.Log("DiplomacyEventPopup", "Cleanup/callback failed: " + ex.Message); }
    }
}
