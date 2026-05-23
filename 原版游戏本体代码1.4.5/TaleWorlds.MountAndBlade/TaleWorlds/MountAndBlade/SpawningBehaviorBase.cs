using System;
using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public abstract class SpawningBehaviorBase
{
	public delegate void OnSpawningEndedEventDelegate();

	private const float SecondsToWaitForEachMountBeforeSelectingToFadeOut = 30f;

	private const float SecondsToWaitBeforeNextMountCleanup = 5f;

	private static readonly int _maxAgentCount = MBAPI.IMBAgent.GetMaximumNumberOfAgents();

	private static readonly int _agentCountThreshold = (int)((float)_maxAgentCount * 0.9f);

	protected MissionMultiplayerGameModeBase GameMode;

	protected SpawnComponent SpawnComponent;

	private bool _equipmentUpdatingExpired;

	protected bool IsSpawningEnabled;

	protected Timer SpawnCheckTimer;

	protected float SpawningEndDelay = 1f;

	protected float SpawningDelayTimer;

	private bool _hasCalledSpawningEnded;

	protected MissionLobbyComponent MissionLobbyComponent;

	protected MissionLobbyEquipmentNetworkComponent MissionLobbyEquipmentNetworkComponent;

	private List<AgentBuildData> _agentsToBeSpawnedCache;

	private MissionTime _nextTimeToCleanUpMounts;

	private int[] _botsCountForSides;

	protected MultiplayerMissionAgentVisualSpawnComponent AgentVisualSpawnComponent { get; private set; }

	protected Mission Mission => SpawnComponent.Mission;

	protected event Action<MissionPeer> OnAllAgentsFromPeerSpawnedFromVisuals;

	protected event Action<MissionPeer> OnPeerSpawnedFromVisuals;

	public event OnSpawningEndedEventDelegate OnSpawningEnded;

	public virtual void Initialize(SpawnComponent spawnComponent)
	{
		SpawnComponent = spawnComponent;
		AgentVisualSpawnComponent = Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>();
		GameMode = Mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		MissionLobbyComponent = Mission.GetMissionBehavior<MissionLobbyComponent>();
		MissionLobbyEquipmentNetworkComponent = Mission.GetMissionBehavior<MissionLobbyEquipmentNetworkComponent>();
		MissionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed += OnPeerEquipmentUpdated;
		SpawnCheckTimer = new Timer(Mission.Current.CurrentTime, 0.2f);
		_agentsToBeSpawnedCache = new List<AgentBuildData>();
		_nextTimeToCleanUpMounts = MissionTime.Now;
		_botsCountForSides = new int[2];
	}

	public virtual void Clear()
	{
		MissionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed -= OnPeerEquipmentUpdated;
		_agentsToBeSpawnedCache = null;
	}

	public virtual void OnTick(float dt)
	{
		int count = Mission.Current.AllAgents.Count;
		int num = 0;
		_agentsToBeSpawnedCache.Clear();
		BasicCultureObject attackerCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(attackerCulture, defenderCulture);
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsSynchronized)
			{
				continue;
			}
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component == null || component.ControlledAgent != null || !component.HasSpawnedAgentVisuals || CanUpdateSpawnEquipment(component))
			{
				continue;
			}
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
			MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncPerksForCurrentlySelectedTroop(networkPeer, component.Perks[component.SelectedTroopIndex]));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
			int num2 = 0;
			bool flag = false;
			int intValue = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue();
			if (intValue > 0 && (GameMode.WarmupComponent == null || !GameMode.WarmupComponent.IsInWarmup))
			{
				num2 = MPPerkObject.GetTroopCount(mPHeroClassForPeer, intValue, onSpawnPerkHandler);
				foreach (MPPerkObject selectedPerk in component.SelectedPerks)
				{
					if (selectedPerk.HasBannerBearer)
					{
						flag = true;
						break;
					}
				}
			}
			if (num2 > 0)
			{
				num2 = (int)((float)num2 * GameMode.GetTroopNumberMultiplierForMissingPlayer(component));
			}
			num2 += ((!flag) ? 1 : 2);
			IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
			int num3 = 0;
			while (num3 < num2)
			{
				bool flag2 = num3 == 0;
				BasicCharacterObject basicCharacterObject = (flag2 ? mPHeroClassForPeer.HeroCharacter : ((flag && num3 == 1) ? mPHeroClassForPeer.BannerBearerCharacter : mPHeroClassForPeer.TroopCharacter));
				MultiplayerBattleColors.MultiplayerCultureColorInfo peerColors = multiplayerBattleColors.GetPeerColors(component);
				uint clothingColor1Uint = peerColors.ClothingColor1Uint;
				uint clothingColor2Uint = peerColors.ClothingColor2Uint;
				uint bannerBackgroundColorUint = peerColors.BannerBackgroundColorUint;
				uint bannerForegroundColorUint = peerColors.BannerForegroundColorUint;
				Banner banner = new Banner(component.Peer.BannerCode, bannerBackgroundColorUint, bannerForegroundColorUint);
				AgentBuildData agentBuildData = new AgentBuildData(basicCharacterObject).VisualsIndex(num3).Team(component.Team).TroopOrigin(new BasicBattleAgentOrigin(basicCharacterObject))
					.Formation(component.ControlledFormation)
					.IsFemale(flag2 ? component.Peer.IsFemale : basicCharacterObject.IsFemale)
					.ClothingColor1(clothingColor1Uint)
					.ClothingColor2(clothingColor2Uint)
					.Banner(banner);
				if (flag2)
				{
					agentBuildData.MissionPeer(component);
				}
				else
				{
					agentBuildData.OwningMissionPeer(component);
				}
				Equipment equipment = (flag2 ? basicCharacterObject.Equipment.Clone() : Equipment.GetRandomEquipmentElements(basicCharacterObject, randomEquipmentModifier: false, Equipment.EquipmentType.Battle, MBRandom.RandomInt()));
				IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable2 = ((!flag2) ? enumerable : onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true));
				if (enumerable2 != null)
				{
					foreach (var item in enumerable2)
					{
						equipment[item.Item1] = item.Item2;
					}
				}
				agentBuildData.Equipment(equipment);
				if (flag2)
				{
					GameMode.AddCosmeticItemsToEquipment(equipment, GameMode.GetUsedCosmeticsFromPeer(component, basicCharacterObject));
				}
				if (flag2)
				{
					agentBuildData.BodyProperties(GetBodyProperties(component, component.Culture));
					agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
				}
				else
				{
					agentBuildData.EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(basicCharacterObject, agentBuildData.AgentVisualsIndex));
					agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, basicCharacterObject.GetBodyPropertiesMin(), basicCharacterObject.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, basicCharacterObject.BodyPropertyRange.HairTags, basicCharacterObject.BodyPropertyRange.BeardTags, basicCharacterObject.BodyPropertyRange.TattooTags));
				}
				if (component.ControlledFormation != null && component.ControlledFormation.Banner == null)
				{
					component.ControlledFormation.Banner = banner;
				}
				MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(component.Team, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, component.SpawnCountThisRound == 0);
				if (spawnFrame.IsIdentity)
				{
					goto IL_04ff;
				}
				Vec3 origin = spawnFrame.origin;
				Vec3? agentInitialPosition = agentBuildData.AgentInitialPosition;
				if (!(origin != agentInitialPosition))
				{
					Vec2 value = spawnFrame.rotation.f.AsVec2.Normalized();
					Vec2? agentInitialDirection = agentBuildData.AgentInitialDirection;
					if (!(value != agentInitialDirection))
					{
						goto IL_04ff;
					}
				}
				agentBuildData.InitialPosition(in spawnFrame.origin);
				agentBuildData.InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized());
				goto IL_0518;
				IL_0518:
				if (component.ControlledAgent != null && !flag2)
				{
					MatrixFrame frame = component.ControlledAgent.Frame;
					frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					MatrixFrame matrixFrame = frame;
					matrixFrame.origin -= matrixFrame.rotation.f.NormalizedCopy() * 3.5f;
					Mat3 rotation = matrixFrame.rotation;
					rotation.MakeUnit();
					bool flag3 = !basicCharacterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty;
					int num4 = TaleWorlds.Library.MathF.Min(num2, 10);
					MatrixFrame matrixFrame2 = Formation.GetFormationFramesForBeforeFormationCreation((float)num4 * Formation.GetDefaultUnitDiameter(flag3) + (float)(num4 - 1) * Formation.GetDefaultMinimumUnitInterval(flag3), num2, flag3, new WorldPosition(Mission.Current.Scene, matrixFrame.origin), rotation)[num3 - 1].ToGroundMatrixFrame();
					agentBuildData.InitialPosition(in matrixFrame2.origin);
					agentBuildData.InitialDirection(matrixFrame2.rotation.f.AsVec2.Normalized());
				}
				_agentsToBeSpawnedCache.Add(agentBuildData);
				num++;
				if (!agentBuildData.AgentOverridenSpawnEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
				{
					num++;
				}
				num3++;
				continue;
				IL_04ff:
				Debug.FailedAssert("Spawn frame could not be found.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\SpawnBehaviors\\SpawningBehaviors\\SpawningBehaviorBase.cs", "OnTick", 213);
				goto IL_0518;
			}
		}
		int num5 = num + count;
		if (num5 > _agentCountThreshold && _nextTimeToCleanUpMounts.IsPast)
		{
			_nextTimeToCleanUpMounts = MissionTime.SecondsFromNow(5f);
			for (int num6 = Mission.Current.MountsWithoutRiders.Count - 1; num6 >= 0; num6--)
			{
				KeyValuePair<Agent, MissionTime> keyValuePair = Mission.Current.MountsWithoutRiders[num6];
				Agent key = keyValuePair.Key;
				if (keyValuePair.Value.ElapsedSeconds > 30f)
				{
					key.FadeOut(hideInstantly: false, hideMount: false);
				}
			}
		}
		int num7 = _maxAgentCount - num5;
		if (num7 >= 0)
		{
			for (int num8 = _agentsToBeSpawnedCache.Count - 1; num8 >= 0; num8--)
			{
				AgentBuildData agentBuildData2 = _agentsToBeSpawnedCache[num8];
				bool flag4 = agentBuildData2.AgentMissionPeer != null;
				MissionPeer missionPeer = (flag4 ? agentBuildData2.AgentMissionPeer : agentBuildData2.OwningAgentMissionPeer);
				MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler2 = MPPerkObject.GetOnSpawnPerkHandler(missionPeer);
				Agent agent = Mission.SpawnAgent(agentBuildData2, spawnFromAgentVisuals: true);
				agent.AddComponent(new MPPerksAgentComponent(agent));
				agent.MountAgent?.UpdateAgentProperties();
				agent.HealthLimit += onSpawnPerkHandler2?.GetHitpoints(flag4) ?? 0f;
				agent.Health = agent.HealthLimit;
				if (!flag4)
				{
					agent.SetWatchState(Agent.WatchState.Alarmed);
				}
				agent.WieldInitialWeapons();
				if (flag4)
				{
					missionPeer.SpawnCountThisRound++;
					this.OnPeerSpawnedFromVisuals?.Invoke(missionPeer);
					this.OnAllAgentsFromPeerSpawnedFromVisuals?.Invoke(missionPeer);
					AgentVisualSpawnComponent.RemoveAgentVisuals(missionPeer, sync: true);
					if (GameNetwork.IsServerOrRecorder)
					{
						GameNetwork.BeginBroadcastModuleEvent();
						GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(missionPeer.GetNetworkPeer()));
						GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
					}
					missionPeer.HasSpawnedAgentVisuals = false;
					MPPerkObject.GetPerkHandler(missionPeer)?.OnEvent(MPPerkCondition.PerkEventFlags.SpawnEnd);
				}
			}
			int intValue2 = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
			int intValue3 = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
			if (GameMode.IsGameModeUsingOpposingTeams && (intValue2 > 0 || intValue3 > 0))
			{
				(Team, BasicCultureObject, int)[] array = new(Team, BasicCultureObject, int)[2]
				{
					(Mission.DefenderTeam, MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue()), intValue3 - _botsCountForSides[0]),
					(Mission.AttackerTeam, MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue()), intValue2 - _botsCountForSides[1])
				};
				if (num7 >= 4)
				{
					int num9 = Math.Min(num7 / 2, array[0].Item3 + array[1].Item3);
					BattleSideEnum battleSideEnum = BattleSideEnum.Defender;
					while (num9 > 0)
					{
						int num10 = (int)battleSideEnum;
						if (array[num10].Item3 > 0)
						{
							SpawnBot(array[num10].Item1, array[num10].Item2);
							array[num10].Item3--;
							num9--;
						}
						battleSideEnum = battleSideEnum.GetOppositeSide();
					}
				}
			}
		}
		if (IsSpawningEnabled || !IsRoundInProgress())
		{
			return;
		}
		if (SpawningDelayTimer >= SpawningEndDelay && !_hasCalledSpawningEnded)
		{
			Mission.Current.AllowAiTicking = true;
			if (this.OnSpawningEnded != null)
			{
				this.OnSpawningEnded();
			}
			_hasCalledSpawningEnded = true;
		}
		SpawningDelayTimer += dt;
	}

	public bool AreAgentsSpawning()
	{
		return IsSpawningEnabled;
	}

	protected void ResetSpawnCounts()
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component != null)
			{
				component.SpawnCountThisRound = 0;
			}
		}
	}

	protected void ResetSpawnTimers()
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			networkPeer.GetComponent<MissionPeer>()?.SpawnTimer.Reset(Mission.Current.CurrentTime, 0f);
		}
	}

	public virtual void RequestStartSpawnSession()
	{
		IsSpawningEnabled = true;
		SpawningDelayTimer = 0f;
		_hasCalledSpawningEnded = false;
		ResetSpawnCounts();
	}

	public void RequestStopSpawnSession()
	{
		IsSpawningEnabled = false;
		SpawningDelayTimer = 0f;
		_hasCalledSpawningEnded = false;
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component != null)
			{
				AgentVisualSpawnComponent.RemoveAgentVisuals(component, sync: true);
				if (GameNetwork.IsServerOrRecorder)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(component.GetNetworkPeer()));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
				component.HasSpawnedAgentVisuals = false;
			}
		}
		foreach (NetworkCommunicator disconnectedNetworkPeer in GameNetwork.DisconnectedNetworkPeers)
		{
			MissionPeer missionPeer = disconnectedNetworkPeer?.GetComponent<MissionPeer>();
			if (missionPeer != null)
			{
				AgentVisualSpawnComponent.RemoveAgentVisuals(missionPeer);
				if (GameNetwork.IsServerOrRecorder)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(missionPeer.GetNetworkPeer()));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
				missionPeer.HasSpawnedAgentVisuals = false;
			}
		}
	}

	public void SetRemainingAgentsInvulnerable()
	{
		foreach (Agent agent in Mission.Agents)
		{
			agent.SetMortalityState(Agent.MortalityState.Invulnerable);
		}
	}

	protected abstract void SpawnAgents();

	protected BodyProperties GetBodyProperties(MissionPeer missionPeer, BasicCultureObject cultureLimit)
	{
		NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
		if (networkPeer != null)
		{
			return networkPeer.PlayerConnectionInfo.GetParameter<PlayerData>("PlayerData").BodyProperties;
		}
		Debug.FailedAssert("networkCommunicator != null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\SpawnBehaviors\\SpawningBehaviors\\SpawningBehaviorBase.cs", "GetBodyProperties", 518);
		Team team = missionPeer.Team;
		BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(cultureLimit).ToMBList().GetRandomElement()
			.TroopCharacter;
		MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(team, troopCharacter.HasMount(), isInitialSpawn: true);
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, defenderCulture);
		MultiplayerBattleColors.MultiplayerCultureColorInfo multiplayerCultureColorInfo = ((cultureLimit == basicCultureObject) ? multiplayerBattleColors.AttackerColors : multiplayerBattleColors.DefenderColors);
		AgentBuildData agentBuildData = new AgentBuildData(troopCharacter).Team(team).InitialPosition(in spawnFrame.origin).InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized())
			.TroopOrigin(new BasicBattleAgentOrigin(troopCharacter))
			.EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(troopCharacter))
			.ClothingColor1(multiplayerCultureColorInfo.ClothingColor1Uint)
			.ClothingColor2(multiplayerCultureColorInfo.ClothingColor2Uint)
			.IsFemale(troopCharacter.IsFemale);
		agentBuildData.Equipment(Equipment.GetRandomEquipmentElements(troopCharacter, !GameNetwork.IsMultiplayer, Equipment.EquipmentType.Battle, agentBuildData.AgentEquipmentSeed));
		agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, troopCharacter.GetBodyPropertiesMin(), troopCharacter.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, troopCharacter.BodyPropertyRange.HairTags, troopCharacter.BodyPropertyRange.BeardTags, troopCharacter.BodyPropertyRange.TattooTags));
		return agentBuildData.AgentBodyProperties;
	}

	protected void SpawnBot(Team agentTeam, BasicCultureObject cultureLimit)
	{
		BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(cultureLimit).ToMBList().GetRandomElement()
			.TroopCharacter;
		MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(agentTeam, troopCharacter.HasMount(), isInitialSpawn: true);
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, defenderCulture);
		MultiplayerBattleColors.MultiplayerCultureColorInfo multiplayerCultureColorInfo = ((cultureLimit == basicCultureObject) ? multiplayerBattleColors.AttackerColors : multiplayerBattleColors.DefenderColors);
		AgentBuildData agentBuildData = new AgentBuildData(troopCharacter).Team(agentTeam).InitialPosition(in spawnFrame.origin).InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized())
			.TroopOrigin(new BasicBattleAgentOrigin(troopCharacter))
			.EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(troopCharacter))
			.ClothingColor1(multiplayerCultureColorInfo.ClothingColor1Uint)
			.ClothingColor2(multiplayerCultureColorInfo.ClothingColor2Uint)
			.IsFemale(troopCharacter.IsFemale);
		agentBuildData.Equipment(Equipment.GetRandomEquipmentElements(troopCharacter, !GameNetwork.IsMultiplayer, Equipment.EquipmentType.Battle, agentBuildData.AgentEquipmentSeed));
		agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, troopCharacter.GetBodyPropertiesMin(), troopCharacter.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, troopCharacter.BodyPropertyRange.HairTags, troopCharacter.BodyPropertyRange.BeardTags, troopCharacter.BodyPropertyRange.TattooTags));
		Agent agent = Mission.SpawnAgent(agentBuildData);
		agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		_botsCountForSides[(int)agent.Team.Side]++;
	}

	private void OnPeerEquipmentUpdated(MissionPeer peer)
	{
		if (IsSpawningEnabled && CanUpdateSpawnEquipment(peer))
		{
			peer.HasSpawnedAgentVisuals = false;
			Debug.Print("HasSpawnedAgentVisuals = false for peer: " + peer.Name + " because he just updated his equipment");
			if (peer.ControlledFormation != null)
			{
				peer.ControlledFormation.HasBeenPositioned = false;
				peer.ControlledFormation.SetSpawnIndex();
			}
		}
	}

	public virtual bool CanUpdateSpawnEquipment(MissionPeer missionPeer)
	{
		if (!missionPeer.EquipmentUpdatingExpired)
		{
			return !_equipmentUpdatingExpired;
		}
		return false;
	}

	public void ToggleUpdatingSpawnEquipment(bool canUpdate)
	{
		_equipmentUpdatingExpired = !canUpdate;
	}

	public abstract bool AllowEarlyAgentVisualsDespawning(MissionPeer missionPeer);

	public virtual int GetMaximumReSpawnPeriodForPeer(MissionPeer peer)
	{
		return 3;
	}

	protected abstract bool IsRoundInProgress();

	public virtual void OnClearScene()
	{
		for (int i = 0; i < _botsCountForSides.Length; i++)
		{
			_botsCountForSides[i] = 0;
		}
	}

	public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.IsHuman && affectedAgent.MissionPeer == null && affectedAgent.OwningAgentMissionPeer == null)
		{
			_botsCountForSides[(int)affectedAgent.Team.Side]--;
		}
	}
}
