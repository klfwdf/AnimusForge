using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerPollComponent : MissionNetwork
{
	private abstract class MultiplayerPoll
	{
		public enum Type
		{
			KickPlayer,
			BanPlayer,
			ChangeGame
		}

		private const int TimeoutInSeconds = 30;

		public Action<MultiplayerPoll> OnClosedOnServer;

		public Action<MultiplayerPoll> OnCancelledOnServer;

		public int AcceptedCount;

		public int RejectedCount;

		private readonly List<NetworkCommunicator> _participantsToVote;

		private readonly MultiplayerGameType _gameType;

		public Type PollType { get; }

		public bool IsOpen { get; private set; }

		private int OpenTime { get; }

		private int CloseTime { get; set; }

		public List<NetworkCommunicator> ParticipantsToVote => _participantsToVote;

		protected MultiplayerPoll(MultiplayerGameType gameType, Type pollType, List<NetworkCommunicator> participantsToVote)
		{
			_gameType = gameType;
			PollType = pollType;
			if (participantsToVote != null)
			{
				_participantsToVote = participantsToVote;
			}
			OpenTime = Environment.TickCount;
			CloseTime = 0;
			AcceptedCount = 0;
			RejectedCount = 0;
			IsOpen = true;
		}

		public virtual bool IsCancelled()
		{
			return false;
		}

		public virtual List<NetworkCommunicator> GetPollProgressReceivers()
		{
			return GameNetwork.NetworkPeers.ToList();
		}

		public void Tick()
		{
			if (!GameNetwork.IsServer)
			{
				return;
			}
			for (int num = _participantsToVote.Count - 1; num >= 0; num--)
			{
				if (!_participantsToVote[num].IsConnectionActive)
				{
					_participantsToVote.RemoveAt(num);
				}
			}
			if (IsCancelled())
			{
				OnCancelledOnServer?.Invoke(this);
			}
			else if (OpenTime < Environment.TickCount - 30000 || ResultsFinalized())
			{
				OnClosedOnServer?.Invoke(this);
			}
		}

		public void Close()
		{
			CloseTime = Environment.TickCount;
			IsOpen = false;
		}

		public void Cancel()
		{
			Close();
		}

		public bool ApplyVote(NetworkCommunicator peer, bool accepted)
		{
			bool result = false;
			if (_participantsToVote.Contains(peer))
			{
				if (accepted)
				{
					AcceptedCount++;
				}
				else
				{
					RejectedCount++;
				}
				_participantsToVote.Remove(peer);
				result = true;
			}
			return result;
		}

		public bool GotEnoughAcceptVotesToEnd()
		{
			bool flag = false;
			if (_gameType == MultiplayerGameType.Skirmish || _gameType == MultiplayerGameType.Captain)
			{
				return AcceptedByAllParticipants();
			}
			return AcceptedByMajority();
		}

		private bool GotEnoughRejectVotesToEnd()
		{
			bool flag = false;
			if (_gameType == MultiplayerGameType.Skirmish || _gameType == MultiplayerGameType.Captain)
			{
				return RejectedByAtLeastOneParticipant();
			}
			return RejectedByMajority();
		}

		private bool AcceptedByAllParticipants()
		{
			return AcceptedCount == GetPollParticipantCount();
		}

		private bool AcceptedByMajority()
		{
			return (float)AcceptedCount / (float)GetPollParticipantCount() > 0.50001f;
		}

		private bool RejectedByAtLeastOneParticipant()
		{
			return RejectedCount > 0;
		}

		private bool RejectedByMajority()
		{
			return (float)RejectedCount / (float)GetPollParticipantCount() > 0.50001f;
		}

		private int GetPollParticipantCount()
		{
			return _participantsToVote.Count + AcceptedCount + RejectedCount;
		}

		private bool ResultsFinalized()
		{
			if (!GotEnoughAcceptVotesToEnd() && !GotEnoughRejectVotesToEnd())
			{
				return _participantsToVote.Count == 0;
			}
			return true;
		}
	}

	private class KickPlayerPoll : MultiplayerPoll
	{
		public const int RequestLimitPerPeer = 2;

		private readonly Team _team;

		public NetworkCommunicator TargetPeer { get; }

		public KickPlayerPoll(MultiplayerGameType gameType, List<NetworkCommunicator> participantsToVote, NetworkCommunicator targetPeer, Team team)
			: base(gameType, Type.KickPlayer, participantsToVote)
		{
			TargetPeer = targetPeer;
			_team = team;
		}

		public override bool IsCancelled()
		{
			if (TargetPeer.IsConnectionActive)
			{
				return TargetPeer.QuitFromMission;
			}
			return true;
		}

		public override List<NetworkCommunicator> GetPollProgressReceivers()
		{
			List<NetworkCommunicator> list = new List<NetworkCommunicator>();
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				MissionPeer component = networkPeer.GetComponent<MissionPeer>();
				if (component != null && component.Team == _team)
				{
					list.Add(networkPeer);
				}
			}
			return list;
		}
	}

	private class BanPlayerPoll : MultiplayerPoll
	{
		public NetworkCommunicator TargetPeer { get; }

		public BanPlayerPoll(MultiplayerGameType gameType, List<NetworkCommunicator> participantsToVote, NetworkCommunicator targetPeer)
			: base(gameType, Type.BanPlayer, participantsToVote)
		{
			TargetPeer = targetPeer;
		}
	}

	private class ChangeGamePoll : MultiplayerPoll
	{
		public string GameType { get; }

		public string MapName { get; }

		public ChangeGamePoll(MultiplayerGameType currentGameType, List<NetworkCommunicator> participantsToVote, string gameType, string scene)
			: base(currentGameType, Type.ChangeGame, participantsToVote)
		{
			GameType = gameType;
			MapName = scene;
		}
	}

	public const int MinimumParticipantCountRequired = 3;

	public Action<MissionPeer, MissionPeer, bool> OnKickPollOpened;

	public Action<MultiplayerPollRejectReason> OnPollRejected;

	public Action<int, int> OnPollUpdated;

	public Action OnPollClosed;

	public Action OnPollCancelled;

	private MissionLobbyComponent _missionLobbyComponent;

	private MultiplayerGameNotificationsComponent _notificationsComponent;

	private MultiplayerPoll _ongoingPoll;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_notificationsComponent = base.Mission.GetMissionBehavior<MultiplayerGameNotificationsComponent>();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		_ongoingPoll?.Tick();
	}

	public void Vote(bool accepted)
	{
		if (GameNetwork.IsServer)
		{
			if (GameNetwork.MyPeer != null)
			{
				ApplyVote(GameNetwork.MyPeer, accepted);
			}
		}
		else if (_ongoingPoll != null && _ongoingPoll.IsOpen)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new PollResponse(accepted));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	private void ApplyVote(NetworkCommunicator peer, bool accepted)
	{
		if (_ongoingPoll != null && _ongoingPoll.ApplyVote(peer, accepted))
		{
			List<NetworkCommunicator> pollProgressReceivers = _ongoingPoll.GetPollProgressReceivers();
			int count = pollProgressReceivers.Count;
			for (int i = 0; i < count; i++)
			{
				GameNetwork.BeginModuleEventAsServer(pollProgressReceivers[i]);
				GameNetwork.WriteMessage(new PollProgress(_ongoingPoll.AcceptedCount, _ongoingPoll.RejectedCount));
				GameNetwork.EndModuleEventAsServer();
			}
			UpdatePollProgress(_ongoingPoll.AcceptedCount, _ongoingPoll.RejectedCount);
		}
	}

	private void RejectPollOnServer(NetworkCommunicator pollCreatorPeer, MultiplayerPollRejectReason rejectReason)
	{
		if (pollCreatorPeer.IsMine)
		{
			RejectPoll(rejectReason);
			return;
		}
		GameNetwork.BeginModuleEventAsServer(pollCreatorPeer);
		GameNetwork.WriteMessage(new PollRequestRejected((int)rejectReason));
		GameNetwork.EndModuleEventAsServer();
	}

	private void RejectPoll(MultiplayerPollRejectReason rejectReason)
	{
		if (!GameNetwork.IsDedicatedServer)
		{
			_notificationsComponent.PollRejected(rejectReason);
		}
		OnPollRejected?.Invoke(rejectReason);
	}

	private void UpdatePollProgress(int votesAccepted, int votesRejected)
	{
		OnPollUpdated?.Invoke(votesAccepted, votesRejected);
	}

	private void CancelPoll()
	{
		if (_ongoingPoll != null)
		{
			_ongoingPoll.Cancel();
			_ongoingPoll = null;
		}
		OnPollCancelled?.Invoke();
	}

	private void OnPollCancelledOnServer(MultiplayerPoll multiplayerPoll)
	{
		List<NetworkCommunicator> pollProgressReceivers = multiplayerPoll.GetPollProgressReceivers();
		int count = pollProgressReceivers.Count;
		for (int i = 0; i < count; i++)
		{
			GameNetwork.BeginModuleEventAsServer(pollProgressReceivers[i]);
			GameNetwork.WriteMessage(new PollCancelled());
			GameNetwork.EndModuleEventAsServer();
		}
		CancelPoll();
	}

	public void RequestKickPlayerPoll(NetworkCommunicator peer, bool banPlayer)
	{
		if (GameNetwork.IsServer)
		{
			if (GameNetwork.MyPeer != null)
			{
				OpenKickPlayerPollOnServer(GameNetwork.MyPeer, peer, banPlayer);
			}
		}
		else
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new KickPlayerPollRequested(peer, banPlayer));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	private void OpenKickPlayerPollOnServer(NetworkCommunicator pollCreatorPeer, NetworkCommunicator targetPeer, bool banPlayer)
	{
		if (_ongoingPoll == null)
		{
			bool num = pollCreatorPeer?.IsConnectionActive ?? false;
			bool flag = targetPeer?.IsConnectionActive ?? false;
			if (!(num && flag))
			{
				return;
			}
			if (targetPeer.IsSynchronized)
			{
				MissionPeer component = pollCreatorPeer.GetComponent<MissionPeer>();
				if (component == null)
				{
					return;
				}
				if (component.RequestedKickPollCount < 2)
				{
					List<NetworkCommunicator> list = new List<NetworkCommunicator>();
					foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
					{
						if (networkPeer != targetPeer && networkPeer.IsSynchronized)
						{
							MissionPeer component2 = networkPeer.GetComponent<MissionPeer>();
							if (component2 != null && component2.Team == component.Team)
							{
								list.Add(networkPeer);
							}
						}
					}
					int count = list.Count;
					if (count + 1 >= 3)
					{
						OpenKickPlayerPoll(targetPeer, pollCreatorPeer, banPlayer: false, list);
						for (int i = 0; i < count; i++)
						{
							GameNetwork.BeginModuleEventAsServer(_ongoingPoll.ParticipantsToVote[i]);
							GameNetwork.WriteMessage(new KickPlayerPollOpened(pollCreatorPeer, targetPeer, banPlayer));
							GameNetwork.EndModuleEventAsServer();
						}
						GameNetwork.BeginModuleEventAsServer(targetPeer);
						GameNetwork.WriteMessage(new KickPlayerPollOpened(pollCreatorPeer, targetPeer, banPlayer));
						GameNetwork.EndModuleEventAsServer();
						component.IncrementRequestedKickPollCount();
					}
					else
					{
						RejectPollOnServer(pollCreatorPeer, MultiplayerPollRejectReason.NotEnoughPlayersToOpenPoll);
					}
				}
				else
				{
					RejectPollOnServer(pollCreatorPeer, MultiplayerPollRejectReason.TooManyPollRequests);
				}
			}
			else
			{
				RejectPollOnServer(pollCreatorPeer, MultiplayerPollRejectReason.KickPollTargetNotSynced);
			}
		}
		else
		{
			RejectPollOnServer(pollCreatorPeer, MultiplayerPollRejectReason.HasOngoingPoll);
		}
	}

	private void OpenKickPlayerPoll(NetworkCommunicator targetPeer, NetworkCommunicator pollCreatorPeer, bool banPlayer, List<NetworkCommunicator> participantsToVote)
	{
		MissionPeer component = pollCreatorPeer.GetComponent<MissionPeer>();
		MissionPeer component2 = targetPeer.GetComponent<MissionPeer>();
		_ongoingPoll = new KickPlayerPoll(_missionLobbyComponent.MissionType, participantsToVote, targetPeer, component.Team);
		if (GameNetwork.IsServer)
		{
			MultiplayerPoll ongoingPoll = _ongoingPoll;
			ongoingPoll.OnClosedOnServer = (Action<MultiplayerPoll>)Delegate.Combine(ongoingPoll.OnClosedOnServer, new Action<MultiplayerPoll>(OnKickPlayerPollClosedOnServer));
			MultiplayerPoll ongoingPoll2 = _ongoingPoll;
			ongoingPoll2.OnCancelledOnServer = (Action<MultiplayerPoll>)Delegate.Combine(ongoingPoll2.OnCancelledOnServer, new Action<MultiplayerPoll>(OnPollCancelledOnServer));
		}
		OnKickPollOpened?.Invoke(component, component2, banPlayer);
		if (GameNetwork.MyPeer == pollCreatorPeer)
		{
			Vote(accepted: true);
		}
	}

	private void OnKickPlayerPollClosedOnServer(MultiplayerPoll multiplayerPoll)
	{
		KickPlayerPoll kickPlayerPoll = multiplayerPoll as KickPlayerPoll;
		bool flag = kickPlayerPoll.GotEnoughAcceptVotesToEnd();
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new KickPlayerPollClosed(kickPlayerPoll.TargetPeer, flag));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		CloseKickPlayerPoll(flag, kickPlayerPoll.TargetPeer);
		if (flag)
		{
			DisconnectInfo disconnectInfo = kickPlayerPoll.TargetPeer.PlayerConnectionInfo.GetParameter<DisconnectInfo>("DisconnectInfo") ?? new DisconnectInfo();
			disconnectInfo.Type = DisconnectType.KickedByPoll;
			kickPlayerPoll.TargetPeer.PlayerConnectionInfo.AddParameter("DisconnectInfo", disconnectInfo);
			GameNetwork.AddNetworkPeerToDisconnectAsServer(kickPlayerPoll.TargetPeer);
		}
	}

	private void CloseKickPlayerPoll(bool accepted, NetworkCommunicator targetPeer)
	{
		if (_ongoingPoll != null)
		{
			_ongoingPoll.Close();
			_ongoingPoll = null;
		}
		OnPollClosed?.Invoke();
		if (!GameNetwork.IsDedicatedServer && accepted && !targetPeer.IsMine)
		{
			_notificationsComponent.PlayerKicked(targetPeer);
		}
	}

	private void OnBanPlayerPollClosedOnServer(MultiplayerPoll multiplayerPoll)
	{
		MissionPeer component = (multiplayerPoll as BanPlayerPoll).TargetPeer.GetComponent<MissionPeer>();
		if (component != null)
		{
			NetworkCommunicator networkPeer = component.GetNetworkPeer();
			DisconnectInfo disconnectInfo = networkPeer.PlayerConnectionInfo.GetParameter<DisconnectInfo>("DisconnectInfo") ?? new DisconnectInfo();
			disconnectInfo.Type = DisconnectType.BannedByPoll;
			networkPeer.PlayerConnectionInfo.AddParameter("DisconnectInfo", disconnectInfo);
			GameNetwork.AddNetworkPeerToDisconnectAsServer(networkPeer);
			if (GameNetwork.IsServer)
			{
				CustomGameBannedPlayerManager.AddBannedPlayer(component.Peer.Id, Environment.TickCount + 600000);
			}
			if (GameNetwork.IsDedicatedServer)
			{
				throw new NotImplementedException();
			}
			NetworkMain.GameClient.KickPlayer(component.Peer.Id, banPlayer: true);
		}
	}

	private void StartChangeGamePollOnServer(NetworkCommunicator pollCreatorPeer, string gameType, string scene)
	{
		if (_ongoingPoll == null)
		{
			List<NetworkCommunicator> participantsToVote = GameNetwork.NetworkPeers.ToList();
			_ongoingPoll = new ChangeGamePoll(_missionLobbyComponent.MissionType, participantsToVote, gameType, scene);
			if (GameNetwork.IsServer)
			{
				MultiplayerPoll ongoingPoll = _ongoingPoll;
				ongoingPoll.OnClosedOnServer = (Action<MultiplayerPoll>)Delegate.Combine(ongoingPoll.OnClosedOnServer, new Action<MultiplayerPoll>(OnChangeGamePollClosedOnServer));
			}
			if (!GameNetwork.IsDedicatedServer)
			{
				ShowChangeGamePoll(gameType, scene);
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new NetworkMessages.FromServer.ChangeGamePoll(gameType, scene));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		else
		{
			RejectPollOnServer(pollCreatorPeer, MultiplayerPollRejectReason.HasOngoingPoll);
		}
	}

	private void StartChangeGamePoll(string gameType, string map)
	{
		if (GameNetwork.IsServer)
		{
			if (GameNetwork.MyPeer != null)
			{
				StartChangeGamePollOnServer(GameNetwork.MyPeer, gameType, map);
			}
		}
		else
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new NetworkMessages.FromClient.ChangeGamePoll(gameType, map));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	private void ShowChangeGamePoll(string gameType, string scene)
	{
	}

	private void OnChangeGamePollClosedOnServer(MultiplayerPoll multiplayerPoll)
	{
		ChangeGamePoll changeGamePoll = multiplayerPoll as ChangeGamePoll;
		MultiplayerOptions.OptionType.GameType.SetValue(changeGamePoll.GameType);
		MultiplayerOptions.Instance.OnGameTypeChanged();
		MultiplayerOptions.OptionType.Map.SetValue(changeGamePoll.MapName);
		_missionLobbyComponent.SetStateEndingAsServer();
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<PollRequestRejected>(HandleServerEventPollRequestRejected);
			registerer.RegisterBaseHandler<PollProgress>(HandleServerEventUpdatePollProgress);
			registerer.RegisterBaseHandler<PollCancelled>(HandleServerEventPollCancelled);
			registerer.RegisterBaseHandler<KickPlayerPollOpened>(HandleServerEventKickPlayerPollOpened);
			registerer.RegisterBaseHandler<KickPlayerPollClosed>(HandleServerEventKickPlayerPollClosed);
			registerer.RegisterBaseHandler<NetworkMessages.FromServer.ChangeGamePoll>(HandleServerEventChangeGamePoll);
		}
		else if (GameNetwork.IsServer)
		{
			registerer.RegisterBaseHandler<PollResponse>(HandleClientEventPollResponse);
			registerer.RegisterBaseHandler<KickPlayerPollRequested>(HandleClientEventKickPlayerPollRequested);
			registerer.RegisterBaseHandler<NetworkMessages.FromClient.ChangeGamePoll>(HandleClientEventChangeGamePoll);
		}
	}

	private bool HandleClientEventChangeGamePoll(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		NetworkMessages.FromClient.ChangeGamePoll changeGamePoll = (NetworkMessages.FromClient.ChangeGamePoll)baseMessage;
		StartChangeGamePollOnServer(peer, changeGamePoll.GameType, changeGamePoll.Map);
		return true;
	}

	private bool HandleClientEventKickPlayerPollRequested(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		KickPlayerPollRequested kickPlayerPollRequested = (KickPlayerPollRequested)baseMessage;
		OpenKickPlayerPollOnServer(peer, kickPlayerPollRequested.PlayerPeer, kickPlayerPollRequested.BanPlayer);
		return true;
	}

	private bool HandleClientEventPollResponse(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		PollResponse pollResponse = (PollResponse)baseMessage;
		ApplyVote(peer, pollResponse.Accepted);
		return true;
	}

	private void HandleServerEventChangeGamePoll(GameNetworkMessage baseMessage)
	{
		NetworkMessages.FromServer.ChangeGamePoll changeGamePoll = (NetworkMessages.FromServer.ChangeGamePoll)baseMessage;
		ShowChangeGamePoll(changeGamePoll.GameType, changeGamePoll.Map);
	}

	private void HandleServerEventKickPlayerPollOpened(GameNetworkMessage baseMessage)
	{
		KickPlayerPollOpened kickPlayerPollOpened = (KickPlayerPollOpened)baseMessage;
		OpenKickPlayerPoll(kickPlayerPollOpened.PlayerPeer, kickPlayerPollOpened.InitiatorPeer, kickPlayerPollOpened.BanPlayer, null);
	}

	private void HandleServerEventUpdatePollProgress(GameNetworkMessage baseMessage)
	{
		PollProgress pollProgress = (PollProgress)baseMessage;
		UpdatePollProgress(pollProgress.VotesAccepted, pollProgress.VotesRejected);
	}

	private void HandleServerEventPollCancelled(GameNetworkMessage baseMessage)
	{
		CancelPoll();
	}

	private void HandleServerEventKickPlayerPollClosed(GameNetworkMessage baseMessage)
	{
		KickPlayerPollClosed kickPlayerPollClosed = (KickPlayerPollClosed)baseMessage;
		CloseKickPlayerPoll(kickPlayerPollClosed.Accepted, kickPlayerPollClosed.PlayerPeer);
	}

	private void HandleServerEventPollRequestRejected(GameNetworkMessage baseMessage)
	{
		PollRequestRejected pollRequestRejected = (PollRequestRejected)baseMessage;
		RejectPoll((MultiplayerPollRejectReason)pollRequestRejected.Reason);
	}
}
