using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerTeamSelectComponent : MissionNetwork
{
	public delegate void OnSelectingTeamDelegate(List<Team> disableTeams);

	private MissionNetworkComponent _missionNetworkComponent;

	private MissionMultiplayerGameModeBase _gameModeServer;

	private HashSet<PlayerId> _platformFriends;

	private Dictionary<Team, IEnumerable<VirtualPlayer>> _friendsPerTeam;

	public bool TeamSelectionEnabled { get; private set; }

	public event OnSelectingTeamDelegate OnSelectingTeam;

	public event Action OnMyTeamChange;

	public event Action OnUpdateTeams;

	public event Action OnUpdateFriendsPerTeam;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionNetworkComponent = base.Mission.GetMissionBehavior<MissionNetworkComponent>();
		_gameModeServer = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		if (BannerlordNetwork.LobbyMissionType == LobbyMissionType.Matchmaker)
		{
			TeamSelectionEnabled = false;
		}
		else
		{
			TeamSelectionEnabled = true;
		}
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsServer)
		{
			registerer.RegisterBaseHandler<TeamChange>(HandleClientEventTeamChange);
		}
	}

	private void OnMyClientSynchronized()
	{
		base.Mission.GetMissionBehavior<MissionNetworkComponent>().OnMyClientSynchronized -= OnMyClientSynchronized;
		if (Mission.Current.GetMissionBehavior<MissionLobbyComponent>().CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending && GameNetwork.MyPeer.GetComponent<MissionPeer>().Team == null)
		{
			SelectTeam();
		}
	}

	public override void AfterStart()
	{
		_platformFriends = new HashSet<PlayerId>();
		foreach (PlayerId allFriendsInAllPlatform in FriendListService.GetAllFriendsInAllPlatforms())
		{
			_platformFriends.Add(allFriendsInAllPlatform);
		}
		_friendsPerTeam = new Dictionary<Team, IEnumerable<VirtualPlayer>>();
		MissionPeer.OnTeamChanged += UpdateTeams;
		if (GameNetwork.IsClient)
		{
			MissionNetworkComponent missionBehavior = base.Mission.GetMissionBehavior<MissionNetworkComponent>();
			if (TeamSelectionEnabled)
			{
				missionBehavior.OnMyClientSynchronized += OnMyClientSynchronized;
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		MissionPeer.OnTeamChanged -= UpdateTeams;
		this.OnMyTeamChange = null;
		base.OnRemoveBehavior();
	}

	private bool HandleClientEventTeamChange(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		TeamChange teamChange = (TeamChange)baseMessage;
		if (TeamSelectionEnabled)
		{
			if (teamChange.AutoAssign)
			{
				AutoAssignTeam(peer);
			}
			else
			{
				Team teamFromTeamIndex = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(teamChange.TeamIndex);
				ChangeTeamServer(peer, teamFromTeamIndex);
			}
		}
		return true;
	}

	public void SelectTeam()
	{
		if (this.OnSelectingTeam != null)
		{
			List<Team> disabledTeams = GetDisabledTeams();
			this.OnSelectingTeam(disabledTeams);
		}
	}

	public void UpdateTeams(NetworkCommunicator peer, Team oldTeam, Team newTeam)
	{
		if (this.OnUpdateTeams != null)
		{
			this.OnUpdateTeams();
		}
		if (GameNetwork.IsMyPeerReady)
		{
			CacheFriendsForTeams();
		}
		if (newTeam.Side != BattleSideEnum.None)
		{
			MissionPeer component = peer.GetComponent<MissionPeer>();
			component.SelectedTroopIndex = 0;
			component.NextSelectedTroopIndex = 0;
			component.OverrideCultureWithTeamCulture();
		}
	}

	public List<Team> GetDisabledTeams()
	{
		List<Team> list = new List<Team>();
		if (MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue() == 0)
		{
			return list;
		}
		Team myTeam = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
		Team[] array = (from q in base.Mission.Teams
			where q != base.Mission.SpectatorTeam
			orderby (myTeam != null) ? ((q != myTeam) ? GetPlayerCountForTeam(q) : (GetPlayerCountForTeam(q) - 1)) : GetPlayerCountForTeam(q)
			select q).ToArray();
		Team[] array2 = array;
		foreach (Team team in array2)
		{
			int num2 = GetPlayerCountForTeam(team);
			int num3 = GetPlayerCountForTeam(array[0]);
			if (myTeam == team)
			{
				num2--;
			}
			if (myTeam == array[0])
			{
				num3--;
			}
			if (num2 - num3 >= MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue())
			{
				list.Add(team);
			}
		}
		return list;
	}

	public void ChangeTeamServer(NetworkCommunicator networkPeer, Team team)
	{
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		Team team2 = component.Team;
		if (team2 != null && team2 != base.Mission.SpectatorTeam && team2 != team && component.ControlledAgent != null)
		{
			Blow b = new Blow(component.ControlledAgent.Index);
			b.DamageType = DamageTypes.Invalid;
			b.BaseMagnitude = 10000f;
			b.GlobalPosition = component.ControlledAgent.Position;
			b.DamagedPercentage = 1f;
			component.ControlledAgent.Die(b, Agent.KillInfo.TeamSwitch);
		}
		component.Team = team;
		BasicCultureObject culture = (component.Team.IsAttacker ? MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue()) : MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue()));
		component.Culture = culture;
		if (team != team2)
		{
			if (component.HasSpawnedAgentVisuals)
			{
				component.HasSpawnedAgentVisuals = false;
				MBDebug.Print("HasSpawnedAgentVisuals = false for peer: " + component.Name + " because he just changed his team");
				component.SpawnCountThisRound = 0;
				Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().RemoveAgentVisuals(component, sync: true);
				if (GameNetwork.IsServerOrRecorder)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(component.GetNetworkPeer()));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
				component.HasSpawnedAgentVisuals = false;
			}
			if (!_gameModeServer.IsGameModeHidingAllAgentVisuals && !networkPeer.IsServerPeer)
			{
				_missionNetworkComponent?.OnPeerSelectedTeam(component);
			}
			_gameModeServer.OnPeerChangedTeam(networkPeer, team2, team);
			component.SpawnTimer.Reset(Mission.Current.CurrentTime, 0.1f);
			component.WantsToSpawnAsBot = false;
			component.HasSpawnTimerExpired = false;
		}
		UpdateTeams(networkPeer, team2, team);
	}

	public void ChangeTeam(Team team)
	{
		if (team == GameNetwork.MyPeer.GetComponent<MissionPeer>().Team)
		{
			return;
		}
		if (GameNetwork.IsServer)
		{
			Mission.Current.PlayerTeam = team;
			ChangeTeamServer(GameNetwork.MyPeer, team);
		}
		else
		{
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				networkPeer.GetComponent<MissionPeer>()?.ClearAllVisuals();
			}
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new TeamChange(autoAssign: false, team.TeamIndex));
			GameNetwork.EndModuleEventAsClient();
		}
		if (this.OnMyTeamChange != null)
		{
			this.OnMyTeamChange();
		}
	}

	public int GetPlayerCountForTeam(Team team)
	{
		int num = 0;
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component?.Team != null && component.Team == team)
			{
				num++;
			}
		}
		return num;
	}

	private void CacheFriendsForTeams()
	{
		_friendsPerTeam.Clear();
		if (_platformFriends.Count <= 0)
		{
			return;
		}
		List<MissionPeer> list = new List<MissionPeer>();
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component != null && _platformFriends.Contains(networkPeer.VirtualPlayer.Id))
			{
				list.Add(component);
			}
		}
		foreach (Team team in base.Mission.Teams)
		{
			if (team != null)
			{
				_friendsPerTeam.Add(team, from x in list
					where x.Team == team
					select x.Peer);
			}
		}
		if (this.OnUpdateFriendsPerTeam != null)
		{
			this.OnUpdateFriendsPerTeam();
		}
	}

	public IEnumerable<VirtualPlayer> GetFriendsForTeam(Team team)
	{
		if (_friendsPerTeam.ContainsKey(team))
		{
			return _friendsPerTeam[team];
		}
		return new List<VirtualPlayer>();
	}

	public void BalanceTeams()
	{
		if (MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue() == 0)
		{
			return;
		}
		int num = GetPlayerCountForTeam(Mission.Current.AttackerTeam);
		int i;
		for (i = GetPlayerCountForTeam(Mission.Current.DefenderTeam); num > i + MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue(); i++)
		{
			MissionPeer missionPeer = null;
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				if (networkPeer.IsSynchronized)
				{
					MissionPeer component = networkPeer.GetComponent<MissionPeer>();
					if (component?.Team != null && component.Team == base.Mission.AttackerTeam && (missionPeer == null || component.JoinTime >= missionPeer.JoinTime))
					{
						missionPeer = component;
					}
				}
			}
			ChangeTeamServer(missionPeer.GetNetworkPeer(), Mission.Current.DefenderTeam);
			num--;
		}
		while (i > num + MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue())
		{
			MissionPeer missionPeer2 = null;
			foreach (NetworkCommunicator networkPeer2 in GameNetwork.NetworkPeers)
			{
				if (networkPeer2.IsSynchronized)
				{
					MissionPeer component2 = networkPeer2.GetComponent<MissionPeer>();
					if (component2?.Team != null && component2.Team == base.Mission.DefenderTeam && (missionPeer2 == null || component2.JoinTime >= missionPeer2.JoinTime))
					{
						missionPeer2 = component2;
					}
				}
			}
			ChangeTeamServer(missionPeer2.GetNetworkPeer(), Mission.Current.AttackerTeam);
			num++;
			i--;
		}
	}

	public void AutoAssignTeam(NetworkCommunicator peer)
	{
		if (GameNetwork.IsServer)
		{
			List<Team> disabledTeams = GetDisabledTeams();
			List<Team> list = base.Mission.Teams.Where((Team x) => !disabledTeams.Contains(x) && x.Side != BattleSideEnum.None).ToList();
			Team team;
			if (list.Count > 1)
			{
				int[] array = new int[list.Count];
				foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
				{
					MissionPeer component = networkPeer.GetComponent<MissionPeer>();
					if (component?.Team == null)
					{
						continue;
					}
					for (int num = 0; num < list.Count; num++)
					{
						if (component.Team == list[num])
						{
							array[num]++;
						}
					}
				}
				int num2 = -1;
				int num3 = -1;
				for (int num4 = 0; num4 < array.Length; num4++)
				{
					if (num3 < 0 || array[num4] < num2)
					{
						num3 = num4;
						num2 = array[num4];
					}
				}
				team = list[num3];
			}
			else
			{
				team = list[0];
			}
			if (!peer.IsMine)
			{
				ChangeTeamServer(peer, team);
			}
			else
			{
				ChangeTeam(team);
			}
		}
		else
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new TeamChange(autoAssign: true, -1));
			GameNetwork.EndModuleEventAsClient();
			if (this.OnMyTeamChange != null)
			{
				this.OnMyTeamChange();
			}
		}
	}
}
