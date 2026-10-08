using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge.DialogueUI.Scene;

// RollOpen is the existing VM's local docket state. Only its visual clip is
// animated; fixed-size artwork/rows stay anchored to the screen's top edge.
public sealed class AFSceneAudienceDocketWidget : Widget
{
    private const float RollSeconds = 0.25f;
    private bool _rollOpen = true, _initialized, _animating;
    private float _fullHeight = 640f, _height = 640f, _startHeight, _elapsed;

    public AFSceneAudienceDocketWidget(UIContext context) : base(context)
    {
        ClipContents = true;
        HeightSizePolicy = SizePolicy.Fixed;
    }

    public bool RollOpen
    {
        get => _rollOpen;
        set
        {
            if (_rollOpen == value) return;
            _rollOpen = value;
            if (!_initialized) return; // First binding is a snapshot, not an entrance animation.
            _startHeight = _height;
            _elapsed = 0f;
            _animating = true;
            IsVisible = true; // Retain the drawing until the closing clip reaches zero.
            DoNotPassEventsToChildren = !value;
        }
    }

    public float FullHeight
    {
        get => _fullHeight;
        set
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            value = Math.Max(0f, value);
            if (_fullHeight == value) return;
            float ratio = _fullHeight > 0f ? value / _fullHeight : 0f;
            _fullHeight = value;
            if (!_initialized) return;
            _height *= ratio;
            _startHeight *= ratio;
            if (!_animating) _height = _rollOpen ? value : 0f;
            ApplyHeight();
        }
    }

    protected override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        if (!_initialized)
        {
            _initialized = true;
            _height = _rollOpen ? _fullHeight : 0f;
            ApplyHeight();
            IsVisible = _rollOpen;
            DoNotPassEventsToChildren = !_rollOpen;
            return;
        }
        if (!_animating || dt <= 0f || float.IsNaN(dt)) return;
        _elapsed = Math.Min(RollSeconds, _elapsed + dt);
        float progress = _elapsed / RollSeconds;
        float eased = progress * progress * (3f - 2f * progress);
        _height = _startHeight + ((_rollOpen ? _fullHeight : 0f) - _startHeight) * eased;
        ApplyHeight();
        if (_elapsed < RollSeconds) return;
        _animating = false;
        IsVisible = _rollOpen;
    }

    private void ApplyHeight()
    {
        if (Math.Abs(SuggestedHeight - _height) > 0.001f) SuggestedHeight = _height;
    }
}
