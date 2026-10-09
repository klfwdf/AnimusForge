using System;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using SaveAndExitStage = AnimusForge.MyBehavior.SaveAndExitStage;
using SaveAndExitReason = AnimusForge.MyBehavior.SaveAndExitReason;
namespace AnimusForge;
// Transient ONNX admission and save completion state. No persistence containers or campaign host ownership.
internal sealed class CampaignSaveExitController
{
    private static string Localized(string id, string fallback)
        => new TextObject("{=" + id + "}" + fallback).ToString();

    private static string LocalizedError(string id, string fallback, string error)
    {
        TextObject text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable("ERROR", error ?? string.Empty);
        return text.ToString();
    }

 private readonly Action _showWeeklyFailure;
 internal CampaignSaveExitController(Action showWeeklyFailure) { _showWeeklyFailure=showWeeklyFailure; }
	internal bool _missingOnnxGateActive;
	internal long _missingOnnxGateResumeAfterUtcTicks;
	internal bool _pendingMissingOnnxGateCheck;
	internal long _pendingMissingOnnxGateCheckAfterUtcTicks;
	internal SaveAndExitStage _saveAndExitStage;
	internal SaveAndExitReason _saveAndExitReason;
    private Task<bool> _onnxInitialization;
    private Campaign _onnxCampaign;
    private long _onnxGeneration;
    private long _onnxStartedAtUtcTicks;
    private string _onnxFailureMessage;
    private static readonly TimeSpan OnnxInitializationTimeout = TimeSpan.FromSeconds(60);

    private static bool IsOnnxGateUiBusy => InformationManager.IsAnyInquiryActive()
        || AnimusForgeApiOnboardingPopup.IsOpen || DevHistoryEditPopup.IsOpen || DevLargeSelectionPopup.IsOpen
        || ModOnboardingBehavior.Instance?.IsSetupUiActive == true;

internal void QueueMissingOnnxGateCheckAfterOnboarding()
	{
		QueueMissingOnnxGateCheck(TimeSpan.FromMilliseconds(100.0));
	}

internal void QueueMissingOnnxGateCheck(TimeSpan delay)
    {
        // Detach the previous campaign's result; the process-wide engine still owns its one worker.
        _onnxInitialization = null;
        _onnxCampaign = Campaign.Current;
        _onnxGeneration = SaveRuntimeGuard.CaptureGeneration();
        _onnxFailureMessage = null;
        _missingOnnxGateActive = false;
        _pendingMissingOnnxGateCheck = true;
        _pendingMissingOnnxGateCheckAfterUtcTicks = DateTime.UtcNow.Ticks + delay.Ticks;
    }

internal void ProcessPendingMissingOnnxGateCheck()
    {
        if (!ReferenceEquals(_onnxCampaign, Campaign.Current) || !SaveRuntimeGuard.IsCurrentGeneration(_onnxGeneration))
        {
            _onnxInitialization = null;
            _pendingMissingOnnxGateCheck = false;
            _missingOnnxGateActive = false;
            return;
        }
        if (Campaign.Current == null || !Campaign.Current.GameStarted) return;
        if (_onnxInitialization != null)
        {
            bool completed = _onnxInitialization.IsCompleted;
            if (!completed && DateTime.UtcNow.Ticks - _onnxStartedAtUtcTicks < OnnxInitializationTimeout.Ticks) return;
            bool available = false;
            try
            {
                // Never wait on an incomplete task from the engine tick.
                if (completed) available = _onnxInitialization.GetAwaiter().GetResult();
            }
            catch (Exception ex) { Logger.Log("OnnxGate", "initialization failed: " + ex.Message); }
            _onnxInitialization = null;
            _missingOnnxGateActive = !available;
            _missingOnnxGateResumeAfterUtcTicks = 0L;
            if (available)
            {
                Logger.Log("OnnxGate", "background initialization complete");
                InformationManager.DisplayMessage(new InformationMessage(Localized("AF_UI_SaveExit_OnnxReady", "The local knowledge model is ready.")));
            }
            else
            {
                _onnxFailureMessage = completed
                    ? Localized("AF_UI_SaveExit_OnnxUnavailable", "The local knowledge model is missing or failed to load. Check the Modules\\AnimusForge\\ONNX model package and Mod_Logic.txt, then restart the game.")
                    : Localized("AF_UI_SaveExit_OnnxTimeout", "Local knowledge model initialization exceeded 60 seconds and stopped waiting. The game remains usable; save and exit, then check the model package and Mod_Logic.txt before restarting.");
                Logger.Log("OnnxGate", completed ? "background initialization unavailable" : "background initialization timed out; native worker not aborted");
            }
            return;
        }
        if (!_pendingMissingOnnxGateCheck || DateTime.UtcNow.Ticks < _pendingMissingOnnxGateCheckAfterUtcTicks || IsOnnxGateUiBusy) return;
        _pendingMissingOnnxGateCheck = false;
        _pendingMissingOnnxGateCheckAfterUtcTicks = 0L;
        _missingOnnxGateActive = true;
        try
        {
            string moduleRoot = AnimusForgeModulePaths.GetCurrentModuleRoot();
            _onnxStartedAtUtcTicks = DateTime.UtcNow.Ticks;
            _onnxInitialization = OnnxEmbeddingEngine.Instance.InitializeAsync(moduleRoot);
            Logger.Log("OnnxGate", "background initialization started");
            InformationManager.DisplayMessage(new InformationMessage(Localized("AF_UI_SaveExit_OnnxInitializing", "Initializing the local knowledge model. The first load may take a while; AF conversations are unavailable until it finishes.")));
        }
        catch (Exception ex)
        {
            _onnxFailureMessage = Localized("AF_UI_SaveExit_OnnxStartFailed", "Could not start the local knowledge model loader. Check Mod_Logic.txt and restart the game.");
            Logger.Log("OnnxGate", "initialization could not start: " + ex.Message);
        }
    }

internal void ProcessMissingOnnxGateUiResume()
	{
		if (!_missingOnnxGateActive || _onnxInitialization != null)
		{
			return;
		}
		if (Campaign.Current == null || !Campaign.Current.GameStarted)
		{
			return;
		}
		if (IsOnnxGateUiBusy)
		{
			return;
		}
		if (DateTime.UtcNow.Ticks < _missingOnnxGateResumeAfterUtcTicks)
		{
			return;
		}
		ShowMissingOnnxGatePopup();
	}

internal void ShowMissingOnnxGatePopup()
    {
        if (_onnxInitialization != null)
        {
            InformationManager.DisplayMessage(new InformationMessage(Localized("AF_UI_SaveExit_OnnxStillLoading", "The local knowledge model is still loading. Please wait; you do not need to re-enter the player's background.")));
            return;
        }
		_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(100.0).Ticks;
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(Localized("AF_UI_SaveExit_OnnxPopupTitle", "Local knowledge model unavailable"), _onnxFailureMessage ?? Localized("AF_UI_SaveExit_OnnxPopupBody", "Extract the complete RAG model package into Modules\\AnimusForge\\ONNX, then restart the game."), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Localized("AF_UI_SaveExit_SaveAndExit", "Save and exit"), "", ExitCurrentGameBecauseOnnxMissing, null), pauseGameActiveState: true);
	}

