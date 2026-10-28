using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions;
using NavalDLC.Missions.AI.Tactics;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using NavalDLC.Missions.ShipInput;
using NavalDLC.Storyline.Objectives.Captivity;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
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

public class HelpingAnAllySetPieceBattleMissionController : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	private const string PlayerShipId = "longship_storyline_q1";

	private const string AllyShipId = "ship_trade_cog_q1";

	private const string EnemyShip1Id = "northern_medium_ship";

	private const string EnemyShip2Id = "ship_lightlongship_q1";

	private const string AllyShipTroopType = "vlandian_fortune_seekers";

	private const int AllyShipTroopCount = 14;

	private const int PlayerShipTroopType1Count = 32;

	private const int PlayerShipTroopType2Count = 1;

	private const int EnemyShip1TroopType1Count = 28;

	private const string EnemyShip1TroopType2 = "sea_hounds";

	private const int EnemyShip1TroopType2Count = 2;

	private const int EnemyShip2TroopType1Count = 16;

	private const int EnemyShip2TroopType2Count = 2;

	private const string PlayerShipTroopType1 = "gangradirs_kin_melee";

	private const string PlayerShipTroopType2 = "gangradirs_kin_ranged";

	private const string EnemyShip1TroopType1 = "sea_hounds_pups";

	private MissionShip _playerShip;

	private const string EnemyShip2TroopType1 = "sea_hounds_pups";

	private MissionShip _allyShip;

	private const string EnemyShip2TroopType2 = "sea_hounds";

	private MissionShip _pursuerShip1;

	private const float WindStrength = 2f;

	private const int WayPointCount = 6;

	private const float AiPlayerEngagementDistance = 10f;

	private MissionObjectiveLogic _missionObjectiveLogic;

	private NavalAgentsLogic _agentsLogic;

	private const float ShipAgentsAlarmDistance = 30f;

	private const float DefeatFadeOutDelayDuration = 2f;

	private const float DefeatFadeOutDuration = 1f;

	private const float DefeatBlackScreenDuration = 2f;

	private static readonly Dictionary<string, string> PlayerShipUpgradePieces = new Dictionary<string, string>
	{
		{ "oars", "oars_wide_lvl3" },
		{ "sail", "sails_lvl2" },
		{ "side", "side_northern_shields_lvl2" }
	};

	private static readonly Dictionary<string, string> AllyShipUpgradePieces = new Dictionary<string, string>
	{
		{ "oars", "oars_wide_lvl3" },
		{ "sail", "sails_lvl2" }
	};

	private static readonly Dictionary<string, string> Enemy1ShipUpgradePieces = new Dictionary<string, string>
	{
		{ "sail", "sails_lvl2" },
		{ "side", "side_northern_shields_lvl1" }
	};

	private static readonly Dictionary<string, string> Enemy2ShipUpgradePieces = new Dictionary<string, string>
	{
		{ "sail", "sails_lvl2" },
		{ "side", "side_northern_shields_lvl1" }
	};

	private List<GameEntity> _entities = new List<GameEntity>();

	private MobileParty _merchantParty;

	private MobileParty _seaHoundsParty;

	private MissionShip _pursuerShip2;

	private List<GameEntity> _waypoints = new List<GameEntity>();

	private bool _isAllyBoardedNotificationGiven;

	private int _currentWaypointIndex;

	private bool _isMissionInitialized;

	private bool _isMissionSuccessful;

	private bool _isAllyAboutToBeBoardedNotificationGiven;

	private bool _hasPlayerEngagedEnemyNotificationGiven;

	private bool _hasPlayerClearedFirstEnemyNotificationGiven;

	private bool _hasPlayerClearedSecondEnemyNotificationGiven;

	private bool _isPursuer1ShipEngaged;

	private bool _isMissionFailed;

	private bool _isPursuer2ShipEngaged;

	private float _drownCheckTimer;

	private float _drownCheckDuration = 3f;

	private bool _isVictoryQueued;

	private float _victoryPopUpTimer;

	private float _victoryPopUpDelay = 3f;

	private bool _isDefeatQueued;

	private bool _isFadeOutTriggered;

	private float _defeatTimer;

	private float _notificationTimer;

	public Action OnShipsInitializedEvent;

	public Action<float> OnDefeatedEvent;

	public BattleSideEnum PlayerSide => BattleSideEnum.None;

	public HelpingAnAllySetPieceBattleMissionController(MobileParty merchantParty, MobileParty seaHoundsParty)
	{
		_merchantParty = merchantParty;
		_seaHoundsParty = seaHoundsParty;
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isMissionInitialized)
		{
			_isMissionInitialized = true;
			UpdateEntityReferences();
			_agentsLogic = Mission.Current.GetMissionBehavior<NavalAgentsLogic>();
			NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
			_agentsLogic.SetDeploymentMode(value: true);
			missionBehavior.SetDeploymentMode(value: true);
			missionBehavior.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerTeam, NavalShipDeploymentLimit.Max());
			missionBehavior.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerAllyTeam, NavalShipDeploymentLimit.Max());
			missionBehavior.SetTeamShipDeploymentLimit(TeamSideEnum.EnemyTeam, NavalShipDeploymentLimit.Max());
			Team team = Mission.GetTeam(TeamSideEnum.PlayerTeam);
			Formation formation = team.GetFormation(FormationClass.Infantry);
			team.SetPlayerRole(isPlayerGeneral: true, isPlayerSergeant: true);
			Formation formation2 = Mission.GetTeam(TeamSideEnum.PlayerAllyTeam).GetFormation(FormationClass.Infantry);
			Team team2 = Mission.GetTeam(TeamSideEnum.EnemyTeam);
			Formation formation3 = team2.GetFormation(FormationClass.Infantry);
			Formation formation4 = team2.GetFormation(FormationClass.Ranged);
			_playerShip = CreateShip("longship_storyline_q1", "player_ship_sp", formation, PartyBase.MainParty, "generated_square__h4_09", DefaultFigureheads.Dragon, PlayerShipUpgradePieces);
			missionBehavior?.TeleportShip(_playerShip, _playerShip.GlobalFrame, checkFreeArea: false);
			Mission.Current.Scene.SetGlobalWindVelocity(_playerShip.GlobalFrame.rotation.f.AsVec2);
			_allyShip = CreateShip("ship_trade_cog_q1", "ally_ship_sp", formation2, _merchantParty.Party, "generated_square_l1_h4_04", null, AllyShipUpgradePieces);
			missionBehavior?.TeleportShip(_allyShip, _allyShip.GlobalFrame, checkFreeArea: false);
			_pursuerShip1 = CreateShip("northern_medium_ship", "sea_hound_ship_1_sp", formation3, _seaHoundsParty.Party, "generated_square_l1_h4_10", DefaultFigureheads.Viper, Enemy1ShipUpgradePieces);
			_pursuerShip2 = CreateShip("ship_lightlongship_q1", "sea_hound_ship_2_sp", formation4, _seaHoundsParty.Party, "generated_square_l1_h4_10", DefaultFigureheads.Ram, Enemy2ShipUpgradePieces);
			missionBehavior?.TeleportShip(_pursuerShip1, _pursuerShip1.GlobalFrame, checkFreeArea: false);
			missionBehavior?.TeleportShip(_pursuerShip2, _pursuerShip2.GlobalFrame, checkFreeArea: false);
			base.Mission.DefenderTeam.TeamAI.ClearTacticOptions();
			base.Mission.DefenderTeam.AddTacticOption(new TacticNavalLineDefense(base.Mission.DefenderTeam));
			base.Mission.AttackerTeam.TeamAI.ClearTacticOptions();
			base.Mission.AttackerTeam.AddTacticOption(new TacticNavalBalancedOffense(base.Mission.AttackerTeam));
			_playerShip.SetController(ShipControllerType.Player);
			_playerShip.SetAnchor(isAnchored: false);
			missionBehavior?.SetCanHaveConnectionCooldown(value: false);
			_pursuerShip1.SetController(ShipControllerType.AI);
			_pursuerShip2.SetController(ShipControllerType.AI);
			_pursuerShip1.ShipOrder.SetShipEngageOrder(_allyShip);
			_pursuerShip2.ShipOrder.SetShipEngageOrder(_allyShip);
			_pursuerShip1.SetShipOrderActive(isOrderActive: true);
			_pursuerShip2.SetShipOrderActive(isOrderActive: true);
			_pursuerShip1.SetCanBeTakenOver(value: false);
			_pursuerShip2.SetCanBeTakenOver(value: false);
			_allyShip.SetShipOrderActive(isOrderActive: true);
			UpdateEntityReferences();
			SpawnPlayer();
			formation.PlayerOwner = Agent.Main;
			SpawnPlayerShipAgents();
			SpawnAllyShipAgents(_allyShip);
			SpawnEnemyAgents(_pursuerShip1, "sea_hounds_pups", 28, "sea_hounds", 2);
			SpawnEnemyAgents(_pursuerShip2, "sea_hounds_pups", 16, "sea_hounds", 2);
			team.SetPlayerRole(isPlayerGeneral: true, isPlayerSergeant: true);
			foreach (Team team3 in Mission.Current.Teams)
			{
				team3.MasterOrderController.SelectAllFormations();
				team3.MasterOrderController.SetOrder(OrderType.Mount);
				team3.MasterOrderController.ClearSelectedFormations();
			}
			int i;
			for (i = 1; i <= 6; i++)
			{
				GameEntity item = _entities.FirstOrDefault((GameEntity t) => t.HasTag("volume_box_" + i));
				_waypoints.Add(item);
			}
			_agentsLogic.AssignCaptainToShipForDeploymentMode(Agent.Main, _playerShip);
			_agentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.PlayerTeam);
			_agentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.PlayerAllyTeam);
			_agentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.EnemyTeam);
			base.Mission.OnInitialSpawnCompleted();
			_agentsLogic.SetDeploymentMode(value: false);
			missionBehavior.SetDeploymentMode(value: false);
			Mission.Current.Scene.SetGlobalWindStrengthVector(_entities.First((GameEntity t) => t.HasTag("sp_wind")).GetGlobalFrame().rotation.f.AsVec2.Normalized() * 2f);
			CampaignInformationManager.AddDialogLine(new TextObject("{=FkFpeYSI}Look - there's two of them giving chase. We'll have to take one down quickly, and hope the Vlandians can hold the other off until we reach them."), NavalStorylineData.Gunnar.CharacterObject);
			HelpingAnAllyMissionObjective objective = new HelpingAnAllyMissionObjective(Mission.Current);
			_missionObjectiveLogic.StartObjective(objective);
			_playerShip.SetCustomSailSetting(enableCustomSailSetting: false, SailInput.Raised);
			OnShipsInitializedEvent();
		}
		HandleShipOrders();
		_drownCheckTimer += dt;
		if (_drownCheckTimer >= _drownCheckDuration)
		{
			_drownCheckTimer = 0f;
			CheckDrowningAgents(_pursuerShip1);
			CheckDrowningAgents(_pursuerShip2);
		}
		if (_isVictoryQueued)
		{
			_victoryPopUpTimer += dt;
			if (_victoryPopUpTimer >= _victoryPopUpDelay)
			{
				_isVictoryQueued = false;
				OpenVictoryPopUp();
			}
		}
		if (_isDefeatQueued)
		{
			_defeatTimer += dt;
			if (!_isFadeOutTriggered && _defeatTimer >= 2f)
			{
				_isFadeOutTriggered = true;
				StartDefeatFadeOut();
			}
			if (_defeatTimer >= 5f)
			{
				_isDefeatQueued = false;
				OnMissionFailed();
			}
		}
		if (!_playerShip.GetIsConnected())
		{
			_notificationTimer += dt;
			if (!(_notificationTimer > 10f))
			{
				return;
			}
			_notificationTimer = 0f;
			if (HasSailThrust())
			{
				if (_playerShip.SailTargetSetting < 1f)
				{
					CampaignInformationManager.AddDialogLine(new TextObject("{=cGay4oWJ}The wind is with us. Should we unfurl the sail?"), NavalStorylineData.Gunnar.CharacterObject);
				}
			}
			else if (_playerShip.SailTargetSetting > 0f)
			{
				CampaignInformationManager.AddDialogLine(new TextObject("{=IpjMuSVa}The wind is blowing against us. Best furl the sail."), NavalStorylineData.Gunnar.CharacterObject);
			}
		}
		else
		{
			_notificationTimer = 0f;
		}
	}

	public override void OnBehaviorInitialize()
	{
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
		_missionObjectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
	}

	private void UpdateEntityReferences()
	{
		base.Mission.Scene.GetEntities(ref _entities);
	}

	private MissionShip CreateShip(string shipHullId, string spawnPointId, Formation formation, PartyBase owner, string materialName, Figurehead figurehead = null, Dictionary<string, string> upgradePieces = null)
	{
		Ship ship = new Ship(Campaign.Current.ObjectManager.GetObject<ShipHull>(shipHullId));
		if (figurehead != null)
		{
			ship.ChangeFigurehead(figurehead);
		}
		if (upgradePieces != null)
		{
			foreach (KeyValuePair<string, string> upgradePiece in upgradePieces)
			{
				if (ship.HasSlot(upgradePiece.Key))
				{
					ship.EquipUpgradePiece(upgradePiece.Key, MBObjectManager.Instance.GetObject<ShipUpgradePiece>(upgradePiece.Value));
				}
			}
		}
		MissionShip missionShip = CreateMissionShip(ship, spawnPointId, formation);
		if (owner.MobileParty.IsBandit)
		{
			ChangeShipColors(missionShip, NavalStorylineData.CorsairBanner.GetPrimaryColor(), NavalStorylineData.CorsairBanner.GetSecondaryColor(), materialName);
		}
		else
		{
			ChangeShipColors(missionShip, owner.MapFaction.Color, owner.MapFaction.Color2, materialName);
		}
		return missionShip;
	}

	private void ChangeShipColors(MissionShip missionShip, uint color1, uint color2, string materialName)
	{
		foreach (GameEntity sailMeshEntity in missionShip.SailMeshEntities)
		{
			SetSailColors(sailMeshEntity, color1, color2, materialName);
		}
	}

	private void SetSailColors(GameEntity sailEntity, uint sailColor1, uint sailColor2, string materialName)
	{
		if (sailEntity.Skeleton != null)
		{
			foreach (Mesh allMesh in sailEntity.Skeleton.GetAllMeshes())
			{
				if (allMesh.HasTag("faction_color"))
				{
					Material fromResource = Material.GetFromResource(materialName);
					if (fromResource != null)
					{
						allMesh.SetMaterial(fromResource);
					}
					allMesh.Color = sailColor1;
					allMesh.Color2 = sailColor2;
				}
			}
		}
		foreach (Mesh item in sailEntity.WeakEntity.GetAllMeshesWithTag("faction_color"))
		{
			item.Color = sailColor1;
			item.Color2 = sailColor2;
		}
	}

	private void OnShipsEngaged(MissionShip ship1, MissionShip ship2)
	{
		int activeAgentCountOfShip = _agentsLogic.GetActiveAgentCountOfShip(ship1);
		int activeAgentCountOfShip2 = _agentsLogic.GetActiveAgentCountOfShip(ship2);
		if (activeAgentCountOfShip > 0 && activeAgentCountOfShip2 > 0)
		{
			ship1.ShipOrder.SetShipEngageOrder(ship2);
			ship2.ShipOrder.SetShipEngageOrder(ship1);
			AddFightBehaviors(ship1);
			AddFightBehaviors(ship2);
		}
	}

	private void HandleShipOrders()
	{
		if (AreShipsWithinDistance(_pursuerShip1, _playerShip, 30f))
		{
			OnShipsEngaged(_pursuerShip1, _playerShip);
			_isPursuer1ShipEngaged = true;
		}
		else if (_isPursuer1ShipEngaged)
		{
			CalmAgentsOfShip(_playerShip);
			CalmAgentsOfShip(_pursuerShip1);
			_isPursuer1ShipEngaged = false;
		}
		if (AreShipsWithinDistance(_pursuerShip2, _playerShip, 30f))
		{
			OnShipsEngaged(_pursuerShip2, _playerShip);
			_isPursuer2ShipEngaged = true;
		}
		else if (_isPursuer2ShipEngaged)
		{
			CalmAgentsOfShip(_playerShip);
			CalmAgentsOfShip(_pursuerShip2);
			_isPursuer2ShipEngaged = false;
		}
		if (AreShipsWithinDistance(_pursuerShip1, _allyShip, 30f))
		{
			OnShipsEngaged(_pursuerShip1, _allyShip);
			OnMerchantsAboutToBeBoarded();
		}
		if (AreShipsWithinDistance(_pursuerShip2, _allyShip, 30f))
		{
			OnShipsEngaged(_pursuerShip2, _allyShip);
			OnMerchantsAboutToBeBoarded();
		}
		if (AreShipsWithinDistance(_pursuerShip1, _playerShip, 10f))
		{
			_pursuerShip1.ShipOrder.SetShipEngageOrder(_playerShip);
			_pursuerShip1.ShipOrder.SetBoardingTargetShip(_playerShip);
		}
		else if (!_pursuerShip1.GetIsConnected())
		{
			_pursuerShip1.ShipOrder.SetShipEngageOrder(_allyShip);
			_pursuerShip1.ShipOrder.SetBoardingTargetShip(_allyShip);
		}
		if (AreShipsWithinDistance(_pursuerShip2, _playerShip, 10f))
		{
			_pursuerShip2.ShipOrder.SetShipEngageOrder(_playerShip);
			_pursuerShip2.ShipOrder.SetBoardingTargetShip(_playerShip);
		}
		else if (!_pursuerShip2.GetIsConnected())
		{
			_pursuerShip2.ShipOrder.SetShipEngageOrder(_allyShip);
			_pursuerShip2.ShipOrder.SetBoardingTargetShip(_allyShip);
		}
		GameEntity gameEntity = _waypoints[_currentWaypointIndex];
		if ((gameEntity.GlobalPosition - _allyShip.GlobalFrame.origin).LengthSquared <= 10000f)
		{
			_currentWaypointIndex = (_currentWaypointIndex + 1) % 6;
		}
		_allyShip.ShipOrder.SetShipMovementOrder(gameEntity.GlobalPosition.AsVec2);
		if (!_isAllyBoardedNotificationGiven && (_allyShip.GetIsThereActiveBridgeTo(_pursuerShip1) || _allyShip.GetIsThereActiveBridgeTo(_pursuerShip2)))
		{
			_isAllyBoardedNotificationGiven = true;
			CampaignInformationManager.AddDialogLine(new TextObject("{=J83UkY9F}They're boarding the Vlandians!"), NavalStorylineData.Gunnar.CharacterObject);
		}
		if (!_hasPlayerEngagedEnemyNotificationGiven && (_playerShip.GetIsThereActiveBridgeTo(_pursuerShip1) || _playerShip.GetIsThereActiveBridgeTo(_pursuerShip2)))
		{
			_hasPlayerEngagedEnemyNotificationGiven = true;
			CampaignInformationManager.AddDialogLine(new TextObject("{=LABFnNwV}The grapples have caught. Cut them down!"), NavalStorylineData.Gunnar.CharacterObject);
		}
	}

	private void OnMerchantsAboutToBeBoarded()
	{
		if (!_isAllyAboutToBeBoardedNotificationGiven)
		{
			_isAllyAboutToBeBoardedNotificationGiven = true;
			CampaignInformationManager.AddDialogLine(new TextObject("{=Iy0a0ucw}I think the Vlandians are about to be overtaken and boarded."), NavalStorylineData.Gunnar.CharacterObject);
		}
	}

	private MissionShip CreateMissionShip(Ship ship, string spawnPointId, Formation formation)
	{
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag(spawnPointId));
		MatrixFrame shipFrame = gameEntity.GetGlobalFrame();
		shipFrame.origin = new Vec3(z: Mission.Current.Scene.GetWaterLevelAtPosition(gameEntity.GlobalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false), x: gameEntity.GlobalPosition.x, y: gameEntity.GlobalPosition.y);
		return missionBehavior.SpawnShip(ship, in shipFrame, formation.Team, formation);
	}

	private void SpawnPlayer()
	{
		WeakGameEntity weakGameEntity = _playerShip.GameEntity.CollectChildrenEntitiesWithTag("sp_troop_captain").FirstOrDefault();
		Formation formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
		AgentBuildData agentBuildData = new AgentBuildData(Hero.MainHero.CharacterObject).TroopOrigin(new SimpleAgentOrigin(Hero.MainHero.CharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(weakGameEntity.GlobalPosition)
			.InitialDirection(weakGameEntity.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false)
			.Formation(formation);
		Mission.Current.SpawnAgent(agentBuildData).Controller = AgentControllerType.Player;
		_agentsLogic.AddAgentToShip(Agent.Main, _playerShip);
	}

	private void SpawnPlayerShipAgents()
	{
		NavalAgentsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		int num = 33;
		missionBehavior.SetDesiredTroopCountOfShip(_playerShip, num + 1);
		CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>("gangradirs_kin_melee");
		CharacterObject characterObject2 = Campaign.Current.ObjectManager.GetObject<CharacterObject>("gangradirs_kin_ranged");
		int deckFrameCount = _playerShip.DeckFrameCount;
		for (int i = 0; i < deckFrameCount && i < num; i++)
		{
			CharacterObject characterObject3 = characterObject;
			if (i >= 32)
			{
				characterObject3 = characterObject2;
			}
			MatrixFrame nextOuterInnerSpawnGlobalFrame = _playerShip.GetNextOuterInnerSpawnGlobalFrame();
			AgentBuildData agentBuildData = new AgentBuildData(characterObject3).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, characterObject3, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerTeam).InitialPosition(in nextOuterInnerSpawnGlobalFrame.origin)
				.InitialDirection(nextOuterInnerSpawnGlobalFrame.rotation.f.AsVec2.Normalized())
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			missionBehavior.AddAgentToShip(agent, _playerShip);
		}
	}

	private void SpawnEnemyAgents(MissionShip ship, string troopType1, int troopType1Count, string troopType2, int troopType2Count)
	{
		int num = troopType1Count + troopType2Count;
		NavalAgentsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		missionBehavior.SetDesiredTroopCountOfShip(ship, num);
		CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>(troopType1);
		CharacterObject characterObject2 = Campaign.Current.ObjectManager.GetObject<CharacterObject>(troopType2);
		int deckFrameCount = ship.DeckFrameCount;
		for (int i = 0; i < deckFrameCount; i++)
		{
			CharacterObject characterObject3 = characterObject;
			if (i < num)
			{
				if (i >= troopType1Count)
				{
					characterObject3 = characterObject2;
				}
				MatrixFrame nextOuterInnerSpawnGlobalFrame = ship.GetNextOuterInnerSpawnGlobalFrame();
				AgentBuildData agentBuildData = new AgentBuildData(characterObject3).TroopOrigin(new PartyAgentOrigin(_seaHoundsParty.Party, characterObject3, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(in nextOuterInnerSpawnGlobalFrame.origin)
					.InitialDirection(nextOuterInnerSpawnGlobalFrame.rotation.f.AsVec2.Normalized())
					.NoHorses(noHorses: true)
					.NoWeapons(noWeapons: false);
				Agent agent = Mission.Current.SpawnAgent(agentBuildData);
				missionBehavior.AddAgentToShip(agent, ship);
				continue;
			}
			break;
		}
	}

	private void SpawnAllyShipAgents(MissionShip ship)
	{
		NavalAgentsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		missionBehavior.SetDesiredTroopCountOfShip(ship, 14);
		CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>("vlandian_fortune_seekers");
		int deckFrameCount = ship.DeckFrameCount;
		for (int i = 0; i < deckFrameCount && i < 14; i++)
		{
			MatrixFrame nextOuterInnerSpawnGlobalFrame = ship.GetNextOuterInnerSpawnGlobalFrame();
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new PartyAgentOrigin(_merchantParty.Party, characterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerAllyTeam).InitialPosition(in nextOuterInnerSpawnGlobalFrame.origin)
				.InitialDirection(nextOuterInnerSpawnGlobalFrame.rotation.f.AsVec2.Normalized())
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			missionBehavior.AddAgentToShip(agent, ship);
			ship.Formation.PlayerOwner = Agent.Main;
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (_isMissionFailed || _isMissionSuccessful || Mission.Current.CurrentState != Mission.State.Continuing)
		{
			return;
		}
		if (_isPursuer1ShipEngaged && _agentsLogic.GetActiveAgentCountOfShip(_pursuerShip1) == 0)
		{
			CalmAgentsOfShip(_playerShip);
			_isPursuer1ShipEngaged = false;
			if (!_hasPlayerClearedFirstEnemyNotificationGiven)
			{
				_hasPlayerClearedFirstEnemyNotificationGiven = true;
				CampaignInformationManager.AddDialogLine(new TextObject("{=Xjm7x5vu}Hah! That's the end of them! Now, about the other one..."), NavalStorylineData.Gunnar.CharacterObject);
			}
		}
		if (_isPursuer2ShipEngaged && _agentsLogic.GetActiveAgentCountOfShip(_pursuerShip2) == 0)
		{
			CalmAgentsOfShip(_playerShip);
			_isPursuer2ShipEngaged = false;
			if (!_hasPlayerClearedSecondEnemyNotificationGiven)
			{
				_hasPlayerClearedSecondEnemyNotificationGiven = true;
				CampaignInformationManager.AddDialogLine(new TextObject("{=2lX2bIwy}That's the last of them!"), NavalStorylineData.Gunnar.CharacterObject);
			}
		}
		MBReadOnlyList<Agent> activeAgents = base.Mission.PlayerAllyTeam.ActiveAgents;
		if (activeAgents != null && !_isDefeatQueued && !_isVictoryQueued)
		{
			if ((float)activeAgents.Count <= 2.8f || base.Mission.PlayerTeam.ActiveAgents.IsEmpty())
			{
				StartDefeatSequence();
			}
			else if (activeAgents.Count == 7)
			{
				CampaignInformationManager.AddDialogLine(new TextObject("{=zdQoMBZd}Most of the Vlandians are down! We haven't much time!"), NavalStorylineData.Gunnar.CharacterObject);
			}
		}
		if (_isMissionSuccessful)
		{
			return;
		}
		MBReadOnlyList<Agent> activeAgentsOfShip = _agentsLogic.GetActiveAgentsOfShip(_pursuerShip1);
		MBReadOnlyList<Agent> activeAgentsOfShip2 = _agentsLogic.GetActiveAgentsOfShip(_pursuerShip2);
		if (activeAgentsOfShip != null && activeAgentsOfShip2 != null)
		{
			IEnumerable<Agent> source = activeAgentsOfShip.Where((Agent t) => t.Team == base.Mission.PlayerEnemyTeam);
			IEnumerable<Agent> source2 = activeAgentsOfShip2.Where((Agent t) => t.Team == base.Mission.PlayerEnemyTeam);
			if (source.IsEmpty() && source2.IsEmpty())
			{
				OnAllPursuingShipsDefeated();
			}
		}
	}

	private void CheckDrowningAgents(MissionShip ship)
	{
		foreach (Agent item in _agentsLogic.GetActiveAgentsOfShip(ship).ToList())
		{
			if (!item.IsMainAgent && item.Team != base.Mission.PlayerTeam && item.CurrentMortalityState == Agent.MortalityState.Mortal && item.IsInWater())
			{
				item.GetComponent<AgentNavalComponent>()?.DrownAgent();
			}
		}
	}

	private void CalmAgentsOfShip(MissionShip targetShip)
	{
		foreach (Agent item in _agentsLogic.GetActiveAgentsOfShip(targetShip))
		{
			item.SetAlarmState(Agent.AIStateFlag.None);
			item.GetComponent<CampaignAgentComponent>().AgentNavigator?.RemoveBehaviorGroup<AlarmedBehaviorGroup>();
		}
	}

	private bool AreShipsWithinDistance(MissionShip ship1, MissionShip ship2, float distance)
	{
		return (ship1.GlobalFrame.origin - ship2.GlobalFrame.origin).LengthSquared <= distance * distance;
	}

	private void OnAllPursuingShipsDefeated()
	{
		_playerShip.ShipOrder.SetShipStopOrder();
		_allyShip.ShipOrder.SetShipStopOrder();
		_isVictoryQueued = true;
	}

	private void OpenVictoryPopUp()
	{
		TextObject textObject = new TextObject("{=R4Gqskgq}Victory");
		TextObject textObject2 = new TextObject("{=p0HTLZzH}After the last Sea Hound is defeated, the merchants approach you...");
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: null, affirmativeAction: OnVictoryPopUpClosed, negativeAction: null), pauseGameActiveState: true);
	}

	private void OnVictoryPopUpClosed()
	{
		_isMissionSuccessful = true;
		PlayerEncounter.Battle.SetOverrideWinner(PlayerEncounter.Battle.PlayerSide);
		base.Mission.EndMission();
	}

	private void StartDefeatSequence()
	{
		_isDefeatQueued = true;
		MBInformationManager.AddQuickInformation(new TextObject("{=fhEaEedK}Vlandian merchants have been destroyed."));
	}

	private void StartDefeatFadeOut()
	{
		OnDefeatedEvent(1f);
	}

	private void OnMissionFailed()
	{
		_isMissionFailed = true;
		PlayerEncounter.Battle.SetOverrideWinner(PlayerEncounter.Battle.GetOtherSide(PlayerEncounter.Battle.PlayerSide));
		ScreenFadeController.BeginFadeIn();
		base.Mission.EndMission();
	}

	private void AddFightBehaviors(MissionShip ship)
	{
		foreach (Agent item in _agentsLogic.GetActiveAgentsOfShip(ship))
		{
			AgentFlag agentFlags = item.GetAgentFlags();
			item.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
			CampaignAgentComponent component = item.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component.AgentNavigator;
			if (agentNavigator == null)
			{
				agentNavigator = component.CreateAgentNavigator();
			}
			AlarmedBehaviorGroup alarmedBehaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			if (alarmedBehaviorGroup == null)
			{
				alarmedBehaviorGroup = agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
				alarmedBehaviorGroup.AddBehavior<FightBehavior>();
			}
			alarmedBehaviorGroup.SetScriptedBehavior<FightBehavior>();
			item.SetAlarmState(Agent.AIStateFlag.Alarmed);
		}
	}

	private bool HasSailThrust()
	{
		Vec2 globalWindVelocity = base.Mission.Scene.GetGlobalWindVelocity();
		Vec2 windDir = _playerShip.GameEntity.GetGlobalFrame().rotation.TransformToLocal(globalWindVelocity.ToVec3()).AsVec2.Normalized();
		MBReadOnlyList<MissionSail> sails = _playerShip.Sails;
		float num = 0f;
		foreach (MissionSail item in sails)
		{
			float num2 = 0f - item.SailObject.RightRotationLimit;
			float leftRotationLimit = item.SailObject.LeftRotationLimit;
			float num3 = (leftRotationLimit - num2) * 0.01f;
			for (float num4 = num2; num4 <= leftRotationLimit; num4 += num3)
			{
				Vec2 forward = Vec2.Forward;
				forward.RotateCCW(num4);
				num += SailWindProfile.Instance.ComputeSailThrustValue(item.SailObject.Type, forward, Vec2.Forward, windDir);
			}
		}
		return num > 0.1f;
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		bool result = false;
		if (_isMissionSuccessful)
		{
			missionResult = MissionResult.CreateSuccessful(base.Mission, enemyRetreated: true);
			result = true;
		}
		else if (_isMissionFailed)
		{
			missionResult = MissionResult.CreateDefeated(base.Mission);
			result = true;
		}
		return result;
	}

	public void StartSpawner(BattleSideEnum side)
	{
	}

	public void StopSpawner(BattleSideEnum side)
	{
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		return false;
	}

	public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
	{
		return 0f;
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		return false;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		switch (side)
		{
		case BattleSideEnum.Attacker:
			return Mission.Current.PlayerEnemyTeam.ActiveAgents.Select((Agent t) => t.Origin);
		case BattleSideEnum.Defender:
		{
			List<IAgentOriginBase> list = new List<IAgentOriginBase>();
			list.AddRange(Mission.Current.PlayerTeam.ActiveAgents.Select((Agent t) => t.Origin));
			list.AddRange(Mission.Current.PlayerAllyTeam.ActiveAgents.Select((Agent t) => t.Origin));
			return list;
		}
		default:
			return null;
		}
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		return 1;
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		return false;
	}
}
