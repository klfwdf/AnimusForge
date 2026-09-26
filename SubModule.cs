using System;
using System.Collections.Generic;
using AnimusForge.PolicyEffects;
using System.IO;
using System.Text;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using AnimusForge.Refactor.Modules;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using AFWarStatsTerminal.UI;

namespace AnimusForge;

public class SubModule : MBSubModuleBase
{
	// 此标记存于模块日志目录，独立于任何存档，用于跨游戏重启去重主界面欢迎弹窗。
	private const string InitialApiGuideNoticeMarkerFileName = ".initial_api_guide_notice_v1";

	private const string InitialApiGuideNoticeMarkerValue = "animusforge-main-menu-welcome-v1";

	private UIExtender _uiExtender;

	private static bool _uiExtenderInitialized;

	private bool _pendingInitialApiGuideNotice;

	private bool _initialApiGuideNoticeShown;

	private long _initialApiGuideNoticeAfterUtcTicks;
	private AfWarStatsMapButtonLayer _mapButtonLayer;
	private float _mapButtonRetryDelay;

	public override void OnInitialState()
	{
		base.OnInitialState();
		MarkPendingInitialApiGuideNotice();
	}

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		if (FeatureBridgeRuntime.Initialize(out string featureBridgeReason))
		{
			Logger.LogTrace("SubModule", ">>> Feature bridge catalog initialized: " + featureBridgeReason);
		}
		else
		{
			Logger.LogTrace("SubModule", ">>> Feature bridge catalog failed closed: " + featureBridgeReason);
		}
		// 只装配同 DLL 的内部接缝与只读 API 目录，不切换任何渠道的默认执行路径。
		ModuleFrameworkRuntime.Initialize(out string moduleFrameworkReason);
		Logger.LogTrace("SubModule", ">>> Module framework: " + moduleFrameworkReason);
		SceneActionsIntegrationBoundary.InitializeRuntime();
		if (_uiExtenderInitialized)
		{
			return;
		}
		_uiExtenderInitialized = true;
		try
		{
			_uiExtender = UIExtender.Create("AnimusForge");
			if (_uiExtender != null)
			{
				_uiExtender.Register(typeof(SubModule).Assembly);
				_uiExtender.Enable();
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> UIExtenderEx init failed: " + ex.Message);
			_uiExtenderInitialized = false;
		}
	}

	public override void OnConfigChanged()
	{
		base.OnConfigChanged();
		SceneActionsIntegrationBoundary.RefreshMcmOverrides();
	}
	public override void OnBeforeMissionBehaviorInitialize(Mission mission)
	{
		base.OnBeforeMissionBehaviorInitialize(mission);
		SceneActionsIntegrationBoundary.RegisterBeforeMissionInitialization(mission);
	}

	public override void OnMissionBehaviorInitialize(Mission mission)
	{
		base.OnMissionBehaviorInitialize(mission);
		SceneActionsIntegrationBoundary.VerifyMissionInitialization(mission);
	}

	public override void OnGameEnd(Game game)
	{
		RemoveMapButtonLayer();
		AfCampaignRuntimeLifecycle.End(game);
		base.OnGameEnd(game);
	}

	protected override void OnSubModuleUnloaded()
	{
		RemoveMapButtonLayer();
		AfCampaignRuntimeLifecycle.Stop();
		ModuleFrameworkRuntime.Shutdown();
		SceneActionsIntegrationBoundary.ShutdownRuntime();
		base.OnSubModuleUnloaded();
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		StartupPatchComposition.Register();
	}

	protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
	{
		CampaignGameStarter campaignStarter = starterObject as CampaignGameStarter;
		if (campaignStarter != null) AfCampaignRuntimeLifecycle.Begin(game);
		try
		{
			ModuleFrameworkRuntime.RegisterCampaign(starterObject);
			if (campaignStarter != null) AfCampaignRuntimeLifecycle.CaptureOwners(game, campaignStarter);
		}
		catch
		{
			if (campaignStarter != null)
			{
				try { AfCampaignRuntimeLifecycle.CaptureOwners(game, campaignStarter); }
				catch (Exception) { } // Preserve the original registration failure.
				try { AfCampaignRuntimeLifecycle.End(game); }
				catch (Exception) { }
			}
			throw;
		}
	}

