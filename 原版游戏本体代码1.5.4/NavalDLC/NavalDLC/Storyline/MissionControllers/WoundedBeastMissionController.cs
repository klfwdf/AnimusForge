using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipInput;
using NavalDLC.Storyline.Objectives.WoundedBeast;
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

namespace NavalDLC.Storyline.MissionControllers;

public class WoundedBeastMissionController : MissionLogic
{
	private struct StorylineTroop
	{
		public string TroopId { get; }

		public int TroopCount { get; }

		public StorylineTroop(string troopId, int troopCount)
		{
			TroopCount = troopCount;
			TroopId = troopId;
		}
	}

	private const string WindDirection = "sp_wind_direction";

	private const string TargetEntityTag = "targeting_entity";

	private const string GunnarInitialDestination = "sp_gangradir_ship_destination";

	private const string LaharShipSpawnId = "sp_lahar_ship";

	private const string GunnarShipSpawnId = "sp_gangradir_ship";

	private const string LaharShipHullId = "ship_liburna_q2_storyline";

	private const string GunnarShipHullId = "northern_medium_ship";

	private const string LaharMeleeTroopId = "southern_pirates_raider";

	private const string LaharRangedTroopId = "aserai_marine_t5";

	private const string GunnarMeleeTroopId = "gangradirs_kin_melee";

	private const string GunnarRangedTroopId = "gangradirs_kin_ranged";

	private readonly List<StorylineTroop> _laharShipTroops = new List<StorylineTroop>();

	private readonly List<StorylineTroop> _gunnarShipTroops = new List<StorylineTroop>();

	private readonly List<Ship> _playerShips = new List<Ship>();

	private readonly MBList<MissionShip> _playerMissionShips = new MBList<MissionShip>();

	private Ship _gunnarShip;

	private Ship _laharShip;

	private MissionShip _laharMissionShip;

	private MissionShip _gunnarMissionShip;

