using System;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

// UI transition only: suspend a named parent, never scan or reprioritize all game layers.
public sealed class DevPopupInputLease : IDisposable
{
    private readonly ScreenBase _screen;
    private readonly ScreenLayer _layer;
    private readonly Func<bool> _isOwnerAlive;
    private readonly InputUsageMask _mask;
    private readonly bool _wasActive, _lastActiveState, _wasFocusLayer, _wasFocused, _mouseVisible;
    private bool _disposed, _acquired, _externallyChanged;

    public DevPopupInputLease(ScreenBase screen, ScreenLayer layer, Func<bool> isOwnerAlive)
    {
        _screen = screen;
        _layer = layer;
        _isOwnerAlive = isOwnerAlive;
        if (layer == null || layer.IsFinalized || !layer.IsActive || screen == null || !screen.HasLayer(layer)) return;
        _mask = layer.InputUsageMask;
        _mouseVisible = layer.InputRestrictions.MouseVisibility;
        _acquired = true;
        _wasActive = layer.IsActive;
        _lastActiveState = layer.LastActiveState;
        _wasFocusLayer = layer.IsFocusLayer;
        _wasFocused = ReferenceEquals(ScreenManager.FocusedLayer, layer);
        try
        {
            layer.InputRestrictions.ResetInputRestrictions();
            layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(layer);
            if (_wasActive) ScreenManager.SetSuspendLayer(layer, isSuspended: true);
            ScreenLayer.OnLayerActiveStateChanged += OnParentActiveStateChanged;
        }
        catch { Dispose(); throw; }
    }

    private void OnParentActiveStateChanged(ScreenLayer layer)
    { if (ReferenceEquals(layer, _layer)) _externallyChanged = true; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ScreenLayer.OnLayerActiveStateChanged -= OnParentActiveStateChanged;
        if (!_acquired || _layer == null || _layer.IsFinalized || _screen == null || _screen.IsFinalized
            || !_screen.HasLayer(_layer) || _isOwnerAlive?.Invoke() == false) return;
        // Even after a screen push/pop, the old layer remains in the screen and keeps its
        // InputRestrictions. Restore those scalars, but never reactivate or refocus a parent
        // whose screen lifecycle changed outside this lease.
        _layer.InputRestrictions.SetInputRestrictions(_mouseVisible, _mask);
        _layer.IsFocusLayer = _wasFocusLayer;
        if (!_externallyChanged && _wasActive && !_layer.IsActive && ReferenceEquals(ScreenManager.TopScreen, _screen))
            ScreenManager.SetSuspendLayer(_layer, isSuspended: false);
        if (!_externallyChanged) _layer.LastActiveState = _lastActiveState;
        if (!_externallyChanged && _wasFocused && _layer.IsActive && ReferenceEquals(ScreenManager.TopScreen, _screen))
            ScreenManager.TrySetFocus(_layer);
    }
}
