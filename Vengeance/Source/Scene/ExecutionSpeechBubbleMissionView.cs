using System;
using System.Collections.Generic;
using RichExecutions.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace RichExecutions.Scene;

/// <summary>
/// Non-interactive overlay that renders the pre-execution address as bare
/// outlined text pinned beside each speaker's head.
///
/// Ownership split:
///   * <see cref="ExecutionSpeechDirector"/> decides what is said and when.
///   * This view owns the GauntletLayer, the per-label typing cursors, and the
///     world-to-screen projection.
///
/// Each short line finishes revealing and remains readable before the next
/// cue. An outgoing label can briefly fade beside the next speaker's label.
///
/// The layer is deliberately never focused and never takes input, so the
/// player can still press F, talk to the executioner, or leave the mission
/// while a line is still streaming.
/// </summary>
public sealed class ExecutionSpeechBubbleMissionView : MissionView
{
    // Head offset above the speaker's eye position for the label anchor.
    // 0.15m matches AnimusForge's primary eye anchor, so the text sits just
    // above the head top rather than floating over the forehead.
    private const float BubbleAnchorHeightOffset = 0.15f;
    // Extra upward shift so the label clears the head rather than sitting on
    // it. Kept small: the label is compact now, so a large lift would push it
    // away from the speaker.
    private const float BubbleVerticalLift = 6f;
    // Off-screen parking coordinate when the speaker cannot be projected.
    private const float OffScreenCoordinate = -2000f;
    private const float FadeInSeconds = 0.25f;

    // Diagnostics throttles. The address is short-lived, so these only need to
    // keep the log readable rather than fully silent.
    private const int MaximumReportedProjectionFailures = 3;

    /// <summary>
    /// One streamed label. The director treats it as an opaque handle; all
    /// state that matters for rendering lives here.
    /// </summary>
    internal sealed class Label
    {
        internal Label(ExecutionSpeechBubbleItemVM item, Agent speaker, string speakerName, string fullText)
        {
            Item = item;
            Speaker = speaker;
            SpeakerName = speakerName;
            FullText = fullText;
            Cursor = new ExecutionSpeechRevealCursor(fullText);
        }

        internal ExecutionSpeechBubbleItemVM Item { get; }
        internal Agent Speaker { get; }
        internal string SpeakerName { get; }
        internal string FullText { get; }
        internal ExecutionSpeechRevealCursor Cursor { get; }

        /// <summary>True once the last character has been revealed and its hold elapsed.</summary>
        internal bool IsComplete => Cursor.IsComplete;

        /// <summary>True while the label is fading out and no longer blocking the ceremony.</summary>
        internal bool IsRetiring { get; set; }

        internal float Alpha { get; set; }
        internal bool FirstRevealLogged { get; set; }
        internal bool FirstProjectionLogged { get; set; }
        internal bool ProjectionFailureLogged { get; set; }
        internal bool WasProjected { get; set; }
    }

    private ExecutionSpeechBubbleVM? _viewModel;
    private GauntletLayer? _layer;
    private bool _isLayerAdded;
    private bool _isLoadFailed;
    private bool _layerCreationLogged;
    private bool _isFinalized;

    private readonly List<Label> _labels = new();
    private readonly List<Label> _retiredLabels = new();
    private float _globalAlpha;

    private int _reportedProjectionFailures;

    /// <summary>True when the overlay exists and can accept a new label.</summary>
    internal bool IsBubbleReady() => TryCreateLayer();

    internal bool HasFailed => _isLoadFailed || _isFinalized;

    public override void OnMissionScreenTick(float dt)
    {
        base.OnMissionScreenTick(dt);
        // Default views exist in every mission. Only an execution's readiness
        // check or first line may allocate the layer; idle missions do no work.
        if (!_isLayerAdded || _isLoadFailed || _isFinalized)
        {
            return;
        }

        try
        {
            var step = float.IsNaN(dt) || float.IsInfinity(dt) ? 0f : MathF.Max(0f, dt);
            TickGlobalFade(step);
            TickLabels(step);
            EnsureLayerIsPassive();
        }
        catch (Exception exception)
        {
            _isLoadFailed = true;
            RexLog.Error("The speech overlay failed; the address will stop without blocking execution.", exception);
            AbortLines();
        }
    }

    public override void OnMissionScreenFinalize()
    {
        _isFinalized = true;
        AbortLines();
        ReleaseResources();
        base.OnMissionScreenFinalize();
    }

