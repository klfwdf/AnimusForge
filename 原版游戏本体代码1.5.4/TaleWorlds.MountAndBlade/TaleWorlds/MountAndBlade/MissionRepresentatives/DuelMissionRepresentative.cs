using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.MissionRepresentatives;

public class DuelMissionRepresentative : MissionRepresentativeBase
{
	public const int DuelPrepTime = 3;

	public Action<MissionPeer, TroopType> OnDuelRequestedEvent;

	public Action<MissionPeer> OnDuelRequestSentEvent;

	public Action<MissionPeer, int> OnDuelPrepStartedEvent;

	public Action OnAgentSpawnedWithoutDuelEvent;

	public Action<MissionPeer, MissionPeer, int> OnDuelPreparationStartedForTheFirstTimeEvent;

	public Action<MissionPeer> OnDuelEndedEvent;

	public Action<MissionPeer> OnDuelRoundEndedEvent;

	public Action<TroopType> OnMyPreferredZoneChanged;

	private List<Tuple<MissionPeer, MissionTime>> _requesters;

	private MissionMultiplayerDuel _missionMultiplayerDuel;

	private IFocusable _focusedObject;

	public int Bounty { get; private set; }

	public int Score { get; private set; }

	public int NumberOfWins { get; private set; }

	private bool _isInDuel
	{
		get
		{
			if (base.MissionPeer != null && base.MissionPeer.Team != null)
			{
				return base.MissionPeer.Team.IsDefender;
			}
			return false;
		}
	}

	public override void Initialize()
	{
		_requesters = new List<Tuple<MissionPeer, MissionTime>>();
		if (GameNetwork.IsServerOrRecorder)
		{
			_missionMultiplayerDuel = Mission.Current.GetMissionBehavior<MissionMultiplayerDuel>();
		}
		Mission.Current.SetMissionMode(MissionMode.Duel, atStart: true);
	}

