using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

// UI transition only: suspend a named UI parent; scene parents release input but keep rendering.
public sealed class DevPopupInputLease : IDisposable
{
    private readonly ScreenBase _screen;
    private readonly ScreenLayer _layer;
    private readonly Func<bool> _isOwnerAlive;
    private readonly bool _suspendParent;
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
        _suspendParent = layer is GauntletLayer;
        try
        {
            layer.InputRestrictions.ResetInputRestrictions();
            layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(layer);
            // Map SceneViews can keep rendering while inactive, moving above every active UI layer.
            if (_wasActive && _suspendParent) ScreenManager.SetSuspendLayer(layer, isSuspended: true);
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
        if (_suspendParent && !_externallyChanged && _wasActive && !_layer.IsActive && ReferenceEquals(ScreenManager.TopScreen, _screen))
            ScreenManager.SetSuspendLayer(_layer, isSuspended: false);
        if (!_externallyChanged) _layer.LastActiveState = _lastActiveState;
        // A conversation or another panel may have taken focus while the popup
        // was closing. Restore the parent input without taking that focus back.
        if (!_externallyChanged && _wasFocused && _layer.IsActive && ReferenceEquals(ScreenManager.TopScreen, _screen)
            && (ScreenManager.FocusedLayer == null || ReferenceEquals(ScreenManager.FocusedLayer, _layer)))
            ScreenManager.TrySetFocus(_layer);
    }
}
