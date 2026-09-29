using System;
using System.Collections.Generic;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace RichExecutions.UI;

internal sealed class ExecutionJudgementPanel
{
    private static ExecutionJudgementPanel? _activePanel;

    private readonly ScreenBase _screen;
    private readonly GauntletLayer _layer;
    private readonly ExecutionJudgementVM _dataSource;
    private readonly Action _onClose;
    private readonly Action<ExecutionJudgementSelection> _onExecute;
    private GauntletMovieIdentifier? _movie;
    private bool _isClosed;
    private bool _isLayerAdded;
    private bool _isDataSourceFinalized;
    private bool _isExecutionForwarded;

    private ExecutionJudgementPanel(
        ScreenBase screen,
        Settlement venue,
        IReadOnlyList<ExecutionPrisonerOption> prisoners,
        IReadOnlyList<ExecutionMethodDefinition> methods,
        IReadOnlyList<ExecutionChargeDefinition> charges,
        Func<TaleWorlds.CampaignSystem.Hero, ExecutionChargeDefinition, ExecutionEvidencePresentation> evidenceProvider,
        Func<TaleWorlds.CampaignSystem.Hero, IReadOnlyList<ExecutionCrimeIncidentPresentation>> crimeHistoryProvider,
        int influenceCost,
        Action<ExecutionJudgementSelection> onExecute,
        Action onClose)
    {
        _screen = screen;
        _onExecute = onExecute;
        _onClose = onClose;
        _dataSource = new ExecutionJudgementVM(
            venue,
            prisoners,
            methods,
            charges,
            evidenceProvider,
            crimeHistoryProvider,
            influenceCost,
            HandleExecuteRequested,
            HandleCloseRequested);
        _layer = new GauntletLayer("RichExecutionJudgement", 320, false);
    }

    public static bool IsOpen => _activePanel is not null;

    public static bool Show(
        Settlement venue,
        IReadOnlyList<ExecutionPrisonerOption> prisoners,
        IReadOnlyList<ExecutionMethodDefinition> methods,
        IReadOnlyList<ExecutionChargeDefinition> charges,
        Func<TaleWorlds.CampaignSystem.Hero, ExecutionChargeDefinition, ExecutionEvidencePresentation> evidenceProvider,
        Func<TaleWorlds.CampaignSystem.Hero, IReadOnlyList<ExecutionCrimeIncidentPresentation>> crimeHistoryProvider,
        int influenceCost,
        Action<ExecutionJudgementSelection> onExecute,
        Action onClose)
    {
        var screen = ScreenManager.TopScreen;
        if (screen is null)
        {
            RexLog.Info("Judgement panel could not open because no top screen is active.");
            return false;
        }

        ExecutionJudgementPanel? panel = null;
        try
        {
            _activePanel?.Close(silent: true, notify: false);
            panel = new ExecutionJudgementPanel(
                screen,
                venue,
                prisoners,
                methods,
                charges,
                evidenceProvider,
                crimeHistoryProvider,
                influenceCost,
                onExecute,
                onClose);
            panel.Open();
            _activePanel = panel;
            RexLog.Info($"Opened the three-column judgement panel on {screen.GetType().FullName}.");
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not open the three-column judgement panel.", exception);
            panel?.Close(silent: true, notify: false);
            if (_activePanel is not null && !ReferenceEquals(_activePanel, panel))
            {
                _activePanel.Close(silent: true, notify: false);
            }

            _activePanel = null;
            return false;
        }
    }

    public static void Tick()
    {
        var panel = _activePanel;
        if (panel is null || panel._isClosed)
        {
            return;
        }

        try
        {
            if (!ReferenceEquals(ScreenManager.TopScreen, panel._screen))
            {
                RexLog.Info("Judgement panel closed because its owning screen is no longer active.");
                panel.Close(silent: true, notify: true);
                return;
            }

            if (panel._layer.Input.IsHotKeyReleased("Exit") ||
                panel._layer.Input.IsKeyReleased(InputKey.Escape))
            {
                panel.Close(silent: true, notify: true);
            }
        }
        catch (Exception exception)
        {
            RexLog.Error("Judgement panel input handling failed; closing the panel safely.", exception);
            panel.Close(silent: true, notify: true);
        }
    }

    public static void CloseActive(bool notify = false) =>
        _activePanel?.Close(silent: true, notify: notify);

    private void Open()
    {
        _movie = _layer.LoadMovie("RichExecutionJudgement", _dataSource);
        _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
        try
        {
            _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
        }
        catch
        {
        }

        _screen.AddLayer(_layer);
        _isLayerAdded = true;
        _layer.IsFocusLayer = true;
        ScreenManager.TrySetFocus(_layer);
    }

    private void HandleExecuteRequested(ExecutionJudgementSelection selection)
    {
        if (_isExecutionForwarded || _isClosed)
        {
            return;
        }

        _isExecutionForwarded = true;
        Close(silent: true, notify: false);
        _onExecute(selection);
    }

    private void HandleCloseRequested() => Close(silent: true, notify: true);

    private void Close(bool silent, bool notify)
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        Exception? firstFailure = null;
        try
        {
            _layer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_layer);
        }
        catch (Exception exception)
        {
            firstFailure ??= exception;
            RexLog.Error("Judgement panel could not release keyboard focus.", exception);
        }

        try
        {
            _layer.InputRestrictions.ResetInputRestrictions();
        }
        catch (Exception exception)
        {
            firstFailure ??= exception;
            RexLog.Error("Judgement panel could not reset input restrictions.", exception);
        }

        try
        {
            if (_movie is not null)
            {
                _layer.ReleaseMovie(_movie);
                _movie = null;
            }
        }
        catch (Exception exception)
        {
            firstFailure ??= exception;
            RexLog.Error("Judgement panel could not release its Gauntlet movie.", exception);
        }

        try
        {
            if (_isLayerAdded)
            {
                _screen.RemoveLayer(_layer);
                _isLayerAdded = false;
            }
        }
        catch (Exception exception)
        {
            firstFailure ??= exception;
            RexLog.Error("Judgement panel could not remove its screen layer.", exception);
        }

        try
        {
            if (!_isDataSourceFinalized)
            {
                _dataSource.OnFinalize();
                _isDataSourceFinalized = true;
            }
        }
        catch (Exception exception)
        {
            firstFailure ??= exception;
            RexLog.Error("Judgement panel ViewModel finalization failed.", exception);
        }

        if (ReferenceEquals(_activePanel, this))
        {
            _activePanel = null;
        }

        if (notify)
        {
            _onClose();
        }

        if (!silent && firstFailure is not null)
        {
            throw firstFailure;
        }
    }
}
