using System;
using System.Threading.Tasks;
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
                InformationManager.DisplayMessage(new InformationMessage("本地知识模型已就绪。"));
            }
            else
            {
                _onnxFailureMessage = completed
                    ? "本地知识模型缺失或加载失败。请检查 Modules\\AnimusForge\\ONNX 模型包和 Mod_Logic.txt，修复后重新启动游戏。"
                    : "本地知识模型初始化超过 60 秒，已停止等待。游戏界面仍可操作，请保存退出并检查模型包和 Mod_Logic.txt 后重启。";
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
            InformationManager.DisplayMessage(new InformationMessage("正在初始化本地知识模型，首次加载可能较慢；完成前暂不可使用 AF 对话。"));
        }
        catch (Exception ex)
        {
            _onnxFailureMessage = "无法启动本地知识模型加载，请检查 Mod_Logic.txt 后重启游戏。";
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
            InformationManager.DisplayMessage(new InformationMessage("本地知识模型仍在加载，请稍候；无需重新填写玩家背景。"));
            return;
        }
		_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(100.0).Ticks;
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData("本地知识模型不可用", _onnxFailureMessage ?? "请将完整 RAG 模型包解压到 Modules\\AnimusForge\\ONNX 后重新启动游戏。", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "保存并退出", "", ExitCurrentGameBecauseOnnxMissing, null), pauseGameActiveState: true);
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
			HandleSaveAndExitFailure(SaveAndExitReason.MissingOnnx, "保存并退出失败：" + ex.Message);
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
