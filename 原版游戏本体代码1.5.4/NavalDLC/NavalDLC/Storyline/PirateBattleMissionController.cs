using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using NavalDLC.Storyline.Objectives.PirateBattle;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline;

public class PirateBattleMissionController : MissionLogic
{
	private const int InitialAllyMeleeTroopCount = 10;

	private const int InitialAllyRangedTroopCount = 10;

	private const int SecondPhaseMinTotalAllyTroopCount = 14;

	private const int SecondPhasePrisonerMeleeTroopCount = 7;

	private const int SecondPhasePrisonerRangedTroopCount = 7;

	private const float AfterFightShipChangeDuration = 0.5f;

	private const float BoardingImminentRadiusSqr = 2500f;

	private const float NotificationRepeatDuration = 27f;

	private const string AllyMeleeTroopStringId = "gangradirs_kin_melee";

	private const string AllyRangedTroopStringId = "gangradirs_kin_ranged";

	private const string EnemyTroopStringId = "sea_hounds_pups";

	private const float MissionStateChangeTimer = 3f;

	private const float WindStrength = 1.5f;

	private const float FadeDuration = 0.5f;

	private const float BlackScreenDuration = 0.75f;

	private static readonly Dictionary<string, string> PlayerShipUpgradePieces = new Dictionary<string, string> { { "sail", "sails_lvl2" } };

	private static readonly Dictionary<string, string> SecondShipUpgradePieces = new Dictionary<string, string> { { "sail", "sails_lvl2" } };

	private static readonly Dictionary<string, string> ReinforcementShipUpgradePieces = new Dictionary<string, string> { { "sail", "sails_lvl2" } };

	private bool _isMissionInitialized;

	private List<GameEntity> _entities = new List<GameEntity>();

	private Agent _gunnarAgent;

	private MissionShip _playerShip;

	private MissionShip _secondShip;

	private MissionShip _reinforcementShip;

	private readonly MobileParty _pirateParty;

	private MissionTimer _victoryTimer;

	private MissionTimer _defeatTimer;

	private float _notificationTimer = 15f;

	private TextObject _currentNotificationText;

	private bool _isInSecondPhase;

	private bool _isMissionSuccessful;

	private bool _isMissionFailed;

	private bool _hasShownChargeNotification;

	private bool _hasShownSecondPhaseChargeNotification;

	private bool _hasShownBoardImminentNotification;

	private bool _hasIncreasedMusicIntensityForSecondPhase;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private MissionObjectiveLogic _missionObjectiveLogic;

	private bool _isGunnarAfterFightFirstNotificationShown;

	private bool _isGunnarAfterFightSecondNotificationShown;

	private float _afterFightShipChangeTimer;

	private bool _isShipTransferQueued;

	private bool _isSecondShipSelected;

	private readonly int _pirateTroopCount;

	private bool _isDialogueQueued;

	private bool _isSecondPhaseSetup;

	private float _dialogueTimer;

	public bool IsFirstShipCleared { get; private set; }

	public bool HasSelectedShip { get; private set; }

	public event Action<float, float> OnBeginScreenFadeEvent;

	public event Action<float> OnCameraBearingNeedsUpdateEvent;

	public event Action OnShipsInitializedEvent;

