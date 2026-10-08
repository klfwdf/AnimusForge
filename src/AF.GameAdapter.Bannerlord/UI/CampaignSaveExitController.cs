using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.Library;
using SaveAndExitStage = AnimusForge.MyBehavior.SaveAndExitStage;
using SaveAndExitReason = AnimusForge.MyBehavior.SaveAndExitReason;
namespace AnimusForge;
// Transient ONNX admission and save completion state. No persistence containers or campaign host ownership.
internal sealed class CampaignSaveExitController
{
 private readonly Action _showWeeklyFailure;
 internal CampaignSaveExitController(Action showWeeklyFailure) { _showWeeklyFailure=showWeeklyFailure; }
	internal bool _missingOnnxGateActive;
	internal long _missingOnnxGateResumeAfterUtcTicks;
	internal bool _pendingMissingOnnxGateCheck;
	internal long _pendingMissingOnnxGateCheckAfterUtcTicks;
	internal SaveAndExitStage _saveAndExitStage;
	internal SaveAndExitReason _saveAndExitReason;
internal void QueueMissingOnnxGateCheckAfterOnboarding()
	{
		QueueMissingOnnxGateCheck(TimeSpan.FromMilliseconds(100.0));
	}

internal void QueueMissingOnnxGateCheck(TimeSpan delay)
	{
		try
		{
			_pendingMissingOnnxGateCheck = true;
			_pendingMissingOnnxGateCheckAfterUtcTicks = DateTime.UtcNow.Ticks + delay.Ticks;
		}
		catch
		{
			_pendingMissingOnnxGateCheck = true;
			_pendingMissingOnnxGateCheckAfterUtcTicks = 0L;
		}
	}

internal void ProcessPendingMissingOnnxGateCheck()
	{
		if (!_pendingMissingOnnxGateCheck)
		{
			return;
		}
		if (Campaign.Current == null || !Campaign.Current.GameStarted)
		{
			return;
		}
		if (DateTime.UtcNow.Ticks < _pendingMissingOnnxGateCheckAfterUtcTicks)
		{
			return;
		}
        if (InformationManager.IsAnyInquiryActive() || AnimusForgeApiOnboardingPopup.IsOpen
            || ModOnboardingBehavior.Instance?.IsSetupUiActive == true)
            return; // Keep the one-shot check pending until the owning onboarding UI completes.
		_pendingMissingOnnxGateCheck = false;
		_pendingMissingOnnxGateCheckAfterUtcTicks = 0L;
		EvaluateMissingOnnxGate();
	}

internal void EvaluateMissingOnnxGate()
	{
		try
		{
			if (HasCompleteRequiredOnnxFiles())
			{
				_missingOnnxGateActive = false;
				_missingOnnxGateResumeAfterUtcTicks = 0L;
				return;
			}
			_missingOnnxGateActive = true;
			_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks;
			ShowMissingOnnxGatePopup();
			try
			{
				Logger.Log("OnnxGate", "campaign start blocked because required ONNX files are missing.");
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			_missingOnnxGateActive = true;
			_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks;
			ShowMissingOnnxGatePopup();
			try
			{
				Logger.Log("OnnxGate", "failed to evaluate ONNX gate: " + ex.Message);
			}
			catch
			{
			}
		}
	}

internal void ProcessMissingOnnxGateUiResume()
	{
		if (!_missingOnnxGateActive)
		{
			return;
		}
		if (Campaign.Current == null || !Campaign.Current.GameStarted)
		{
			return;
		}
		if (InformationManager.IsAnyInquiryActive() || AnimusForgeApiOnboardingPopup.IsOpen || ModOnboardingBehavior.Instance?.IsSetupUiActive == true)
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
		_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(100.0).Ticks;
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData("缺少ONNX文件", "请将单独获取的RAG模型包解压到当前游戏的 Modules\\AnimusForge\\ONNX，模型齐备后重新启动游戏。", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "保存并退出", "", ExitCurrentGameBecauseOnnxMissing, null), pauseGameActiveState: true);
	}

internal void ExitCurrentGameBecauseOnnxMissing()
	{
		try
		{
			_missingOnnxGateActive = false;
			_missingOnnxGateResumeAfterUtcTicks = 0L;
			InformationManager.HideInquiry();
			BeginSaveAndExitCurrentGame(SaveAndExitReason.MissingOnnx);
		}
		catch (Exception ex)
		{
			HandleSaveAndExitFailure(SaveAndExitReason.MissingOnnx, "保存并退出失败：" + ex.Message);
		}
	}

internal static bool HasCompleteRequiredOnnxFiles()
	{
		try
		{
			AnimusForgeModelStore.ResolveEmbedding();
			return OnnxEmbeddingEngine.Instance.IsAvailable;
		}
		catch (Exception ex)
		{
            Logger.Log("OnnxGate", "Required ONNX file validation failed: " + ex.Message);
			return false;
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
			InformationManager.DisplayMessage(new InformationMessage("检测到当前已有保存进行中，完成后将再保存一次并自动退出。"));
			return;
		}
		_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
		saveHandler.QuickSaveCurrentGame();
		InformationManager.DisplayMessage(new InformationMessage("正在保存当前存档，保存完成后将自动退出。"));
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
					HandleSaveAndExitFailure(saveAndExitReason, "保存并退出失败：未找到存档保存器。");
					return;
				}
				_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
				saveHandler.QuickSaveCurrentGame();
				InformationManager.DisplayMessage(new InformationMessage("正在保存当前存档，保存完成后将自动退出。"));
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
			HandleSaveAndExitFailure(saveAndExitReason2, "保存当前存档失败，已取消退出。");
		}
		catch (Exception ex)
		{
			SaveAndExitReason saveAndExitReason3 = _saveAndExitReason;
			_saveAndExitStage = SaveAndExitStage.None;
			_saveAndExitReason = SaveAndExitReason.None;
			HandleSaveAndExitFailure(saveAndExitReason3, "保存并退出失败：" + ex.Message);
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
