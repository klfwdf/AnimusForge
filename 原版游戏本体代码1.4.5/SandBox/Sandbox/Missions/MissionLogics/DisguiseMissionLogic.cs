using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Helpers;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Source.Objects;

namespace SandBox.Missions.MissionLogics;

public class DisguiseMissionLogic : MissionLogic, IPlayerInputEffector, IMissionBehavior
{
	public class ShadowingAgentOffenseInfo
	{
		public Agent Agent { get; }

		public bool CanPlayerCameraSeeTheAgent { get; private set; }

		public StealthOffenseTypes OffenseType { get; private set; }

		public ShadowingAgentOffenseInfo(Agent agent, StealthOffenseTypes offenseType)
		{
			Agent = agent;
			OffenseType = offenseType;
		}

		public void SetCanPlayerCameraSeeTheAgent(bool value)
		{
			CanPlayerCameraSeeTheAgent = value;
		}

		internal void SetOffenseType(StealthOffenseTypes offenseType)
		{
			OffenseType = offenseType;
		}
	}

	public const float PlayerSuspiciousLevelMin = 0f;

	public const float PlayerSuspiciousLevelMax = 1f;

	public const float ToggleStealthModeSuspiciousThreshold = 0.95f;

	public const float MissionFailDistanceToTargetAgent = 5000f;

	private const float StartSuspiciousDecayAfterSeconds = 2f;

	private const float OfficerAgentPersonalZoneRadius = 3.5f;

	private const float DefaultAgentPersonalZoneRadius = 0f;

	private const float InConsistentMovementToleranceFactor = 0.2f;

	private const float MaximumWorstMovementRotationFactor = 1f;

	private const float InconsistentMovementDecayFactor = 2f;

	private const float CircularMovementDetectRadiusSquared = 4f;

	private const float DefaultDecayFactor = -0.01f;

	private const float DefaultSuspiciousFactor = 0.1f;

	private const float GuardSpawnDistanceThreshold = 20f;

	private const float MaximumContactAgentDistance = 250f;

	private const float StaticGuardSpawnPercentage = 0.3f;

	private readonly List<CharacterObject> _troopPool;

	private Dictionary<Agent, ShadowingAgentOffenseInfo> _disguiseAgentOffenseInfos;

	private Agent _contactAgent;

	private Timer _isAgentDeadTimer;

	private readonly List<GameEntity> _customPoints = new List<GameEntity>();

	private readonly List<GameEntity> _dynamicPoints = new List<GameEntity>();

	public float PlayerSuspiciousLevel;

	private Vec2 _lastFramePlayerPosition;

	private int _disabledFaceId;

	private readonly CharacterObject _defaultContractorCharacter;

	private readonly List<Agent> _officerAgents;

	private readonly List<Agent> _defaultDisguiseAgents;

	private readonly List<Agent> _agentsToBeRemoved;

	private readonly bool _willSetUpContact;

	private readonly Location _fromLocation;

	private Dictionary<Agent, AlarmedBehaviorGroup> _agentAlarmedBehaviorCache;

	private List<Agent> _suspiciousAgentsThisFrame;

	private MBList<GameEntity> _stealthIndoorLightingAreas;

	private bool _isBehaviorInitialized;

	private bool _firstTickPassed;

	private bool _firstEventControlTickPassed;

	private bool _disguiseAgentsStealthModeIsOn;

	private float _angleDifferenceBetweenCurrentAndLastPositionOfPlayer;

	private float _cumulativePositionAndRotationDifference;

	private Vec3 _averagePlayerPosition;

	private MissionTimer _lastSuspiciousTimer;

	private bool _contactSet;

	private int _staticGuardsCount;

	private bool _playerWillBeTakenPrisoner;

	public bool IsInStealthMode => PlayerSuspiciousLevel >= 0.95f;

	public ReadOnlyDictionary<Agent, ShadowingAgentOffenseInfo> ThreatAgentInfos { get; }

	public DisguiseMissionLogic(CharacterObject contractorCharacter, Location fromLocation, bool willSetUpContact)
	{
		_troopPool = CharacterHelper.GetTroopTree(Settlement.CurrentSettlement.Culture.BasicTroop, 2f, 3f).ToList();
		_defaultContractorCharacter = contractorCharacter;
		_fromLocation = fromLocation;
		_defaultDisguiseAgents = new List<Agent>();
		_officerAgents = new List<Agent>();
		_suspiciousAgentsThisFrame = new List<Agent>();
		_agentsToBeRemoved = new List<Agent>();
		_agentAlarmedBehaviorCache = new Dictionary<Agent, AlarmedBehaviorGroup>();
		_disguiseAgentOffenseInfos = new Dictionary<Agent, ShadowingAgentOffenseInfo>();
		ThreatAgentInfos = new ReadOnlyDictionary<Agent, ShadowingAgentOffenseInfo>(_disguiseAgentOffenseInfos);
		Game.Current.EventManager.RegisterEvent<LocationCharacterAgentSpawnedMissionEvent>(OnLocationCharacterAgentSpawned);
		CampaignEvents.BeforePlayerAgentSpawnEvent.AddNonSerializedListener(this, OnBeforePlayerAgentSpawn);
		CampaignEvents.CanPlayerMeetWithHeroAfterConversationEvent.AddNonSerializedListener(this, CanPlayerMeetWithHeroAfterConversation);
		_willSetUpContact = willSetUpContact;
		PlayerEncounter.LocationEncounter.RemoveAllAccompanyingCharacters();
	}

