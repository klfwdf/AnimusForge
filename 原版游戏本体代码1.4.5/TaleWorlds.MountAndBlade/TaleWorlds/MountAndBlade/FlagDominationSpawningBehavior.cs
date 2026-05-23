using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class FlagDominationSpawningBehavior : SpawningBehaviorBase
{
	private const int EnforcedSpawnTimeInSeconds = 15;

	private float _spawningTimer;

	private bool _spawningTimerTicking;

	private bool _roundInitialSpawnOver;

	private MissionMultiplayerFlagDomination _flagDominationMissionController;

	private MultiplayerRoundController _roundController;

	private List<KeyValuePair<MissionPeer, Timer>> _enforcedSpawnTimers;

	public FlagDominationSpawningBehavior()
	{
		_enforcedSpawnTimers = new List<KeyValuePair<MissionPeer, Timer>>();
	}

	public override void Initialize(SpawnComponent spawnComponent)
	{
		base.Initialize(spawnComponent);
		_flagDominationMissionController = base.Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
		_roundController = base.Mission.GetMissionBehavior<MultiplayerRoundController>();
		_roundController.OnRoundStarted += RequestStartSpawnSession;
		_roundController.OnRoundEnding += base.RequestStopSpawnSession;
		_roundController.OnRoundEnding += base.SetRemainingAgentsInvulnerable;
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
		{
			_roundController.EnableEquipmentUpdate();
		}
		base.OnAllAgentsFromPeerSpawnedFromVisuals += OnAllAgentsFromPeerSpawnedFromVisuals;
		base.OnPeerSpawnedFromVisuals += OnPeerSpawnedFromVisuals;
	}

	public override void Clear()
	{
		base.Clear();
		_roundController.OnRoundStarted -= RequestStartSpawnSession;
		_roundController.OnRoundEnding -= base.SetRemainingAgentsInvulnerable;
		_roundController.OnRoundEnding -= base.RequestStopSpawnSession;
		base.OnAllAgentsFromPeerSpawnedFromVisuals -= OnAllAgentsFromPeerSpawnedFromVisuals;
		base.OnPeerSpawnedFromVisuals -= OnPeerSpawnedFromVisuals;
	}

	public override void OnTick(float dt)
	{
		if (_spawningTimerTicking)
		{
			_spawningTimer += dt;
		}
		if (IsSpawningEnabled)
		{
			if (!_roundInitialSpawnOver && IsRoundInProgress())
			{
				foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
				{
					MissionPeer component = networkPeer.GetComponent<MissionPeer>();
					if (component?.Team != null && component.Team.Side != BattleSideEnum.None)
					{
						SpawnComponent.SetEarlyAgentVisualsDespawning(component);
					}
				}
				_roundInitialSpawnOver = true;
				base.Mission.AllowAiTicking = true;
			}
			SpawnAgents();
			if (_roundInitialSpawnOver && _flagDominationMissionController.GameModeUsesSingleSpawning && _spawningTimer > (float)MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue())
			{
				IsSpawningEnabled = false;
				_spawningTimer = 0f;
				_spawningTimerTicking = false;
			}
		}
		base.OnTick(dt);
	}

	public override void RequestStartSpawnSession()
	{
		if (!IsSpawningEnabled)
		{
			Mission.Current.SetBattleAgentCount(-1);
			IsSpawningEnabled = true;
			_spawningTimerTicking = true;
			ResetSpawnCounts();
			ResetSpawnTimers();
		}
	}

	protected override void SpawnAgents()
	{
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject basicCultureObject2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, basicCultureObject2);
		int intValue = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue();
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (!networkPeer.IsSynchronized || component.Team == null || component.Team.Side == BattleSideEnum.None || (intValue == 0 && CheckIfEnforcedSpawnTimerExpiredForPeer(component)))
			{
				continue;
			}
			Team team = component.Team;
			bool num = team == base.Mission.AttackerTeam;
			_ = base.Mission.DefenderTeam;
			BasicCultureObject basicCultureObject3 = (num ? basicCultureObject : basicCultureObject2);
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
			int num2 = ((_flagDominationMissionController.GetMissionType() == MultiplayerGameType.Battle) ? mPHeroClassForPeer.TroopBattleCost : mPHeroClassForPeer.TroopCost);
			if (component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || !component.SpawnTimer.Check(base.Mission.CurrentTime))
			{
				continue;
			}
			int currentGoldForPeer = _flagDominationMissionController.GetCurrentGoldForPeer(component);
			if (mPHeroClassForPeer == null || (_flagDominationMissionController.UseGold() && num2 > currentGoldForPeer))
			{
				if (currentGoldForPeer >= MultiplayerClassDivisions.GetMinimumTroopCost(basicCultureObject3) && component.SelectedTroopIndex != 0)
				{
					component.SelectedTroopIndex = 0;
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, 0));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
				}
				continue;
			}
			if (intValue == 0)
			{
				CreateEnforcedSpawnTimerForPeer(component, 15);
			}
			Formation formation = component.ControlledFormation;
			if (intValue > 0 && formation == null)
			{
				FormationClass formationIndex = component.Team.FormationsIncludingEmpty.First((Formation x) => x.PlayerOwner == null && !x.ContainsAgentVisuals && x.CountOfUnits == 0).FormationIndex;
				formation = team.GetFormation(formationIndex);
				formation.ContainsAgentVisuals = true;
				if (string.IsNullOrEmpty(formation.BannerCode))
				{
					formation.BannerCode = component.Peer.BannerCode;
				}
			}
			MultiplayerBattleColors.MultiplayerCultureColorInfo peerColors = multiplayerBattleColors.GetPeerColors(component);
			BasicCharacterObject heroCharacter = mPHeroClassForPeer.HeroCharacter;
			AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Team(component.Team).VisualsIndex(0)
				.Formation(formation)
				.MakeUnitStandOutOfFormationDistance(7f)
				.IsFemale(component.Peer.IsFemale)
				.BodyProperties(GetBodyProperties(component, (component.Culture == basicCultureObject) ? basicCultureObject : basicCultureObject2))
				.ClothingColor1(peerColors.ClothingColor1Uint)
				.ClothingColor2(peerColors.ClothingColor2Uint);
			MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
			Equipment equipment = heroCharacter.Equipment.Clone();
			IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true);
			if (enumerable != null)
			{
				foreach (var item in enumerable)
				{
					equipment[item.Item1] = item.Item2;
				}
			}
			int amountOfAgentVisualsForPeer = component.GetAmountOfAgentVisualsForPeer();
			bool flag = amountOfAgentVisualsForPeer > 0;
			agentBuildData.Equipment(equipment);
			if (intValue == 0)
			{
				if (!flag)
				{
					MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(component.Team, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, isInitialSpawn: true);
					agentBuildData.InitialPosition(in spawnFrame.origin);
					agentBuildData.InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized());
				}
				else
				{
					MatrixFrame frame = component.GetAgentVisualForPeer(0).GetFrame();
					agentBuildData.InitialPosition(in frame.origin);
					agentBuildData.InitialDirection(frame.rotation.f.AsVec2.Normalized());
				}
			}
			if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
			{
				base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(component, agentBuildData, component.SelectedTroopIndex);
				if (agentBuildData.AgentVisualsIndex == 0)
				{
					component.HasSpawnedAgentVisuals = true;
					component.EquipmentUpdatingExpired = false;
				}
			}
			GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);
			component.ControlledFormation = formation;
			if (intValue <= 0)
			{
				continue;
			}
			int troopCount = MPPerkObject.GetTroopCount(mPHeroClassForPeer, intValue, onSpawnPerkHandler);
			IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipments = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
			for (int num3 = 0; num3 < troopCount; num3++)
			{
				if (num3 + 1 >= amountOfAgentVisualsForPeer)
				{
					flag = false;
				}
				SpawnBotVisualsInPlayerFormation(component, num3 + 1, team, basicCultureObject3, mPHeroClassForPeer.TroopCharacter.StringId, formation, flag, troopCount, alternativeEquipments);
			}
		}
	}

	private new void OnPeerSpawnedFromVisuals(MissionPeer peer)
	{
		if (peer.ControlledFormation != null)
		{
			peer.ControlledAgent.Team.AssignPlayerAsSergeantOfFormation(peer, peer.ControlledFormation.FormationIndex);
		}
	}

	private new void OnAllAgentsFromPeerSpawnedFromVisuals(MissionPeer peer)
	{
		if (peer.ControlledFormation != null)
		{
			peer.ControlledFormation.OnFormationDispersed();
			peer.ControlledFormation.SetMovementOrder(MovementOrder.MovementOrderFollow(peer.ControlledAgent));
			NetworkCommunicator networkPeer = peer.GetNetworkPeer();
			if (peer.BotsUnderControlAlive != 0 || peer.BotsUnderControlTotal != 0)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new BotsControlledChange(networkPeer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				base.Mission.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>().OnBotsControlledChanged(peer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal);
			}
			if (peer.Team == base.Mission.AttackerTeam)
			{
				base.Mission.NumOfFormationsSpawnedTeamOne++;
			}
			else
			{
				base.Mission.NumOfFormationsSpawnedTeamTwo++;
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetSpawnedFormationCount(base.Mission.NumOfFormationsSpawnedTeamOne, base.Mission.NumOfFormationsSpawnedTeamTwo));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		if (_flagDominationMissionController.UseGold())
		{
			bool flag = peer.Team == base.Mission.AttackerTeam;
			_ = base.Mission.DefenderTeam;
			MultiplayerClassDivisions.MPHeroClass mPHeroClass = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(flag ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue())).ElementAt(peer.SelectedTroopIndex);
			int num = ((_flagDominationMissionController.GetMissionType() == MultiplayerGameType.Battle) ? mPHeroClass.TroopBattleCost : mPHeroClass.TroopCost);
			_flagDominationMissionController.ChangeCurrentGoldForPeer(peer, _flagDominationMissionController.GetCurrentGoldForPeer(peer) - num);
		}
	}

	private void BotFormationSpawned(Team team)
	{
		if (team == base.Mission.AttackerTeam)
		{
			base.Mission.NumOfFormationsSpawnedTeamOne++;
		}
		else if (team == base.Mission.DefenderTeam)
		{
			base.Mission.NumOfFormationsSpawnedTeamTwo++;
		}
	}

	private void AllBotFormationsSpawned()
	{
		if (base.Mission.NumOfFormationsSpawnedTeamOne != 0 || base.Mission.NumOfFormationsSpawnedTeamTwo != 0)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetSpawnedFormationCount(base.Mission.NumOfFormationsSpawnedTeamOne, base.Mission.NumOfFormationsSpawnedTeamTwo));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}

	public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
	{
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
		{
			if (!_roundController.IsRoundInProgress)
			{
				return false;
			}
			if (!lobbyPeer.HasSpawnTimerExpired && lobbyPeer.SpawnTimer.Check(Mission.Current.CurrentTime))
			{
				lobbyPeer.HasSpawnTimerExpired = true;
			}
			return lobbyPeer.HasSpawnTimerExpired;
		}
		return false;
	}

	protected override bool IsRoundInProgress()
	{
		return _roundController.IsRoundInProgress;
	}

	private void CreateEnforcedSpawnTimerForPeer(MissionPeer peer, int durationInSeconds)
	{
		if (!_enforcedSpawnTimers.Any((KeyValuePair<MissionPeer, Timer> pair) => pair.Key == peer))
		{
			_enforcedSpawnTimers.Add(new KeyValuePair<MissionPeer, Timer>(peer, new Timer(base.Mission.CurrentTime, durationInSeconds)));
			Debug.Print("EST for " + peer.Name + " set to " + durationInSeconds + " seconds.", 0, Debug.DebugColor.Yellow, 64uL);
		}
	}

	private bool CheckIfEnforcedSpawnTimerExpiredForPeer(MissionPeer peer)
	{
		KeyValuePair<MissionPeer, Timer> keyValuePair = _enforcedSpawnTimers.FirstOrDefault((KeyValuePair<MissionPeer, Timer> pr) => pr.Key == peer);
		if (keyValuePair.Key == null)
		{
			return false;
		}
		if (peer.ControlledAgent != null)
		{
			_enforcedSpawnTimers.RemoveAll((KeyValuePair<MissionPeer, Timer> p) => p.Key == peer);
			Debug.Print("EST for " + peer.Name + " is no longer valid (spawned already).", 0, Debug.DebugColor.Yellow, 64uL);
			return false;
		}
		Timer value = keyValuePair.Value;
		if (peer.HasSpawnedAgentVisuals && value.Check(Mission.Current.CurrentTime))
		{
			SpawnComponent.SetEarlyAgentVisualsDespawning(peer);
			_enforcedSpawnTimers.RemoveAll((KeyValuePair<MissionPeer, Timer> p) => p.Key == peer);
			Debug.Print("EST for " + peer.Name + " has expired.", 0, Debug.DebugColor.Yellow, 64uL);
			return true;
		}
		return false;
	}

	public override void OnClearScene()
	{
		base.OnClearScene();
		_enforcedSpawnTimers.Clear();
		_roundInitialSpawnOver = false;
	}

	protected void SpawnBotInBotFormation(int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, BasicCharacterObject character, Formation formation)
	{
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, defenderCulture);
		MultiplayerBattleColors.MultiplayerCultureColorInfo multiplayerCultureColorInfo = ((cultureLimit == basicCultureObject) ? multiplayerBattleColors.AttackerColors : multiplayerBattleColors.DefenderColors);
		AgentBuildData agentBuildData = new AgentBuildData(character).Team(agentTeam).TroopOrigin(new BasicBattleAgentOrigin(character)).VisualsIndex(visualsIndex)
			.EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(character, visualsIndex))
			.Formation(formation)
			.IsFemale(character.IsFemale)
			.ClothingColor1(multiplayerCultureColorInfo.ClothingColor1Uint)
			.ClothingColor2(multiplayerCultureColorInfo.ClothingColor2Uint);
		agentBuildData.Equipment(Equipment.GetRandomEquipmentElements(character, !GameNetwork.IsMultiplayer, Equipment.EquipmentType.Battle, agentBuildData.AgentEquipmentSeed));
		agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.BodyPropertyRange.HairTags, character.BodyPropertyRange.BeardTags, character.BodyPropertyRange.TattooTags));
		base.Mission.SpawnAgent(agentBuildData).SetAlarmState(Agent.AIStateFlag.Alarmed);
	}

	protected void SpawnBotVisualsInPlayerFormation(MissionPeer missionPeer, int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, string troopName, Formation formation, bool updateExistingAgentVisuals, int totalCount, IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipments)
	{
		BasicCharacterObject basicCharacterObject = MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopName);
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, defenderCulture);
		MultiplayerBattleColors.MultiplayerCultureColorInfo multiplayerCultureColorInfo = ((cultureLimit == basicCultureObject) ? multiplayerBattleColors.AttackerColors : multiplayerBattleColors.DefenderColors);
		AgentBuildData agentBuildData = new AgentBuildData(basicCharacterObject).Team(agentTeam).OwningMissionPeer(missionPeer).VisualsIndex(visualsIndex)
			.TroopOrigin(new BasicBattleAgentOrigin(basicCharacterObject))
			.EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(basicCharacterObject, visualsIndex))
			.Formation(formation)
			.IsFemale(basicCharacterObject.IsFemale)
			.ClothingColor1(multiplayerCultureColorInfo.ClothingColor1Uint)
			.ClothingColor2(multiplayerCultureColorInfo.ClothingColor2Uint);
		Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(basicCharacterObject, !GameNetwork.IsMultiplayer, Equipment.EquipmentType.Battle, MBRandom.RandomInt());
		if (alternativeEquipments != null)
		{
			foreach (var alternativeEquipment in alternativeEquipments)
			{
				randomEquipmentElements[alternativeEquipment.Item1] = alternativeEquipment.Item2;
			}
		}
		agentBuildData.Equipment(randomEquipmentElements);
		agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, basicCharacterObject.GetBodyPropertiesMin(), basicCharacterObject.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, basicCharacterObject.BodyPropertyRange.HairTags, basicCharacterObject.BodyPropertyRange.BeardTags, basicCharacterObject.BodyPropertyRange.TattooTags));
		NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
		if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
		{
			base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(missionPeer, agentBuildData, -1, isBot: true, totalCount);
			if (agentBuildData.AgentVisualsIndex == 0)
			{
				missionPeer.HasSpawnedAgentVisuals = true;
				missionPeer.EquipmentUpdatingExpired = false;
			}
		}
		GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData, totalCount, useCosmetics: false);
	}
}