    public override void OnMissionScreenDeactivate()
    {
        base.OnMissionScreenDeactivate();
        // A deactivated mission screen would keep the labels frozen on top of
        // another screen; hide them without destroying the layer.
        if (_viewModel is not null)
        {
            _viewModel.Alpha = 0f;
            _viewModel.IsVisible = false;
        }
    }

    public override void OnMissionScreenActivate()
    {
        base.OnMissionScreenActivate();
        if (_viewModel is not null)
        {
            _viewModel.Alpha = _globalAlpha;
            _viewModel.IsVisible = _labels.Count > 0 || _retiredLabels.Count > 0;
        }
    }

    /// <summary>
    /// Starts a new streamed label at its own speaker's head. Earlier labels
    /// are left untouched: the director retires them when their hold expires,
    /// which is what produces the overlapping back-and-forth look.
    /// </summary>
    internal bool TryStartLine(Agent speaker, string speakerName, string speakerColor, string text)
    {
        try
        {
            return TryStartLineCore(speaker, speakerName, speakerColor, text);
        }
        catch (Exception exception)
        {
            _isLoadFailed = true;
            RexLog.Error("The speech label failed to display; the address will be skipped.", exception);
            AbortLines();
            return false;
        }
    }

    private bool TryStartLineCore(Agent speaker, string speakerName, string speakerColor, string text)
    {
        if (speaker is null || !speaker.IsActive())
        {
            RexLog.Warning("A ceremony speech line was refused because its speaker is not active.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            RexLog.Warning("A ceremony speech line was refused because it resolved to empty text.");
            return false;
        }

        if (!TryCreateLayer() || _viewModel is null)
        {
            RexLog.Warning("A ceremony speech line was refused because the overlay layer is unavailable.");
            return false;
        }

        // Consecutive sentences by one speaker share an anchor. Do not leave
        // the old sentence fading underneath the new text at that same head.
        for (var i = _retiredLabels.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(_retiredLabels[i].Speaker, speaker))
            {
                continue;
            }

            _viewModel.RemoveBubble(_retiredLabels[i].Item);
            _retiredLabels.RemoveAt(i);
        }

        var item = _viewModel.CreateBubble(speakerName, speakerColor);
        if (item is null)
        {
            RexLog.Warning("A ceremony speech line was refused because its label could not be created.");
            return false;
        }

        var label = new Label(item, speaker, speakerName, text);
        // Size the wrap width and the lift from the whole line up front so the
        // text does not rewiden as it streams in.
        item.SetLine(text);
        // Start fully opaque. A fade-in that began at zero made the first frames
        // invisible and, if the mission ended early, could leave the impression
        // that nothing had been shown at all.
        label.Alpha = 1f;
        item.Alpha = 1f;
        item.IsVisible = true;
        ProjectLabel(label);

        _labels.Add(label);
        LerpGlobalAlphaTo(1f);
        _viewModel.IsVisible = true;

        RexLog.Info(
            $"Ceremony speech label started for '{speakerName}' " +
            $"(speaker={speaker.Index}, chars={text.Length}, wrap={item.BubbleWidth:F0}, " +
            $"height={item.EstimatedHeight:F0}, live={_labels.Count}, " +
            $"at=({item.ScreenX:F0}, {item.ScreenY:F0})).");
        return true;
    }