	private void OnBeforePlayerAgentSpawn(ref MatrixFrame matrixFrame)
	{
		if (_fromLocation != null)
		{
			matrixFrame = GetSpawnFrameOfPassage(_fromLocation);
			matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		}
	}

	public override void OnCreated()
	{
		CampaignMission.Current.Location = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
	}

	public MatrixFrame GetSpawnFrameOfPassage(Location location)
	{
		MatrixFrame result = MatrixFrame.Identity;
		UsableMachine usableMachine = Mission.Current.GetMissionBehavior<MissionAgentHandler>().TownPassageProps.FirstOrDefault((UsableMachine x) => ((Passage)x).ToLocation == location) ?? Mission.Current.GetMissionBehavior<MissionAgentHandler>().DisabledPassages.FirstOrDefault((UsableMachine x) => ((Passage)x).ToLocation == location);
		if (usableMachine != null)
		{
			MatrixFrame globalFrame = usableMachine.PilotStandingPoint.GameEntity.GetGlobalFrame();
			globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			globalFrame.origin.z = base.Mission.Scene.GetGroundHeightAtPosition(globalFrame.origin);
			globalFrame.rotation.RotateAboutUp(System.MathF.PI);
			result = globalFrame;
		}
		return result;
	}

	public bool IsContactAgentTracked(Agent agent)
	{
		if (agent == _contactAgent)
		{
			return !_contactSet;
		}
		return false;
	}

	public bool CanCommonAreaFightBeTriggered()
	{
		return ContactAlreadySetCommonCondition();
	}

	private void CanPlayerMeetWithHeroAfterConversation(Hero hero, ref bool result)
	{
		result = ContactAlreadySetCommonCondition();
	}

	public bool ContactAlreadySetCommonCondition()
	{
		if (!_contactSet)
		{
			return !_willSetUpContact;
		}
		return true;
	}

