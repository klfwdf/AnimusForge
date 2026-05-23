using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MissionPeer : PeerComponent
{
	public delegate void OnUpdateEquipmentSetIndexEventDelegate(MissionPeer lobbyPeer, int equipmentSetIndex);

	public delegate void OnPerkUpdateEventDelegate(MissionPeer peer);

	public delegate void OnTeamChangedDelegate(NetworkCommunicator peer, Team previousTeam, Team newTeam);

	public delegate void OnCultureChangedDelegate(BasicCultureObject newCulture);

	public delegate void OnPlayerKilledDelegate(MissionPeer killerPeer, MissionPeer killedPeer);

	public const int NumberOfPerkLists = 3;

	public const int MaxNumberOfTroopTypesPerCulture = 16;

	private const float InactivityKickInSeconds = 180f;

	private const float InactivityWarnInSeconds = 120f;

	public const int MinKDACount = -1000;

	public const int MaxKDACount = 100000;

	public const int MinScore = -1000000;

	public const int MaxScore = 1000000;

	public const int MinSpawnTimer = 3;

	public int CaptainBeingDetachedThreshold = 125;

	private List<PeerVisualsHolder> _visuals = new List<PeerVisualsHolder>();

	private Dictionary<MissionPeer, int> _numberOfTimesPeerKilledPerPeer = new Dictionary<MissionPeer, int>();

	private MissionTime _lastActiveTime = MissionTime.Zero;

	private (Agent.MovementControlFlag, Vec2, Vec3) _previousActivityStatus;

	private bool _inactiveWarningGiven;

	private int _selectedTroopIndex;

	private Agent _followedAgent;

	private Team _team;

	private BasicCultureObject _culture;

	private Formation _controlledFormation;

	private MissionRepresentativeBase _representative;

	private readonly MBList<int[]> _perks;

	private int _killCount;

	private int _assistCount;

	private int _deathCount;

	private int _score;

	private (int, MBList<MPPerkObject>) _selectedPerks;

	private int _botsUnderControlAlive;

	public DateTime JoinTime { get; internal set; }

	public bool EquipmentUpdatingExpired { get; set; }

	public bool TeamInitialPerkInfoReady { get; private set; }

	public bool HasSpawnedAgentVisuals { get; set; }

	public int SelectedTroopIndex
	{
		get
		{
			return _selectedTroopIndex;
		}
		set
		{
			if (_selectedTroopIndex != value)
			{
				_selectedTroopIndex = value;
				ResetSelectedPerks();
				MissionPeer.OnEquipmentIndexRefreshed?.Invoke(this, value);
			}
		}
	}

	public int NextSelectedTroopIndex { get; set; }

	public MissionRepresentativeBase Representative
	{
		get
		{
			if (_representative == null)
			{
				_representative = base.Peer.GetComponent<MissionRepresentativeBase>();
			}
			return _representative;
		}
	}

	public MBReadOnlyList<int[]> Perks => _perks;

	public string DisplayedName
	{
		get
		{
			if (GameNetwork.IsDedicatedServer)
			{
				return base.Name;
			}
			if (NetworkMain.CommunityClient.IsInGame)
			{
				return base.Name;
			}
			if (NetworkMain.GameClient.HasUserGeneratedContentPrivilege && (NetworkMain.GameClient.IsKnownPlayer(base.Peer.Id) || !BannerlordConfig.EnableGenericNames))
			{
				return base.Peer?.UserName ?? "";
			}
			if (Culture == null || MultiplayerClassDivisions.GetMPHeroClassForPeer(this) == null)
			{
				return new TextObject("{=RN6zHak0}Player").ToString();
			}
			return MultiplayerClassDivisions.GetMPHeroClassForPeer(this).TroopName.ToString();
		}
	}

	public MBReadOnlyList<MPPerkObject> SelectedPerks
	{
		get
		{
			if (SelectedTroopIndex < 0 || Team == null || Team.Side == BattleSideEnum.None)
			{
				return new MBList<MPPerkObject>();
			}
			if ((_selectedPerks.Item2 == null || SelectedTroopIndex != _selectedPerks.Item1 || _selectedPerks.Item2.Count < 3) && !RefreshSelectedPerks())
			{
				return new MBReadOnlyList<MPPerkObject>();
			}
			return _selectedPerks.Item2;
		}
	}

	public Timer SpawnTimer { get; internal set; }

	public bool HasSpawnTimerExpired { get; set; }

	public BasicCultureObject VotedForBan { get; private set; }

	public BasicCultureObject VotedForSelection { get; private set; }

	public bool WantsToSpawnAsBot { get; set; }

	public int SpawnCountThisRound { get; set; }

	public int RequestedKickPollCount { get; private set; }

	public int KillCount
	{
		get
		{
			return _killCount;
		}
		internal set
		{
			_killCount = MBMath.ClampInt(value, -1000, 100000);
		}
	}

	public int AssistCount
	{
		get
		{
			return _assistCount;
		}
		internal set
		{
			_assistCount = MBMath.ClampInt(value, -1000, 100000);
		}
	}

	public int DeathCount
	{
		get
		{
			return _deathCount;
		}
		internal set
		{
			_deathCount = MBMath.ClampInt(value, -1000, 100000);
		}
	}

	public int Score
	{
		get
		{
			return _score;
		}
		internal set
		{
			_score = MBMath.ClampInt(value, -1000000, 1000000);
		}
	}

	public int BotsUnderControlAlive
	{
		get
		{
			return _botsUnderControlAlive;
		}
		set
		{
			if (_botsUnderControlAlive != value)
			{
				_botsUnderControlAlive = value;
				MPPerkObject.GetPerkHandler(this)?.OnEvent(MPPerkCondition.PerkEventFlags.AliveBotCountChange);
			}
		}
	}

	public int BotsUnderControlTotal { get; internal set; }

	public bool IsControlledAgentActive
	{
		get
		{
			if (ControlledAgent != null)
			{
				return ControlledAgent.IsActive();
			}
			return false;
		}
	}

	public Agent ControlledAgent
	{
		get
		{
			return this.GetNetworkPeer().ControlledAgent;
		}
		set
		{
			NetworkCommunicator networkPeer = this.GetNetworkPeer();
			if (networkPeer.ControlledAgent != value)
			{
				ResetSelectedPerks();
				Agent controlledAgent = networkPeer.ControlledAgent;
				networkPeer.ControlledAgent = value;
				if (controlledAgent != null && controlledAgent.MissionPeer == this && controlledAgent.IsActive())
				{
					controlledAgent.MissionPeer = null;
				}
				if (networkPeer.ControlledAgent != null && networkPeer.ControlledAgent.MissionPeer != this)
				{
					networkPeer.ControlledAgent.MissionPeer = this;
				}
				networkPeer.VirtualPlayer.GetComponent<MissionRepresentativeBase>()?.SetAgent(value);
				if (value != null)
				{
					MPPerkObject.GetPerkHandler(this)?.OnEvent(value, MPPerkCondition.PerkEventFlags.PeerControlledAgentChange);
				}
			}
		}
	}

	public Agent FollowedAgent
	{
		get
		{
			return _followedAgent;
		}
		set
		{
			if (_followedAgent != value)
			{
				_followedAgent = value;
				if (GameNetwork.IsClient)
				{
					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new SetFollowedAgent(_followedAgent?.Index ?? (-1)));
					GameNetwork.EndModuleEventAsClient();
				}
			}
		}
	}

	public Team Team
	{
		get
		{
			return _team;
		}
		set
		{
			if (_team == value)
			{
				return;
			}
			if (MissionPeer.OnPreTeamChanged != null)
			{
				MissionPeer.OnPreTeamChanged(this.GetNetworkPeer(), _team, value);
			}
			Team team = _team;
			_team = value;
			Debug.Print("Set the team to: " + (_team?.Side.ToString() ?? "null") + ", for peer: " + base.Name);
			_controlledFormation = null;
			if (_team != null)
			{
				if (GameNetwork.IsServer)
				{
					MBAPI.IMBPeer.SetTeam(base.Peer.Index, _team.MBTeam.Index);
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SetPeerTeam(this.GetNetworkPeer(), _team.TeamIndex));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
				}
				if (MissionPeer.OnTeamChanged != null)
				{
					MissionPeer.OnTeamChanged(this.GetNetworkPeer(), team, _team);
				}
			}
			else if (GameNetwork.IsServer)
			{
				MBAPI.IMBPeer.SetTeam(base.Peer.Index, -1);
			}
		}
	}

	public BasicCultureObject Culture
	{
		get
		{
			return _culture;
		}
		set
		{
			_ = _culture;
			_culture = value;
			if (GameNetwork.IsServerOrRecorder)
			{
				TeamInitialPerkInfoReady = base.Peer.IsMine;
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new ChangeCulture(this, _culture));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
			if (this.OnCultureChanged != null)
			{
				this.OnCultureChanged(_culture);
			}
		}
	}

	public Formation ControlledFormation
	{
		get
		{
			return _controlledFormation;
		}
		set
		{
			if (_controlledFormation != value)
			{
				_controlledFormation = value;
			}
		}
	}

	public bool IsAgentAliveForChatting
	{
		get
		{
			MissionPeer component = GetComponent<MissionPeer>();
			if (component == null)
			{
				return false;
			}
			if (!IsControlledAgentActive)
			{
				return component.HasSpawnedAgentVisuals;
			}
			return true;
		}
	}

	public bool IsMutedFromPlatform { get; private set; }

	public bool IsMuted { get; private set; }

	public bool IsMutedFromGameOrPlatform
	{
		get
		{
			if (!IsMutedFromPlatform)
			{
				return IsMuted;
			}
			return true;
		}
	}

	public static event OnUpdateEquipmentSetIndexEventDelegate OnEquipmentIndexRefreshed;

	public static event OnPerkUpdateEventDelegate OnPerkSelectionUpdated;

	public static event OnTeamChangedDelegate OnPreTeamChanged;

	public static event OnTeamChangedDelegate OnTeamChanged;

	private event OnCultureChangedDelegate OnCultureChanged;

	public static event OnPlayerKilledDelegate OnPlayerKilled;

	public MissionPeer()
	{
		SpawnTimer = new Timer(Mission.Current.CurrentTime, 3f, autoReset: false);
		_selectedPerks = (0, null);
		_perks = new MBList<int[]>();
		for (int i = 0; i < 16; i++)
		{
			int[] item = new int[3];
			_perks.Add(item);
		}
	}

	public void SetMutedFromPlatform(bool isMuted)
	{
		IsMutedFromPlatform = isMuted;
	}

	public void SetMuted(bool isMuted)
	{
		IsMuted = isMuted;
	}

	public void ResetRequestedKickPollCount()
	{
		RequestedKickPollCount = 0;
	}

	public void IncrementRequestedKickPollCount()
	{
		RequestedKickPollCount++;
	}

	public int GetSelectedPerkIndexWithPerkListIndex(int troopIndex, int perkListIndex)
	{
		return _perks[troopIndex][perkListIndex];
	}

	public bool SelectPerk(int perkListIndex, int perkIndex, int enforcedSelectedTroopIndex = -1)
	{
		if (SelectedTroopIndex >= 0 && enforcedSelectedTroopIndex >= 0 && SelectedTroopIndex != enforcedSelectedTroopIndex)
		{
			Debug.Print("SelectedTroopIndex < 0 || enforcedSelectedTroopIndex < 0 || SelectedTroopIndex == enforcedSelectedTroopIndex", 0, Debug.DebugColor.White, 17179869184uL);
			Debug.Print($"SelectedTroopIndex: {SelectedTroopIndex} enforcedSelectedTroopIndex: {enforcedSelectedTroopIndex}", 0, Debug.DebugColor.White, 17179869184uL);
		}
		int num = ((enforcedSelectedTroopIndex >= 0) ? enforcedSelectedTroopIndex : SelectedTroopIndex);
		if (perkIndex != _perks[num][perkListIndex])
		{
			_perks[num][perkListIndex] = perkIndex;
			if (this.GetNetworkPeer().IsMine)
			{
				List<MultiplayerClassDivisions.MPHeroClass> list = MultiplayerClassDivisions.GetMPHeroClasses(Culture).ToList();
				int count = list.Count;
				for (int i = 0; i < count; i++)
				{
					if (num == i)
					{
						MultiplayerClassDivisions.MPHeroClass currentHeroClass = list[i];
						List<MPPerkSelectionManager.MPPerkSelection> list2 = new List<MPPerkSelectionManager.MPPerkSelection>();
						for (int j = 0; j < 3; j++)
						{
							list2.Add(new MPPerkSelectionManager.MPPerkSelection(_perks[i][j], j));
						}
						MPPerkSelectionManager.Instance.SetSelectionsForHeroClassTemporarily(currentHeroClass, list2);
						break;
					}
				}
			}
			if (num == SelectedTroopIndex)
			{
				ResetSelectedPerks();
			}
			MissionPeer.OnPerkSelectionUpdated?.Invoke(this);
			return true;
		}
		return false;
	}

	public void HandleVoteChange(CultureVoteTypes voteType, BasicCultureObject culture)
	{
		switch (voteType)
		{
		case CultureVoteTypes.Ban:
			VotedForBan = culture;
			break;
		case CultureVoteTypes.Select:
			VotedForSelection = culture;
			break;
		}
		if (GameNetwork.IsServer)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new CultureVoteServer(this.GetNetworkPeer(), voteType, (voteType == CultureVoteTypes.Ban) ? VotedForBan : VotedForSelection));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (base.IsMine)
		{
			MPPerkSelectionManager.Instance.TryToApplyAndSavePendingChanges();
		}
		ResetKillRegistry();
		if (HasSpawnedAgentVisuals && Mission.Current != null)
		{
			Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>()?.RemoveAgentVisuals(this);
			HasSpawnedAgentVisuals = false;
			OnCultureChanged -= CultureChanged;
		}
	}

	public override void OnInitialize()
	{
		base.OnInitialize();
		OnCultureChanged += CultureChanged;
	}

	public int GetAmountOfAgentVisualsForPeer()
	{
		return _visuals.Count((PeerVisualsHolder v) => v != null);
	}

	public PeerVisualsHolder GetVisuals(int visualIndex)
	{
		if (_visuals.Count <= 0)
		{
			return null;
		}
		return _visuals[visualIndex];
	}

	public void ClearVisuals(int visualIndex)
	{
		if (visualIndex >= _visuals.Count || _visuals[visualIndex] == null)
		{
			return;
		}
		if (!GameNetwork.IsDedicatedServer)
		{
			MBAgentVisuals visuals = _visuals[visualIndex].AgentVisuals.GetVisuals();
			visuals.ClearVisualComponents(removeSkeleton: true);
			visuals.ClearAllWeaponMeshes();
			visuals.Reset();
			if (_visuals[visualIndex].MountAgentVisuals != null)
			{
				MBAgentVisuals visuals2 = _visuals[visualIndex].MountAgentVisuals.GetVisuals();
				visuals2.ClearVisualComponents(removeSkeleton: true);
				visuals2.ClearAllWeaponMeshes();
				visuals2.Reset();
			}
		}
		_visuals[visualIndex] = null;
	}

	public void ClearAllVisuals(bool freeResources = false)
	{
		if (_visuals == null)
		{
			return;
		}
		for (int num = _visuals.Count - 1; num >= 0; num--)
		{
			if (_visuals[num] != null)
			{
				ClearVisuals(num);
			}
		}
		if (freeResources)
		{
			_visuals = null;
		}
	}

	public void OnVisualsSpawned(PeerVisualsHolder visualsHolder, int visualIndex)
	{
		if (visualIndex >= _visuals.Count)
		{
			int num = visualIndex - _visuals.Count;
			for (int i = 0; i < num + 1; i++)
			{
				_visuals.Add(null);
			}
		}
		_visuals[visualIndex] = visualsHolder;
	}

	public IEnumerable<IAgentVisual> GetAllAgentVisualsForPeer()
	{
		int count = GetAmountOfAgentVisualsForPeer();
		for (int i = 0; i < count; i++)
		{
			yield return GetVisuals(i).AgentVisuals;
		}
	}

	public IAgentVisual GetAgentVisualForPeer(int visualsIndex)
	{
		IAgentVisual mountAgentVisuals;
		return GetAgentVisualForPeer(visualsIndex, out mountAgentVisuals);
	}

	public IAgentVisual GetAgentVisualForPeer(int visualsIndex, out IAgentVisual mountAgentVisuals)
	{
		PeerVisualsHolder visuals = GetVisuals(visualsIndex);
		mountAgentVisuals = visuals?.MountAgentVisuals;
		return visuals?.AgentVisuals;
	}

	public void TickInactivityStatus()
	{
		NetworkCommunicator networkPeer = this.GetNetworkPeer();
		if (networkPeer.IsMine)
		{
			return;
		}
		if (ControlledAgent != null && ControlledAgent.IsActive())
		{
			if (_lastActiveTime == MissionTime.Zero)
			{
				_lastActiveTime = MissionTime.Now;
				_previousActivityStatus = ValueTuple.Create(ControlledAgent.MovementFlags, ControlledAgent.MovementInputVector, ControlledAgent.LookDirection);
				_inactiveWarningGiven = false;
				return;
			}
			(Agent.MovementControlFlag, Vec2, Vec3) previousActivityStatus = ValueTuple.Create(ControlledAgent.MovementFlags, ControlledAgent.MovementInputVector, ControlledAgent.LookDirection);
			if (_previousActivityStatus.Item1 != previousActivityStatus.Item1 || _previousActivityStatus.Item2.DistanceSquared(previousActivityStatus.Item2) > 1E-05f || _previousActivityStatus.Item3.DistanceSquared(previousActivityStatus.Item3) > 1E-05f)
			{
				_lastActiveTime = MissionTime.Now;
				_previousActivityStatus = previousActivityStatus;
				_inactiveWarningGiven = false;
			}
			if (_lastActiveTime.ElapsedSeconds > 180f)
			{
				DisconnectInfo disconnectInfo = networkPeer.PlayerConnectionInfo.GetParameter<DisconnectInfo>("DisconnectInfo") ?? new DisconnectInfo();
				disconnectInfo.Type = DisconnectType.Inactivity;
				networkPeer.PlayerConnectionInfo.AddParameter("DisconnectInfo", disconnectInfo);
				GameNetwork.AddNetworkPeerToDisconnectAsServer(networkPeer);
			}
			else if (_lastActiveTime.ElapsedSeconds > 120f && !_inactiveWarningGiven)
			{
				Mission.Current.GetMissionBehavior<MultiplayerGameNotificationsComponent>()?.PlayerIsInactive(this.GetNetworkPeer());
				_inactiveWarningGiven = true;
			}
		}
		else
		{
			_lastActiveTime = MissionTime.Now;
			_inactiveWarningGiven = false;
		}
	}

	public void OnKillAnotherPeer(MissionPeer victimPeer)
	{
		if (victimPeer != null)
		{
			if (!_numberOfTimesPeerKilledPerPeer.ContainsKey(victimPeer))
			{
				_numberOfTimesPeerKilledPerPeer.Add(victimPeer, 1);
			}
			else
			{
				_numberOfTimesPeerKilledPerPeer[victimPeer]++;
			}
			MissionPeer.OnPlayerKilled?.Invoke(this, victimPeer);
		}
	}

	public void OverrideCultureWithTeamCulture()
	{
		MultiplayerOptions.OptionType optionType = ((Team.Side == BattleSideEnum.Attacker) ? MultiplayerOptions.OptionType.CultureTeam1 : MultiplayerOptions.OptionType.CultureTeam2);
		Culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(optionType.GetStrValue());
	}

	public int GetNumberOfTimesPeerKilledPeer(MissionPeer killedPeer)
	{
		if (_numberOfTimesPeerKilledPerPeer.ContainsKey(killedPeer))
		{
			return _numberOfTimesPeerKilledPerPeer[killedPeer];
		}
		return 0;
	}

	public void ResetKillRegistry()
	{
		_numberOfTimesPeerKilledPerPeer.Clear();
	}

	public bool RefreshSelectedPerks()
	{
		MBList<MPPerkObject> mBList = new MBList<MPPerkObject>();
		List<List<IReadOnlyPerkObject>> availablePerksForPeer = MultiplayerClassDivisions.GetAvailablePerksForPeer(this);
		if (availablePerksForPeer.Count == 3)
		{
			for (int i = 0; i < 3; i++)
			{
				int num = _perks[SelectedTroopIndex][i];
				if (availablePerksForPeer[i].Count > 0)
				{
					mBList.Add(availablePerksForPeer[i][(num >= 0 && num < availablePerksForPeer[i].Count) ? num : 0].Clone(this));
				}
			}
			_selectedPerks = (SelectedTroopIndex, mBList);
			return true;
		}
		return false;
	}

	private void ResetSelectedPerks()
	{
		if (_selectedPerks.Item2 == null)
		{
			return;
		}
		foreach (MPPerkObject item in _selectedPerks.Item2)
		{
			item.Reset();
		}
	}

	private void CultureChanged(BasicCultureObject newCulture)
	{
		List<MultiplayerClassDivisions.MPHeroClass> list = MultiplayerClassDivisions.GetMPHeroClasses(newCulture).ToList();
		int count = list.Count;
		for (int i = 0; i < count; i++)
		{
			MultiplayerClassDivisions.MPHeroClass currentHeroClass = list[i];
			List<MPPerkSelectionManager.MPPerkSelection> selectionsForHeroClass = MPPerkSelectionManager.Instance.GetSelectionsForHeroClass(currentHeroClass);
			if (selectionsForHeroClass != null)
			{
				int count2 = selectionsForHeroClass.Count;
				for (int j = 0; j < count2; j++)
				{
					MPPerkSelectionManager.MPPerkSelection mPPerkSelection = selectionsForHeroClass[j];
					_perks[i][mPPerkSelection.ListIndex] = mPPerkSelection.Index;
				}
			}
			else
			{
				for (int k = 0; k < 3; k++)
				{
					_perks[i][k] = 0;
				}
			}
		}
		if (base.IsMine && GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new TeamInitialPerkInfoMessage(_perks[SelectedTroopIndex]));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void OnTeamInitialPerkInfoReceived(int[] perks)
	{
		for (int i = 0; i < 3; i++)
		{
			SelectPerk(i, perks[i]);
		}
		TeamInitialPerkInfoReady = true;
	}
}