    /// <summary>True when the newest label finished revealing and its hold elapsed.</summary>
    internal bool IsLineComplete
    {
        get
        {
            for (var i = _labels.Count - 1; i >= 0; i--)
            {
                if (!_labels[i].IsRetiring)
                {
                    return _labels[i].IsComplete;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Retires the newest label: it begins fading out at its own head and stops
    /// blocking the ceremony immediately.
    /// </summary>
    internal void ClearLine()
    {
        for (var i = _labels.Count - 1; i >= 0; i--)
        {
            var label = _labels[i];
            if (label.IsRetiring)
            {
                continue;
            }

            Retire(label, i);
            return;
        }
    }

    /// <summary>Immediately removes all text when execution starts, is cancelled, or exits.</summary>
    internal void AbortLines() => ClearAllLabels();

    /// <summary>Discards every label, live or retiring, and hides the overlay.</summary>
    private void ClearAllLabels()
    {
        _labels.Clear();
        _retiredLabels.Clear();
        _globalAlpha = 0f;
        try
        {
            _viewModel?.ClearBubbles();
        }
        catch (Exception exception)
        {
            // UI binding callbacks must not turn an execution/exit cleanup
            // into an exception that escapes to the mission's state machine.
            _isLoadFailed = true;
            RexLog.Warning($"Could not clear a failed speech binding: {exception.Message}");
        }
    }

    private void TickGlobalFade(float step)
    {
        if (_viewModel is null)
        {
            return;
        }

        var target = _labels.Count > 0 || _retiredLabels.Count > 0 ? 1f : 0f;
        if (MathF.Abs(_globalAlpha - target) > 0.001f)
        {
            var rate = step / (target > _globalAlpha ? FadeInSeconds : ExecutionSpeechTiming.FadeSeconds);
            _globalAlpha = MoveTowards(_globalAlpha, target, rate);
        }

        _viewModel.Alpha = _globalAlpha;
    }

    private void LerpGlobalAlphaTo(float target)
    {
        // Only the global overlay alpha is lerped; per-label alpha starts at 1
        // so a line is never invisible on its first frame.
        if (_viewModel is null)
        {
            return;
        }

        if (target > _globalAlpha)
        {
            _globalAlpha = 1f;
        }

        _viewModel.Alpha = _globalAlpha;
    }

    private void TickLabels(float step)
    {
        // Live labels: stream characters, then hold.
        for (var i = _labels.Count - 1; i >= 0; i--)
        {
            var label = _labels[i];
            if (label.IsRetiring)
            {
                continue;
            }

            if (!label.Speaker.IsActive())
            {
                RexLog.Info(
                    $"The ceremony speech speaker '{label.SpeakerName}' left the scene; " +
                    "retiring the label.");
                Retire(label, i);
                continue;
            }

            // The clock advances even when projection parks the label off screen.
            label.Cursor.Advance(step);
            var total = label.FullText.Length;
            var revealed = label.Cursor.RevealedLength;
            label.Item.Text = BuildRevealedText(label.FullText, revealed);
            label.Item.RefreshVisibleHeight();
            ProjectLabel(label);

            if (!label.FirstRevealLogged && revealed > 0)
            {
                label.FirstRevealLogged = true;
                RexLog.Info(
                    $"Ceremony speech label is revealing characters " +
                    $"(speaker={label.Speaker.Index}, revealed={revealed}/{total}).");
            }
        }

        // Retiring labels: sweep them off their own head, then drop them.
        for (var i = _retiredLabels.Count - 1; i >= 0; i--)
        {
            var label = _retiredLabels[i];
            ProjectLabel(label);
            label.Alpha = MoveTowards(label.Alpha, 0f, step / ExecutionSpeechTiming.FadeSeconds);
            label.Item.Alpha = label.Alpha;
            if (label.Alpha > 0.001f)
            {
                continue;
            }

            _retiredLabels.RemoveAt(i);
            _viewModel?.RemoveBubble(label.Item);
        }
    }

    private void Retire(Label label, int liveIndex)
    {
        if (label.IsRetiring)
        {
            return;
        }

        if (liveIndex >= 0 && liveIndex < _labels.Count && ReferenceEquals(_labels[liveIndex], label))
        {
            _labels.RemoveAt(liveIndex);
        }
        else
        {
            _labels.Remove(label);
        }

        label.IsRetiring = true;
        _retiredLabels.Add(label);
    }

    private bool TryCreateLayer()
    {
        if (_isLoadFailed || _isFinalized)
        {
            return false;
        }

        if (_isLayerAdded)
        {
            return true;
        }

        if (MissionScreen is null)
        {
            return false;
        }

        try
        {
            _viewModel = new ExecutionSpeechBubbleVM();
            // Priority above the ordinary mission HUD but below menus and
            // conversations, so native UI still draws on top of the labels.
            _layer = new GauntletLayer("RichExecutionSpeechBubble", 305, false);
            var movie = _layer.LoadMovie("rex_speech_bubble", _viewModel);
            if (movie is null || movie.Movie is null)
            {
                throw new InvalidOperationException(
                    "The ceremony speech-bubble movie could not be loaded.");
            }

            MissionScreen.AddLayer(_layer);
            _isLayerAdded = true;
            EnsureLayerIsPassive();
            if (!_layerCreationLogged)
            {
                _layerCreationLogged = true;
                RexLog.Info(
                    "Ceremony speech-label overlay layer created " +
                    "(non-interactive, movie='rex_speech_bubble').");
            }

            return true;
        }
        catch (Exception exception)
        {
            _isLoadFailed = true;
            RexLog.Error(
                "The ceremony speech-label overlay could not be created; the address will be skipped.",
                exception);
            ReleaseResources();
            return false;
        }
    }

    /// <summary>
    /// Keeps the overlay from ever taking focus or swallowing input. Called
    /// every tick because conversations and other layers can steal or push
    /// focus while a line is streaming.
    /// </summary>
    private void EnsureLayerIsPassive()
    {
        if (_layer is null)
        {
            return;
        }

        if (_layer.IsFocusLayer)
        {
            _layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_layer);
        }

        // Only clear this overlay's own restrictions. Resetting the shared
        // input state here also clears a scene-shout popup that opened above it,
        // which makes that popup close on the next tick.
        _layer.InputRestrictions?.SetInputRestrictions(false, InputUsageMask.Invalid);
    }

    /// <summary>
    /// Returns the prefix of <paramref name="text"/> that the cursor has
    /// reached. The reveal cursor supplies complete text-element boundaries.
    /// </summary>
    internal static string BuildRevealedText(string text, int revealedCharacters)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (revealedCharacters >= text.Length)
        {
            return text;
        }

        if (revealedCharacters <= 0)
        {
            return string.Empty;
        }

        return text.Substring(0, revealedCharacters);
    }

    private void ProjectLabel(Label label)
    {
        var item = label.Item;
        var speaker = label.Speaker;
        if (speaker is null || !speaker.IsActive())
        {
            item.ScreenX = OffScreenCoordinate;
            item.ScreenY = OffScreenCoordinate;
            return;
        }

        if (!TryProjectToScreen(label, speaker, out var screenX, out var screenY))
        {
            // The label is anchored to a character, never to the screen. When
            // the character cannot be projected (behind the camera, missing
            // camera, teardown) the label is parked off-screen rather than
            // falling back to a fixed screen position.
            item.ScreenX = OffScreenCoordinate;
            item.ScreenY = OffScreenCoordinate;
            return;
        }

        label.WasProjected = true;
        // Centre the label horizontally on the speaker and lift it by its own
        // height plus a small gap, so the text sits beside/above the head
        // instead of over the face.
        item.ScreenX = screenX - item.BubbleWidth * 0.5f;
        item.ScreenY = screenY - item.EstimatedHeight - BubbleVerticalLift;
    }

    /// <summary>
    /// Projects the speaker anchor to UI-space pixels.
    ///
    /// The engine's <see cref="SceneView.WorldPointToScreenPoint"/> returns
    /// normalised (0..1) screen coordinates, which is exactly the space a
    /// GauntletLayer's PositionXOffset/PositionYOffset are expressed in once
    /// scaled by the layer's UI canvas size.
    /// </summary>
    private bool TryProjectToScreen(Label label, Agent speaker, out float screenX, out float screenY)
    {
        screenX = OffScreenCoordinate;
        screenY = OffScreenCoordinate;

        var missionScreen = MissionScreen;
        if (missionScreen is null)
        {
            LogProjectionFailureOnce(label, "the mission screen is unavailable");
            return false;
        }

        SceneView? sceneView = null;
        try
        {
            sceneView = missionScreen.SceneLayer?.SceneView;
        }
        catch (Exception exception)
        {
            LogProjectionFailureOnce(label, $"the scene view could not be read: {exception.Message}");
            return false;
        }

        if (sceneView is null)
        {
            LogProjectionFailureOnce(label, "the mission screen has no scene view to project through");
            return false;
        }

        Vec3 anchor;
        try
        {
            anchor = speaker.GetEyeGlobalPosition();
            anchor.z += BubbleAnchorHeightOffset;
        }
        catch (Exception exception)
        {
            LogProjectionFailureOnce(label, $"the speaker anchor could not be sampled: {exception.Message}");
            return false;
        }

        // Behind-camera rejection. A dot product below zero means the anchor is
        // behind the camera plane, where the projection becomes meaningless.
        var camera = missionScreen.CombatCamera;
        if (camera is not null)
        {
            try
            {
                var toAnchor = anchor - camera.Position;
                if (Vec3.DotProduct(camera.Direction, toAnchor) < 0.1f)
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                LogProjectionFailureOnce(label, $"the camera facing test threw: {exception.Message}");
                return false;
            }
        }

        Vec2 normalised;
        try
        {
            normalised = sceneView.WorldPointToScreenPoint(anchor);
        }
        catch (Exception exception)
        {
            LogProjectionFailureOnce(label, $"the world-to-screen projection threw: {exception.Message}");
            return false;
        }

        if (float.IsNaN(normalised.x) || float.IsNaN(normalised.y) ||
            float.IsInfinity(normalised.x) || float.IsInfinity(normalised.y))
        {
            LogProjectionFailureOnce(
                label, $"the anchor produced a non-finite projection ({normalised.x}, {normalised.y})");
            return false;
        }

        // A little outside the viewport is tolerated (the label follows the
        // head as it moves), but a wildly off-screen value is not usable.
        if (normalised.x < -0.25f || normalised.x > 1.25f ||
            normalised.y < -0.25f || normalised.y > 1.25f)
        {
            LogProjectionFailureOnce(
                label, $"the anchor projected outside the viewport ({normalised.x:F3}, {normalised.y:F3})");
            return false;
        }

        GetUiCanvasSize(out var canvasWidth, out var canvasHeight);
        if (canvasWidth <= 2f || canvasHeight <= 2f)
        {
            LogProjectionFailureOnce(
                label, $"the UI canvas size is unusable ({canvasWidth:F0}x{canvasHeight:F0})");
            return false;
        }

        screenX = normalised.x * canvasWidth;
        screenY = normalised.y * canvasHeight;

        if (!label.FirstProjectionLogged)
        {
            label.FirstProjectionLogged = true;
            RexLog.Info(
                $"Ceremony speech-label anchor projected to ({screenX:F0}, {screenY:F0}) " +
                $"(normalised=({normalised.x:F3}, {normalised.y:F3}), " +
                $"canvas={canvasWidth:F0}x{canvasHeight:F0}, " +
                $"speaker={speaker.Index}, lift={label.Item.EstimatedHeight:F0}+{BubbleVerticalLift:F0}).");
        }

        return true;
    }

    /// <summary>
    /// Size of the layer's Gauntlet UI canvas in the same pixel space that
    /// PositionXOffset/PositionYOffset use. Falls back to the raw resolution
    /// when the layer is not ready yet.
    /// </summary>
    private void GetUiCanvasSize(out float width, out float height)
    {
        width = Screen.RealScreenResolutionWidth;
        height = Screen.RealScreenResolutionHeight;
        try
        {
            var layer = _layer;
            if (layer?.UIContext is null || layer is not ScreenLayer screenLayer)
            {
                return;
            }

            var inverseScale = MathF.Max(0.0001f, layer.UIContext.CustomInverseScale);
            var usableX = MathF.Max(0.0001f, screenLayer.UsableArea.x);
            var usableY = MathF.Max(0.0001f, screenLayer.UsableArea.y);
            width = Screen.RealScreenResolutionWidth * usableX * inverseScale;
            height = Screen.RealScreenResolutionHeight * usableY * inverseScale;
        }
        catch
        {
            // Diagnostics/projection must never throw out of a mission tick.
        }

        if (width <= 2f)
        {
            width = Screen.RealScreenResolutionWidth;
        }

        if (height <= 2f)
        {
            height = Screen.RealScreenResolutionHeight;
        }
    }

    private void LogProjectionFailureOnce(Label label, string detail)
    {
        if (label.ProjectionFailureLogged)
        {
            return;
        }

        label.ProjectionFailureLogged = true;
        if (_reportedProjectionFailures >= MaximumReportedProjectionFailures)
        {
            // Keep the log readable: several speakers can fail at once (for
            // example when the whole scene is behind the camera).
            return;
        }

        _reportedProjectionFailures++;
        RexLog.Warning(
            $"The ceremony speech label for '{label.SpeakerName}' has no head anchor: {detail}.");
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (maxDelta <= 0f)
        {
            return current;
        }

        var difference = target - current;
        if (MathF.Abs(difference) <= maxDelta)
        {
            return target;
        }

        return current + MathF.Sign(difference) * maxDelta;
    }

    private void ReleaseResources()
    {
        if (_isLayerAdded && _layer is not null && MissionScreen is not null)
        {
            try
            {
                MissionScreen.RemoveLayer(_layer);
            }
            catch (Exception exception)
            {
                RexLog.Error("Could not remove the speech-label overlay layer.", exception);
            }
        }

        _isLayerAdded = false;
        ClearAllLabels();
        _viewModel?.OnFinalize();
        _viewModel = null;
        _layer = null;
    }
}