	public bool IsOnLeftSide(Vec2 lineA, Vec2 lineB, Vec2 point)
	{
		return (lineB.x - lineA.x) * (point.y - lineA.y) - (lineB.y - lineA.y) * (point.x - lineA.x) > 0f;
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (agent.IsHuman)
		{
			if (_troopPool.Contains(agent.Character))
			{
				_defaultDisguiseAgents.Add(agent);
				_disguiseAgentOffenseInfos[agent] = new ShadowingAgentOffenseInfo(agent, StealthOffenseTypes.None);
			}
			else if (agent.Character is CharacterObject characterObject && (characterObject.Occupation == Occupation.Guard || characterObject.Occupation == Occupation.Soldier))
			{
				_defaultDisguiseAgents.Add(agent);
				_disguiseAgentOffenseInfos[agent] = new ShadowingAgentOffenseInfo(agent, StealthOffenseTypes.None);
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.IsHuman)
		{
			if (_defaultDisguiseAgents.Contains(affectedAgent))
			{
				_defaultDisguiseAgents.Remove(affectedAgent);
			}
			if (_officerAgents.Contains(affectedAgent))
			{
				_officerAgents.Remove(affectedAgent);
			}
			if (affectedAgent.IsMainAgent)
			{
				Campaign.Current.GameMenuManager.SetNextMenu(_contactSet ? "settlement_player_unconscious_when_disguise_contact_set" : "settlement_player_unconscious_when_disguise_contact_not_set");
			}
		}
	}

	private void SetStealthModeToDisguiseAgents(bool isActive)
	{
		foreach (Agent defaultDisguiseAgent in _defaultDisguiseAgents)
		{
			SetStealthModeInternal(defaultDisguiseAgent, isActive);
		}
		foreach (Agent officerAgent in _officerAgents)
		{
			SetStealthModeInternal(officerAgent, isActive);
		}
		_disguiseAgentsStealthModeIsOn = isActive;
	}

	private void SetStealthModeInternal(Agent agent, bool isActive)
	{
		if (!_agentAlarmedBehaviorCache.TryGetValue(agent, out var value))
		{
			return;
		}
		value.DoNotCheckForAlarmFactorIncrease = !isActive;
		if (isActive)
		{
			value.DoNotIncreaseAlarmFactorDueToSeeingOrHearingTheEnemy = false;
			if (agent.InteractingWithAnyGameObject())
			{
				agent.StopUsingGameObject();
			}
		}
	}

	protected override void OnEndMission()
	{
		_officerAgents.Clear();
		_defaultDisguiseAgents.Clear();
		_agentsToBeRemoved.Clear();
		_agentAlarmedBehaviorCache = null;
		_suspiciousAgentsThisFrame = null;
		if (!_playerWillBeTakenPrisoner && Agent.Main != null && Agent.Main.IsActive())
		{
			foreach (Agent agent in base.Mission.Agents)
			{
				if (!agent.IsMainAgent && agent.IsAlarmed())
				{
					Campaign.Current.GameMenuManager.SetNextMenu("settlement_player_run_away_when_disguise");
				}
			}
		}
		Game.Current.EventManager.UnregisterEvent<LocationCharacterAgentSpawnedMissionEvent>(OnLocationCharacterAgentSpawned);
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		Campaign.Current.ConversationManager.RemoveRelatedLines(this);
	}

	private void InitializeMissionBehavior()
	{
		Mission.Current.IsKingdomWindowAccessible = false;
		Mission.Current.IsBannerWindowAccessible = false;
		Mission.Current.IsClanWindowAccessible = false;
		Mission.Current.IsCharacterWindowAccessible = false;
		Mission.Current.IsEncyclopediaWindowAccessible = false;
		Mission.Current.IsInventoryAccessible = false;
		Mission.Current.IsPartyWindowAccessible = false;
		SandBoxHelpers.MissionHelper.SpawnPlayer(base.Mission.Scene.FindEntityWithTag("spawnpoint_player"), civilianEquipment: false, noHorses: true);
		List<GameEntity> entities = new List<GameEntity>();
		base.Mission.Scene.GetAllEntitiesWithScriptComponent<StealthIndoorLightingArea>(ref entities);
		_stealthIndoorLightingAreas = new MBList<GameEntity>(entities);
		Mission.Current.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters();
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
		if (gameEntity != null)
		{
			NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
			_disabledFaceId = firstScriptOfType.DisableFaceWithId;
		}
		SetStealthModeToDisguiseAgents(isActive: false);
		_lastFramePlayerPosition = Agent.Main.Position.AsVec2;
		_averagePlayerPosition = Agent.Main.Position - Agent.Main.Frame.rotation.f * 2f;
		_lastSuspiciousTimer = new MissionTimer(2f);
		foreach (Agent item in _agentsToBeRemoved)
		{
			item.FadeOut(hideInstantly: true, hideMount: true);
		}
		_agentsToBeRemoved.Clear();
		Campaign.Current.ConversationManager.AddDialogFlow(GetContactDialogFlow(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlow1(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlow2(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlow3(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(GetNotableDialogFlow4(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(GetThugDialogFlow(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(FailedDialogFlow(), this);
		if (_willSetUpContact)
		{
			SpawnContactAgent();
			TogglePassages(isActive: false);
			_contactSet = false;
		}
		else
		{
			_contactSet = true;
		}
		TurnGuardsToDisguiseAgents();
		SpawnCustomGuards();
	}

	private void TogglePassages(bool isActive)
	{
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("npc_passage"))
		{
			PassageUsePoint firstScriptOfTypeRecursive = item.GetFirstScriptOfTypeRecursive<PassageUsePoint>();
			if (firstScriptOfTypeRecursive != null)
			{
				if (isActive)
				{
					firstScriptOfTypeRecursive.SetEnabledAndMakeVisible();
				}
				else
				{
					firstScriptOfTypeRecursive.SetDisabledAndMakeInvisible();
				}
			}
		}
	}

	private void SpawnCustomGuards()
	{
		List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag("npc_common").ToList();
		list.AddRange(Mission.Current.Scene.FindEntitiesWithTag("npc_wait").ToList());
		List<AreaMarker> list2 = (from x in Mission.Current.Scene.FindEntitiesWithTag("alley_marker")
			select x.GetFirstScriptOfType<AreaMarker>()).ToList();
		list2.AddRange(from x in Mission.Current.Scene.FindEntitiesWithTag("workshop_area_marker")
			select x.GetFirstScriptOfType<AreaMarker>());
		foreach (GameEntity item in list)
		{
			Vec3 position = item.GlobalPosition;
			if (!(Mission.Current.Scene.GetNavigationMeshForPosition(in position, out var faceGroupId, 1.5f, excludeDynamicNavigationMeshes: false) != UIntPtr.Zero) || item.GetFirstScriptOfTypeRecursive<StandingPoint>() == null || item.GetFirstScriptOfTypeRecursive<StandingPoint>().IsDeactivated || faceGroupId == _disabledFaceId)
			{
				continue;
			}
			bool flag = false;
			foreach (AreaMarker item2 in list2)
			{
				if (item2.IsPositionInRange(position))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				continue;
			}
			bool flag2 = false;
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent.Position.Distance(position) < 2f)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				_customPoints.Add(item);
			}
		}
		for (int num = _customPoints.Count - 1; num >= 0; num--)
		{
			for (int num2 = 0; num2 < num; num2++)
			{
				GameEntity gameEntity = _customPoints[num];
				GameEntity gameEntity2 = _customPoints[num2];
				if (gameEntity.GlobalPosition.Distance(gameEntity2.GlobalPosition) < 20f)
				{
					_customPoints.RemoveAt(num);
					break;
				}
			}
		}
		_staticGuardsCount = (int)((float)_customPoints.Count * 0.3f);
		for (int num3 = 0; num3 < _customPoints.Count; num3++)
		{
			GameEntity gameEntity3 = _customPoints[num3];
			CharacterObject randomElementInefficiently = _troopPool.GetRandomElementInefficiently();
			Agent ownerAgent = SpawnDisguiseMissionAgentInternal(randomElementInefficiently, gameEntity3.GlobalPosition, gameEntity3.GetFrame().rotation.f.AsVec2.Normalized(), "_guard");
			if (num3 > _staticGuardsCount)
			{
				ScriptBehavior.AddTargetWithDelegate(ownerAgent, GuardAgentSelectTargetDelegate(), GuardAgentWaitDelegate, GuardAgentOnTargetReachDelegate);
				_dynamicPoints.Add(gameEntity3);
			}
		}
	}

	private bool GuardAgentOnTargetReachDelegate(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame)
	{
		GameEntity randomElement = _dynamicPoints.GetRandomElement();
		WorldFrame worldFrame = new WorldFrame(randomElement.GetGlobalFrame().rotation, new WorldPosition(Mission.Current.Scene, randomElement.GetGlobalFrame().origin));
		targetFrame = worldFrame;
		return true;
	}

	private void GuardAgentWaitDelegate(Agent agent, ref float waitTimeInSeconds)
	{
		waitTimeInSeconds = MBRandom.RandomInt(10, 80);
	}

	private ScriptBehavior.SelectTargetDelegate GuardAgentSelectTargetDelegate()
	{
		return delegate(Agent agent1, ref Agent targetAgent, ref UsableMachine machine, ref WorldFrame frame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold)
		{
			customTargetReachedRangeThreshold = 2.5f;
			customTargetReachedRotationThreshold = 0.8f;
			GameEntity randomElement = _dynamicPoints.GetRandomElement();
			frame = new WorldFrame(randomElement.GetGlobalFrame().rotation, new WorldPosition(Mission.Current.Scene, randomElement.GetGlobalFrame().origin));
			return true;
		};
	}

	private void TurnGuardsToDisguiseAgents()
	{
		for (int num = base.Mission.Agents.Count - 1; num >= 0; num--)
		{
			Agent agent = base.Mission.Agents[num];
			if (agent.IsHuman && agent.Character is CharacterObject { IsFemale: false } characterObject && (characterObject.Occupation == Occupation.Soldier || characterObject.Occupation == Occupation.Guard))
			{
				AddBehaviorGroups(agent);
				agent.SetTeam(base.Mission.PlayerEnemyTeam, sync: true);
				agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanWieldWeapon | AgentFlag.CanGetAlarmed);
				string actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agent.Monster, isFemale: false, "_guard");
				AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(MBGlobals.GetActionSet(actionSetCode), agent.Character.GetStepSize(), hasClippingPlane: false);
				agent.SetActionSet(ref animationSystemData);
				SetStealthModeInternal(agent, _disguiseAgentsStealthModeIsOn);
				agent.SetMortalityState(Agent.MortalityState.Immortal);
				if (agent.Character.IsRanged)
				{
					agent.InitializeSpawnEquipment(agent.Character.FirstBattleEquipment.Clone(cloneWithoutWeapons: true));
				}
			}
		}
	}

	public Agent SpawnDisguiseMissionAgentInternal(CharacterObject agentCharacter, Vec3 initialPosition, Vec2 initialDirection, string actionSetId, bool isEnemy = true)
	{
		Equipment equipment = agentCharacter.FirstBattleEquipment.Clone(cloneWithoutWeapons: true);
		AgentBuildData agentBuildData = new AgentBuildData(agentCharacter).InitialPosition(in initialPosition).InitialDirection(in initialDirection).CivilianEquipment(civilianEquipment: false)
			.Equipment(equipment)
			.NoHorses(noHorses: true)
			.TroopOrigin(new SimpleAgentOrigin(agentCharacter));
		if (isEnemy)
		{
			agentBuildData.Team(base.Mission.PlayerEnemyTeam);
		}
		Agent agent = Mission.Current.SpawnAgent(agentBuildData);
		AddBehaviorGroups(agent);
		if (isEnemy)
		{
			agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanWieldWeapon | AgentFlag.CanGetAlarmed);
		}
		string actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agent.Monster, isFemale: false, actionSetId);
		AnimationSystemData animationSystemData = agentBuildData.AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSet(actionSetCode), agentCharacter.GetStepSize(), hasClippingPlane: false);
		agent.SetActionSet(ref animationSystemData);
		SetStealthModeInternal(agent, _disguiseAgentsStealthModeIsOn);
		agent.SetMortalityState(Agent.MortalityState.Immortal);
		return agent;
	}

	private void AddBehaviorGroups(Agent agent)
	{
		AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
		agentNavigator.AddBehaviorGroup<DailyBehaviorGroup>();
		agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<CautiousBehavior>();
		behaviorGroup.AddBehavior<FightBehavior>();
		agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, isExcluded: true);
		_agentAlarmedBehaviorCache.Add(agent, behaviorGroup);
	}

	private void SpawnContactAgent()
	{
		float minDistance = 2.5f;
		float maxDistance = 10f;
		IEnumerable<GameEntity> enumerable = Mission.Current.Scene.FindEntitiesWithTag("npc_passage");
		List<GameEntity> list = new List<GameEntity>();
		foreach (GameEntity item in enumerable)
		{
			Passage firstScriptOfType = item.GetFirstScriptOfType<Passage>();
			if (firstScriptOfType != null)
			{
				if (firstScriptOfType.ToLocation == Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern"))
				{
					list.Add(item);
				}
				else if (firstScriptOfType.ToLocation == Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("arena"))
				{
					list.Add(item);
				}
			}
		}
		IEnumerable<GameEntity> source = Mission.Current.Scene.FindEntitiesWithTag("workshop_area_marker");
		list.AddRange(source.ToList());
		float num = float.MinValue;
		float num2 = 250f;
		GameEntity gameEntity = null;
		foreach (GameEntity item2 in list)
		{
			WorldPosition point = new WorldPosition(Mission.Current.Scene, item2.GlobalPosition);
			WorldPosition point2 = new WorldPosition(Mission.Current.Scene, Agent.Main.Position);
			float pathDistance;
			bool pathDistanceBetweenPositions = Mission.Current.Scene.GetPathDistanceBetweenPositions(ref point, ref point2, 0.1f, out pathDistance);
			PathFaceRecord record = new PathFaceRecord(-1, -1, -1);
			Mission.Current.Scene.GetNavMeshFaceIndex(ref record, item2.GlobalPosition, checkIfDisabled: false);
			if (gameEntity == null && record.IsValid())
			{
				gameEntity = item2;
			}
			if (record.IsValid() && pathDistanceBetweenPositions && pathDistance < num2 && pathDistance > num)
			{
				num = pathDistance;
				gameEntity = item2;
			}
		}
		if (gameEntity == null)
		{
			gameEntity = list.First();
		}
		WorldPosition point3 = new WorldPosition(Mission.Current.Scene, Agent.Main.Position);
		Vec3 position = gameEntity.GlobalPosition;
		PathFaceRecord record2 = new PathFaceRecord(-1, -1, -1);
		Mission.Current.Scene.GetNavMeshFaceIndex(ref record2, position, checkIfDisabled: false);
		WorldPosition point4 = new WorldPosition(Mission.Current.Scene, position);
		int num3 = 0;
		float pathDistance2;
		while ((record2.FaceGroupIndex != _disabledFaceId || !Mission.Current.Scene.GetPathDistanceBetweenPositions(ref point4, ref point3, 0.3f, out pathDistance2) || pathDistance2 < 5f || pathDistance2 > 40f) && num3 <= 150)
		{
			position = Mission.Current.GetRandomPositionAroundPoint(gameEntity.GetFrame().origin, minDistance, maxDistance, MBRandom.RandomFloat < 0.5f);
			point4 = new WorldPosition(Mission.Current.Scene, position);
			Mission.Current.Scene.GetNavMeshFaceIndex(ref record2, position, checkIfDisabled: true);
			num3++;
		}
		AgentBuildData agentBuildData = new AgentBuildData(_defaultContractorCharacter).TroopOrigin(new SimpleAgentOrigin(_defaultContractorCharacter)).Team(base.Mission.SpectatorTeam).InitialPosition(in position)
			.InitialDirection(Vec2.One.Normalized())
			.CivilianEquipment(civilianEquipment: true)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: true)
			.ClothingColor1(base.Mission.PlayerTeam.Color)
			.ClothingColor2(base.Mission.PlayerTeam.Color2);
		_contactAgent = base.Mission.SpawnAgent(agentBuildData);
		_contactAgent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
		Campaign.Current.VisualTrackerManager.SetDirty();
	}

	private DialogFlow GetNotableDialogFlow1()
	{
		TextObject npcText = new TextObject("{=7hlGVkbq}{PLAYER.NAME}... I don't know why you're dressed like that, and I don't think I want to know. If you look around, though, I think you'll find someone who can help you out.");
		return DialogFlow.CreateDialogFlow("start", 1000).NpcLine(npcText).Condition(() => GeneralNotableDialogCondition() && ConversationMission.OneToOneConversationCharacter.HeroObject.HasMet)
			.CloseDialog();
	}

	private DialogFlow GetNotableDialogFlow2()
	{
		return DialogFlow.CreateDialogFlow("start", 1000).NpcLine(new TextObject("{=RAA6bEw8}If you're a stranger in this town, I'm sure you can find someone who'll let you stay on a pile of straw or under a bridge for a few coppers.")).Condition(() => DialogCondition2() || BlackSmithCondition())
			.CloseDialog();
	}

	private DialogFlow GetNotableDialogFlow3()
	{
		return DialogFlow.CreateDialogFlow("start", 1000).NpcLine(new TextObject("{=tgUUxK7Z}Look, mate - I can't really help you right now, but I'm sure if you look around you can find someone who'll give you whatever you need.")).Condition(DialogCondition3)
			.CloseDialog();
	}

	private DialogFlow GetNotableDialogFlow4()
	{
		return DialogFlow.CreateDialogFlow("start", 1000).NpcLine(new TextObject("{=qdDRe8QC}Clear off, you beggar. Find someone who caters to the likes of you.")).Condition(DialogCondition4)
			.CloseDialog();
	}

	private bool DialogCondition2()
	{
		if (GeneralNotableDialogCondition())
		{
			int traitLevel = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Generosity);
			int traitLevel2 = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Mercy);
			return traitLevel + traitLevel2 > 0;
		}
		return false;
	}

	private bool DialogCondition3()
	{
		if (GeneralNotableDialogCondition())
		{
			int traitLevel = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Generosity);
			int traitLevel2 = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Mercy);
			return traitLevel + traitLevel2 == 0;
		}
		return false;
	}

	private bool DialogCondition4()
	{
		if (GeneralNotableDialogCondition())
		{
			int traitLevel = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Generosity);
			int traitLevel2 = ConversationMission.OneToOneConversationCharacter.HeroObject.GetTraitLevel(DefaultTraits.Mercy);
			return traitLevel + traitLevel2 < 0;
		}
		return false;
	}

	private bool GeneralNotableDialogCondition()
	{
		if (!_contactSet)
		{
			if (ConversationMission.OneToOneConversationCharacter.IsHero)
			{
				return ConversationMission.OneToOneConversationCharacter.HeroObject.IsNotable;
			}
			return false;
		}
		return false;
	}

	private bool BlackSmithCondition()
	{
		if (!_contactSet)
		{
			return ConversationMission.OneToOneConversationCharacter.Occupation == Occupation.Blacksmith;
		}
		return false;
	}

	private DialogFlow GetThugDialogFlow()
	{
		return DialogFlow.CreateDialogFlow("start", 101).NpcLine(new TextObject("{=3buSOoHl}Get lost!")).Condition(ThugConversationCondition)
			.CloseDialog();
	}

	private bool ThugConversationCondition()
	{
		AgentNavigator agentNavigator = ConversationMission.OneToOneConversationAgent?.GetComponent<CampaignAgentComponent>().AgentNavigator;
		if (_willSetUpContact && !_contactSet && agentNavigator?.MemberOfAlley != null && agentNavigator.MemberOfAlley.State == Alley.AreaState.OccupiedByGangLeader)
		{
			return agentNavigator.MemberOfAlley.Owner != Hero.MainHero;
		}
		return false;
	}

	private DialogFlow FailedDialogFlow()
	{
		return DialogFlow.CreateDialogFlow("start", 101).NpcLine(new TextObject("{=91x5mjXa}Hey! You thought you could fool us, wearing that nonsense? To the dungeons you go, until we decide what to do with you!")).Condition(() => _defaultDisguiseAgents.Contains(ConversationMission.OneToOneConversationAgent))
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += mission_failed_through_dialog_consequence;
			})
			.CloseDialog();
	}

	private void mission_failed_through_dialog_consequence()
	{
		_playerWillBeTakenPrisoner = true;
		Campaign.Current.GameMenuManager.SetNextMenu("menu_captivity_castle_taken_prisoner");
		Mission.Current.EndMission();
	}

	private DialogFlow GetContactDialogFlow()
	{
		return DialogFlow.CreateDialogFlow("start", 101).BeginNpcOptions("start").NpcOption(new TextObject("{=fT57TeqJ}You can go about your business now and we don't need to see each other ever again."), () => _contactSet && ConversationMission.OneToOneConversationAgent == _contactAgent)
			.CloseDialog()
			.NpcOption(new TextObject("{=mdJapWRd}Right… Something tells me that you're not just an ordinary beggar. Look, I can help you lie low and stay out of sight for a bit, if that's what you need."), () => !_contactSet && ConversationMission.OneToOneConversationAgent == _contactAgent)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=toHJ01dX}What do you want in exchange?"))
			.NpcLine(new TextObject("{=G3sImCKI}Nothing... I suspect your good favor is worth more than the few coppers I normally charge for my services."))
			.PlayerLine(new TextObject("{=rshplAOt}Hmm."))
			.GotoDialogState("start")
			.PlayerOption(new TextObject("{=QuNcB0dA}Very well. I accept."))
			.NpcLine(new TextObject("{=bNRfxIy7}Very good. I think it will be safe for you to go about your business in a short time. The less time we spend talking together the better, so you might not see me again."))
			.Consequence(delegate
			{
				_contactSet = true;
				base.Mission.GetMissionBehavior<MissionConversationLogic>()?.DisableStartConversation(isDisabled: false);
				Campaign.Current.GetCampaignBehavior<EncounterGameMenuBehavior>().AddCurrentSettlementAsAlreadySneakedIn();
				MBInformationManager.AddQuickInformation(new TextObject("{=MZJhzaUJ}You now have a contact in this town."), 0, null, null, "event:/ui/notification/quest_update");
				TogglePassages(isActive: true);
			})
			.CloseDialog()
			.EndNpcOptions();
	}

	private void OnLocationCharacterAgentSpawned(LocationCharacterAgentSpawnedMissionEvent eventData)
	{
		if (eventData.LocationCharacter.Character.IsHero && eventData.LocationCharacter.Character.HeroObject.IsPlayerCompanion)
		{
			_agentsToBeRemoved.Add(eventData.Agent);
		}
		else if (eventData.LocationCharacter.Character.Occupation == Occupation.Musician || eventData.LocationCharacter.Character.Culture.FemaleDancer == eventData.LocationCharacter.Character)
		{
			_agentsToBeRemoved.Add(eventData.Agent);
		}
		else if (eventData.LocationCharacter.MemberOfAlley != null && eventData.LocationCharacter.MemberOfAlley.Owner == Hero.MainHero)
		{
			_agentsToBeRemoved.Add(eventData.Agent);
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (!_firstTickPassed)
		{
			_firstTickPassed = true;
			return;
		}
		if (!_isBehaviorInitialized)
		{
			InitializeMissionBehavior();
			_isBehaviorInitialized = true;
			return;
		}
		_suspiciousAgentsThisFrame.Clear();
		if (Agent.Main != null)
		{
			PlayerSuspiciousLevel += GetPlayerSuspiciousFactor(dt) * dt * Campaign.Current.Models.DifficultyModel.GetDisguiseDifficultyMultiplier();
			PlayerSuspiciousLevel = TaleWorlds.Library.MathF.Clamp(PlayerSuspiciousLevel, 0f, 1f);
			if (PlayerSuspiciousLevel >= 0.95f)
			{
				if (!_disguiseAgentsStealthModeIsOn)
				{
					SetStealthModeToDisguiseAgents(isActive: true);
				}
				foreach (Agent item in _suspiciousAgentsThisFrame)
				{
					if (_agentAlarmedBehaviorCache.TryGetValue(item, out var value) && value.AlarmFactor < 0.25f)
					{
						value.AddAlarmFactor(0.25f - value.AlarmFactor, Agent.Main.GetWorldPosition());
					}
				}
			}
			else if (_disguiseAgentsStealthModeIsOn)
			{
				SetStealthModeToDisguiseAgents(isActive: false);
			}
			CheckCaughtConversationActivation();
		}
		else if (Agent.Main == null || !Agent.Main.IsActive())
		{
			if (_isAgentDeadTimer == null)
			{
				_isAgentDeadTimer = new Timer(Mission.Current.CurrentTime, 5f);
			}
			if (_isAgentDeadTimer.Check(Mission.Current.CurrentTime))
			{
				Mission.Current.NextCheckTimeEndMission = 0f;
				Mission.Current.EndMission();
			}
		}
		else if (_isAgentDeadTimer != null)
		{
			_isAgentDeadTimer = null;
		}
	}

	private void CheckCaughtConversationActivation()
	{
		if (Campaign.Current.ConversationManager.IsConversationFlowActive)
		{
			return;
		}
		foreach (Agent officerAgent in _officerAgents)
		{
			if (officerAgent.IsAlarmed() && officerAgent.Position.DistanceSquared(Agent.Main.Position) < 9f)
			{
				ConversationMission.StartConversationWithAgent(officerAgent);
				break;
			}
		}
		if (!Campaign.Current.ConversationManager.IsConversationFlowActive)
		{
			foreach (Agent defaultDisguiseAgent in _defaultDisguiseAgents)
			{
				if (defaultDisguiseAgent.IsAlarmed() && defaultDisguiseAgent.Position.DistanceSquared(Agent.Main.Position) < 9f)
				{
					ConversationMission.StartConversationWithAgent(defaultDisguiseAgent);
					break;
				}
			}
		}
		if (!Campaign.Current.ConversationManager.IsConversationFlowActive)
		{
			return;
		}
		SetStealthModeToDisguiseAgents(isActive: false);
		foreach (Agent agent in Mission.Current.Agents)
		{
			agent.SetAlarmState(Agent.AIStateFlag.None);
			agent.SetAgentFlags((AgentFlag)((uint)agent.GetAgentFlags() & 0xFFFEFFFFu));
		}
	}

	public ShadowingAgentOffenseInfo GetAgentOffenseInfo(Agent agent)
	{
		if (agent == null)
		{
			return null;
		}
		if (!_disguiseAgentOffenseInfos.TryGetValue(agent, out var value))
		{
			return null;
		}
		return value;
	}

	private float GetPlayerSuspiciousFactor(float dt)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		bool hasVisualOnCorpse;
		foreach (Agent officerAgent in _officerAgents)
		{
			StealthOffenseTypes offenseType = StealthOffenseTypes.None;
			if (CanAgentSeeAgent(officerAgent, Agent.Main, _stealthIndoorLightingAreas, out hasVisualOnCorpse))
			{
				num++;
				num2++;
				offenseType = StealthOffenseTypes.IsVisible;
				_suspiciousAgentsThisFrame.Add(officerAgent);
			}
			if (IsInOfficerPersonalZone(officerAgent))
			{
				num3++;
				offenseType = StealthOffenseTypes.IsInPersonalZone;
			}
			if (_disguiseAgentOffenseInfos.TryGetValue(officerAgent, out var value))
			{
				value.SetOffenseType(offenseType);
			}
		}
		foreach (Agent defaultDisguiseAgent in _defaultDisguiseAgents)
		{
			StealthOffenseTypes offenseType2 = StealthOffenseTypes.None;
			if (CanAgentSeeAgent(defaultDisguiseAgent, Agent.Main, _stealthIndoorLightingAreas, out hasVisualOnCorpse))
			{
				num++;
				offenseType2 = StealthOffenseTypes.IsVisible;
				_suspiciousAgentsThisFrame.Add(defaultDisguiseAgent);
			}
			if (IsInDefaultAgentPersonalZone(defaultDisguiseAgent))
			{
				num3 += 15;
				offenseType2 = StealthOffenseTypes.IsInPersonalZone;
			}
			if (_disguiseAgentOffenseInfos.TryGetValue(defaultDisguiseAgent, out var value2))
			{
				value2.SetOffenseType(offenseType2);
			}
		}
		float num4 = TaleWorlds.Library.MathF.Sqrt(num3 * 2 + num + num2);
		bool flag = num4 <= 0f;
		bool flag2 = Agent.Main.MovementVelocity.Length > 1E-05f;
		bool flag3 = Agent.Main.IsUsingGameObject || ConversationMission.OneToOneConversationAgent != null;
		bool crouchMode = Agent.Main.CrouchMode;
		bool walkMode = Agent.Main.WalkMode;
		bool flag4 = Agent.Main.IsAbleToUseMachine();
		bool flag5 = Agent.Main.GetPrimaryWieldedItemIndex() != EquipmentIndex.None || Agent.Main.GetOffhandWieldedItemIndex() != EquipmentIndex.None;
		bool flag6 = MBMath.IsBetween((int)Agent.Main.GetCurrentActionType(0), 19, 23);
		float num5 = 0f;
		bool flag7 = false;
		if (!flag3)
		{
			flag7 = CalculateErraticMovementSuspiciousValue(dt);
			if (!flag)
			{
				num5 = CalculateCircularMovementSuspiciousValue(dt);
			}
		}
		float num6 = ((!(num4 > 0f)) ? (-0.07f) : ((num3 > 0 && !flag3) ? 0.13f : (flag6 ? 0.75f : (flag5 ? 0.55f : ((!flag4) ? 0.2f : (crouchMode ? 0.15f : ((num2 > 0 && !flag3) ? 0.040000003f : ((!walkMode && flag2 && !flag3) ? 0.3f : ((flag7 && _cumulativePositionAndRotationDifference > 0.2f) ? (0.1f * _cumulativePositionAndRotationDifference) : ((num5 > 0f) ? (0.1f * num5) : ((!flag2 && !flag && !flag3) ? 0.1f : (flag3 ? (-0.07f) : ((!flag || flag2) ? (-0.049999997f) : (-0.07f))))))))))))));
		if (num4 > 0f)
		{
			num6 *= num4;
		}
		if (num6 > 0.05f)
		{
			_lastSuspiciousTimer.Reset();
		}
		else if (!_lastSuspiciousTimer.Check())
		{
			num6 = 0f;
		}
		if (num6 < 0f && (_defaultDisguiseAgents.Any((Agent x) => !x.IsAlarmStateNormal()) || _officerAgents.Any((Agent x) => !x.IsAlarmStateNormal())))
		{
			num6 = 0f;
		}
		return num6;
	}

	private float CalculateCircularMovementSuspiciousValue(float dt)
	{
		Vec3 position = Agent.Main.Position;
		_averagePlayerPosition = Vec3.Lerp(_averagePlayerPosition, position, dt * 0.6f);
		return Math.Max(0f, (4f - _averagePlayerPosition.DistanceSquared(position)) / 4f);
	}

	public bool IsAgentInDetectionRadius(Agent offenderAgent, Agent detectorAgent)
	{
		return offenderAgent.Position.DistanceSquared(detectorAgent.Position) < 4f;
	}

	private bool CalculateErraticMovementSuspiciousValue(float dt)
	{
		Vec2 asVec = Agent.Main.Position.AsVec2;
		bool result = false;
		float num = TaleWorlds.Library.MathF.Atan2(asVec.Y - _lastFramePlayerPosition.Y, asVec.X - _lastFramePlayerPosition.X);
		if (num > System.MathF.PI)
		{
			num = System.MathF.PI * 2f - num;
		}
		num /= System.MathF.PI;
		float num2 = TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Abs(_angleDifferenceBetweenCurrentAndLastPositionOfPlayer - num) * 0.5f);
		if (num2 > 0.02f)
		{
			_cumulativePositionAndRotationDifference += _cumulativePositionAndRotationDifference / 1f * num2;
			result = true;
		}
		_angleDifferenceBetweenCurrentAndLastPositionOfPlayer = num;
		_lastFramePlayerPosition = asVec;
		_cumulativePositionAndRotationDifference = TaleWorlds.Library.MathF.Clamp(_cumulativePositionAndRotationDifference - 2f * dt, 0.2f, 0.6f);
		return result;
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = PlayerSuspiciousLevel < 0.25f;
		if (!canPlayerLeave)
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=9w6zmKQ1}You can't sneak out while people are suspicious!"));
		}
		return null;
	}

	private bool IsInOfficerPersonalZone(Agent agent)
	{
		return Agent.Main.Position.DistanceSquared(agent.Position) < 12.25f;
	}

	private bool IsInDefaultAgentPersonalZone(Agent agent)
	{
		return Agent.Main.Position.DistanceSquared(agent.Position) < 0f;
	}

	private bool CanAgentSeeAgent(Agent agent1, Agent agent2, MBReadOnlyList<GameEntity> stealthIndoorLightingAreas, out bool hasVisualOnCorpse)
	{
		Vec3 vb = ((agent1.IsHuman && agent1.AgentVisuals.IsValid()) ? agent1.Frame.rotation.TransformToParent(agent1.AgentVisuals.GetCurrentHeadLookDirection()) : agent1.LookDirection);
		vb = vb.RotateAboutAnArbitraryVector(Vec3.CrossProduct(Vec3.Up, vb).NormalizedCopy(), 0.2f);
		bool hasVisualOnEnemy = false;
		hasVisualOnCorpse = false;
		_agentAlarmedBehaviorCache[agent1].GetVisualFactor(vb, agent2, stealthIndoorLightingAreas, ref hasVisualOnCorpse, ref hasVisualOnEnemy);
		return hasVisualOnEnemy;
	}

	public Agent.EventControlFlag OnCollectPlayerEventControlFlags()
	{
		if (!_firstEventControlTickPassed)
		{
			_firstEventControlTickPassed = true;
			return Agent.EventControlFlag.Walk;
		}
		return Agent.EventControlFlag.None;
	}
}
