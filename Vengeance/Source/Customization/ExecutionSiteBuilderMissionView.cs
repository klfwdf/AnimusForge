using System;
using System.Collections.Generic;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace RichExecutions.Customization;

[DefaultView]
public sealed class ExecutionSiteBuilderMissionView : MissionView
{
    private ExecutionSiteBuilderVM? _viewModel;
    private GauntletLayer? _layer;
    private TownExecutionMissionBehavior? _controller;
    private bool _isLayerAdded;
    private bool _isWheelFocused;
    private bool _isPanelFocused;

    public override void OnMissionScreenInitialize()
    {
        base.OnMissionScreenInitialize();
        try
        {
            _controller = Mission.GetMissionBehavior<TownExecutionMissionBehavior>();
            if (_controller is null || !_controller.IsCustomSiteBuilderActive)
            {
                return;
            }

            _viewModel = new ExecutionSiteBuilderVM(
                HandleWheelHighlighted,
                HandleWheelClicked,
                HandlePanelSelected,
                HandleFinishDeployment,
                HandleBack);
            _layer = new GauntletLayer("RichExecutionSiteBuilder", 330, false);
            var movie = _layer.LoadMovie("RichExecutionSiteBuilder", _viewModel);
            if (movie is null || movie.Movie is null)
            {
                throw new InvalidOperationException("The custom execution-site builder movie could not be loaded.");
            }

            TryAddLayer();
        }
        catch (Exception exception)
        {
            RexLog.Error("The custom execution-site builder view could not be initialized.", exception);
            ReleaseResources();
        }
    }

    public override void OnMissionScreenTick(float dt)
    {
        base.OnMissionScreenTick(dt);
        TryAddLayer();
    }

    internal bool ConfigureAndOpenWheel(
        IReadOnlyList<ExecutionSiteBuilderWheelDefinition> definitions,
        string defaultName,
        string defaultDescription,
        string footerText)
    {
        if (_viewModel is null || _layer is null || !_isLayerAdded || MissionScreen is null)
        {
            return false;
        }

        _viewModel.ConfigureWheel(definitions, defaultName, defaultDescription, footerText);
        _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
        _layer.InputRestrictions.SetMouseVisibility(true);
        _layer.IsFocusLayer = true;
        ScreenManager.TrySetFocus(_layer);
        _isWheelFocused = true;
        _viewModel.IsWheelVisible = true;
        return true;
    }