internal void ExitCurrentGameBecauseOnnxMissing()
    {
        _onnxInitialization = null;
        _pendingMissingOnnxGateCheck = false;
		try
		{
			_missingOnnxGateActive = false;
			_missingOnnxGateResumeAfterUtcTicks = 0L;
			InformationManager.HideInquiry();
			BeginSaveAndExitCurrentGame(SaveAndExitReason.MissingOnnx);
		}
		catch (Exception ex)
		{
			HandleSaveAndExitFailure(SaveAndExitReason.MissingOnnx, LocalizedError("AF_UI_SaveExit_Failed", "Save and exit failed: {ERROR}", ex.Message));
		}
	}

internal void BeginSaveAndExitCurrentGame(SaveAndExitReason reason)
	{
		_saveAndExitReason = reason;
		SaveHandler saveHandler = Campaign.Current?.SaveHandler;
		if (saveHandler == null)
		{
			_saveAndExitReason = SaveAndExitReason.None;
			MBGameManager.EndGame();
			return;
		}
		if (saveHandler.IsSaving)
		{
			_saveAndExitStage = SaveAndExitStage.WaitingForCurrentSave;
			InformationManager.DisplayMessage(new InformationMessage(Localized("AF_UI_SaveExit_SaveInProgress", "A save is already in progress. Another save will run after it finishes, then the game will exit automatically.")));
			return;
		}
		_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
		saveHandler.QuickSaveCurrentGame();
		InformationManager.DisplayMessage(new InformationMessage(Localized("AF_UI_SaveExit_Saving", "Saving the current game. The game will exit automatically when saving is complete.")));
	}

internal void OnSaveOver(bool isSuccessful, string saveName)
	{
		if (_saveAndExitStage == SaveAndExitStage.None || _saveAndExitReason == SaveAndExitReason.None)
		{
			return;
		}
		try
		{
			if (_saveAndExitStage == SaveAndExitStage.WaitingForCurrentSave)
			{
				SaveHandler saveHandler = Campaign.Current?.SaveHandler;
				if (saveHandler == null)
				{
					SaveAndExitReason saveAndExitReason = _saveAndExitReason;
					_saveAndExitStage = SaveAndExitStage.None;
					_saveAndExitReason = SaveAndExitReason.None;
					HandleSaveAndExitFailure(saveAndExitReason, Localized("AF_UI_SaveExit_SaveHandlerMissing", "Save and exit failed: no save handler was found."));
					return;
				}
				_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
				saveHandler.QuickSaveCurrentGame();
				InformationManager.DisplayMessage(new InformationMessage(Localized("AF_UI_SaveExit_Saving", "Saving the current game. The game will exit automatically when saving is complete.")));
				return;
			}
			SaveAndExitReason saveAndExitReason2 = _saveAndExitReason;
			_saveAndExitStage = SaveAndExitStage.None;
			_saveAndExitReason = SaveAndExitReason.None;
			if (isSuccessful)
			{
				MBGameManager.EndGame();
				return;
			}
			HandleSaveAndExitFailure(saveAndExitReason2, Localized("AF_UI_SaveExit_SaveFailed", "Saving the current game failed; exit cancelled."));
		}
		catch (Exception ex)
		{
			SaveAndExitReason saveAndExitReason3 = _saveAndExitReason;
			_saveAndExitStage = SaveAndExitStage.None;
			_saveAndExitReason = SaveAndExitReason.None;
			HandleSaveAndExitFailure(saveAndExitReason3, LocalizedError("AF_UI_SaveExit_Failed", "Save and exit failed: {ERROR}", ex.Message));
		}
	}

internal void HandleSaveAndExitFailure(SaveAndExitReason reason, string message)
	{
		InformationManager.DisplayMessage(new InformationMessage(message));
		switch (reason)
		{
		case SaveAndExitReason.MissingOnnx:
			_missingOnnxGateActive = true;
			_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(100.0).Ticks;
			ShowMissingOnnxGatePopup();
			break;
		case SaveAndExitReason.WeeklyReport:
			_showWeeklyFailure();
			break;
		}
	}
}
