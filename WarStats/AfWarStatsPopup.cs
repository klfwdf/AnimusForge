using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AFWarStatsTerminal.UI;

public sealed class AfWarStatsPopup
{
    private static AfWarStatsPopup _activePopup;

    public static bool IsOpen => _activePopup != null;

    private readonly ScreenBase _screen;

    private readonly GauntletLayer _layer;

    private readonly AfWarStatsPopupVM _dataSource;

    private readonly Action _onClose;

    private bool _isClosed;

    private AfWarStatsPopup(ScreenBase screen, Action onClose)
    {
        _screen = screen;
        _onClose = onClose;
        _dataSource = new AfWarStatsPopupVM(HandleCloseRequested);
        // Native EncyclopediaBar uses layer 310. Keep this popup directly below it
        // so encyclopedia links remain visible without discarding the popup state.
        _layer = new GauntletLayer("AFWarStatsPopup", 309, false);
    }

    public static bool Show(Action onClose = null)
    {
        ScreenBase topScreen = ScreenManager.TopScreen;
        if (topScreen == null)
        {
            Debug.Print("[AFWarStatsTerminal] Popup open rejected: no top screen.");
            return false;
        }

        AfWarStatsPopup popup = null;
        try
        {
            _activePopup?.Close(silent: true);
            popup = new AfWarStatsPopup(topScreen, onClose);
            popup.Open();
            _activePopup = popup;
            Debug.Print("[AFWarStatsTerminal] Popup opened on " + topScreen.GetType().FullName + ".");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Print("[AFWarStatsTerminal] Popup open failed: " + ex);
            popup?.Close(silent: true);
            if (_activePopup != null && !ReferenceEquals(_activePopup, popup))
            {
                _activePopup.Close(silent: true);
            }

            _activePopup = null;
            return false;
        }
    }

    private void Open()
    {
        _layer.LoadMovie("AFWarStatsPopup", _dataSource);
        _layer.InputRestrictions.SetInputRestrictions(true, (InputUsageMask)7);
        try
        {
            _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
        }
        catch
        {
        }

        _screen.AddLayer(_layer);
        _layer.IsFocusLayer = true;
        ScreenManager.TrySetFocus(_layer);
    }

    private void HandleCloseRequested()
    {
        Close(silent: true);
        _onClose?.Invoke();
    }

    private void Close(bool silent)
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        try
        {
            _layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_layer);
        }
        catch
        {
        }

        try
        {
            _screen.RemoveLayer(_layer);
        }
        catch
        {
            if (!silent)
            {
                throw;
            }
        }

        _dataSource.OnFinalize();
        if (_activePopup == this)
        {
            _activePopup = null;
        }
    }
}