	private static readonly Dictionary<string, string> LaharShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl3" },
		{ "sail", "sails_lvl2" },
		{ "bow", "bow_northern_reinforced_ram_lvl3" }
	};

	private static readonly Dictionary<string, string> GunnarShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	private const string FahdaShipSpawnId = "sp_fahda_ship";

	private const string FahdaShipHullId = "ship_meditheavy_storyline";

	private const string MediumReinforcementShipHullId = "ship_liburna_storyline";

	private const string LightReinforcementShipHullId = "ship_meditlight_storyline";

	private const string EnemyMeleeTroopId1 = "southern_pirates_raider";

	private const string EnemyMeleeTroopId2 = "aserai_footman";

	private const string EnemyRangedTroopId = "southern_pirates_bandit";

	private readonly List<StorylineTroop> _fahdaShipTroops = new List<StorylineTroop>();

	private readonly List<StorylineTroop> _enemyReinforcementFirstShipTroops = new List<StorylineTroop>();

	private readonly List<StorylineTroop> _enemyReinforcementSecondShipTroops = new List<StorylineTroop>();

	private readonly List<StorylineTroop> _enemyReinforcementThirdShipTroops = new List<StorylineTroop>();

	private readonly MBList<Agent> _enemySideAgents = new MBList<Agent>();

	private readonly List<Formation> _availailableEnemyFormations = new List<Formation>();

	private readonly MBList<MissionShip> _enemyMissionShips = new MBList<MissionShip>();

	private readonly List<Ship> _enemyShips = new List<Ship>();

	private const string EnemyReinforcementFirstShipTargetEntityTag = "targeting_entity_1";

	private const string EnemyReinforcementSecondShipTargetEntityTag = "targeting_entity_2";

	private const string EnemyReinforcementThirdShipTargetEntityTag = "targeting_entity_3";

	private WeakGameEntity EnemyReinforcementFirstShipTargetEntity;

	private WeakGameEntity EnemyReinforcementSecondShipTargetEntity;

	private WeakGameEntity EnemyReinforcementThirdShipTargetEntity;

	private Ship _fahdaShip;

	private MissionShip _fahdaMissionShip;

	private MissionShip _enemyReinforcementFirstMissionShip;

	private MissionShip _enemyReinforcementSecondMissionShip;

	private MissionShip _enemyReinforcementThirdMissionShip;

	private static readonly Dictionary<string, string> FahdaShipUpgradePieces = new Dictionary<string, string> { { "side", "side_southern_shields_lvl2" } };

	private static readonly Dictionary<string, string> MediumReinforcementShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	private static readonly Dictionary<string, string> FirstLightReinforcementShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	private static readonly Dictionary<string, string> SecondLightReinforcementShipUpgradePieces = new Dictionary<string, string>
	{
		{ "side", "side_southern_shields_lvl2" },
		{ "sail", "sails_lvl2" }
	};

	private float _drownCheckTimer;

	private float _drownCheckDuration = 2f;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private MissionObjectiveLogic _missionObjectiveLogic;

	private Vec2 _fleePoint;

	private Vec2 _gunnarInitialDestination;

	private bool _initialized;

	private bool _isMissionSuccessful;

	private bool _isMissionFailed;

	private bool _inPhase1 = true;

	private MissionTimer _failingQuestTimer;

	private float _startDistanceToFleePoint;

	private bool _nearFleePoint;

	private bool _targetedSmallerVessels;

	private bool _targetedBiggerVessel;

	private bool _targetedBySmallerVessels;

	private readonly Dictionary<MissionShip, bool> _shipsToAlert = new Dictionary<MissionShip, bool>();

	private readonly Dictionary<MissionShip, bool> _alertedShips = new Dictionary<MissionShip, bool>();

	public WoundedBeastMissionController()
	{
		_gunnarShipTroops.Add(new StorylineTroop("gangradirs_kin_melee", 15));
		_gunnarShipTroops.Add(new StorylineTroop("gangradirs_kin_ranged", 18));
		_laharShipTroops.Add(new StorylineTroop("southern_pirates_raider", 25));
		_laharShipTroops.Add(new StorylineTroop("aserai_marine_t5", 18));
		_fahdaShipTroops.Add(new StorylineTroop("southern_pirates_raider", 2));
		_fahdaShipTroops.Add(new StorylineTroop("aserai_footman", 66));
		_fahdaShipTroops.Add(new StorylineTroop("southern_pirates_bandit", 0));
		_enemyReinforcementThirdShipTroops.Add(new StorylineTroop("southern_pirates_raider", 10));
		_enemyReinforcementThirdShipTroops.Add(new StorylineTroop("aserai_footman", 13));
		_enemyReinforcementThirdShipTroops.Add(new StorylineTroop("southern_pirates_bandit", 0));
		_enemyReinforcementSecondShipTroops.Add(new StorylineTroop("southern_pirates_raider", 12));
		_enemyReinforcementSecondShipTroops.Add(new StorylineTroop("aserai_footman", 7));
		_enemyReinforcementSecondShipTroops.Add(new StorylineTroop("southern_pirates_bandit", 0));
		_enemyReinforcementFirstShipTroops.Add(new StorylineTroop("southern_pirates_raider", 12));
		_enemyReinforcementFirstShipTroops.Add(new StorylineTroop("aserai_footman", 8));
		_enemyReinforcementFirstShipTroops.Add(new StorylineTroop("southern_pirates_bandit", 0));
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_missionObjectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerTeam, NavalShipDeploymentLimit.Max());
		_navalShipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.EnemyTeam, NavalShipDeploymentLimit.Max());
		_navalShipsLogic.SetDeploymentMode(value: false);
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
		_navalShipsLogic.ShipRammingEvent += OnShipRammed;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_navalShipsLogic.ShipRammingEvent -= OnShipRammed;
	}

	public override void OnMissionStateFinalized()
	{
		SailWindProfile.FinalizeProfile();
	}

	public override void OnMissionTick(float dt)
	{
		if (!_initialized)
		{
			Initialize();
		}
		if ((Agent.Main == null || !Agent.Main.IsActive()) && _failingQuestTimer == null && _inPhase1)
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=ay5y18aq}You pass out from the pain of your wounds."));
			OnFailed();
			_failingQuestTimer = new MissionTimer(5f);
		}
		if (_failingQuestTimer != null)
		{
			if (_failingQuestTimer.Check())
			{
				base.Mission.EndMission();
			}
			return;
		}
		if (!_fahdaMissionShip.IsSinking && _fahdaMissionShip.GameEntity.GlobalPosition.AsVec2.Distance(_fleePoint) < 100f)
		{
			CampaignInformationManager.AddDialogLine(new TextObject("{=9Y1iHrQ4}Ach. We couldn't catch Fahda in time."), NavalStorylineData.Lahar.CharacterObject);
			OnFailed();
			_failingQuestTimer = new MissionTimer(5f);
			return;
		}
		if (_inPhase1)
		{
			OnPhase1Tick(dt);
		}
		if (IsShipActive(_fahdaMissionShip) && !_fahdaMissionShip.GetIsConnected())
		{
			_fahdaMissionShip.ShipOrder.SetShipMovementOrder(in _fleePoint);
		}
		TickGunnarsShip();
		CheckTargetShipNearEscapePoint();
		CheckDrowningAgents(dt);
		CheckMissionEnd();
	}

	private void CheckMissionEnd()
	{
		if (!_isMissionFailed && !_isMissionSuccessful)
		{
			if (GetAgentCountOfSide(base.Mission.PlayerTeam.Side) == 0)
			{
				OnFailed();
			}
			else if (GetAgentCountOfSide(base.Mission.PlayerTeam.Side.GetOppositeSide()) == 0)
			{
				OnSuccess();
			}
			else if (!_enemyMissionShips.Any((MissionShip x) => IsShipActive(x)))
			{
				OnSuccess();
			}
		}
	}

	private void TickGunnarsShip()
	{
		if (!IsShipActive(_gunnarMissionShip) || _gunnarMissionShip.GetIsConnectedToEnemy())
		{
			return;
		}
		if (IsShipAlerted(_gunnarMissionShip))
		{
			if (_gunnarMissionShip.ShipOrder.TargetShip == null || (_gunnarMissionShip.ShipOrder.TargetShip == _fahdaMissionShip && IsShipActive(_fahdaMissionShip)) || !IsShipActive(_gunnarMissionShip.ShipOrder.TargetShip))
			{
				MissionShip missionShip = (from y in _enemyMissionShips
					where y != _fahdaMissionShip && IsShipActive(y)
					orderby y.GameEntity.GlobalPosition.Distance(_gunnarMissionShip.GameEntity.GlobalPosition)
					select y).FirstOrDefault() ?? _gunnarMissionShip.ShipOrder.ClosestEnemyShip;
				if (missionShip == null)
				{
					_gunnarMissionShip.ShipOrder.SetShipStopOrder();
				}
				else if (_gunnarMissionShip.ShipOrder.TargetShip == null || missionShip != _gunnarMissionShip.ShipOrder.TargetShip)
				{
					_gunnarMissionShip.SetAnchor(isAnchored: false);
					_gunnarMissionShip.ShipOrder.SetShipEngageOrder(missionShip);
					_gunnarMissionShip.ShipOrder.SetBoardingTargetShip(missionShip);
					_gunnarMissionShip.ShipOrder.IsBoardingAvailable = true;
				}
			}
		}
		else if (_gunnarMissionShip.GameEntity.GlobalPosition.Distance(_gunnarInitialDestination.ToVec3()) < 10f)
		{
			_gunnarMissionShip.SetAnchor(isAnchored: true, anchorInPlace: true);
		}
		else
		{
			_gunnarMissionShip.SetAnchor(isAnchored: false);
			_gunnarMissionShip.ShipOrder.SetShipMovementOrder(in _gunnarInitialDestination);
		}
	}

	private bool IsShipAlerted(MissionShip ship)
	{
		bool value;
		return _alertedShips.TryGetValue(ship, out value) && value;
	}

	private bool IsShipActive(MissionShip ship)
	{
		if (!ship.IsDisabled && ship.Formation.CountOfUnits > 0)
		{
			return !ship.IsSinking;
		}
		return false;
	}

	private void OnPhase1Tick(float dt)
	{
		MissionShip fahdaMissionShip = _fahdaMissionShip;
		if (fahdaMissionShip != null && fahdaMissionShip.IsSinking)
		{
			OnTargetShipSunk();
			_inPhase1 = false;
			return;
		}
		foreach (MissionShip enemyMissionShip in _enemyMissionShips)
		{
			MissionShip connectedEnemyShip2;
			if (enemyMissionShip != _fahdaMissionShip)
			{
				if (!IsShipActive(enemyMissionShip))
				{
					continue;
				}
				if (!IsShipAlerted(enemyMissionShip))
				{
					bool value;
					if (_laharMissionShip.GetIsConnectedToEnemy(out var connectedEnemyShip))
					{
						if (connectedEnemyShip == enemyMissionShip)
						{
							AlertShip(enemyMissionShip, _laharMissionShip);
							AlertShip(_gunnarMissionShip, enemyMissionShip);
							TriggerSmallerShipNotifications(hasPlayerAttemptedToBoard: true);
						}
						if (connectedEnemyShip == _fahdaMissionShip)
						{
							AlertShip(enemyMissionShip, _laharMissionShip);
							TriggerTargetShipNotifications();
						}
					}
					else if (_gunnarMissionShip.GetIsConnectedToEnemy(out connectedEnemyShip))
					{
						if (connectedEnemyShip == enemyMissionShip)
						{
							AlertShip(enemyMissionShip, _gunnarMissionShip);
						}
					}
					else if (_shipsToAlert.TryGetValue(enemyMissionShip, out value) && value)
					{
						AlertShip(enemyMissionShip);
					}
				}
				else
				{
					TickEnemyShip(enemyMissionShip);
					if (enemyMissionShip.ShipOrder.GetIsAttemptingBoarding() && enemyMissionShip.ShipOrder.TargetShip == _laharMissionShip)
					{
						TriggerSmallerShipNotifications(hasPlayerAttemptedToBoard: false);
					}
				}
			}
			else if (_laharMissionShip.GetIsConnectedToEnemy(out connectedEnemyShip2) && _fahdaMissionShip == connectedEnemyShip2)
			{
				TriggerTargetShipNotifications();
				AlertShip(_gunnarMissionShip, _gunnarMissionShip.ShipOrder.ClosestEnemyShip ?? enemyMissionShip);
			}
		}
		MoveEscortShipsToTheirDefencePositions(dt);
	}

	private void CheckDrowningAgents(float dt)
	{
		_drownCheckTimer += dt;
		if (_drownCheckTimer >= _drownCheckDuration)
		{
			_drownCheckTimer = 0f;
			for (int num = _enemyMissionShips.Count - 1; num >= 0; num--)
			{
				CheckDrowningAgents(_enemyMissionShips[num]);
			}
		}
	}

	private void CheckDrowningAgents(MissionShip ship)
	{
		foreach (Agent item in _navalAgentsLogic.GetActiveAgentsOfShip(ship).ToList())
		{
			if (!item.IsHero && item.CurrentMortalityState == Agent.MortalityState.Mortal && item.IsActive() && item.IsInWater())
			{
				DrownAgent(item, MBRandom.RandomInt(10, 100));
			}
		}
	}

	private void DrownAgent(Agent agent, int inflictedDamage)
	{
		Blow blow = new Blow(agent.Index);
		blow.DamageType = DamageTypes.Blunt;
		blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
		blow.BaseMagnitude = inflictedDamage;
		blow.GlobalPosition = agent.Position;
		blow.GlobalPosition.z += agent.GetEyeGlobalHeight();
		blow.DamagedPercentage = 1f;
		blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
		blow.SwingDirection = agent.LookDirection;
		blow.Direction = blow.SwingDirection;
		blow.InflictedDamage = inflictedDamage;
		blow.DamageCalculated = true;
		sbyte mainHandItemBoneIndex = agent.Monster.MainHandItemBoneIndex;
		AttackCollisionData collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(_attackBlockedWithShield: false, _correctSideShieldBlock: false, _isAlternativeAttack: false, _isColliderAgent: true, _collidedWithShieldOnBack: false, _isMissile: false, _isMissileBlockedWithWeapon: false, _missileHasPhysics: false, _entityExists: false, _thrustTipHit: false, _missileGoneUnderWater: false, _missileGoneOutOfBorder: false, CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head, mainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, agent.Velocity, Vec3.Up);
		agent.RegisterBlow(blow, in collisionData);
		agent.MakeVoice(SkinVoiceManager.VoiceType.Drown, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
		if (agent.Controller == AgentControllerType.AI)
		{
			agent.AddAcceleration(new Vec3(0f, 0f, -20f));
		}
	}

	private void TickEnemyShip(MissionShip ship)
	{
		if (IsShipActive(ship) && !ship.GetIsConnectedToEnemy() && IsShipAlerted(ship) && ship.ShipOrder.TargetShip == null)
		{
			MissionShip missionShip = (IsShipActive(_laharMissionShip) ? _laharMissionShip : ship.ShipOrder.ClosestEnemyShip);
			if (missionShip == null)
			{
				ship.ShipOrder.SetShipStopOrder();
				return;
			}
			ship.SetAnchor(isAnchored: false);
			ship.ShipOrder.SetShipEngageOrder(missionShip);
		}
	}

	private void CheckTargetShipNearEscapePoint()
	{
		if (!_nearFleePoint && IsShipActive(_fahdaMissionShip) && !_fahdaMissionShip.GetIsConnectedToEnemy() && _fahdaMissionShip.GameEntity.GlobalPosition.AsVec2.Distance(_fleePoint) < _startDistanceToFleePoint * 0.33f)
		{
			_nearFleePoint = true;
			if (!_fahdaMissionShip.GetIsConnectedToEnemy())
			{
				CampaignInformationManager.AddDialogLine(new TextObject("{=KMNUcHJ5}The winds are still strong and a new squall could brew up at any time. If she gets much further we might lose sight of her."), NavalStorylineData.Lahar.CharacterObject);
			}
		}
	}

	private void TriggerTargetShipNotifications()
	{
		if (!_targetedBiggerVessel)
		{
			CampaignInformationManager.AddDialogLine(new TextObject("{=isa8iCbC}No! No! If you board that monster we're finished! Cut loose!"), NavalStorylineData.Lahar.CharacterObject);
			_targetedBiggerVessel = true;
		}
	}

	private void TriggerSmallerShipNotifications(bool hasPlayerAttemptedToBoard)
	{
		if (hasPlayerAttemptedToBoard && !_targetedSmallerVessels && IsShipActive(_fahdaMissionShip))
		{
			CampaignInformationManager.AddDialogLine(new TextObject("{=AFdg8UHM}Go for her flagship! We don't want it to get away! We'll deal with the lesser vessels later."), NavalStorylineData.Lahar.CharacterObject);
			_targetedSmallerVessels = true;
		}
		if (!hasPlayerAttemptedToBoard && !_targetedBySmallerVessels && IsShipActive(_fahdaMissionShip))
		{
			CampaignInformationManager.AddDialogLine(new TextObject("{=HOAwSlCQ}One of the others is going to board us! Repel them and cut loose, or we'll never catch her!"), NavalStorylineData.Lahar.CharacterObject);
			_targetedBySmallerVessels = true;
		}
	}

	private void OnTargetShipSunk()
	{
		AlertAllShips();
		List<MissionShip> list = _enemyMissionShips.Where((MissionShip x) => x != _fahdaMissionShip).ToList();
		if (list.Count > 0)
		{
			FinishOffConsortsObjective objective = new FinishOffConsortsObjective(base.Mission, list);
			_missionObjectiveLogic.StartObjective(objective);
			CampaignInformationManager.AddDialogLine(new TextObject("{=CzYbzDM8}Good! You dealt her ship a mortal wound. It's going down! Now, finish off its consorts."), NavalStorylineData.Lahar.CharacterObject, null, 3000);
		}
	}

	private void MoveEscortShipsToTheirDefencePositions(float dt)
	{
		_fahdaMissionShip.ShipOrder.IsBoardingAvailable = false;
		GetDefencePositionsForReinforcementShips(out var leftSide, out var rightSide, out var behind);
		foreach (MissionShip enemyMissionShip in _enemyMissionShips)
		{
			if (enemyMissionShip != _fahdaMissionShip && IsShipActive(enemyMissionShip) && !IsShipAlerted(enemyMissionShip))
			{
				Vec2 targetPosition = enemyMissionShip.GameEntity.GlobalPosition.AsVec2;
				if (enemyMissionShip == _enemyReinforcementFirstMissionShip)
				{
					targetPosition = behind;
				}
				else if (enemyMissionShip == _enemyReinforcementSecondMissionShip)
				{
					targetPosition = rightSide;
				}
				else if (enemyMissionShip == _enemyReinforcementThirdMissionShip)
				{
					targetPosition = leftSide;
				}
				enemyMissionShip.ShipOrder.IsBoardingAvailable = false;
				enemyMissionShip.ShipOrder.SetShipMovementOrder(in targetPosition);
			}
		}
	}

	private void OnSuccess()
	{
		_isMissionSuccessful = true;
		PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(BattleState.DefenderVictory);
		MBInformationManager.AddQuickInformation(new TextObject("{=15aPhWar}Success! You defeated Fahda's fleet."), 2000);
	}

	private void OnFailed()
	{
		_isMissionFailed = true;
		PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(BattleState.AttackerVictory);
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		bool result = false;
		if (_isMissionSuccessful)
		{
			missionResult = MissionResult.CreateSuccessful(base.Mission);
			result = true;
		}
		else if (_isMissionFailed)
		{
			missionResult = MissionResult.CreateDefeated(base.Mission);
			result = true;
		}
		return result;
	}

	private void UpdateSceneWindDirectionAndWaterStrength()
	{
		Mission.Current.Scene.SetGlobalWindVelocity(base.Mission.Scene.FindWeakEntityWithTag("sp_wind_direction").GetGlobalFrame().rotation.f.AsVec2 * 12f);
		Mission.Current.Scene.SetWaterStrength(3f);
	}

	private MissionShip CreateShip(IShipOrigin ship, Team team, Formation formation, WeakGameEntity spawnEntity)
	{
		MatrixFrame shipFrame = spawnEntity.GetGlobalFrame();
		float waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(spawnEntity.GlobalPosition.AsVec2, useWaterRenderer: false, checkWaterBodyEntities: false);
		shipFrame.origin = new Vec3(spawnEntity.GlobalPosition.x, spawnEntity.GlobalPosition.y, waterLevelAtPosition);
		MissionShip missionShip = _navalShipsLogic.SpawnShip(ship, in shipFrame, team, formation);
		missionShip.ShipOrder.FormationJoinShip(formation);
		if (team.IsEnemyOf(base.Mission.PlayerTeam))
		{
			missionShip.ShipControllerMachine.PilotStandingPoint.IsDisabledForPlayers = true;
		}
		return missionShip;
	}

	public void AlertShip(MissionShip missionShip, MissionShip target = null)
	{
		if (CanAlertShip(missionShip))
		{
			if (_shipsToAlert.TryGetValue(missionShip, out var value) && value)
			{
				_shipsToAlert[missionShip] = false;
			}
			missionShip.ShipOrder.IsBoardingAvailable = true;
			_alertedShips[missionShip] = true;
			missionShip.SetAnchor(isAnchored: false);
			target = target ?? missionShip.ShipOrder.ClosestEnemyShip;
			if (target != null)
			{
				missionShip.ShipOrder.SetShipEngageOrder(target);
			}
		}
	}

	private void AlertAllEnemyShips()
	{
		foreach (MissionShip enemyMissionShip in _enemyMissionShips)
		{
			if (enemyMissionShip != _fahdaMissionShip)
			{
				AlertShip(enemyMissionShip, _laharMissionShip);
			}
		}
	}

	private void AlertAllShips()
	{
		AlertAllEnemyShips();
		AlertShip(_gunnarMissionShip);
	}

	private bool CanAlertShip(MissionShip missionShip)
	{
		if (IsShipActive(missionShip))
		{
			return !IsShipAlerted(missionShip);
		}
		return false;
	}

	private void Initialize()
	{
		_inPhase1 = true;
		_fleePoint = base.Mission.Scene.FindWeakEntityWithTag("sp_flee_point").GlobalPosition.AsVec2;
		_gunnarInitialDestination = base.Mission.Scene.FindWeakEntityWithTag("sp_gangradir_ship_destination").GlobalPosition.AsVec2;
		_initialized = true;
		CampaignInformationManager.AddDialogLine(new TextObject("{=Gdaayb1y}Ha! It looks like her ship took a lot of damage. Her crew must not have furled the sails properly before the winds hit, and now she's just limping along. Sink her!"), NavalStorylineData.Lahar.CharacterObject);
		_availailableEnemyFormations.AddRange(base.Mission.PlayerEnemyTeam.FormationsIncludingEmpty);
		_navalShipsLogic.SetDeploymentMode(value: true);
		SpawnPlayerSide();
		SpawnEnemySide();
		foreach (MissionShip playerMissionShip in _playerMissionShips)
		{
			playerMissionShip.SetShipOrderActive(isOrderActive: true);
		}
		foreach (MissionShip enemyMissionShip in _enemyMissionShips)
		{
			enemyMissionShip.SetShipOrderActive(isOrderActive: true);
			foreach (ShipAttachmentMachine attachmentMachine in enemyMissionShip.AttachmentMachines)
			{
				attachmentMachine.SetIsDisabledForAI(isDisabledForAI: true);
			}
		}
		_navalShipsLogic.TeleportShip(_laharMissionShip, _laharMissionShip.GameEntity.GetGlobalFrame(), checkFreeArea: true);
		_navalShipsLogic.TeleportShip(_gunnarMissionShip, _gunnarMissionShip.GameEntity.GetGlobalFrame(), checkFreeArea: true);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.PlayerTeam);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.EnemyTeam);
		base.Mission.OnInitialSpawnCompleted();
		_navalShipsLogic.SetDeploymentMode(value: false);
		UpdateSceneWindDirectionAndWaterStrength();
	}

	private void OnShipRammed(MissionShip ship1, MissionShip ship2, float damagePercent, bool isFirstImpact, CapsuleData data, int ramQuality)
	{
		if (ship1 == _laharMissionShip && ship2 != _fahdaMissionShip && isFirstImpact && _fahdaMissionShip.Formation.CountOfUnits > 0 && ship2.Team.IsEnemyOf(base.Mission.PlayerTeam))
		{
			TriggerSmallerShipNotifications(hasPlayerAttemptedToBoard: true);
			if (CanAlertShip(ship2) && damagePercent < 1f)
			{
				_shipsToAlert[ship2] = true;
			}
		}
		if (!(ship1 == _laharMissionShip && ship2 == _fahdaMissionShip && isFirstImpact))
		{
			return;
		}
		foreach (MissionShip enemyMissionShip in _enemyMissionShips)
		{
			if (enemyMissionShip != _fahdaMissionShip && CanAlertShip(enemyMissionShip))
			{
				_shipsToAlert[enemyMissionShip] = true;
			}
		}
		if (_fahdaMissionShip.Formation.CountOfUnits > 0 && damagePercent < 1f)
		{
			CampaignInformationManager.AddDialogLine(new TextObject("{=18qp71BY}Well done! Give her another one, for luck."), NavalStorylineData.Lahar.CharacterObject);
		}
		if (ship2 == _fahdaMissionShip && isFirstImpact && damagePercent > 1f)
		{
			_navalShipsLogic.ShipRammingEvent -= OnShipRammed;
		}
	}

	private void SpawnPlayerSide()
	{
		Team team = Mission.GetTeam(TeamSideEnum.PlayerTeam);
		WeakGameEntity spawnEntity = base.Mission.Scene.FindWeakEntityWithTag("sp_lahar_ship");
		ShipHull questShipHull = Campaign.Current.ObjectManager.GetObject<ShipHull>("ship_liburna_q2_storyline");
		_laharShip = MobileParty.MainParty.Ships.FirstOrDefault((Ship x) => x.ShipHull == questShipHull) ?? new Ship(questShipHull)
		{
			IsTradeable = false,
			IsUsedByQuest = true,
			Owner = PartyBase.MainParty,
			CanHaveUnlockedPieces = true
		};
		_laharShip.ChangeFigurehead(DefaultFigureheads.Hawk);
		AddShipUpgradePieces(_laharShip, LaharShipUpgradePieces);
		_laharMissionShip = CreateShip(_laharShip, base.Mission.PlayerTeam, team.GetFormation(FormationClass.Infantry), spawnEntity);
		_playerMissionShips.Add(_laharMissionShip);
		_playerShips.Add(_laharShip);
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_laharMissionShip, _laharShipTroops.Sum((StorylineTroop t) => t.TroopCount) + 2);
		SpawnNonHeroAgents(_laharMissionShip, _laharShipTroops, PartyBase.MainParty);
		SpawnHero(CharacterObject.PlayerCharacter, _laharMissionShip, PartyBase.MainParty);
		SpawnHero(NavalStorylineData.Lahar.CharacterObject, _laharMissionShip, PartyBase.MainParty);
		WeakGameEntity spawnEntity2 = base.Mission.Scene.FindWeakEntityWithTag("sp_gangradir_ship");
		ShipHull northernMediumShipHull = Campaign.Current.ObjectManager.GetObject<ShipHull>("northern_medium_ship");
		_gunnarShip = MobileParty.MainParty.Ships.FirstOrDefault((Ship x) => x.ShipHull == northernMediumShipHull) ?? new Ship(Campaign.Current.ObjectManager.GetObject<ShipHull>("northern_medium_ship"))
		{
			IsTradeable = false,
			IsUsedByQuest = true,
			Owner = PartyBase.MainParty,
			CanHaveUnlockedPieces = true
		};
		_gunnarShip.ChangeFigurehead(DefaultFigureheads.Dragon);
		AddShipUpgradePieces(_gunnarShip, GunnarShipUpgradePieces);
		_gunnarMissionShip = CreateShip(_gunnarShip, base.Mission.PlayerTeam, team.GetFormation(FormationClass.Ranged), spawnEntity2);
		_playerMissionShips.Add(_gunnarMissionShip);
		_playerShips.Add(_gunnarShip);
		_alertedShips[_gunnarMissionShip] = false;
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_gunnarMissionShip, _gunnarShipTroops.Sum((StorylineTroop t) => t.TroopCount) + 1);
		SpawnNonHeroAgents(_gunnarMissionShip, _gunnarShipTroops, PartyBase.MainParty);
		SpawnHero(NavalStorylineData.Gunnar.CharacterObject, _gunnarMissionShip, PartyBase.MainParty);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		Agent.Main.Controller = AgentControllerType.Player;
		Agent.Main.Formation.PlayerOwner = Agent.Main;
		Mission.Current.PlayerTeam.PlayerOrderController.Owner = Agent.Main;
		Agent agent = base.Mission.Agents.First((Agent x) => x.IsHero && x.Character == NavalStorylineData.Gunnar.CharacterObject);
		agent.Formation.PlayerOwner = agent;
		base.Mission.PlayerTeam.PlayerOrderController.Owner = Agent.Main;
		_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(Agent.Main, _laharMissionShip);
		_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(agent, _gunnarMissionShip);
	}

	private void SpawnEnemySide()
	{
		PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
		Formation formation = _availailableEnemyFormations.First();
		_availailableEnemyFormations.RemoveAt(0);
		ShipHull fahdaShipHull = Campaign.Current.ObjectManager.GetObject<ShipHull>("ship_meditheavy_storyline");
		_fahdaShip = encounteredParty.Ships.FirstOrDefault((Ship x) => x.ShipHull == fahdaShipHull) ?? new Ship(fahdaShipHull)
		{
			IsTradeable = false,
			IsUsedByQuest = true,
			Owner = encounteredParty
		};
		_fahdaShip.ChangeFigurehead(DefaultFigureheads.Viper);
		AddShipUpgradePieces(_gunnarShip, FahdaShipUpgradePieces);
		_fahdaMissionShip = CreateShip(_fahdaShip, base.Mission.PlayerEnemyTeam, formation, base.Mission.Scene.FindWeakEntityWithTag("sp_fahda_ship"));
		_fahdaMissionShip.SetCustomSailSetting(enableCustomSailSetting: true, SailInput.Raised);
		_fahdaMissionShip.Formation.SetControlledByAI(isControlledByAI: false);
		_fahdaMissionShip.SetCanBeTakenOver(value: false);
		if (_missionObjectiveLogic != null)
		{
			SinkShipObjective objective = new SinkShipObjective(base.Mission, _fahdaMissionShip);
			_missionObjectiveLogic.StartObjective(objective);
		}
		_enemyShips.Add(_fahdaShip);
		_enemyMissionShips.Add(_fahdaMissionShip);
		List<WeakGameEntity> source = _fahdaMissionShip.GameEntity.CollectChildrenEntitiesWithTag("targeting_entity");
		EnemyReinforcementThirdShipTargetEntity = source.FirstOrDefault((WeakGameEntity t) => t.HasTag("targeting_entity_3"));
		EnemyReinforcementSecondShipTargetEntity = source.FirstOrDefault((WeakGameEntity t) => t.HasTag("targeting_entity_2"));
		EnemyReinforcementFirstShipTargetEntity = source.FirstOrDefault((WeakGameEntity t) => t.HasTag("targeting_entity_1"));
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_fahdaMissionShip, _fahdaShipTroops.Sum((StorylineTroop t) => t.TroopCount) + 1);
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(encounteredParty, NavalStorylineData.EmiraAlFahda.CharacterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true), _fahdaMissionShip);
		SpawnNonHeroAgents(_fahdaMissionShip, _fahdaShipTroops, encounteredParty, NavalStorylineData.CorsairBanner);
		_enemyReinforcementFirstMissionShip = SpawnReinforcementShip(EnemyReinforcementThirdShipTargetEntity, _enemyReinforcementThirdShipTroops, "ship_liburna_storyline", MediumReinforcementShipUpgradePieces);
		_enemyReinforcementSecondMissionShip = SpawnReinforcementShip(EnemyReinforcementSecondShipTargetEntity, _enemyReinforcementSecondShipTroops, "ship_meditlight_storyline", FirstLightReinforcementShipUpgradePieces);
		_enemyReinforcementThirdMissionShip = SpawnReinforcementShip(EnemyReinforcementFirstShipTargetEntity, _enemyReinforcementFirstShipTroops, "ship_meditlight_storyline", SecondLightReinforcementShipUpgradePieces);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.EnemyTeam, isReinforcement: false, _enemySideAgents);
		_startDistanceToFleePoint = _fahdaMissionShip.GameEntity.GlobalPosition.AsVec2.Distance(_fleePoint);
	}

	private MissionShip SpawnReinforcementShip(WeakGameEntity targetEntity, List<StorylineTroop> troops, string shipHullId, Dictionary<string, string> upgradePieces)
	{
		PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
		Formation formation = _availailableEnemyFormations.First();
		_availailableEnemyFormations.RemoveAt(0);
		int desiredTroopCount = troops.Sum((StorylineTroop t) => t.TroopCount);
		ShipHull reinforcementShipHull = Campaign.Current.ObjectManager.GetObject<ShipHull>(shipHullId);
		Ship ship = PlayerEncounter.EncounteredParty.Ships.FirstOrDefault((Ship x) => x.ShipHull == reinforcementShipHull) ?? new Ship(reinforcementShipHull)
		{
			IsTradeable = false,
			IsUsedByQuest = true,
			Owner = PlayerEncounter.EncounteredParty
		};
		AddShipUpgradePieces(ship, upgradePieces);
		MissionShip missionShip = CreateShip(ship, base.Mission.PlayerEnemyTeam, formation, targetEntity);
		missionShip.SetCanBeTakenOver(value: false);
		_enemyShips.Add(ship);
		_enemyMissionShips.Add(missionShip);
		_alertedShips[missionShip] = false;
		_navalAgentsLogic.SetDesiredTroopCountOfShip(missionShip, desiredTroopCount);
		SpawnNonHeroAgents(missionShip, troops, encounteredParty, NavalStorylineData.CorsairBanner);
		return missionShip;
	}

	private void SpawnHero(CharacterObject character, MissionShip ship, PartyBase party, Banner customBanner = null)
	{
		Banner banner = customBanner ?? party.Banner;
		character.HeroObject.Heal(character.HeroObject.MaxHitPoints);
		PartyAgentOrigin partyAgentOrigin = new PartyAgentOrigin(party, character, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true);
		partyAgentOrigin.SetBanner(banner);
		_navalAgentsLogic.AddReservedTroopToShip(partyAgentOrigin, ship);
	}

	private void SpawnNonHeroAgents(MissionShip ship, List<StorylineTroop> troopTypes, PartyBase party, Banner customBanner = null)
	{
		Banner banner = customBanner ?? party.Banner;
		foreach (StorylineTroop troopType in troopTypes)
		{
			CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>(troopType.TroopId);
			for (int i = 0; i < troopType.TroopCount; i++)
			{
				PartyAgentOrigin partyAgentOrigin = new PartyAgentOrigin(party, characterObject, -1, default(UniqueTroopDescriptor), alwaysWounded: false, isInvincible: true);
				partyAgentOrigin.SetBanner(banner);
				_navalAgentsLogic.AddReservedTroopToShip(partyAgentOrigin, ship);
			}
		}
	}

	private int GetAgentCountOfSide(BattleSideEnum side)
	{
		BattleSideEnum side2 = base.Mission.PlayerTeam.Side;
		int num = 0;
		if (side2 == side)
		{
			foreach (MissionShip playerMissionShip in _playerMissionShips)
			{
				num += _navalAgentsLogic.GetActiveAgentCountOfShip(playerMissionShip);
			}
		}
		else
		{
			foreach (MissionShip enemyMissionShip in _enemyMissionShips)
			{
				num += _navalAgentsLogic.GetActiveAgentCountOfShip(enemyMissionShip);
			}
		}
		return num;
	}

	private void GetDefencePositionsForReinforcementShips(out Vec2 leftSide, out Vec2 rightSide, out Vec2 behind)
	{
		Vec2 vec = (_laharMissionShip.GameEntity.GlobalPosition.AsVec2 - _fahdaMissionShip.GameEntity.GlobalPosition.AsVec2) / 2f;
		Vec2 asVec = _fahdaMissionShip.GameEntity.GetLocalFrame().rotation.f.AsVec2;
		float num = 300f;
		float num2 = 200f;
		float num3 = System.MathF.PI / 5f;
		float num4 = System.MathF.PI * 4f / 5f;
		behind = EnemyReinforcementThirdShipTargetEntity.GlobalPosition.AsVec2;
		if (asVec.AngleBetween(vec) < 0f - num3 && asVec.AngleBetween(vec) > 0f - num4)
		{
			if (vec.Length == 0f)
			{
				vec = _fahdaMissionShip.GameEntity.GetLocalFrame().rotation.f.AsVec2 * 30f;
			}
			else if (vec.Length < num2)
			{
				vec *= num2 / vec.Length;
			}
			else if (vec.Length > num)
			{
				vec *= num / vec.Length;
			}
			rightSide = _fahdaMissionShip.GameEntity.GlobalPosition.AsVec2 + vec;
		}
		else
		{
			rightSide = EnemyReinforcementSecondShipTargetEntity.GlobalPosition.AsVec2;
		}
		leftSide = rightSide + asVec * num2;
	}

	private void AddShipUpgradePieces(Ship ship, Dictionary<string, string> upgradePieces)
	{
		foreach (KeyValuePair<string, string> kv in upgradePieces)
		{
			ShipUpgradePiece newUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(kv.Value);
			if (ship.ShipHull.AvailableSlots.Any((KeyValuePair<string, ShipSlot> slot) => slot.Key == kv.Key))
			{
				ship.EquipUpgradePiece(kv.Key, newUpgradePiece);
			}
		}
	}
}
