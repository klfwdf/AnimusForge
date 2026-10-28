using System;
using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerKillNotificationUIHandler))]
public class MissionGauntletKillNotificationUIHandler : MissionView
{
	private MPKillFeedVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private MissionMultiplayerTeamDeathmatchClient _tdmClient;

	private MissionMultiplayerSiegeClient _siegeClient;

	private MissionMultiplayerGameModeDuelClient _duelClient;

	private MissionMultiplayerGameModeFlagDominationClient _flagDominationClient;

	private bool _isGeneralFeedEnabled;

	private bool _doesGameModeAllowGeneralFeed = true;

	private bool _isPersonalFeedEnabled;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		ViewOrderPriority = 2;
		_isGeneralFeedEnabled = _doesGameModeAllowGeneralFeed && BannerlordConfig.KillFeedVisualType < 2;
		_isPersonalFeedEnabled = BannerlordConfig.ReportPersonalDamage;
		_dataSource = new MPKillFeedVM();
		_gauntletLayer = new GauntletLayer("MultiplayerKillFeed", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerKillFeed", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		CombatLogManager.OnGenerateCombatLog += OnCombatLogManagerOnPrintCombatLog;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnOptionChange));
	}

	private void OnOptionChange(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		switch (changedManagedOptionsType)
		{
		case ManagedOptions.ManagedOptionsType.ReportCasualtiesType:
			_isGeneralFeedEnabled = _doesGameModeAllowGeneralFeed && BannerlordConfig.KillFeedVisualType < 2;
			break;
		case ManagedOptions.ManagedOptionsType.ReportPersonalDamage:
			_isPersonalFeedEnabled = BannerlordConfig.ReportPersonalDamage;
			break;
		}
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_tdmClient = base.Mission.GetMissionBehavior<MissionMultiplayerTeamDeathmatchClient>();
		if (_tdmClient != null)
		{
			_tdmClient.OnGoldGainEvent += OnGoldGain;
		}
		_siegeClient = base.Mission.GetMissionBehavior<MissionMultiplayerSiegeClient>();
		if (_siegeClient != null)
		{
			_siegeClient.OnGoldGainEvent += OnGoldGain;
		}
		_flagDominationClient = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();
		if (_flagDominationClient != null)
		{
			_flagDominationClient.OnGoldGainEvent += OnGoldGain;
		}
		_duelClient = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeDuelClient>();
		if (_duelClient != null)
		{
			_doesGameModeAllowGeneralFeed = false;
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		CombatLogManager.OnGenerateCombatLog -= OnCombatLogManagerOnPrintCombatLog;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnOptionChange));
		if (_tdmClient != null)
		{
			_tdmClient.OnGoldGainEvent -= OnGoldGain;
		}
		if (_siegeClient != null)
		{
			_siegeClient.OnGoldGainEvent -= OnGoldGain;
		}
		if (_flagDominationClient != null)
		{
			_flagDominationClient.OnGoldGainEvent -= OnGoldGain;
		}
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	private void OnGoldGain(GoldGain goldGainMessage)
	{
		if (!_isPersonalFeedEnabled)
		{
			return;
		}
		foreach (KeyValuePair<ushort, int> goldChangeEvent in goldGainMessage.GoldChangeEventList)
		{
			_dataSource.PersonalCasualty.OnGoldChange(goldChangeEvent.Value, (GoldGainFlags)goldChangeEvent.Key);
		}
	}

	private void OnCombatLogManagerOnPrintCombatLog(CombatLogData logData)
	{
		if (_isPersonalFeedEnabled && (logData.IsAttackerAgentMine || logData.IsAttackerAgentRiderAgentMine) && logData.TotalDamage > 0 && !logData.IsVictimAgentSameAsAttackerAgent)
		{
			_dataSource.OnPersonalDamage(logData.TotalDamage, logData.IsFatalDamage, logData.IsVictimAgentMount, logData.IsFriendlyFire, logData.BodyPartHit == BoneBodyPartType.Head, logData.VictimAgentName);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		if (_isGeneralFeedEnabled && !GameNetwork.IsDedicatedServer && affectorAgent != null && affectedAgent.IsHuman && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
		{
			WeaponClass killWeaponClass = (killingBlow.IsValid ? ((WeaponClass)killingBlow.WeaponClass) : WeaponClass.Undefined);
			_dataSource.OnAgentRemoved(affectedAgent, affectorAgent, _isPersonalFeedEnabled, killWeaponClass);
		}
	}
}
