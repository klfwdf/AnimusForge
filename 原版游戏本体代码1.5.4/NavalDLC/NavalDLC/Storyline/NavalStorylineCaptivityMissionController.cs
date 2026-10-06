using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using NavalDLC.Missions.ShipInput;
using NavalDLC.Storyline.Objectives.Captivity;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline;

public class NavalStorylineCaptivityMissionController : MissionLogic
{
	private const int ScatteredCrewCountPerArea = 2;

	private const string PlayerEquipmentId = "item_set_player_captivity";

	private const string GunnarEquipmentId = "item_set_gangradir_captivity";

	private const float InitialOarForceMultiplier = 0.01f;

	private const float FinalOarForceMultiplier = 0.95f;

	private const float CloseSailsDistanceToFinalHighlight = 90f;

	private const float WindStrength = 1.1f;

	private const float FadeInDuration = 0.75f;

	private const float BlackDuration = 1f;

	private const float FadeOutDuration = 0.75f;

	private int _missionInitializationPeriod;

	private MissionObjectiveLogic _missionObjectiveLogic;

	private Agent _gunnarAgent;

	private readonly List<Agent> _crewAgents = new List<Agent>();

	private readonly CharacterObject _allyCharacterObject;

	private readonly BasicCharacterObject _enemyCharacterObject;

	private readonly CharacterObject _crewCharacterObject;

	private ShipOarMachine _oarUsedByPlayer;

	private ShipOarMachine _oarUsedByAlly;

	private List<GameEntity> _entities = new List<GameEntity>();

	private readonly List<(Agent, bool)> _scatteredCrew = new List<(Agent, bool)>();

	private readonly List<Agent> _savedScatteredAgents = new List<Agent>();

	private bool _allScatteredCrewMembersAreSaved;

	private bool _hasTalkedToGunnarOutro;

	private float _outroSpeechDelayTimer;

	private SpawnedItemEntity _weaponEntity;

	private GameEntity _spawnZone1;

	private GameEntity _spawnZone2;

	private bool _isFinalized;

	private bool _hasSavedOarsmen;

	private SoundEvent _spawnZone1HelpSoundEvent;

	private SoundEvent _spawnZone2HelpSoundEvent;

	private int _savedOarsmenCount;

	private bool _hasTalkedToGunnar;

	private bool _isConversationSetupInProgress;

	private int _spawnedOarsmenCount;

	private float _speechDelayTimer;

	private int _saveTargetAgentCount;

	private ActionIndexCache _tinkeringAction;

	private bool _isPlayerTinkeringWithTheBindsMachine;

	private int _previousOarsmenLevel;

	private List<AgentBindsMachine> _agentBindMachines = new List<AgentBindsMachine>();

	private List<ShipOarMachine> _leftOars = new List<ShipOarMachine>();

	private List<ShipOarMachine> _rightOars = new List<ShipOarMachine>();

	private Dictionary<Agent, ShipOarMachine> _oarAssignments = new Dictionary<Agent, ShipOarMachine>();

	private Agent _crewConversationAgent;

	public Action OnMarkedObjectStatusChangedEvent;

	public Action OnPlayerStartedEscapeEvent;

	public Action<Vec3> OnConversationSetupEvent;

	public Action<int> OnOarsmenLevelChanged;

	public Action<float, float, float> OnStartFadeOutEvent;

	public Action OnFirstHighlightClearedEvent;

	public MissionShip MissionShip { get; private set; }

	public bool IsPlayerFree { get; private set; }

	public bool HasTalkedToGunnar => _hasTalkedToGunnar;

	public bool WasPlayerKnockedOut { get; private set; }

	public NavalStorylineCaptivityMissionController(CharacterObject allyCharacter, BasicCharacterObject enemyCharacter, CharacterObject crewCharacter)
	{
		_allyCharacterObject = allyCharacter;
		_enemyCharacterObject = enemyCharacter;
		_crewCharacterObject = crewCharacter;
	}

	public override void OnBehaviorInitialize()
	{
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
	}

	public bool IsInitialized()
	{
		return _missionInitializationPeriod > 1;
	}