	protected override void OnApplicationTick(float dt)
	{
		ApplicationTickComposition.Run(this, dt);
	}

	private void MarkPendingInitialApiGuideNotice()
	{
		try
		{
			if (_initialApiGuideNoticeShown)
			{
				return;
			}
			_pendingInitialApiGuideNotice = true;
			_initialApiGuideNoticeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(1.0).Ticks;
		}
		catch
		{
		}
	}

	internal void ProcessPendingInitialApiGuideNotice()
	{
		try
		{
			if (!_pendingInitialApiGuideNotice || _initialApiGuideNoticeShown || DateTime.UtcNow.Ticks < _initialApiGuideNoticeAfterUtcTicks)
			{
				return;
			}
			// 仅在启动延迟结束后读取一次标记；已展示过时不再创建或排队弹窗。
			if (HasInitialApiGuideNoticeMarker())
			{
				_pendingInitialApiGuideNotice = false;
				_initialApiGuideNoticeShown = true;
				return;
			}
			_pendingInitialApiGuideNotice = false;
			_initialApiGuideNoticeShown = true;
			InformationManager.ShowInquiry(new InquiryData("欢迎使用 AnimusForge", "若要配置 API 信息，你无需进入 MCM 页面；进入存档之后的首次引导会引导你填写 API 信息。", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "知道了", "", null, null), pauseGameActiveState: false, prioritize: false);
			TryWriteInitialApiGuideNoticeMarker();
		}
		catch
		{
		}
	}

	private static bool HasInitialApiGuideNoticeMarker()
	{
		try
		{
			string markerPath = AnimusForgeModulePaths.GetLogFilePath(InitialApiGuideNoticeMarkerFileName);
			return File.Exists(markerPath) && string.Equals(File.ReadAllText(markerPath, Encoding.UTF8).Trim(), InitialApiGuideNoticeMarkerValue, StringComparison.Ordinal);
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> Initial API guide marker read failed: " + ex.Message);
			return false;
		}
	}

	private static void TryWriteInitialApiGuideNoticeMarker()
	{
		try
		{
			string markerPath = AnimusForgeModulePaths.GetLogFilePath(InitialApiGuideNoticeMarkerFileName);
			string directoryName = Path.GetDirectoryName(markerPath);
			if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(markerPath, InitialApiGuideNoticeMarkerValue, Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> Initial API guide marker write failed: " + ex.Message);
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("reload", "AnimusForge")]
	public static string CommandReloadConfig(List<string> strings)
	{
		AIConfigHandler.ReloadConfig();
		return "Config Reloaded Successfully!";
	}

	internal void TickWarStatsMapButton(float dt)
	{
		if (Campaign.Current == null)
		{
			if (_mapButtonLayer != null)
			{
				RemoveMapButtonLayer();
			}
			return;
		}

		if (_mapButtonLayer != null)
		{
			return;
		}

		_mapButtonRetryDelay = Math.Max(0f, _mapButtonRetryDelay - dt);
		if (_mapButtonRetryDelay > 0f)
		{
			return;
		}

		ScreenBase topScreen = ScreenManager.TopScreen;
		if (!AfWarStatsMapButtonLayer.IsCampaignMapScreen(topScreen))
		{
			return;
		}

		try
		{
			_mapButtonLayer = new AfWarStatsMapButtonLayer();
			ScreenManager.AddGlobalLayer(_mapButtonLayer, true);
			Logger.LogTrace("SubModule", ">>> WarStats map button layer created for " + topScreen.GetType().FullName + ".");
		}
		catch (Exception ex)
		{
			_mapButtonLayer = null;
			_mapButtonRetryDelay = 3f;
			Logger.LogTrace("SubModule", ">>> Failed to create WarStats map button layer: " + ex.Message);
		}
	}

	private void RemoveMapButtonLayer()
	{
		if (_mapButtonLayer == null)
		{
			return;
		}

		try
		{
			ScreenManager.RemoveGlobalLayer(_mapButtonLayer);
		}
		catch
		{
		}

		try
		{
			_mapButtonLayer.FinalizeLayer();
		}
		catch
		{
		}
		_mapButtonLayer = null;
	}
}