	public PirateBattleMissionController(MobileParty pirateParty, int pirateTroopCount)
	{
		_pirateParty = pirateParty;
		_pirateTroopCount = pirateTroopCount;
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isMissionInitialized)
		{
			_isMissionInitialized = true;
			UpdateEntityReferences();
			Team team = Mission.GetTeam(TeamSideEnum.PlayerTeam);
			Formation formation = team.GetFormation(FormationClass.Infantry);
			Formation formation2 = Mission.GetTeam(TeamSideEnum.EnemyTeam).GetFormation(FormationClass.Infantry);
			_playerShip = CreateShip("ship_knarr_storyline_2", "spawnpoint_ship_player", formation, PartyBase.MainParty, PlayerShipUpgradePieces, "generated_square__h4_09");
			_secondShip = CreateShip("ship_lightlongship_storyline", "spawnpoint_ship_first_enemy", formation2, _pirateParty.Party, SecondShipUpgradePieces, "generated_square_l1_h4_10");
			_navalShipsLogic.TeleportShip(_playerShip, _playerShip.GlobalFrame, checkFreeArea: false);
			_navalShipsLogic.TeleportShip(_secondShip, _secondShip.GlobalFrame, checkFreeArea: false);
			UpdateEntityReferences();
			SpawnAllyTroops();
			SpawnEnemyAgents(_secondShip);
			team.SetPlayerRole(isPlayerGeneral: true, isPlayerSergeant: true);
			_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(Agent.Main, _playerShip);
			_playerShip.ShipOrder.SetOrderOarsmenLevel(2);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.PlayerTeam);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.EnemyTeam);
			base.Mission.OnInitialSpawnCompleted();
			_secondShip.SetAnchor(isAnchored: true);
			_secondShip.ShipOrder.SetShipStopOrder();
			_secondShip.SetController(ShipControllerType.None, autoUpdateController: false);
			_secondShip.Formation.SetControlledByAI(isControlledByAI: false);
			_secondShip.SetCanBeTakenOver(value: false);
			TextObject text = new TextObject("{=xz5vyQlF}They must think we're just a fishing vessel. All right now, boys, let's show them that their prey has teeth of its own!");
			ShowNotification(text);
			PirateBattlePhase1Objective objective = new PirateBattlePhase1Objective(Mission.Current, this);
			_missionObjectiveLogic.StartObjective(objective);
			Mission.Current.PlayerTeam.PlayerOrderController.OnOrderIssued += OnPlayerOrdered;
			this.OnShipsInitializedEvent();
			Vec2 windVector = _entities.FirstOrDefault((GameEntity t) => t.HasTag("sp_wind")).GetGlobalFrame().rotation.f.NormalizedCopy().AsVec2 * 1.5f;
			Mission.Current.Scene.SetGlobalWindStrengthVector(in windVector);
			MBMusicManager.Current.StartThemeWithConstantIntensity(MusicTheme.VikingSeaBattle2);
			MBMusicManager.Current.ChangeCurrentThemeIntensity(0.2f);
		}
		if (_defeatTimer != null && _defeatTimer.Check())
		{
			_defeatTimer = null;
			OnPlayerTeamDefeated();
		}
		if (_victoryTimer != null && _victoryTimer.Check())
		{
			_victoryTimer = null;
			OnEnemyTeamDefeated();
		}
		if (_isInSecondPhase && HasSelectedShip)
		{
			if (!_isGunnarAfterFightFirstNotificationShown)
			{
				_isGunnarAfterFightFirstNotificationShown = true;
				_currentNotificationText = new TextObject("{=Ni85tv1G}I think I see them. Untie our ships, and let's have at it!");
			}
			else if (!_isGunnarAfterFightSecondNotificationShown && !_playerShip.GetIsThereActiveBridgeTo(_secondShip))
			{
				_isGunnarAfterFightSecondNotificationShown = true;
				_currentNotificationText = new TextObject("{=BfzIsraW}I'll let you decide how to fight this one. Maneuver a bit, or just go straight at them?");
				PirateBattlePhase2Objective objective2 = new PirateBattlePhase2Objective(Mission.Current, this);
				_missionObjectiveLogic.StartObjective(objective2);
			}
		}
		_notificationTimer += dt;
		if (!_isInSecondPhase)
		{
			if (_playerShip.GetIsConnectedToEnemy())
			{
				if (!_hasShownChargeNotification)
				{
					ShowChargeNotification();
				}
			}
			else if ((_playerShip.GameEntity.GlobalPosition - _secondShip.GameEntity.GlobalPosition).LengthSquared >= 2500f)
			{
				_hasShownBoardImminentNotification = false;
				if (_notificationTimer > 27f)
				{
					_notificationTimer = 0f;
					_currentNotificationText = new TextObject("{=gMhrY6rz}Get us close so we can board.");
				}
			}
			else if (!_hasShownBoardImminentNotification)
			{
				_hasShownBoardImminentNotification = true;
				_currentNotificationText = new TextObject("{=GtSpVtOq}Get ready to board...");
				MBMusicManager.Current.ChangeCurrentThemeIntensity(0.5f);
			}
		}
		else
		{
			if (!_hasShownSecondPhaseChargeNotification && _isGunnarAfterFightSecondNotificationShown && (_playerShip.GetIsConnectedToEnemy() || _secondShip.GetIsConnectedToEnemy()))
			{
				ShowSecondPhaseChargeNotification();
			}
			if (_playerShip.GetIsConnectedToEnemy() && !_hasIncreasedMusicIntensityForSecondPhase)
			{
				MBMusicManager.Current.ChangeCurrentThemeIntensity(0.5f);
				_hasIncreasedMusicIntensityForSecondPhase = true;
			}
		}
		if (_currentNotificationText != null)
		{
			ShowNotification(_currentNotificationText);
		}
		if (_isDialogueQueued)
		{
			_dialogueTimer += dt;
			if (!_isSecondPhaseSetup && _dialogueTimer > 0.5f)
			{
				SetupSecondPhase();
			}
			if (_dialogueTimer > 1.25f)
			{
				StartDialogue();
			}
		}
		if (_isShipTransferQueued)
		{
			_afterFightShipChangeTimer += dt;
			if (_afterFightShipChangeTimer >= 0.5f)
			{
				_isShipTransferQueued = false;
				HandleShipSelection(!_isSecondShipSelected);
			}
		}
	}

	private void UpdateEntityReferences()
	{
		base.Mission.Scene.GetEntities(ref _entities);
	}

	public override void OnBehaviorInitialize()
	{
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_missionObjectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
	}

	private void SpawnAllyTroops()
	{
		CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>("gangradirs_kin_melee");
		CharacterObject characterObject2 = Campaign.Current.ObjectManager.GetObject<CharacterObject>("gangradirs_kin_ranged");
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_playerShip, 22);
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, NavalStorylineData.Gunnar.CharacterObject), _playerShip);
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, CharacterObject.PlayerCharacter), _playerShip);
		for (int i = 0; i < 10; i++)
		{
			_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, characterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true), _playerShip);
		}
		for (int j = 0; j < 10; j++)
		{
			_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, characterObject2, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true), _playerShip);
		}
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		_gunnarAgent = base.Mission.Agents.FirstOrDefault((Agent x) => x.Character == NavalStorylineData.Gunnar.CharacterObject);
		_gunnarAgent.ToggleInvulnerable();
		NavalStorylineData.Gunnar.SetHasMet();
		_playerShip.Formation.PlayerOwner = Agent.Main;
		Mission.Current.PlayerTeam.PlayerOrderController.Owner = Agent.Main;
	}

	private Agent SpawnHero(CharacterObject character, string spawnPointTag)
	{
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag(spawnPointTag));
		AgentBuildData agentBuildData = new AgentBuildData(character).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, character, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true)).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GlobalPosition)
			.InitialDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: false);
		return Mission.Current.SpawnAgent(agentBuildData);
	}

	private void SpawnEnemyAgents(MissionShip ship)
	{
		CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>("sea_hounds_pups");
		_navalAgentsLogic.SetDesiredTroopCountOfShip(ship, _pirateTroopCount);
		for (int i = 0; i < _pirateTroopCount; i++)
		{
			PartyAgentOrigin partyAgentOrigin = new PartyAgentOrigin(_pirateParty.Party, characterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true);
			partyAgentOrigin.SetBanner(NavalStorylineData.CorsairBanner);
			_navalAgentsLogic.AddReservedTroopToShip(partyAgentOrigin, ship);
		}
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.EnemyTeam);
	}

	private void SpawnAllyPrisonerAgents(MissionShip ship)
	{
		CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>("gangradirs_kin_melee");
		CharacterObject characterObject2 = Campaign.Current.ObjectManager.GetObject<CharacterObject>("gangradirs_kin_ranged");
		_navalAgentsLogic.SetDesiredTroopCountOfShip(ship, 16);
		for (int i = 0; i < 7; i++)
		{
			_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, characterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true), ship);
		}
		for (int j = 0; j < 7; j++)
		{
			_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, characterObject2, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true), ship);
		}
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
	}

	private MissionShip CreateShip(string shipHullId, string spawnPointId, Formation formation, PartyBase owner, Dictionary<string, string> upgradePieces, string materialName)
	{
		Ship ship = new Ship(Campaign.Current.ObjectManager.GetObject<ShipHull>(shipHullId));
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
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag(spawnPointId));
		MatrixFrame shipFrame = gameEntity.GetGlobalFrame();
		float waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(gameEntity.GlobalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
		shipFrame.origin = new Vec3(gameEntity.GlobalPosition.x, gameEntity.GlobalPosition.y, waterLevelAtPosition);
		MissionShip missionShip = _navalShipsLogic.SpawnShip(ship, in shipFrame, formation.Team, formation);
		ChangeShipColors(missionShip, owner.MapFaction.Color, owner.MapFaction.Color2, materialName);
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

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		bool num;
		if (!IsFirstShipCleared)
		{
			num = IsShipEffectivelyDepleted(_secondShip);
		}
		else
		{
			if (_reinforcementShip == null)
			{
				goto IL_0047;
			}
			num = IsShipEffectivelyDepleted(_reinforcementShip);
		}
		if (num && _defeatTimer == null)
		{
			_victoryTimer = new MissionTimer(3f);
		}
		goto IL_0047;
		IL_0047:
		if (base.Mission.PlayerTeam.ActiveAgents.IsEmpty() || affectedAgent.IsMainAgent)
		{
			_defeatTimer = new MissionTimer(3f);
			_victoryTimer = null;
		}
	}

	private bool IsShipEffectivelyDepleted(MissionShip ship)
	{
		bool result = true;
		foreach (Agent item in _navalAgentsLogic.GetActiveAgentsOfShip(ship))
		{
			if (!item.IsInWater())
			{
				result = false;
				break;
			}
		}
		return result;
	}

	private void OnEnemyTeamDefeated()
	{
		if (!IsFirstShipCleared)
		{
			IsFirstShipCleared = true;
			OnFirstEnemyShipCleared();
		}
		else
		{
			OnSecondEnemyShipCleared();
		}
	}

	private void ShowNotification(TextObject text)
	{
		CampaignInformationManager.AddDialogLine(text, NavalStorylineData.Gunnar.CharacterObject);
		_currentNotificationText = null;
	}

	private void OnFirstEnemyShipCleared()
	{
		if (Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject();
		}
		TextObject textObject = new TextObject("{=pn7YqjAE}Ship Cleared");
		TextObject textObject2 = new TextObject("{=6UauyvuX}Your men make quick work of the pirates. As the fighting dies down, you find that the Sea Hounds were carrying captives, bound and stashed beneath the rowing benches. You cut their bonds and help them to their feet as your lookouts scan the waters for any sign of the second ship.");
		InquiryData data = new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: null, affirmativeAction: OnFirstFightPopUpClosed, negativeAction: null);
		MBMusicManager.Current.ChangeCurrentThemeIntensity(-0.5f);
		InformationManager.ShowInquiry(data, Campaign.Current.GameMode == CampaignGameMode.Campaign);
	}

	private void OnFirstFightPopUpClosed()
	{
		_isDialogueQueued = true;
		this.OnBeginScreenFadeEvent?.Invoke(0.5f, 0.75f);
	}

	private void SetupSecondPhase()
	{
		_isSecondPhaseSetup = true;
		Formation formation = Mission.GetTeam(TeamSideEnum.EnemyTeam).GetFormation(FormationClass.Ranged);
		_reinforcementShip = CreateShip("ship_lightlongship_storyline", "spawnpoint_ship_reinforcement", formation, _pirateParty.Party, ReinforcementShipUpgradePieces, "generated_square_l1_h4_10");
		_reinforcementShip.FinalizeDeployment(initializeMachines: true);
		SpawnEnemyAgents(_reinforcementShip);
		MatrixFrame globalFrame = _playerShip.GlobalFrame;
		Vec2 position = globalFrame.origin.AsVec2;
		Vec2 direction = globalFrame.rotation.f.AsVec2.Normalized();
		_playerShip.SetAnchor(isAnchored: true);
		_playerShip.SetAnchorFrame(in position, in direction);
		if (_gunnarAgent == null || !_gunnarAgent.IsActive())
		{
			_gunnarAgent = SpawnHero(NavalStorylineData.Gunnar.CharacterObject, "conversation_ally");
			_gunnarAgent.ToggleInvulnerable();
		}
		_gunnarAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
		_gunnarAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
		Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		_playerShip.ShipOrder.SetOrderOarsmenLevel(2);
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(_playerShip);
		_navalAgentsLogic.SetDeploymentMode(value: false);
		if (Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject();
		}
		if (_gunnarAgent.IsUsingGameObject)
		{
			_gunnarAgent.StopUsingGameObject();
		}
		_gunnarAgent.TryAttachToFormation();
		_gunnarAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
		_gunnarAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
		Agent.Main.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
		Agent.Main.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
		GameEntity gameEntity = _entities.Last((GameEntity t) => t.HasTag("conversation_ally"));
		_gunnarAgent.TeleportToPosition(gameEntity.GlobalPosition);
		GameEntity gameEntity2 = _entities.Last((GameEntity t) => t.HasTag("conversation_player"));
		base.Mission.MainAgent.TeleportToPosition(gameEntity2.GlobalPosition);
		Agent.Main.SetLookAgent(_gunnarAgent);
		Vec3 vec = Agent.Main.Position - _gunnarAgent.Position;
		_gunnarAgent.SetMovementDirection(vec.AsVec2.Normalized());
		_gunnarAgent.SetLookAgent(Agent.Main);
		_gunnarAgent.Controller = AgentControllerType.None;
		this.OnCameraBearingNeedsUpdateEvent((-vec).RotationZ);
		_reinforcementShip.SetAnchor(isAnchored: true);
		_reinforcementShip.ShipOrder.SetShipStopOrder();
		_reinforcementShip.SetController(ShipControllerType.AI);
		_reinforcementShip.SetCanBeTakenOver(value: false);
		Agent.Main.Health = Agent.Main.HealthLimit;
		foreach (ShipAttachmentPointMachine item in base.Mission.ActiveMissionObjects.FindAllWithType<ShipAttachmentPointMachine>().ToList())
		{
			item.PilotStandingPoint.IsDisabledForPlayers = true;
		}
		foreach (ShipAttachmentMachine item2 in base.Mission.ActiveMissionObjects.FindAllWithType<ShipAttachmentMachine>().ToList())
		{
			item2.PilotStandingPoint.IsDisabledForPlayers = true;
		}
	}

	private void StartDialogue()
	{
		_isDialogueQueued = false;
		Campaign.Current.ConversationManager.SetupAndStartMissionConversation(_gunnarAgent, base.Mission.MainAgent, setActionsInstantly: true);
		base.Mission.SetMissionMode(MissionMode.Conversation, atStart: true);
	}

	public void OnPlayerSelectedFirstShipToCommand()
	{
		_isSecondShipSelected = false;
		OnPlayerSelectedShipToCommand();
	}

	public void OnPlayerSelectedSecondShipToCommand()
	{
		_isSecondShipSelected = true;
		OnPlayerSelectedShipToCommand();
	}

	private void OnPlayerSelectedShipToCommand()
	{
		_isInSecondPhase = true;
		_isShipTransferQueued = true;
		PirateBattleCutLooseObjective objective = new PirateBattleCutLooseObjective(Mission.Current, this);
		_missionObjectiveLogic.StartObjective(objective);
		this.OnBeginScreenFadeEvent?.Invoke(0.5f, 0.75f);
	}

	private void HandleShipSelection(bool isFirstShipSelected)
	{
		HasSelectedShip = true;
		_playerShip.SetAnchor(isAnchored: false);
		_secondShip.SetAnchor(isAnchored: false);
		_playerShip.SetController((!isFirstShipSelected) ? ShipControllerType.AI : ShipControllerType.Player);
		_secondShip.SetController(isFirstShipSelected ? ShipControllerType.AI : ShipControllerType.Player);
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
		_playerShip.ShipOrder.SetShipStopOrder();
		_secondShip.ShipOrder.SetShipStopOrder();
		_secondShip.BreakAllExistingConnections();
		MatrixFrame bodyWorldTransform = _playerShip.GameEntity.GetBodyWorldTransform();
		bodyWorldTransform.rotation.u = Vec3.Up;
		bodyWorldTransform.rotation.f = bodyWorldTransform.rotation.s.CrossProductWithUpAsLeftParameter().NormalizedCopy();
		bodyWorldTransform.rotation.s = bodyWorldTransform.rotation.f.CrossProductWithUp();
		bodyWorldTransform.origin += bodyWorldTransform.rotation.s * (_playerShip.Physics.PhysicsBoundingBoxSizeWithoutChildren.x * 0.5f + _secondShip.Physics.PhysicsBoundingBoxSizeWithoutChildren.x * 0.5f + 1f);
		_navalShipsLogic.TeleportShip(_secondShip, bodyWorldTransform, checkFreeArea: false);
		_secondShip.TryToMaintainConnectionToAnotherShip(_playerShip);
		if (isFirstShipSelected)
		{
			_navalShipsLogic.TransferShipToTeam(_secondShip, base.Mission.PlayerTeam);
		}
		else
		{
			Formation formation = _playerShip.Formation;
			Formation formation2 = base.Mission.PlayerTeam.GetFormation(FormationClass.Ranged);
			_navalShipsLogic.TransferShipToFormation(_playerShip, formation2);
			_navalShipsLogic.TransferShipToTeam(_secondShip, base.Mission.PlayerTeam, formation);
		}
		_playerShip.Formation.PlayerOwner = Agent.Main;
		_secondShip.Formation.PlayerOwner = Agent.Main;
		MissionShip missionShip = (isFirstShipSelected ? _secondShip : _playerShip);
		MissionShip onShip;
		bool flag = _navalAgentsLogic.IsAgentOnAnyShip(_gunnarAgent, out onShip, TeamSideEnum.PlayerTeam);
		if (flag && onShip != missionShip)
		{
			_navalAgentsLogic.TransferAgentToShip(_gunnarAgent, missionShip);
		}
		else if (!flag)
		{
			_navalAgentsLogic.AddAgentToShip(_gunnarAgent, missionShip);
		}
		MissionShip missionShip2 = (isFirstShipSelected ? _playerShip : _secondShip);
		Team team = Agent.Main.Team;
		foreach (Agent activeAgent in team.ActiveAgents)
		{
			if (activeAgent != _gunnarAgent)
			{
				MissionShip onShip2;
				bool flag2 = _navalAgentsLogic.IsAgentOnAnyShip(activeAgent, out onShip2, team.TeamSide);
				if (flag2 && onShip2 != missionShip2)
				{
					_navalAgentsLogic.TransferAgentToShip(activeAgent, missionShip2);
				}
				else if (!flag2)
				{
					_navalAgentsLogic.AddAgentToShip(activeAgent, missionShip2);
				}
			}
		}
		ReplenishPlayerShipTroops();
		SpawnAllyPrisonerAgents(isFirstShipSelected ? _secondShip : _playerShip);
		_navalAgentsLogic.AssignCaptainToShip(Agent.Main, missionShip2);
		_navalAgentsLogic.AssignCaptainToShip(_gunnarAgent, missionShip);
		_playerShip.Formation.SetControlledByAI(isControlledByAI: false);
		_secondShip.Formation.SetControlledByAI(isControlledByAI: false);
		_playerShip.ShipOrder.SetCutLoose(enable: false);
		_secondShip.ShipOrder.SetCutLoose(enable: false);
		_playerShip.ShipOrder.SetBoardingTargetShip(null);
		_secondShip.ShipOrder.SetBoardingTargetShip(null);
		_playerShip.ShipOrder.MakeEnemyOnShipExpire();
		_secondShip.ShipOrder.MakeEnemyOnShipExpire();
		_playerShip.ShipOrder.SetOrderOarsmenLevel(2);
		_secondShip.ShipOrder.SetOrderOarsmenLevel(2);
		_gunnarAgent.Controller = AgentControllerType.AI;
		string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 80));
		GameTexts.SetVariable("SHIP_COMMANDING_TUTORIAL_GROUP_KEY", keyHyperlinkText);
		_playerShip.ShipControllerMachine.PilotStandingPoint.IsDisabledForPlayers = false;
		_secondShip.ShipControllerMachine.PilotStandingPoint.IsDisabledForPlayers = false;
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		_playerShip.ShipOrder.Tick();
		_secondShip.ShipOrder.Tick();
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.PlayerTeam);
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
		Vec3 vec = _reinforcementShip.GameEntity.GlobalPosition - Agent.Main.Position;
		this.OnCameraBearingNeedsUpdateEvent(vec.RotationZ);
	}

	private void ReplenishPlayerShipTroops()
	{
		int count = Agent.Main.Team.ActiveAgents.Count;
		int num = 14 - count;
		if (num > 0)
		{
			CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>("gangradirs_kin_melee");
			CharacterObject characterObject2 = Campaign.Current.ObjectManager.GetObject<CharacterObject>("gangradirs_kin_ranged");
			int num2 = num / 2;
			int num3 = num / 2;
			num2 += num - (num2 + num3);
			for (int i = 0; i < num2; i++)
			{
				_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, characterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true), _playerShip);
			}
			for (int j = 0; j < num3; j++)
			{
				_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, characterObject2, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true), _playerShip);
			}
			_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		}
	}

	private void OnSecondEnemyShipCleared()
	{
		TextObject textObject = new TextObject("{=R4Gqskgq}Victory");
		InformationManager.ShowInquiry(new InquiryData(text: new TextObject("{=tEK1RK5N}Once again, you are victorious. Gunnar, meanwhile, inspects the fallen pirates, and soon finds one who is only lightly wounded and able to speak.").ToString(), titleText: textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, affirmativeText: GameTexts.FindText("str_ok").ToString(), negativeText: "", affirmativeAction: OnVictoryPopUpClosed, negativeAction: null), pauseGameActiveState: true);
		MBMusicManager.Current.ForceStopThemeWithFadeOut();
	}

	private void OnVictoryPopUpClosed()
	{
		_isMissionSuccessful = true;
		PlayerEncounter.Battle.SetOverrideWinner(PlayerEncounter.Battle.PlayerSide);
		base.Mission.EndMission();
	}

	private void OnPlayerTeamDefeated()
	{
		_isMissionFailed = true;
		PlayerEncounter.Battle.SetOverrideWinner(PlayerEncounter.Battle.GetOtherSide(PlayerEncounter.Battle.PlayerSide));
		base.Mission.EndMission();
	}

	public bool HaveAllyShipsBeenCutLoose()
	{
		return !_playerShip.GetIsThereActiveBridgeTo(_secondShip);
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

	private void OnPlayerOrdered(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, object[] delegateParams)
	{
		if (!_hasShownChargeNotification && !_isSecondPhaseSetup && (orderType == OrderType.Charge || orderType == OrderType.ChargeWithTarget))
		{
			ShowChargeNotification();
		}
	}

	private void ShowChargeNotification()
	{
		_currentNotificationText = new TextObject("{=J0O71ubZ}The lines are holding! At them, lads!");
		_hasShownChargeNotification = true;
	}

	private void ShowSecondPhaseChargeNotification()
	{
		_currentNotificationText = new TextObject("{=8WDTkhc0}Strike hard, boys! Finish them!");
		_hasShownSecondPhaseChargeNotification = true;
	}
}
