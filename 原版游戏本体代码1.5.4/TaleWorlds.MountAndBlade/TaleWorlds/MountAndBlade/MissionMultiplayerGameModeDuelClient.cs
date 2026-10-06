using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.MissionRepresentatives;

namespace TaleWorlds.MountAndBlade;

public class MissionMultiplayerGameModeDuelClient : MissionMultiplayerGameModeBaseClient
{
	public Action OnMyRepresentativeAssigned;

	public override bool IsGameModeUsingGold => false;

	public override bool IsGameModeTactical => false;

	public override bool IsGameModeUsingRoundCountdown => false;

	public override bool IsGameModeUsingAllowCultureChange => true;

	public override bool IsGameModeUsingAllowTroopChange => true;

	public override MultiplayerGameType GameType => MultiplayerGameType.Duel;

	public bool IsInDuel => GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team?.IsDefender ?? false;

	public DuelMissionRepresentative MyRepresentative { get; private set; }

	private void OnMyClientSynchronized()
	{
		MyRepresentative = GameNetwork.MyPeer.GetComponent<DuelMissionRepresentative>();
		OnMyRepresentativeAssigned?.Invoke();
		MyRepresentative.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
	}

	public override int GetGoldAmount()
	{
		return 0;
	}

	public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		base.MissionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		base.MissionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
		if (MyRepresentative != null)
		{
			MyRepresentative.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (MyRepresentative != null)
		{
			MyRepresentative.CheckHasRequestFromAndRemoveRequestIfNeeded(affectedAgent.MissionPeer);
		}
	}

	public override bool CanRequestCultureChange()
	{
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (missionPeer?.Team != null)
		{
			return missionPeer.Team.IsAttacker;
		}
		return false;
	}

	public override bool CanRequestTroopChange()
	{
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (missionPeer?.Team != null)
		{
			return missionPeer.Team.IsAttacker;
		}
		return false;
	}
}