	public override void OnMissionTick(float dt)
	{
		if (_missionInitializationPeriod == 0)
		{
			if (!SailWindProfile.IsSailWindProfileInitialized)
			{
				SailWindProfile.InitializeProfile();
			}
			_missionInitializationPeriod++;
			_missionObjectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
			UpdateEntityReferences();
			base.Mission.PlayerTeam.DisableDetachmentTicking();
			base.Mission.Scene.SetWaterStrength(0f);
			MissionShip = CreateShip();
			UpdateEntityReferences();
			CategorizeOars();
			_entities.FirstOrDefault((GameEntity t) => t.HasTag("spawn_highlight_1")).SetVisibilityExcludeParents(visible: false);
			_entities.FirstOrDefault((GameEntity t) => t.HasTag("spawn_highlight_2")).SetVisibilityExcludeParents(visible: false);
			MissionShip.SetController(ShipControllerType.AI, autoUpdateController: false);
			MissionShip.SetCustomSailSetting(enableCustomSailSetting: true, SailInput.Raised);
			MatrixFrame globalFrame = MissionShip.GlobalFrame;
			MissionShip.ShipOrder.SetShipMovementOrder(globalFrame.origin.AsVec2 + globalFrame.rotation.f.AsVec2 * 50f);
			GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("sp_wind"));
			if (gameEntity != null)
			{
				Vec2 windVector = gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized() * 1.1f;
				Mission.Current.Scene.SetGlobalWindStrengthVector(in windVector);
			}
			base.Mission.Scene.SetWaterStrength(1f);
			base.Mission.OnInitialSpawnCompleted();
			MBMusicManager.Current.StartThemeWithConstantIntensity(MusicTheme.StealthA);
			MBMusicManager.Current.ChangeCurrentThemeIntensity(-1f);
			MBMusicManager.Current.ChangeCurrentThemeIntensity(0.5f);
		}
		else if (_missionInitializationPeriod == 1)
		{
			_missionInitializationPeriod++;
			SpawnPlayerAgent();
			SpawnAllyAgent();
			SpawnEnemyAgents();
			SpawnCrewAgents();
			SpawnWeapon();
			InitializeUsableMachines();
			SetOarForceMultipliers(0.01f);
			MissionShip.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection((-MissionShip.GameEntity.GetGlobalFrame().rotation.f).AsVec2));
			TextObject textObject = new TextObject("{=lRLE9fpA}{PLAYER.NAME}! Your chain is loose. It's now or never! Get up and strike them down!");
			textObject.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			CampaignInformationManager.AddDialogLine(textObject, _allyCharacterObject, _allyCharacterObject.FirstCivilianEquipment, 1000);
			CaptivityEscapeCaptivityObjective objective = new CaptivityEscapeCaptivityObjective(Mission.Current, this);
			_missionObjectiveLogic.StartObjective(objective);
		}
		CheckEnemyAlarmedState();
		CheckIfCrewmenAreNearby();
		if (_isPlayerTinkeringWithTheBindsMachine)
		{
			CheckIfPlayerIsReleasedFromOar();
		}
		if (_hasSavedOarsmen && !_hasTalkedToGunnar)
		{
			_speechDelayTimer += dt;
			if (!_isConversationSetupInProgress && _speechDelayTimer > 0.75f)
			{
				SetupPostFightConversation();
			}
			if (_speechDelayTimer > 1.75f)
			{
				StartPostFightConversation();
				ReenableAllOars();
			}
		}
		if (_allScatteredCrewMembersAreSaved && !_hasTalkedToGunnarOutro)
		{
			_outroSpeechDelayTimer += dt;
			if (!_isConversationSetupInProgress && _outroSpeechDelayTimer > 0.75f)
			{
				SetupSavedCrewConversation();
			}
			if (_outroSpeechDelayTimer > 1.75f)
			{
				StartSavedCrewConversation();
			}
		}
		if (!HasTalkedToGunnar)
		{
			return;
		}
		if (MissionShip.ShipOrder.OarsmenLevel > 0)
		{
			foreach (Agent crewAgent in _crewAgents)
			{
				if (crewAgent.IsActive())
				{
					MakeAgentUseAssignedOarMachine(crewAgent);
				}
			}
			foreach (Agent savedScatteredAgent in _savedScatteredAgents)
			{
				if (savedScatteredAgent.IsActive() && savedScatteredAgent != _crewConversationAgent)
				{
					MakeAgentUseAssignedOarMachine(savedScatteredAgent);
				}
			}
			if (!_allScatteredCrewMembersAreSaved && !Campaign.Current.ConversationManager.IsAgentInConversation(_gunnarAgent))
			{
				MakeAgentUseAssignedOarMachine(_gunnarAgent);
			}
		}
		int oarsmenLevel = MissionShip.ShipOrder.OarsmenLevel;
		if (_previousOarsmenLevel != oarsmenLevel)
		{
			OnOarsmenLevelChanged(oarsmenLevel);
			_previousOarsmenLevel = oarsmenLevel;
		}
	}

	private void MakeAgentUseAssignedOarMachine(Agent agent)
	{
		if (agent.IsDetachedFromFormation)
		{
			return;
		}
		_oarAssignments.TryGetValue(agent, out var value);
		if (value == null)
		{
			value = GetOarMachineToUse();
			if (value != null)
			{
				_oarAssignments.Add(agent, value);
			}
		}
		if (!value.IsDisabledForBattleSideAI(agent.Team.Side))
		{
			value.AddAgentAtSlotIndex(agent, 0);
		}
	}

	private void CheckIfCrewmenAreNearby()
	{
		if (!_hasSavedOarsmen || _allScatteredCrewMembersAreSaved || _scatteredCrew.Count <= 0)
		{
			return;
		}
		Vec3 origin = MissionShip.GlobalFrame.origin;
		for (int num = _scatteredCrew.Count - 1; num >= 0; num--)
		{
			(Agent, bool) tuple = _scatteredCrew[num];
			var (agent, _) = tuple;
			if (MissionShip.GetIsAgentOnShip(agent) && agent.CurrentlyUsedGameObject == null)
			{
				_scatteredCrew.RemoveAt(num);
				_savedScatteredAgents.Add(agent);
				if (_savedScatteredAgents.Count == 2)
				{
					OnFirstHighlightClearedEvent();
					OnFirstHighlightCleared();
				}
				if (_savedScatteredAgents.Count == _saveTargetAgentCount)
				{
					OnAllCrewSaved();
				}
			}
			else if (!tuple.Item2 && origin.DistanceSquared(tuple.Item1.Position) <= 900f)
			{
				_scatteredCrew[num] = (tuple.Item1, true);
				tuple.Item1.ClearTargetFrame();
				tuple.Item1.Formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
				MissionShip.SetShipClimbingOrderStandAloneTickingActive(isShipClimbingMachineStandaloneTickingActive: true);
				NavalAgentsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalAgentsLogic>();
				missionBehavior.AddAgentToShip(tuple.Item1, MissionShip);
				missionBehavior.TransferAgentToShip(tuple.Item1, MissionShip);
				OnPlayerReachedFirstZone();
				if (agent.Position.DistanceSquared(_spawnZone1.GlobalPosition) > agent.Position.DistanceSquared(_spawnZone2.GlobalPosition))
				{
					if (_spawnZone2HelpSoundEvent != null)
					{
						_spawnZone2HelpSoundEvent.Stop();
						_spawnZone2HelpSoundEvent = null;
					}
				}
				else if (_spawnZone1HelpSoundEvent != null)
				{
					_spawnZone1HelpSoundEvent.Stop();
					_spawnZone1HelpSoundEvent = null;
				}
			}
		}
		if (_allScatteredCrewMembersAreSaved)
		{
			return;
		}
		if (_spawnZone1.GlobalPosition.DistanceSquared(origin) <= 900f)
		{
			_entities.First((GameEntity t) => t.HasTag("spawn_highlight_1")).SetVisibilityExcludeParents(visible: false);
		}
		if (_spawnZone2.GlobalPosition.DistanceSquared(origin) <= 900f)
		{
			_entities.First((GameEntity t) => t.HasTag("spawn_highlight_2")).SetVisibilityExcludeParents(visible: false);
		}
	}

	private void UpdateEntityReferences()
	{
		_entities.Clear();
		base.Mission.Scene.GetEntities(ref _entities);
	}

	private void CheckEnemyAlarmedState()
	{
		foreach (Agent activeAgent in base.Mission.PlayerEnemyTeam.ActiveAgents)
		{
			if (activeAgent.IsAlarmed())
			{
				continue;
			}
			foreach (Agent activeAgent2 in base.Mission.PlayerTeam.ActiveAgents)
			{
				bool flag = activeAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.CanSeeAgent(activeAgent2);
				float num = activeAgent.Position.DistanceSquared(activeAgent2.Position);
				if (num <= 5f || (num <= 10f && flag))
				{
					OnAgentEntersFight(activeAgent, activeAgent2);
				}
			}
		}
	}

	public override InquiryData OnEndMissionRequest(out bool canLeave)
	{
		canLeave = _isFinalized;
		return null;
	}

	private MissionShip CreateShip()
	{
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("spawnpoint_ship"));
		MatrixFrame shipFrame = gameEntity.GetGlobalFrame();
		shipFrame.origin = new Vec3(z: Mission.Current.Scene.GetWaterLevelAtPosition(gameEntity.GlobalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false), x: gameEntity.GlobalPosition.x, y: gameEntity.GlobalPosition.y);
		Team team = Mission.GetTeam(TeamSideEnum.PlayerTeam);
		Formation formation = team.GetFormation(FormationClass.Infantry);
		Ship shipOrigin = PartyBase.MainParty.Ships.FirstOrDefault((Ship s) => s.ShipHull.StringId == "ship_knarr_storyline") ?? PartyBase.MainParty.Ships.First();
		MissionShip missionShip = missionBehavior.SpawnShip(shipOrigin, in shipFrame, team, formation);
		missionShip.ShipOrder.SetAIControllableWithoutTroops(value: true);
		missionShip.ShipOrder.SetOrderOarsmenLevel(2);
		missionShip.SetShipOrderActive(isOrderActive: false);
		return missionShip;
	}

	private void SpawnPlayerAgent()
	{
		ShipOarMachine firstScriptOfType = _entities.FirstOrDefault((GameEntity t) => t.HasTag("target_player")).Parent.GetFirstScriptOfType<ShipOarMachine>();
		firstScriptOfType.PilotStandingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: true));
		_oarUsedByPlayer = firstScriptOfType;
		WeakGameEntity gameEntity = _oarUsedByPlayer.PilotStandingPoint.GameEntity;
		Formation formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
		MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("item_set_player_captivity");
		AgentBuildData agentBuildData = new AgentBuildData(Hero.MainHero.CharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, Hero.MainHero.CharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GlobalPosition)
			.InitialDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: true)
			.Formation(formation)
			.Equipment(mBEquipmentRoster.DefaultEquipment);
		Agent agent = Mission.Current.SpawnAgent(agentBuildData);
		agent.Controller = AgentControllerType.Player;
		agent.UseGameObject(_oarUsedByPlayer.PilotStandingPoint);
		_oarUsedByPlayer.OnPilotAssignedDuringSpawn();
	}

	private void SpawnAllyAgent()
	{
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("target_ally"));
		_oarUsedByAlly = gameEntity.Parent.GetFirstScriptOfType<ShipOarMachine>();
		AgentBindsMachine firstScriptOfType = _entities.FirstOrDefault((GameEntity t) => t.HasTag("agentbind_ally")).GetFirstScriptOfType<AgentBindsMachine>();
		firstScriptOfType.SetOarMachine(_oarUsedByAlly);
		_agentBindMachines.Add(firstScriptOfType);
		WeakGameEntity gameEntity2 = _oarUsedByAlly.PilotStandingPoint.GameEntity;
		MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("item_set_gangradir_captivity");
		Formation formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
		AgentBuildData agentBuildData = new AgentBuildData(_allyCharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, _allyCharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity2.GlobalPosition)
			.InitialDirection(gameEntity2.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false)
			.Equipment(mBEquipmentRoster.DefaultEquipment)
			.Formation(formation);
		OnAgentAssignedToOarOnSpawn(_gunnarAgent = Mission.Current.SpawnAgent(agentBuildData), _oarUsedByAlly);
	}

	private Agent SpawnAllyCrewAgent(Vec3 globalPosition, Vec2 globalDirection)
	{
		AgentBuildData agentBuildData = new AgentBuildData(_crewCharacterObject).TroopOrigin(new SimpleAgentOrigin(_crewCharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(in globalPosition)
			.InitialDirection(in globalDirection)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		Agent agent = Mission.Current.SpawnAgent(agentBuildData);
		agent.GetComponent<AgentNavalComponent>().SetCanDrown(canDrown: false);
		agent.SetTargetPosition(agent.Position.AsVec2);
		Mission.Current.GetMissionBehavior<VisualTrackerMissionBehavior>()?.RegisterLocalOnlyObject(agent);
		return agent;
	}

	private void SpawnEnemyAgents()
	{
		foreach (GameEntity item in _entities.Where((GameEntity t) => t.HasTag("spawnpoint_guard")).ToList())
		{
			AgentBuildData agentBuildData = new AgentBuildData(_enemyCharacterObject).TroopOrigin(new SimpleAgentOrigin(_enemyCharacterObject)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(item.GlobalPosition)
				.InitialDirection(item.GetGlobalFrame().rotation.f.AsVec2.Normalized())
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			if (component.AgentNavigator == null)
			{
				component.CreateAgentNavigator();
			}
			string actName = "act_drunk_trio_right";
			if (item.HasTag("guard_1"))
			{
				actName = "act_drunk_trio_middle";
			}
			else if (item.HasTag("guard_2"))
			{
				actName = "act_drunk_trio_left";
			}
			else if (item.HasTag("guard_3"))
			{
				actName = "act_drunk_trio_right";
			}
			AnimationSystemData animationSystemData = MonsterExtensions.FillAnimationSystemData(actionSet: MBGlobals.GetActionSet("as_human_hideout_bandit"), monster: agentBuildData.AgentMonster, stepSize: NavalStorylineData.Gunnar.CharacterObject.GetStepSize(), hasClippingPlane: false);
			agent.SetActionSet(ref animationSystemData);
			agent.SetActionChannel(0, ActionIndexCache.Create(actName), ignorePriority: false, (AnimFlags)0uL);
		}
	}

	private void SpawnCrewAgents()
	{
		GameEntity gameEntity = _entities.First((GameEntity t) => t.HasTag("spawnpoint_neutral_npc_1"));
		ShipOarMachine firstScriptOfType = gameEntity.Parent.GetFirstScriptOfType<ShipOarMachine>();
		AgentBindsMachine firstScriptOfType2 = _entities.First((GameEntity t) => t.HasTag("agentbind_neutral_1")).GetFirstScriptOfType<AgentBindsMachine>();
		firstScriptOfType2.SetOarMachine(firstScriptOfType);
		_agentBindMachines.Add(firstScriptOfType2);
		GameEntity gameEntity2 = _entities.First((GameEntity t) => t.HasTag("spawnpoint_neutral_npc_2"));
		ShipOarMachine firstScriptOfType3 = gameEntity2.Parent.GetFirstScriptOfType<ShipOarMachine>();
		AgentBindsMachine firstScriptOfType4 = _entities.First((GameEntity t) => t.HasTag("agentbind_neutral_2")).GetFirstScriptOfType<AgentBindsMachine>();
		firstScriptOfType4.SetOarMachine(firstScriptOfType3);
		_agentBindMachines.Add(firstScriptOfType4);
		GameEntity gameEntity3 = _entities.First((GameEntity t) => t.HasTag("spawnpoint_neutral_npc_3"));
		ShipOarMachine firstScriptOfType5 = gameEntity3.Parent.GetFirstScriptOfType<ShipOarMachine>();
		AgentBindsMachine firstScriptOfType6 = _entities.First((GameEntity t) => t.HasTag("agentbind_neutral_3")).GetFirstScriptOfType<AgentBindsMachine>();
		firstScriptOfType6.SetOarMachine(firstScriptOfType5);
		_agentBindMachines.Add(firstScriptOfType6);
		GameEntity[] array = new GameEntity[3] { gameEntity, gameEntity2, gameEntity3 };
		foreach (GameEntity gameEntity4 in array)
		{
			PartyBase.MainParty.AddMember(_crewCharacterObject, 1);
			Formation formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
			AgentBuildData agentBuildData = new AgentBuildData(_crewCharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, _crewCharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity4.GlobalPosition)
				.InitialDirection(gameEntity4.GetGlobalFrame().rotation.f.AsVec2.Normalized())
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false)
				.Formation(formation);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			_crewAgents.Add(agent);
			ShipOarMachine firstScriptOfType7 = gameEntity4.Parent.GetFirstScriptOfType<ShipOarMachine>();
			OnAgentAssignedToOarOnSpawn(agent, firstScriptOfType7);
			_spawnedOarsmenCount++;
		}
	}

	private void OnAgentAssignedToOarOnSpawn(Agent agent, ShipOarMachine oarMachine)
	{
		agent.Formation?.DetachUnit(agent, isLoose: false);
		agent.Detachment = oarMachine;
		agent.UseGameObject(oarMachine.PilotStandingPoint);
		_oarAssignments.Add(agent, oarMachine);
		oarMachine.OnPilotAssignedDuringSpawn();
	}

	private void SpawnWeapon()
	{
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("pickup_weapon"));
		ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("shackle");
		MissionWeapon weapon = new MissionWeapon(item, null, null);
		_weaponEntity = Mission.Current.SpawnWeaponWithNewEntity(ref weapon, Mission.WeaponSpawnFlags.WithPhysics, gameEntity.GetGlobalFrame()).GetFirstScriptOfType<SpawnedItemEntity>();
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (userAgent.IsPlayerControlled)
		{
			OnMarkedObjectStatusChangedEvent();
		}
	}

	public override void OnObjectStoppedBeingUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (_isFinalized)
		{
			return;
		}
		if (userAgent.IsPlayerControlled && usedObject == _oarUsedByPlayer.PilotStandingPoint)
		{
			OnPlayerStartedEscape();
		}
		else if (userAgent == _gunnarAgent || userAgent.Character == _crewCharacterObject)
		{
			if (!HasTalkedToGunnar)
			{
				_savedOarsmenCount++;
				AgentBindsMachine agentBindsMachine = _agentBindMachines.FirstOrDefault((AgentBindsMachine t) => t.ShipOarMachine.PilotStandingPoint == usedObject);
				if (agentBindsMachine != null)
				{
					agentBindsMachine.PilotStandingPoint.IsDisabledForPlayers = true;
				}
				if (!_hasSavedOarsmen && _savedOarsmenCount >= _spawnedOarsmenCount + 1)
				{
					_hasSavedOarsmen = true;
					OnStartFadeOutEvent(0.75f, 1f, 0.75f);
				}
			}
			UsableMissionObject usableMissionObject = usedObject;
			if (usableMissionObject != null && usableMissionObject.GameEntity.Parent.HasScriptOfType<ShipOarMachine>())
			{
				Vec3 origin = usedObject.GameEntity.GetGlobalFrame().origin;
				WorldPosition position = new WorldPosition(base.Mission.Scene, origin);
				userAgent.SetScriptedPosition(ref position, addHumanLikeDelay: true);
			}
			else
			{
				WorldPosition position2 = userAgent.GetWorldPosition();
				userAgent.SetScriptedPosition(ref position2, addHumanLikeDelay: true);
			}
		}
		OnMarkedObjectStatusChangedEvent();
	}

	private void HandleChainVisualsAfterDialogue()
	{
		foreach (AgentBindsMachine agentBindMachine in _agentBindMachines)
		{
			agentBindMachine.Deactivate();
			agentBindMachine.GameEntity.SetVisibilityExcludeParents(visible: false);
		}
		_entities.FirstOrDefault((GameEntity t) => t.HasTag("agentbind_ally_broken"))?.SetVisibilityExcludeParents(visible: true);
		_entities.FirstOrDefault((GameEntity t) => t.HasTag("agentbind_neutral_1_broken"))?.SetVisibilityExcludeParents(visible: true);
		_entities.FirstOrDefault((GameEntity t) => t.HasTag("agentbind_neutral_2_broken"))?.SetVisibilityExcludeParents(visible: true);
		_entities.FirstOrDefault((GameEntity t) => t.HasTag("agentbind_neutral_3_broken"))?.SetVisibilityExcludeParents(visible: true);
	}

	private void OnPlayerStartedEscape()
	{
		OnPlayerStartedEscapeEvent();
		_tinkeringAction = ActionIndexCache.Create("act_cutscene_break_chains_1");
		Agent.Main.SetActionChannel(0, in _tinkeringAction, ignorePriority: false, (AnimFlags)0uL);
		_isPlayerTinkeringWithTheBindsMachine = true;
	}

	private void CheckIfPlayerIsReleasedFromOar()
	{
		if (Agent.Main.GetCurrentAction(0) == _tinkeringAction && Agent.Main.GetCurrentActionProgress(0) > 0.95f)
		{
			Agent.Main.ClearHandInverseKinematics();
			CampaignInformationManager.AddDialogLine(new TextObject("{=g1PnXEDa}{PLAYER.NAME}! It's now or never! Go, cut those bastards down!"), _allyCharacterObject, _allyCharacterObject.FirstCivilianEquipment, 1000);
			Agent.Main.OnItemPickup(_weaponEntity, EquipmentIndex.WeaponItemBeginSlot, out var _);
			_isPlayerTinkeringWithTheBindsMachine = false;
			IsPlayerFree = true;
			GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("player_shackle"));
			if (gameEntity != null)
			{
				gameEntity.SetVisibilityExcludeParents(visible: false);
			}
			_oarUsedByPlayer.PilotStandingPoint.IsDisabledForPlayers = true;
			CaptivityDefeatCaptorsObjective objective = new CaptivityDefeatCaptorsObjective(Mission.Current, this);
			_missionObjectiveLogic.StartObjective(objective);
			MissionShip.ShipOrder.SetShipStopOrder();
		}
	}

	private void TriggerEnemies()
	{
		foreach (Agent activeAgent in base.Mission.PlayerEnemyTeam.ActiveAgents)
		{
			if (activeAgent.IsAIControlled && !activeAgent.IsUsingGameObject && !activeAgent.IsAlarmed())
			{
				OnAgentEntersFight(activeAgent);
			}
		}
	}

	private void OnAgentEntersFight(Agent agent, Agent targetAgent = null)
	{
		AgentFlag agentFlags = agent.GetAgentFlags();
		agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
		agent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL);
		CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
		AgentNavigator agentNavigator = component.AgentNavigator ?? component.CreateAgentNavigator();
		AlarmedBehaviorGroup alarmedBehaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		if (alarmedBehaviorGroup == null)
		{
			alarmedBehaviorGroup = agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
			alarmedBehaviorGroup.AddBehavior<FightBehavior>();
		}
		alarmedBehaviorGroup.SetScriptedBehavior<FightBehavior>();
		agent.SetAutomaticTargetSelection(enable: false);
		if (targetAgent == null)
		{
			targetAgent = Agent.Main;
		}
		if (targetAgent != null)
		{
			agent.SetTargetAgent(targetAgent);
			AlarmedBehaviorGroup.AlarmAgent(agent);
		}
	}

	public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (_scatteredCrew != null && _scatteredCrew.Any(((Agent, bool) x) => x.Item1 == affectedAgent))
		{
			Debug.FailedAssert("Should crew to save agent be removed", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\MissionControllers\\NavalStorylineCaptivityMissionController.cs", "OnEarlyAgentRemoved", 796);
		}
		if (affectedAgent.Team != base.Mission.PlayerEnemyTeam)
		{
			return;
		}
		TriggerEnemies();
		if (!base.Mission.PlayerEnemyTeam.ActiveAgents.IsEmpty())
		{
			return;
		}
		CampaignInformationManager.AddDialogLine(new TextObject("{=bu8MRgpS}Well done! Now, help us get these chains off."), _allyCharacterObject, _allyCharacterObject.FirstCivilianEquipment, 1000);
		foreach (AgentBindsMachine agentBindMachine in _agentBindMachines)
		{
			agentBindMachine.PilotStandingPoint.IsDisabledForPlayers = false;
		}
		OnMarkedObjectStatusChangedEvent();
		CaptivityFreePrisonersObjective objective = new CaptivityFreePrisonersObjective(Mission.Current, this);
		_missionObjectiveLogic.StartObjective(objective);
		foreach (Agent crewAgent in _crewAgents)
		{
			SkinVoiceManager.CombatVoiceNetworkPredictionType predictionType = SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction;
			crewAgent.MakeVoice(SkinVoiceManager.VoiceType.Victory, predictionType);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent == Agent.Main)
		{
			FinalizeMission();
		}
	}

	private void SpawnScatteredCrew()
	{
		_saveTargetAgentCount = 0;
		_spawnZone1 = _entities.FirstOrDefault((GameEntity t) => t.HasTag("crewmen_spawn_zone_alt_1"));
		_spawnZone2 = _entities.FirstOrDefault((GameEntity t) => t.HasTag("crewmen_spawn_zone_alt_2"));
		_entities.FirstOrDefault((GameEntity t) => t.HasTag("spawn_highlight_1")).SetVisibilityExcludeParents(visible: true);
		_entities.FirstOrDefault((GameEntity t) => t.HasTag("spawn_highlight_2")).SetVisibilityExcludeParents(visible: true);
		SpawnCrewAroundPosition(_spawnZone1.GlobalPosition, _spawnZone1.GetGlobalFrame().rotation.f.AsVec2.Normalized());
		SpawnCrewAroundPosition(_spawnZone2.GlobalPosition, _spawnZone2.GetGlobalFrame().rotation.f.AsVec2.Normalized());
		int eventGlobalIndex = SoundManager.GetEventGlobalIndex("event:/mission/ambient/special/storyline/drowning_save_us");
		_spawnZone1HelpSoundEvent = SoundEvent.CreateEvent(eventGlobalIndex, Mission.Current.Scene);
		_spawnZone1HelpSoundEvent.SetPosition(_spawnZone1.GlobalPosition);
		_spawnZone1HelpSoundEvent.Play();
		_spawnZone2HelpSoundEvent = SoundEvent.CreateEvent(eventGlobalIndex, Mission.Current.Scene);
		_spawnZone2HelpSoundEvent.SetPosition(_spawnZone2.GlobalPosition);
		_spawnZone2HelpSoundEvent.Play();
		CaptivitySaveTheCrewmenObjective objective = new CaptivitySaveTheCrewmenObjective(Mission.Current, this);
		_missionObjectiveLogic.StartObjective(objective);
	}

	private void SpawnCrewAroundPosition(Vec3 spawnGlobalPosition, Vec2 spawnGlobalDirection)
	{
		spawnGlobalPosition.z = base.Mission.Scene.GetWaterLevelAtPosition(spawnGlobalPosition.AsVec2, useWaterRenderer: false, checkWaterBodyEntities: false) - 3f;
		for (int i = 0; i < 2; i++)
		{
			Agent item = SpawnAllyCrewAgent(spawnGlobalPosition + new Vec3(MBRandom.RandomFloatRanged(1f, 4f), MBRandom.RandomFloatRanged(1f, 4f)), spawnGlobalDirection);
			_scatteredCrew.Add((item, false));
			_saveTargetAgentCount++;
		}
	}

	private void SetupPostFightConversation()
	{
		_isConversationSetupInProgress = true;
		MissionShip.SetAnchor(isAnchored: true, anchorInPlace: true);
		MissionShip.ShipOrder.SetOrderOarsmenLevel(0);
		GameEntity gameEntity = _entities.First((GameEntity t) => t.HasTag("conversation_player"));
		GameEntity gameEntity2 = _entities.First((GameEntity t) => t.HasTag("conversation_ally"));
		if (Agent.Main == null || !Agent.Main.IsActive())
		{
			RespawnMainAgent(gameEntity);
		}
		Agent.Main.AgentVisuals.SetVisible(value: false);
		for (int num = base.Mission.PlayerEnemyTeam.ActiveAgents.Count - 1; num >= 0; num--)
		{
			base.Mission.PlayerEnemyTeam.ActiveAgents[num].FadeOut(hideInstantly: true, hideMount: true);
		}
		foreach (Agent activeAgent in base.Mission.PlayerTeam.ActiveAgents)
		{
			if (!activeAgent.IsPlayerControlled && activeAgent != _gunnarAgent)
			{
				activeAgent.AgentVisuals.SetVisible(value: false);
			}
		}
		if (_gunnarAgent.IsUsingGameObject)
		{
			_gunnarAgent.StopUsingGameObject();
			_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			_gunnarAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
		}
		_gunnarAgent.TeleportToPosition(gameEntity2.GlobalPosition);
		Agent.Main.AgentVisuals.SetVisible(value: true);
		Agent.Main.TeleportToPosition(gameEntity.GlobalPosition);
		Vec3 obj = gameEntity2.GlobalPosition - Agent.Main.Position;
		OnConversationSetupEvent(obj);
		WorldPosition scriptedPosition = new WorldPosition(Mission.Current.Scene, gameEntity2.GlobalPosition);
		_gunnarAgent.SetScriptedPositionAndDirection(ref scriptedPosition, 0f - obj.AsVec2.RotationInRadians, addHumanLikeDelay: false);
		_gunnarAgent.SetMovementDirection(-obj.AsVec2.Normalized());
		_gunnarAgent.Controller = AgentControllerType.None;
	}

	private void StartPostFightConversation()
	{
		_hasTalkedToGunnar = true;
		_isConversationSetupInProgress = false;
		Campaign.Current.ConversationManager.SetupAndStartMissionConversation(_gunnarAgent, base.Mission.MainAgent, setActionsInstantly: true);
		foreach (AgentBindsMachine agentBindMachine in _agentBindMachines)
		{
			agentBindMachine.Deactivate();
			agentBindMachine.GameEntity.SetVisibilityExcludeParents(visible: false);
		}
		SetOarForceMultipliers(0.95f);
		foreach (GameEntity item in _entities.Where((GameEntity t) => t.HasScriptOfType<ShipControllerMachine>()))
		{
			ShipControllerMachine firstScriptOfType = item.GetFirstScriptOfType<ShipControllerMachine>();
			if (firstScriptOfType != null)
			{
				firstScriptOfType.PilotStandingPoint.IsDisabledForPlayers = false;
			}
		}
		OnMarkedObjectStatusChangedEvent();
		Mission.Current.SetMissionMode(MissionMode.Conversation, atStart: true);
	}

	private void SetOarForceMultipliers(float forceMultiplier)
	{
		MissionShip.SetOarAppliedForceMultiplierForStoryMission(forceMultiplier);
	}

	private void CategorizeOars()
	{
		foreach (GameEntity child in _entities.FirstOrDefault((GameEntity t) => t.HasTag("left_oar")).GetChildren())
		{
			IEnumerable<ShipOarMachine> scriptComponents = child.GetScriptComponents<ShipOarMachine>();
			if (!scriptComponents.IsEmpty())
			{
				ShipOarMachine item = scriptComponents.FirstOrDefault();
				_leftOars.Add(item);
			}
		}
		foreach (GameEntity child2 in _entities.FirstOrDefault((GameEntity t) => t.HasTag("right_oar")).GetChildren())
		{
			IEnumerable<ShipOarMachine> scriptComponents2 = child2.GetScriptComponents<ShipOarMachine>();
			if (!scriptComponents2.IsEmpty())
			{
				ShipOarMachine item2 = scriptComponents2.FirstOrDefault();
				_rightOars.Add(item2);
			}
		}
	}

	private ShipOarMachine GetOarMachineToUse()
	{
		IEnumerable<ShipOarMachine> source = _leftOars.Where((ShipOarMachine t) => !t.PilotStandingPoint.HasUser && !t.PilotStandingPoint.HasAIMovingTo && _oarAssignments.All((KeyValuePair<Agent, ShipOarMachine> k) => k.Value != t));
		IEnumerable<ShipOarMachine> source2 = _rightOars.Where((ShipOarMachine t) => !t.PilotStandingPoint.HasUser && !t.PilotStandingPoint.HasAIMovingTo && _oarAssignments.All((KeyValuePair<Agent, ShipOarMachine> k) => k.Value != t));
		if (source.Count() <= source2.Count())
		{
			return source2.FirstOrDefault();
		}
		return source.FirstOrDefault();
	}

	private void RespawnMainAgent(GameEntity respawnPositionEntity)
	{
		WasPlayerKnockedOut = true;
		AgentBuildData agentBuildData = new AgentBuildData(Hero.MainHero.CharacterObject).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, Hero.MainHero.CharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(respawnPositionEntity.GlobalPosition)
			.InitialDirection(respawnPositionEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized())
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		Mission.Current.SpawnAgent(agentBuildData).Controller = AgentControllerType.Player;
	}

	private void ReenableAllOars()
	{
		IEnumerable<GameEntity> enumerable = _entities.Where((GameEntity t) => t.HasScriptOfType<UsableMachine>());
		Formation formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
		foreach (GameEntity item in enumerable)
		{
			UsableMachine firstScriptOfType = item.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType is ShipOarMachine && !formation.Detachments.Contains(firstScriptOfType))
			{
				formation.StartUsingMachine(firstScriptOfType);
			}
		}
	}

	private void InitializeUsableMachines()
	{
		foreach (GameEntity item in _entities.Where((GameEntity t) => t.HasScriptOfType<UsableMachine>()))
		{
			UsableMachine firstScriptOfType = item.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType is ShipOarMachine)
			{
				firstScriptOfType.SetEnemyRangeToStopUsing(-1f);
				if (firstScriptOfType != _oarUsedByPlayer)
				{
					firstScriptOfType.PilotStandingPoint.IsDisabledForPlayers = true;
				}
			}
			if (firstScriptOfType is ShipControllerMachine)
			{
				firstScriptOfType.PilotStandingPoint.IsDisabledForPlayers = true;
			}
		}
		foreach (AgentBindsMachine agentBindMachine in _agentBindMachines)
		{
			agentBindMachine.PilotStandingPoint.IsDisabledForPlayers = true;
		}
	}

	public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
	{
		if (agent.Character == _enemyCharacterObject && (agent.IsUsingGameObject || agent.AIInterestedInAnyGameObject()))
		{
			agent.StopUsingGameObject();
		}
	}

	public void FinalizeMission()
	{
		_isFinalized = true;
		MBMusicManager.Current.ForceStopThemeWithFadeOut();
		base.Mission.EndMission();
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		if (_isFinalized)
		{
			missionResult = MissionResult.CreateSuccessful(base.Mission);
			return true;
		}
		return false;
	}

	public void OnShipCaptured()
	{
		foreach (Agent activeAgent in base.Mission.PlayerTeam.ActiveAgents)
		{
			if (!activeAgent.IsPlayerControlled || activeAgent != _gunnarAgent)
			{
				activeAgent.AgentVisuals.SetVisible(value: true);
			}
		}
		_gunnarAgent.Controller = AgentControllerType.AI;
		_gunnarAgent.ClearTargetFrame();
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
		SpawnScatteredCrew();
		MissionShip.SetController(ShipControllerType.None);
		MissionShip.ShipOrder.SetShipStopOrder();
		MissionShip.SetInputRecord(ShipInputRecord.None());
		MissionShip.SetCustomSailSetting(enableCustomSailSetting: false, SailInput.Raised);
		MissionShip.SetShipOrderActive(isOrderActive: false);
		Formation formation = Mission.GetTeam(TeamSideEnum.PlayerTeam).GetFormation(FormationClass.Infantry);
		MissionShip.ShipOrder.SetFormation(formation);
		if (_oarUsedByAlly.PilotStandingPoint.UserAgent != null && _oarUsedByAlly.PilotStandingPoint.UserAgent != _gunnarAgent)
		{
			_oarUsedByAlly.PilotStandingPoint.UserAgent.StopUsingGameObject();
		}
		HandleChainVisualsAfterDialogue();
		MissionShip.SetAnchor(isAnchored: false);
	}

	public bool IsSavedCrew(IAgent agent)
	{
		return _savedScatteredAgents.Contains(agent);
	}

	private void OnAllCrewSaved()
	{
		_allScatteredCrewMembersAreSaved = true;
		OnStartFadeOutEvent(0.75f, 1f, 0.75f);
		_crewConversationAgent = _savedScatteredAgents[_savedScatteredAgents.Count - 1];
		if (MissionShip.IsPlayerControlled)
		{
			Agent.Main.HandleStopUsingAction();
		}
	}

	private void SetupSavedCrewConversation()
	{
		_isConversationSetupInProgress = true;
		GameEntity gameEntity = _entities.First((GameEntity t) => t.HasTag("conversation_player"));
		GameEntity gameEntity2 = _entities.First((GameEntity t) => t.HasTag("conversation_ally"));
		GameEntity gameEntity3 = _entities.First((GameEntity t) => t.HasTag("conversation_crew"));
		Agent.Main.AgentVisuals.SetVisible(value: true);
		foreach (Agent activeAgent in base.Mission.PlayerTeam.ActiveAgents)
		{
			if (!activeAgent.IsPlayerControlled && activeAgent != _gunnarAgent && activeAgent != _crewConversationAgent)
			{
				activeAgent.AgentVisuals.SetVisible(value: false);
			}
		}
		if (_gunnarAgent.IsUsingGameObject)
		{
			_gunnarAgent.StopUsingGameObject();
			_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			_gunnarAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
		}
		if (_crewConversationAgent.IsUsingGameObject)
		{
			_crewConversationAgent.StopUsingGameObject();
			_crewConversationAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			_crewConversationAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
		}
		if (Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject();
		}
		if (!MissionShip.HasController)
		{
			MissionShip.SetInputRecord(ShipInputRecord.None());
		}
		else if (MissionShip.IsAIControlled)
		{
			MissionShip.ShipOrder.SetShipStopOrder();
		}
		if (!MissionShip.Physics.IsAnchored)
		{
			MissionShip.Physics.SetAnchor(isAnchored: true);
		}
		_gunnarAgent.ClearTargetFrame();
		_gunnarAgent.TeleportToPosition(gameEntity2.GlobalPosition);
		Agent.Main.TeleportToPosition(gameEntity.GlobalPosition);
		_crewConversationAgent.TeleportToPosition(gameEntity3.GlobalPosition);
		_crewConversationAgent.ClearTargetFrame();
		WorldPosition position = new WorldPosition(base.Mission.Scene, gameEntity3.GlobalPosition);
		_crewConversationAgent.SetScriptedPosition(ref position, addHumanLikeDelay: true, Agent.AIScriptedFrameFlags.DoNotRun);
		Vec3 obj = _crewConversationAgent.Position - Agent.Main.Position;
		OnConversationSetupEvent(obj);
		_crewConversationAgent.SetMovementDirection(-obj.AsVec2.Normalized());
		_crewConversationAgent.Controller = AgentControllerType.None;
		WorldPosition scriptedPosition = new WorldPosition(Mission.Current.Scene, gameEntity2.GlobalPosition);
		_gunnarAgent.SetScriptedPositionAndDirection(ref scriptedPosition, 0f - obj.AsVec2.RotationInRadians, addHumanLikeDelay: false);
		Vec3 vec = Agent.Main.Position - gameEntity2.GlobalPosition;
		_gunnarAgent.SetMovementDirection(vec.AsVec2.Normalized());
		_gunnarAgent.Controller = AgentControllerType.None;
		MissionShip.ShipOrder.SetShipStopOrder();
	}

	private void OnPlayerReachedFirstZone()
	{
		CampaignInformationManager.AddDialogLine(new TextObject("{=wYMz91k4}Right - now let's slow down so that they can climb aboard."), _allyCharacterObject, _allyCharacterObject.FirstCivilianEquipment, 1000);
	}

	private void OnFirstHighlightCleared()
	{
		CampaignInformationManager.AddDialogLine(new TextObject("{=HuChgeJp}There's two more of them over there. Let's go fish them out."), _allyCharacterObject, _allyCharacterObject.FirstCivilianEquipment, 1000);
	}

	private void StartSavedCrewConversation()
	{
		_hasTalkedToGunnarOutro = true;
		_isConversationSetupInProgress = false;
		Mission.Current.SetMissionMode(MissionMode.Conversation, atStart: true);
		Campaign.Current.ConversationManager.SetupAndStartMissionConversation(_crewConversationAgent, base.Mission.MainAgent, setActionsInstantly: true);
		Campaign.Current.ConversationManager.AddConversationAgents(new List<IAgent> { _gunnarAgent }, setActionsInstantly: true);
	}

	public override void OnTutorialCompleted(string completedTutorialIdentifier)
	{
		if (completedTutorialIdentifier == "ShipCameraTutorial")
		{
			OnCameraTutorialFinished();
		}
	}

	private void OnCameraTutorialFinished()
	{
		CampaignInformationManager.AddDialogLine(new TextObject("{=o8Jj8RJ1}Can you see those poor lads thrashing in the water over there?"), _allyCharacterObject, _allyCharacterObject.FirstCivilianEquipment, 1000);
	}

	public ShipControllerMachine GetMarkedShipControllerMachine()
	{
		if (HasTalkedToGunnar)
		{
			Agent userAgent = MissionShip.ShipControllerMachine.PilotStandingPoint.UserAgent;
			if (userAgent == null || !userAgent.IsPlayerControlled)
			{
				return MissionShip.ShipControllerMachine;
			}
		}
		return null;
	}

	public List<AgentBindsMachine> GetMarkedAgentBinds()
	{
		return _agentBindMachines.Where((AgentBindsMachine t) => !t.PilotStandingPoint.IsDisabledForPlayers).ToList();
	}

	public List<Agent> GetScatteredCrewmen()
	{
		return _scatteredCrew.Select(((Agent, bool) t) => t.Item1).ToList();
	}

	public List<Agent> GetCaptorAgents()
	{
		return Mission.Current.PlayerEnemyTeam.ActiveAgents.ToList();
	}

	public bool IsFirstHighlightCleared()
	{
		return _savedScatteredAgents.Count((Agent t) => t.IsOnLand()) == 2;
	}

	public bool IsReadyToCloseSails()
	{
		if (IsFirstHighlightCleared() && _scatteredCrew.Count > 0)
		{
			return (_scatteredCrew.FirstOrDefault().Item1.Position - MissionShip.GlobalFrame.origin).LengthSquared <= 8100f;
		}
		return false;
	}

	public float GetStoppedShipSpeedThreshold()
	{
		return 2f;
	}

	public bool IsPlayerInShipControls()
	{
		if (MissionShip != null && Agent.Main != null)
		{
			return MissionShip.ShipControllerMachine.PilotStandingPoint.UserAgent == Agent.Main;
		}
		return false;
	}
}