	public void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(mode);
			networkMessageHandlerRegisterer.Register<NetworkMessages.FromServer.DuelRequest>(HandleServerEventDuelRequest);
			networkMessageHandlerRegisterer.Register<DuelSessionStarted>(HandleServerEventDuelSessionStarted);
			networkMessageHandlerRegisterer.Register<DuelPreparationStartedForTheFirstTime>(HandleServerEventDuelStarted);
			networkMessageHandlerRegisterer.Register<DuelEnded>(HandleServerEventDuelEnded);
			networkMessageHandlerRegisterer.Register<DuelRoundEnded>(HandleServerEventDuelRoundEnded);
			networkMessageHandlerRegisterer.Register<DuelPointsUpdateMessage>(HandleServerPointUpdate);
		}
	}

	public void OnInteraction()
	{
		if (_focusedObject == null)
		{
			return;
		}
		Agent focusedAgent;
		if ((focusedAgent = _focusedObject as Agent) != null)
		{
			if (!focusedAgent.IsActive())
			{
				return;
			}
			if (_requesters.Any((Tuple<MissionPeer, MissionTime> req) => req.Item1 == focusedAgent.MissionPeer))
			{
				for (int num = 0; num < _requesters.Count; num++)
				{
					if (_requesters[num].Item1 == base.MissionPeer)
					{
						_requesters.Remove(_requesters[num]);
						break;
					}
				}
				switch (base.PlayerType)
				{
				case PlayerTypes.Client:
					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new DuelResponse(focusedAgent.MissionRepresentative.Peer.Communicator as NetworkCommunicator, accepted: true));
					GameNetwork.EndModuleEventAsClient();
					break;
				case PlayerTypes.Server:
					_missionMultiplayerDuel.DuelRequestAccepted(focusedAgent, base.ControlledAgent);
					break;
				}
			}
			else
			{
				switch (base.PlayerType)
				{
				case PlayerTypes.Client:
					OnDuelRequestSentEvent?.Invoke(focusedAgent.MissionPeer);
					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new NetworkMessages.FromClient.DuelRequest(focusedAgent.Index));
					GameNetwork.EndModuleEventAsClient();
					break;
				case PlayerTypes.Server:
					_missionMultiplayerDuel.DuelRequestReceived(base.MissionPeer, focusedAgent.MissionPeer);
					break;
				}
			}
		}
		else if (_focusedObject is DuelZoneLandmark duelZoneLandmark)
		{
			if (_isInDuel)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=v5EqMSlD}Can't change arena preference while in duel.").ToString()));
				return;
			}
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestChangePreferredTroopType(duelZoneLandmark.ZoneTroopType));
			GameNetwork.EndModuleEventAsClient();
			OnMyPreferredZoneChanged?.Invoke(duelZoneLandmark.ZoneTroopType);
		}
	}

	private void HandleServerEventDuelRequest(NetworkMessages.FromServer.DuelRequest message)
	{
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(message.RequesterAgentIndex);
		Mission.MissionNetworkHelper.GetAgentFromIndex(message.RequestedAgentIndex);
		DuelRequested(agentFromIndex, message.SelectedAreaTroopType);
	}

	private void HandleServerEventDuelSessionStarted(DuelSessionStarted message)
	{
		OnDuelPreparation(message.RequesterPeer.GetComponent<MissionPeer>(), message.RequestedPeer.GetComponent<MissionPeer>());
	}

	private void HandleServerEventDuelStarted(DuelPreparationStartedForTheFirstTime message)
	{
		MissionPeer component = message.RequesterPeer.GetComponent<MissionPeer>();
		MissionPeer component2 = message.RequesteePeer.GetComponent<MissionPeer>();
		OnDuelPreparationStartedForTheFirstTimeEvent?.Invoke(component, component2, message.AreaIndex);
	}

	private void HandleServerEventDuelEnded(DuelEnded message)
	{
		OnDuelEndedEvent?.Invoke(message.WinnerPeer.GetComponent<MissionPeer>());
	}

	private void HandleServerEventDuelRoundEnded(DuelRoundEnded message)
	{
		OnDuelRoundEndedEvent?.Invoke(message.WinnerPeer.GetComponent<MissionPeer>());
	}

	private void HandleServerPointUpdate(DuelPointsUpdateMessage message)
	{
		DuelMissionRepresentative component = message.NetworkCommunicator.GetComponent<DuelMissionRepresentative>();
		component.Bounty = message.Bounty;
		component.Score = message.Score;
		component.NumberOfWins = message.NumberOfWins;
	}

	public void DuelRequested(Agent requesterAgent, TroopType selectedAreaTroopType)
	{
		_requesters.Add(new Tuple<MissionPeer, MissionTime>(requesterAgent.MissionPeer, MissionTime.Now + MissionTime.Seconds(10f)));
		switch (base.PlayerType)
		{
		case PlayerTypes.Bot:
			_missionMultiplayerDuel.DuelRequestAccepted(requesterAgent, base.ControlledAgent);
			break;
		case PlayerTypes.Server:
			OnDuelRequestedEvent?.Invoke(requesterAgent.MissionPeer, selectedAreaTroopType);
			break;
		case PlayerTypes.Client:
			if (base.IsMine)
			{
				OnDuelRequestedEvent?.Invoke(requesterAgent.MissionPeer, selectedAreaTroopType);
				break;
			}
			GameNetwork.BeginModuleEventAsServer(base.Peer);
			GameNetwork.WriteMessage(new NetworkMessages.FromServer.DuelRequest(requesterAgent.Index, base.ControlledAgent.Index, selectedAreaTroopType));
			GameNetwork.EndModuleEventAsServer();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public bool CheckHasRequestFromAndRemoveRequestIfNeeded(MissionPeer requestOwner)
	{
		if (requestOwner != null && requestOwner.Representative == this)
		{
			_requesters.Clear();
			return false;
		}
		Tuple<MissionPeer, MissionTime> tuple = _requesters.FirstOrDefault((Tuple<MissionPeer, MissionTime> req) => req.Item1 == requestOwner);
		if (tuple == null)
		{
			return false;
		}
		if (requestOwner.ControlledAgent == null || !requestOwner.ControlledAgent.IsActive())
		{
			_requesters.Remove(tuple);
			return false;
		}
		if (!tuple.Item2.IsPast)
		{
			return true;
		}
		_requesters.Remove(tuple);
		return false;
	}

	public void OnDuelPreparation(MissionPeer requesterPeer, MissionPeer requesteePeer)
	{
		switch (base.PlayerType)
		{
		case PlayerTypes.Client:
			if (base.IsMine)
			{
				OnDuelPrepStartedEvent?.Invoke((base.MissionPeer == requesterPeer) ? requesteePeer : requesterPeer, 3);
				break;
			}
			GameNetwork.BeginModuleEventAsServer(base.Peer);
			GameNetwork.WriteMessage(new DuelSessionStarted(requesterPeer.GetNetworkPeer(), requesteePeer.GetNetworkPeer()));
			GameNetwork.EndModuleEventAsServer();
			break;
		case PlayerTypes.Server:
			OnDuelPrepStartedEvent?.Invoke((base.MissionPeer == requesterPeer) ? requesteePeer : requesterPeer, 3);
			break;
		}
		Tuple<MissionPeer, MissionTime> tuple = _requesters.FirstOrDefault((Tuple<MissionPeer, MissionTime> req) => req.Item1 == requesterPeer);
		if (tuple != null)
		{
			_requesters.Remove(tuple);
		}
	}

	public void OnObjectFocused(IFocusable focusedObject)
	{
		_focusedObject = focusedObject;
	}

	public void OnObjectFocusLost()
	{
		_focusedObject = null;
	}

	public override void OnAgentSpawned()
	{
		if (base.ControlledAgent.Team != null && base.ControlledAgent.Team.Side == BattleSideEnum.Attacker)
		{
			OnAgentSpawnedWithoutDuelEvent?.Invoke();
		}
	}

	public void ResetBountyAndNumberOfWins()
	{
		Bounty = 0;
		NumberOfWins = 0;
	}

	public void OnDuelWon(float gainedScore)
	{
		Bounty += (int)(gainedScore / 5f);
		Score += (int)gainedScore;
		NumberOfWins++;
	}
}