    internal void CloseWheel()
    {
        if (_viewModel is not null)
        {
            _viewModel.IsWheelVisible = false;
        }

        if (_layer is not null && _isWheelFocused)
        {
            _layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_layer);
            _layer.InputRestrictions.ResetInputRestrictions();
            _isWheelFocused = false;
        }
    }

    internal bool ShowPanel(
        string title,
        IReadOnlyList<ExecutionSiteBuilderPanelDefinition> definitions,
        bool isMainPanel = false)
    {
        if (_viewModel is null || _layer is null || !_isLayerAdded || MissionScreen is null)
        {
            return false;
        }

        _viewModel.ConfigurePanel(title, definitions, isMainPanel);
        _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
        _layer.InputRestrictions.SetMouseVisibility(true);
        _layer.IsFocusLayer = true;
        ScreenManager.TrySetFocus(_layer);
        _isPanelFocused = true;
        return true;
    }

    internal void ConfigurePanelSummary(
        string methodText,
        string presetText,
        string personnelText,
        string deploymentStateText,
        bool canFinish)
    {
        _viewModel?.ConfigurePanelSummary(
            methodText,
            presetText,
            personnelText,
            deploymentStateText,
            canFinish);
    }

    internal void ConfigurePrisoner(
        BasicCharacterObject? character,
        string? bannerCode,
        string nameText,
        string clanText,
        string chargeText,
        string titleText,
        string originText,
        IReadOnlyList<ExecutionSiteBuilderIncidentDefinition> incidents)
    {
        _viewModel?.SetPrisonerModel(character, bannerCode);
        _viewModel?.SetPrisonerSummary(nameText, clanText, chargeText);
        _viewModel?.SetPrisonerDetails(titleText, originText);
        _viewModel?.SetIncidents(incidents);
    }

    internal void ClosePanel()
    {
        _viewModel?.ClosePanel();
        if (_layer is null || !_isPanelFocused)
        {
            return;
        }

        _layer.IsFocusLayer = false;
        ScreenManager.TryLoseFocus(_layer);
        _layer.InputRestrictions.ResetInputRestrictions();
        _isPanelFocused = false;
    }

    internal void SetStatus(string text)
    {
        if (_viewModel is not null)
        {
            _viewModel.StatusText = text ?? string.Empty;
        }
    }

    internal bool TryGetMouseSurfacePosition(out Vec3 position, out Vec3 normal)
    {
        position = Vec3.Zero;
        normal = Vec3.Up;
        if (MissionScreen is null)
        {
            return false;
        }

        try
        {
            if (MissionScreen.SceneLayer?.Input is not null && Mission?.Scene is not null)
            {
                var mousePosition = MissionScreen.SceneLayer.Input.GetMousePositionPixel();
                MissionScreen.ScreenPointToWorldRay(mousePosition, out var rayBegin, out var rayEnd);
                if (Mission.Scene.RayCastForClosestEntityOrTerrain(
                        rayBegin,
                        rayEnd,
                        out _,
                        out var hitPosition,
                        out _,
                        0.01f,
                        BodyFlags.CommonCollisionExcludeFlags))
                {
                    position = hitPosition;
                    return true;
                }
            }
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Builder mouse raycast fell back to ground projection: {exception.Message}");
        }

        return MissionScreen.GetProjectedMousePositionOnGround(
            out position,
            out normal,
            BodyFlags.CommonCollisionExcludeFlags,
            true);
    }

    internal bool IsEscapeReleased =>
        _layer?.Input.IsKeyReleased(InputKey.Escape) == true;

    internal bool IsLeftMousePressed =>
        _layer?.Input.IsKeyPressed(InputKey.LeftMouseButton) == true;

    internal void SelectCommandIndex(int index) =>
        _viewModel?.SelectWheelIndex(index);

    internal bool TryGetCommandMouseIndex(int optionCount, out int index)
    {
        index = -1;
        if (_layer is null || !_isLayerAdded || optionCount <= 0 ||
            Screen.RealScreenResolutionWidth <= 0f || Screen.RealScreenResolutionHeight <= 0f)
        {
            return false;
        }

        var mouse = _layer.Input.GetMousePositionPixel();
        var centerX = Screen.RealScreenResolutionWidth * 0.5f;
        var centerY = Screen.RealScreenResolutionHeight * 0.5f;
        var halfWidth = MathF.Min(560f, Screen.RealScreenResolutionWidth * 0.40f);
        var left = centerX - halfWidth;
        var right = centerX + halfWidth;
        var top = centerY - 170f;
        var bottom = centerY + 90f;
        if (mouse.x < left || mouse.x > right || mouse.y < top || mouse.y > bottom)
        {
            return false;
        }

        var fraction = (mouse.x - left) / MathF.Max(1f, right - left);
        index = (int)(fraction * optionCount);
        if (index < 0) index = 0;
        if (index >= optionCount) index = optionCount - 1;
        return true;
    }

    public override void OnMissionScreenFinalize()
    {
        CloseWheel();
        ClosePanel();
        _controller?.DetachCustomSiteBuilderView(this);
        if (_isLayerAdded && _layer is not null && MissionScreen is not null)
        {
            MissionScreen.RemoveLayer(_layer);
        }

        _isLayerAdded = false;
        ReleaseResources();
        base.OnMissionScreenFinalize();
    }

    private void TryAddLayer()
    {
        if (_isLayerAdded || _layer is null || _controller is null || MissionScreen is null)
        {
            return;
        }

        MissionScreen.AddLayer(_layer);
        _isLayerAdded = true;
        _controller.AttachCustomSiteBuilderView(this);
    }


    private void HandleWheelHighlighted(string optionId) =>
        _controller?.SelectCustomBuilderWheelOption(optionId);

    private void HandleWheelClicked(string optionId) =>
        _controller?.ConfirmCustomBuilderWheelOption(optionId);

    private void HandlePanelSelected(string itemId) =>
        _controller?.SelectCustomBuilderPanelItem(itemId);

    private void HandleFinishDeployment() =>
        _controller?.FinishCustomBuilderDeployment();

    private void HandleBack() =>
        _controller?.ReturnToCustomBuilderMainPanel();

    private void ReleaseResources()
    {
        if (_layer is not null)
        {
            if (_isWheelFocused || _isPanelFocused)
            {
                _layer.IsFocusLayer = false;
                ScreenManager.TryLoseFocus(_layer);
            }

            _layer.InputRestrictions.ResetInputRestrictions();
            _isWheelFocused = false;
            _isPanelFocused = false;
        }

        _viewModel?.OnFinalize();
        _viewModel = null;
        _layer = null;
        _controller = null;
    }
}