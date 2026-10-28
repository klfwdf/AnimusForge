using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionSingleplayerKillNotificationUIHandler))]
public class MissionGauntletKillNotificationSingleplayerUIHandler : MissionBattleUIBaseView
{
	protected SPKillFeedVM _dataSource;

	private GauntletLayer _gauntletLayer;

	protected bool _isGeneralFeedEnabled = true;

	protected bool _isPersonalFeedEnabled = true;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		ViewOrderPriority = 17;
		_isGeneralFeedEnabled = BannerlordConfig.KillFeedVisualType < 2;
		_isPersonalFeedEnabled = BannerlordConfig.ReportPersonalDamage;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnOptionChange));
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnOptionChange));
	}

	protected override void OnCreateView()
	{
		_dataSource = new SPKillFeedVM();
		_gauntletLayer = new GauntletLayer("MissionSPKillFeed", ViewOrderPriority);
		_gauntletLayer.LoadMovie("SingleplayerKillfeed", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		CombatLogManager.OnGenerateCombatLog += OnCombatLogManagerOnPrintCombatLog;
	}

	protected override void OnDestroyView()
	{
		CombatLogManager.OnGenerateCombatLog -= OnCombatLogManagerOnPrintCombatLog;
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	protected override void OnSuspendView()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		}
	}

	protected override void OnResumeView()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_dataSource != null)
		{
			bool isPaused = MBCommon.IsPaused;
			for (int i = 0; i < _dataSource.GeneralCasualty.NotificationList.Count; i++)
			{
				_dataSource.GeneralCasualty.NotificationList[i].IsPaused = isPaused;
			}
			for (int j = 0; j < _dataSource.PersonalFeed.NotificationList.Count; j++)
			{
				_dataSource.PersonalFeed.NotificationList[j].IsPaused = isPaused;
			}
		}
	}

	private void OnOptionChange(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		switch (changedManagedOptionsType)
		{
		case ManagedOptions.ManagedOptionsType.ReportCasualtiesType:
			_isGeneralFeedEnabled = BannerlordConfig.KillFeedVisualType < 2;
			break;
		case ManagedOptions.ManagedOptionsType.ReportPersonalDamage:
			_isPersonalFeedEnabled = BannerlordConfig.ReportPersonalDamage;
			break;
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		if (base.IsViewCreated && affectorAgent != null && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
		{
			bool isHeadshot = killingBlow.IsHeadShot();
			if (_isPersonalFeedEnabled && affectorAgent == Agent.Main && (affectedAgent.IsHuman || affectedAgent.IsMount))
			{
				bool isFriendlyFire = affectedAgent.Team == affectorAgent.Team || affectedAgent.IsFriendOf(affectorAgent);
				_dataSource.OnPersonalKill(killingBlow.InflictedDamage, affectedAgent.IsMount, isFriendlyFire, isHeadshot, affectedAgent.Character?.Name.ToString(), agentState == AgentState.Unconscious);
			}
			if (_isGeneralFeedEnabled && affectedAgent.IsHuman)
			{
				_dataSource.OnAgentRemoved(affectedAgent, affectorAgent, isHeadshot, affectedAgent == affectorAgent, affectedAgent == affectorAgent && affectedAgent.IsInWater());
			}
		}
	}

	private void OnCombatLogManagerOnPrintCombatLog(CombatLogData logData)
	{
		if (_isPersonalFeedEnabled && (logData.IsAttackerAgentMine || logData.IsAttackerAgentRiderAgentMine) && logData.TotalDamage > 0 && (!logData.IsFatalDamage || (logData.IsEntityToEntityCollisionDamage && logData.IsSpecialDamage)))
		{
			_dataSource.OnPersonalDamage(logData.TotalDamage, logData.IsVictimAgentMount, logData.IsFriendlyFire || logData.IsVictimAgentMine, logData.VictimAgentName);
		}
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (base.IsViewCreated)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (base.IsViewCreated)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}
}
