using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Storyline.Objectives.Quest3;
using NavalDLC.Storyline.Objects;
using NavalDLC.Storyline.Quests;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Objects.Usables;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.MissionControllers;

public class BlockedEstuaryMissionController : MissionLogic
{
	public enum BattlePhase
	{
		Phase1,
		Phase2,
		Phase3
	}

	private class BurningProjectile
	{
		private const string ProjectileFireParticleId = "fire_obstacle";

		private float _minLifeTime;

		private float _timer;

		private float _spawnTime;

		private Vec3 _position;

		private Func<bool> _endCondition;

		public bool Initialized { get; private set; }

		public GameEntity GameEntity { get; private set; }

		public BurningProjectile(Vec3 position, float minLifeTime = 10f, float spawnAfterTime = 1f, Func<bool> enderFunction = null)
		{
			_position = position;
			_spawnTime = spawnAfterTime;
			_endCondition = enderFunction;
			_minLifeTime = minLifeTime;
		}

		public void Tick(float dt, out bool shouldBeRemoved)
		{
			shouldBeRemoved = false;
			if (Initialized)
			{
				shouldBeRemoved = _timer >= _minLifeTime || (_endCondition != null && _endCondition());
			}
			else if (_timer >= _spawnTime)
			{
				SpawnEntity(_position);
				_timer = 0f;
			}
			_timer += dt;
		}

		private void SpawnEntity(Vec3 position)
		{
			GameEntity = GameEntity.Instantiate(Mission.Current.Scene, "fire_obstacle", callScriptCallbacks: true);
			MatrixFrame frame = GameEntity.GetGlobalFrame();
			frame.origin = position;
			GameEntity.SetFrame(ref frame);
			Initialized = true;
		}

		public void Clear()
		{
			Mission.Current.Scene.RemoveEntity(GameEntity, 0);
			GameEntity = null;
			Initialized = false;
		}
	}

	private class EnemySpawnPoint
	{
		private const float GroupRadius = 20f;

		private GameEntity _entity;

		private AgentNavigator _navigator;

		public bool IsAlerted { get; private set; }

		public Vec3 Position => _entity.GlobalPosition;

		public Agent Agent { get; private set; }

		public EnemySpawnPoint(string spawnId, CharacterObject character, bool isNight)
		{
			IsAlerted = false;
			_entity = Mission.Current.Scene.FindEntityWithTag(spawnId);
			SpawnAgent(character, isNight);
		}

		public EnemySpawnPoint(GameEntity spawnEntity, CharacterObject character, bool isNight)
		{
			IsAlerted = false;
			_entity = spawnEntity;
			SpawnAgent(character, isNight);
		}

		public void CalmDown()
		{
			Agent.SetAlarmState(Agent.AIStateFlag.Cautious);
			if (Agent.Position.Distance(Position) >= 20f)
			{
				Vec3 randomPositionAroundPoint = Mission.Current.GetRandomPositionAroundPoint(Position, 1f, 3f);
				Agent.SetTargetPosition(randomPositionAroundPoint.AsVec2);
			}
			IsAlerted = false;
		}

		public bool CanSeeAgent(Agent agent)
		{
			if (Agent != null && Agent.IsActive() && _navigator.CanSeeAgent(agent))
			{
				return true;
			}
			return false;
		}

		public void Alert()
		{
			Agent.SetTeam(Mission.Current.PlayerEnemyTeam, sync: true);
			Agent.SetAgentFlags(Agent.GetAgentFlags() | AgentFlag.CanGetAlarmed);
			Agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			Agent.ClearTargetFrame();
			IsAlerted = true;
		}

		public void Clear()
		{
			if (Agent != null && Agent.IsActive())
			{
				Agent.FadeOut(hideInstantly: true, hideMount: true);
			}
			IsAlerted = false;
			Agent = null;
			_navigator = null;
		}

		private void SpawnAgent(CharacterObject character, bool isNight)
		{
			Vec3 globalPosition = _entity.GlobalPosition;
			Vec3 randomPositionAroundPoint = Mission.Current.GetRandomPositionAroundPoint(globalPosition, 1f, 3f);
			Vec2 direction = (randomPositionAroundPoint - globalPosition).AsVec2.Normalized();
			Agent = SpawnAgentAux(randomPositionAroundPoint, direction, character, isNight);
			_navigator = Agent.GetComponent<CampaignAgentComponent>().AgentNavigator;
		}

		private Agent SpawnAgentAux(Vec3 position, Vec2 direction, CharacterObject character, bool isNight, string patrolTag = null)
		{
			Equipment equipment = character.Equipment.Clone();
			if (isNight)
			{
				equipment[EquipmentIndex.ExtraWeaponSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("torch"));
			}
			AgentBuildData agentBuildData = new AgentBuildData(character).TroopOrigin(new SimpleAgentOrigin(character)).Team(Team.Invalid).InitialPosition(in position)
				.InitialDirection(in direction)
				.Equipment(equipment)
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false)
				.Banner(NavalStorylineData.CorsairBanner);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			agent.SpawnEquipment.GetInitialWeaponIndicesToEquip(out var _, out var offHandWeaponIndex, out var _);
			if (offHandWeaponIndex != EquipmentIndex.None)
			{
				agent.TryToWieldWeaponInSlot(offHandWeaponIndex, Agent.WeaponWieldActionType.InstantAfterPickUp, isWieldedOnSpawn: true);
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			component.CreateAgentNavigator();
			SandBoxManager.Instance.AgentBehaviorManager.AddFixedGuardBehaviors(agent);
			if (!string.IsNullOrEmpty(patrolTag))
			{
				component.AgentNavigator.SpecialTargetTag = patrolTag;
			}
			return agent;
		}

		public bool IsDepleted()
		{
			if (Agent != null)
			{
				return !Agent.IsActive();
			}
			return true;
		}

		public void Tick(float dt, BlockedEstuaryMissionController controller)
		{
			if (!IsAlerted)
			{
				if (Agent.Main != null && Agent.Main.IsActive() && (Position.DistanceSquared(Agent.Main.Position) < 5625f || CanSeeAgent(Agent.Main)))
				{
					Alert();
				}
				else if (controller.IsGunnarActive() && (Position.DistanceSquared(controller._gunnarAgent.Position) < 3600f || CanSeeAgent(controller._gunnarAgent)))
				{
					Alert();
				}
			}
		}
	}

	private class EnemyShipTrigger
	{
		private VolumeBox _trigger;

		private IShipOrigin _shipOrigin;

		private GameEntity _spawnEntity;

		private GameEntity _destination;

		private bool _isTriggered;

		public MissionShip Ship { get; private set; }

		public EnemyShipTrigger(GameEntity spawnPoint, VolumeBox volumeBox, IShipOrigin shipOrigin, string destinationId = null)
		{
			_trigger = volumeBox;
			_shipOrigin = shipOrigin;
			if (!string.IsNullOrEmpty(destinationId))
			{
				_destination = Mission.Current.Scene.FindEntityWithTag(destinationId);
			}
			_spawnEntity = spawnPoint;
			SpawnShip();
		}

		public void Tick(MissionShip target, float dt)
		{
			if (!_isTriggered && _destination != null && _destination.GlobalPosition.DistanceSquared(Ship.GameEntity.GlobalPosition) < 100f && !Ship.Physics.IsAnchored)
			{
				AnchorShip();
			}
			if (!_isTriggered && (_trigger.IsPointIn(target.GameEntity.GlobalPosition) || target.GameEntity.GlobalPosition.DistanceSquared(Ship.GameEntity.GlobalPosition) < 10000f))
			{
				TriggerShip();
			}
		}

		private void SpawnShip()
		{
			BlockedEstuaryMissionController missionBehavior = Mission.Current.GetMissionBehavior<BlockedEstuaryMissionController>();
			Ship = missionBehavior.SpawnEnemyChaserShip(_spawnEntity, _shipOrigin);
			AnchorShip();
			missionBehavior.ToggleShipBallistas(Ship, enabled: false);
		}

		private void AnchorShip()
		{
			Ship.SetAnchor(isAnchored: true, anchorInPlace: true);
			Ship.ShipOrder.SetShipStopOrder();
			Ship.SetShipOrderActive(isOrderActive: false);
			Ship.Formation.SetControlledByAI(isControlledByAI: false);
		}

		public void SendToDestination()
		{
			if (_destination != null)
			{
				Ship.ShipOrder.SetShipMovementOrder(_destination.GlobalPosition.AsVec2, (_destination.GlobalPosition.AsVec2 - Ship.GameEntity.GetGlobalFrame().rotation.f.AsVec2).Normalized());
				Ship.Formation.SetControlledByAI(isControlledByAI: false);
			}
		}

		public void TriggerShip()
		{
			BlockedEstuaryMissionController missionBehavior = Mission.Current.GetMissionBehavior<BlockedEstuaryMissionController>();
			missionBehavior.TriggerEnemyShip(Ship, missionBehavior._playerShip);
			_isTriggered = true;
		}
	}

	private const string EscapeZoneId = "escape_zone";

	private const string JumpingZoneId = "jumping_zone";

	private const string Fire2ZoneId = "fire_2_zone";

	private const string InitialTriggerZoneId = "burning_zone";

	private const string FireSystemId = "fire_particles";

	private const string Fire3ZoneId = "fire_3_zone";

	private const string CheckPointZoneId = "dismount_zone";

	private const string RampHolderId = "ramp_holder";

	private const string EnemyShipSpawnIdBase = "sp_enemy_ship_";

	private const string EnemyShipTriggerSpawnIdBase = "sp_enemy_trigger_";

	private const string EnemyShipDestinationIdBase = "sp_enemy_ship_destination_";

	private const string TargetShipSpawnId = "sp_enemy_ship_1";

	private const string PlayerBurningShipSpawnId = "sp_player_burning_ship";

	private const string PlayerBurningShipCheckpointSpawnId = "sp_player_burning_ship_checkpoint";

	private const string PlayerShipSpawnId = "sp_player_ship";

	private const string PlayerWaterSpawnPointAfterFadeToBlackId = "sp_player_mount";

	private const string PlayerCheckPointSpawnPointId = "sp_player_checkpoint";

	private const string GunnarBurningShipSpawnId = "sp_gangradir_burning_ship";

	private const string HorseSpawnPointId = "sp_horse";

	private const string HorseItemId = "sturgia_horse_tournament";

	private const string EnemyAgentPatrolPointBaseId = "sp_guard_patrol";

	private const string EnemyAgentSpawnPointBaseId = "enemy_group_parent";

	private const float WindStrength = 4f;

	private const float BurningSpreadRateMultiplier = 20f;

	private static readonly int BurningSoundEventId = SoundManager.GetEventGlobalIndex("event:/mission/ambient/detail/fire/fire_dynamic");

	private const float FirePatchFireDamage = 100f;

	private const float DefaultSpreadRate = 0.15f;

	private const float EscapePhaseNotificationCooldown = 15f;

	private static MBList<string> _enemyAgentCharacterIds = new MBList<string> { "vlandian_spearman", "vlandian_billman", "vlandian_marine_t4" };

	private MissionObjectiveLogic _missionObjectiveLogic;

	public Action OnCheckPointReachedEvent;

	public Action OnLastExitZoneReachedEvent;

	public Action OnPhaseEnd;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private DefaultNavalMissionAgentSpawnLogic _shipAgentSpawnLogic;

	private AgentNavalComponent _mainAgentNavalComponent;

	private VolumeBox _escapeZone;

	private VolumeBox _jumpingZone;

	private VolumeBox _fire2Zone;

	private VolumeBox _initialTriggerZone;

	private VolumeBox _fire3Zone;

	private VolumeBox _checkPointZone;

	private MBList<EnemyShipTrigger> _triggers = new MBList<EnemyShipTrigger>();

	private Dictionary<BurnShipObject, (BurningSystem, float)> _playerShipBurningSystems;

	private BurningSystem _enemyShipBurningSystem;

	private List<BurningProjectile> _projectileParticles = new List<BurningProjectile>();

	private float _shipDamageCheckTimer;

	private float _shipBurnProgress;

	private List<Agent> _burntShipAgents;

	private MBList<BurnShipObject> _burningMachines;

	private bool _initializeGunnarBurningShip;

	private bool _showedLastWarning;

	private SoundEvent _burningShipSoundEvent;

	private bool _sightedEnemies;

	private bool _firstCollisionFirePatch;

	private bool _firePatchSpawned;

	private float _boardedNotificationTimer;

	private float _incomingShotNotificationTimer;

	private float _shipHitNotificationTimer;

	private bool _playerShipHasLowHealth;

	private bool _enemyGotClose;

	private BattlePhase _currentPhase;

	private IShipOrigin _playerBurningShipOrigin;

	private MissionTimer _gunnarHorsePhaseCheckTimer;

	private IShipOrigin _enemyBurningShipOrigin;

	private bool _enemyAreaReached;

	private bool _playerLeftBehind;

	private IShipOrigin _playerShipOrigin;

	private MBList<IShipOrigin> _enemyShipOrigins = new MBList<IShipOrigin>();

	private bool _isShipBurning;

	private MissionShip _playerShip;

	private bool _initialized;

	private bool _enemiesPanicked;

	private bool _shipsCollided;

	private MissionTimer _missionEndTimer;

	private MissionTimer _missionPhaseEndTimer;

	private MissionTimer _collisionTimer;

	private bool _talkedToGunnar;

	private Agent _playerHorse;

	private Agent _horse;

	private Agent _gunnarAgent;

	private bool _shouldGunnarEscape;

	private Vec3 _escapePosition;

	private readonly MobileParty _enemyParty;

	private readonly bool _startFromCheckPoint;

	private bool _checkPointReached;

	private MBList<EnemySpawnPoint> _enemyAgentSpawnPoints = new MBList<EnemySpawnPoint>();

	public bool CanEndBattleNatively => CurrentPhase == BattlePhase.Phase3;

	public BattlePhase CurrentPhase
	{
		get
		{
			return _currentPhase;
		}
		private set
		{
			if (value != _currentPhase)
			{
				_currentPhase = value;
				OnPhaseEnd?.Invoke();
			}
		}
	}

	public MissionShip BurningShip { get; private set; }

	public bool IsShipBurning
	{
		get
		{
			return _isShipBurning;
		}
		private set
		{
			_isShipBurning = value;
			if (value && _burningShipSoundEvent == null && BurningShip != null)
			{
				_burningShipSoundEvent = SoundEvent.CreateEvent(BurningSoundEventId, base.Mission.Scene);
				_burningShipSoundEvent.SetPosition(BurningShip.GlobalFrame.origin);
				_burningShipSoundEvent.SetParameter("FireIntensity", 0.1f);
				_burningShipSoundEvent.Play();
			}
		}
	}

	public bool ShipsCollided => _shipsCollided;

	private bool IsEnding => _missionEndTimer != null;

	public bool CollisionImminent { get; private set; }

	public bool LastExitZoneReached { get; private set; }

	private MissionShip TargetShip { get; set; }

	public BlockedEstuaryMissionController(MobileParty enemyParty, bool startFromCheckPoint)
	{
		_enemyParty = enemyParty;
		_startFromCheckPoint = startFromCheckPoint;
		_checkPointReached = _startFromCheckPoint;
		CollectShips();
	}

	private void CollectShips()
	{
		new MBList<IShipOrigin>();
		Ship playerShipOrigin = MobileParty.MainParty.Ships.FirstOrDefault((Ship x) => x.ShipHull.StringId == "ship_trade_cog_q3") ?? MobileParty.MainParty.Ships.First();
		Ship enemyBurningShip = _enemyParty.Ships.FirstOrDefault((Ship x) => x.ShipHull.StringId == "burning_cog_ship");
		_enemyShipOrigins = _enemyParty.Ships.Where((Ship x) => x != enemyBurningShip).Cast<IShipOrigin>().ToMBList();
		_playerBurningShipOrigin = MobileParty.MainParty.Ships.FirstOrDefault((Ship x) => x.ShipHull.StringId == "burning_fishing_ship");
		_enemyBurningShipOrigin = enemyBurningShip;
		_playerShipOrigin = playerShipOrigin;
	}

	public override void OnMissionTick(float dt)
	{
		if (!_initialized)
		{
			Initialize();
		}
		if (_missionEndTimer != null && _missionEndTimer.Check())
		{
			OnFinalize();
		}
		if ((Agent.Main == null || !Agent.Main.IsActive()) && !IsEnding)
		{
			OnFail(new TextObject("{=ay5y18aq}You pass out from the pain of your wounds."));
		}
		switch (CurrentPhase)
		{
		case BattlePhase.Phase1:
			TickMissionPhase1(dt);
			break;
		case BattlePhase.Phase2:
			TickMissionPhase2(dt);
			break;
		case BattlePhase.Phase3:
			TickMissionPhase3(dt);
			break;
		}
		TickParticlesAndBurningSystems(dt);
		TickGunnar(dt);
	}

	private void TickMissionPhase1(float dt)
	{
		MatrixFrame globalFrame = BurningShip.GlobalFrame;
		if (_collisionTimer != null && _collisionTimer.Check() && !IsEnding)
		{
			OnFail(new TextObject("{=CAyVaV0Y}Your fireship missed its target! The enemy flagship is unscathed."));
		}
		else if (IsShipBurning && !IsEnding)
		{
			if (_missionPhaseEndTimer != null && _missionPhaseEndTimer.Check())
			{
				ProceedToPhase2();
				_missionPhaseEndTimer = null;
			}
			else if (Agent.Main.IsInWater())
			{
				if (_shipsCollided && _missionPhaseEndTimer == null)
				{
					DestroyCollidingShips();
					_missionPhaseEndTimer = new MissionTimer(6f);
					MBMusicManager.Current.ChangeCurrentThemeIntensity(1f);
				}
			}
			else if (_jumpingZone.IsPointIn(globalFrame.origin))
			{
				OnFail(new TextObject("{=Uj6t6FES}You missed the oppurtunity to jump off the ship."));
			}
			if (BurningShip.IsDisabled && _collisionTimer == null && !_shipsCollided)
			{
				OnFail(new TextObject("{=S0L5Zi8a}Your ship is engulfed by flames."));
			}
			if (_jumpingZone.IsPointIn(globalFrame.origin) && _collisionTimer == null)
			{
				_collisionTimer = new MissionTimer(15f);
				CollisionImminent = true;
			}
			if (CollisionImminent && !_enemiesPanicked && WillHitBoundingBox(BurningShip.GameEntity.GlobalPosition, BurningShip.Physics.LinearVelocity.AsVec2 * 3f, TargetShip.GameEntity.GlobalPosition + TargetShip.GameEntity.GetBoundingBoxMin(), TargetShip.GameEntity.GlobalPosition + TargetShip.GameEntity.GetBoundingBoxMax()))
			{
				MakeEnemiesPanic(TargetShip);
			}
		}
		if ((_fire3Zone.IsPointIn(globalFrame.origin) || _shipBurnProgress >= 0.6f) && !_shouldGunnarEscape)
		{
			ShowGunnarEscapeNotification();
			_shouldGunnarEscape = true;
			if (_gunnarAgent != null)
			{
				SetEscapePosition();
			}
		}
		if (!LastExitZoneReached && !_showedLastWarning && !BurningShip.IsDisabled && !BurningShip.IsSinking && BurningShip.GameEntity.GlobalPosition.Distance(TargetShip.GameEntity.GlobalPosition) < 120f && !IsEnding && !Agent.Main.IsInWater())
		{
			ShowNotification(new TextObject("{=yYkI9ezi}Jump now! You want your breeks to catch fire?"), isAnnouncedByGunnar: true, MBInformationManager.NotificationPriority.High);
			_showedLastWarning = true;
		}
		if (_jumpingZone.IsPointIn(globalFrame.origin) && !LastExitZoneReached)
		{
			LastExitZoneReached = true;
			OnLastExitZoneReachedEvent?.Invoke();
			if (!IsShipBurning)
			{
				ActivateAllBurningSystems(0.15f);
			}
		}
		if (!CollisionImminent)
		{
			TickShipHealth(dt);
		}
		if (_initialTriggerZone.IsPointIn(globalFrame.origin) && !_initializeGunnarBurningShip)
		{
			_initializeGunnarBurningShip = true;
		}
		MakeEnemiesStand(TargetShip);
	}

	private void TickShipHealth(float dt)
	{
		if (_shipsCollided || !IsShipBurning || !(BurningShip.HitPoints > 0f) || LastExitZoneReached)
		{
			return;
		}
		float num = 0f;
		foreach (KeyValuePair<BurnShipObject, (BurningSystem, float)> playerShipBurningSystem in _playerShipBurningSystems)
		{
			if (playerShipBurningSystem.Value.Item1 != null)
			{
				num += playerShipBurningSystem.Value.Item1.GetFlameProgress();
			}
		}
		num = TaleWorlds.Library.MathF.Clamp(num / (float)Math.Max(1, _playerShipBurningSystems.Count((KeyValuePair<BurnShipObject, (BurningSystem, float)> x) => x.Key.IsDeactivated)), 0f, 1f);
		_shipDamageCheckTimer += dt;
		while (_shipDamageCheckTimer > 0.1f)
		{
			_shipDamageCheckTimer -= 0.1f;
			float rawDamage = (num - _shipBurnProgress) * BurningShip.MaxHealth;
			_shipBurnProgress = num;
			BurningShip.DealDamage(rawDamage, null, out var _, out var _, out var _, out var _);
			float num2 = (num - (1f - BurningShip.FireHitPoints / BurningShip.MaxFireHealth)) * BurningShip.MaxFireHealth;
			if (num2 > 0f)
			{
				BurningShip.DealFireDamage(num2);
			}
		}
	}

	private void EnableRamp(MissionShip targetShip)
	{
		targetShip.GameEntity.GetFirstChildEntityWithTagRecursive("ramp_holder").SetVisibilityExcludeParents(visible: true);
	}

	private void MakeEnemiesStand(MissionShip targetShip)
	{
		foreach (ShipOarMachine leftSideShipOarMachine in targetShip.LeftSideShipOarMachines)
		{
			leftSideShipOarMachine.SetDisabledAndMakeInvisible();
		}
		foreach (ShipOarMachine rightSideShipOarMachine in targetShip.RightSideShipOarMachines)
		{
			rightSideShipOarMachine.SetDisabledAndMakeInvisible();
		}
	}

	private void MakeEnemiesPanic(MissionShip targetShip)
	{
		EnableRamp(targetShip);
		_burntShipAgents = _navalAgentsLogic.GetActiveAgentsOfShip(targetShip).ToList();
		_navalAgentsLogic.RemoveAllReservedTroopsFromShip(targetShip);
		targetShip.Formation.SetControlledByAI(isControlledByAI: true);
		targetShip.ShipOrder.FormationLeaveShip();
		for (int num = _burntShipAgents.Count - 1; num >= 0; num--)
		{
			Agent agent = _burntShipAgents[num];
			Vec3 globalPosition = targetShip.GameEntity.GlobalPosition;
			Vec2 vec = new Vec2(MBRandom.RandomFloatRanged(60f, 110f), MBRandom.RandomFloatRanged(70f, 120f));
			Vec2 targetPosition = globalPosition.AsVec2 + ((MBRandom.RandomFloat < 0.5f) ? vec : (-vec));
			agent.SetTargetPositionAndDirection(in targetPosition, (targetPosition.ToVec3() - agent.Position).NormalizedCopy());
		}
		_enemiesPanicked = true;
	}

	private void TickParticlesAndBurningSystems(float dt)
	{
		float num = 0f;
		if (IsShipBurning)
		{
			foreach (KeyValuePair<BurnShipObject, (BurningSystem, float)> playerShipBurningSystem in _playerShipBurningSystems)
			{
				if (playerShipBurningSystem.Value.Item1 != null)
				{
					playerShipBurningSystem.Value.Item1.Tick(dt);
					num += playerShipBurningSystem.Value.Item1.GetFlameProgress();
					playerShipBurningSystem.Value.Item1.CheckWater();
				}
			}
		}
		if (!BurningShip.IsDisabled && (!LastExitZoneReached || _shipsCollided))
		{
			bool flag = true;
			foreach (KeyValuePair<BurnShipObject, (BurningSystem, float)> playerShipBurningSystem2 in _playerShipBurningSystems)
			{
				if (playerShipBurningSystem2.Value.Item1 != null && !playerShipBurningSystem2.Value.Item1.FlamesReachedEnd())
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				DisableShip(BurningShip);
			}
		}
		if (_shipsCollided)
		{
			_enemyShipBurningSystem.Tick(dt);
			_enemyShipBurningSystem.CheckWater();
		}
		if (_enemyShipBurningSystem.FlamesReachedEnd() && !TargetShip.IsDisabled)
		{
			DisableShip(TargetShip);
		}
		bool flag2 = false;
		for (int num2 = _projectileParticles.Count - 1; num2 >= 0; num2--)
		{
			BurningProjectile burningProjectile = _projectileParticles[num2];
			burningProjectile.Tick(dt, out var shouldBeRemoved);
			if (shouldBeRemoved)
			{
				burningProjectile.Clear();
				_projectileParticles.RemoveAt(num2);
			}
			else if (!flag2 && CurrentPhase == BattlePhase.Phase1 && DoesShipCollideWithProjectile(BurningShip, burningProjectile))
			{
				flag2 = true;
				if (!_firstCollisionFirePatch)
				{
					_firstCollisionFirePatch = true;
					ShowNotification(new TextObject("{=xrdbaPop}Watch out! Let's not go up in flames until we reach them!"), isAnnouncedByGunnar: true);
				}
			}
			else if (!_firePatchSpawned && burningProjectile.GameEntity != null && !IsShipBurning && CurrentPhase == BattlePhase.Phase1)
			{
				_firePatchSpawned = true;
				ShowNotification(new TextObject("{=dmyrUCZ3}Steer clear of those flames, eh?"), isAnnouncedByGunnar: true);
			}
		}
		bool flag3 = BurningShip.FireHitPoints <= 0f;
		if (CurrentPhase == BattlePhase.Phase1)
		{
			if (IsShipBurning)
			{
				if (!LastExitZoneReached)
				{
					foreach (KeyValuePair<BurnShipObject, (BurningSystem, float)> playerShipBurningSystem3 in _playerShipBurningSystems)
					{
						if (playerShipBurningSystem3.Value.Item1 != null)
						{
							float spreadRate = (flag2 ? (playerShipBurningSystem3.Value.Item2 * 20f) : playerShipBurningSystem3.Value.Item2);
							playerShipBurningSystem3.Value.Item1.SetSpreadRate(spreadRate);
						}
					}
				}
			}
			else if (flag3)
			{
				ActivateAllBurningSystems(0.15f);
			}
			else if (flag2)
			{
				BurningShip.DealFireDamage(100f * dt);
			}
		}
		if (IsShipBurning)
		{
			_burningShipSoundEvent.SetParameter("FireIntensity", num * 20f);
			_burningShipSoundEvent.SetPosition(BurningShip.GlobalFrame.origin);
		}
	}

	private void BurnSails(MissionShip ship)
	{
		foreach (MissionSail sail in ship.Sails)
		{
			if (!sail.IsBurning())
			{
				sail.StartFire();
			}
		}
	}

	private void ToggleShipBallistas(MissionShip ship, bool enabled)
	{
		if (ship.ShipSiegeWeapon == null)
		{
			return;
		}
		foreach (StandingPoint standingPoint in ship.ShipSiegeWeapon.StandingPoints)
		{
			standingPoint.IsDeactivated = !enabled;
		}
	}

	private void DisableShip(MissionShip ship, bool burnSails = true)
	{
		if (ship.IsDisabled)
		{
			return;
		}
		foreach (ShipAttachmentMachine attachmentMachine in ship.AttachmentMachines)
		{
			attachmentMachine.SetDisabled();
		}
		ship.ShipControllerMachine.SetDisabled();
		foreach (ClimbingMachine climbingMachine in ship.ClimbingMachines)
		{
			climbingMachine.SetDisabled();
		}
		foreach (ShipOarMachine leftSideShipOarMachine in ship.LeftSideShipOarMachines)
		{
			leftSideShipOarMachine.SetDisabled();
		}
		foreach (ShipOarMachine rightSideShipOarMachine in ship.RightSideShipOarMachines)
		{
			rightSideShipOarMachine.SetDisabled();
		}
		ToggleShipBallistas(ship, enabled: false);
		if (ship.ShipControllerMachine.PilotAgent != null)
		{
			ship.ShipControllerMachine.PilotAgent.StopUsingGameObject();
		}
		ship.ShipControllerMachine.SetDisabled();
		ship.SetDisabled();
		DisableTargetShipObject(ship);
		ship.SetAnchor(isAnchored: true, anchorInPlace: true);
		if (burnSails)
		{
			BurnSails(ship);
		}
	}

	private void SetWindStrengthAndDirection(Vec2 direction, float strength)
	{
		Mission.Current.Scene.SetGlobalWindVelocity(strength * direction);
	}

	private void ProceedToPhase2()
	{
		IsShipBurning = true;
		_shipsCollided = true;
		SpawnPlayerTradeShip();
		FadeoutEnemyAgents();
		SpawnEnemyAgentsOnRoad();
		DisableShip(BurningShip);
		DisableShip(TargetShip);
		MBMusicManager.Current.ChangeCurrentThemeIntensity(-0.4f);
		CollisionImminent = false;
		_playerHorse = SpawnPlayerHorse();
		Vec3 randomPositionAroundPoint = base.Mission.GetRandomPositionAroundPoint(_playerHorse.Position, 2f, 4f);
		_horse = SpawnHorse(randomPositionAroundPoint, (randomPositionAroundPoint - Agent.Main.Position).AsVec2);
		TeleportMainAgent("sp_player_mount");
		PrepareGunnarForSecondPhase();
		if (IsGunnarActive())
		{
			ShowNotification(new TextObject("{=NB2HCGUq}Head for shore! There are a pair of horses waiting for us. We must ride quickly back to the Sturgians before the Sea Hounds can reorganize the blockade."), isAnnouncedByGunnar: true);
		}
		else
		{
			ShowNotification(new TextObject("{=mlMbHCaG}Head for shore! There are a pair of horses waiting for you. Ride quickly back to the Sturgians before the Sea Hounds can reorganize the blockade."), isAnnouncedByGunnar: true);
		}
		CurrentPhase = BattlePhase.Phase2;
		_missionObjectiveLogic.StartObjective(new SwimToShoreObjective(base.Mission, _gunnarAgent));
	}

	public List<Agent> GetAgentsOfInterest()
	{
		List<Agent> list = new List<Agent>();
		if (CurrentPhase == BattlePhase.Phase2)
		{
			if (_horse != null && _horse.IsActive())
			{
				list.Add(_horse);
			}
			if (_playerHorse != null && _playerHorse.IsActive())
			{
				list.Add(_playerHorse);
			}
		}
		if (IsGunnarActive())
		{
			list.Add(_gunnarAgent);
		}
		return list;
	}

	private void PrepareGunnarForSecondPhase()
	{
		Vec3 randomPositionAroundPoint = base.Mission.GetRandomPositionAroundPoint(Agent.Main.Position, 1f, 3f);
		if (_gunnarAgent == null || !_gunnarAgent.IsActive())
		{
			SpawnGunnar(randomPositionAroundPoint, noHorses: true);
		}
		else if (Agent.Main.Position.Distance(_gunnarAgent.Position) > 5f)
		{
			_gunnarAgent.TeleportToPosition(randomPositionAroundPoint);
		}
		_gunnarAgent.SetTeam(Team.Invalid, sync: true);
		foreach (Agent activeAgent in base.Mission.PlayerEnemyTeam.ActiveAgents)
		{
			activeAgent.ResetEnemyCaches();
		}
		_gunnarAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.ClearTarget();
		_gunnarAgent.SetAgentFlags(_gunnarAgent.GetAgentFlags() | AgentFlag.CanRide);
		_gunnarAgent.SetRidingOrder(RidingOrder.RidingOrderEnum.Mount);
		_gunnarAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		WorldPosition scriptedPosition = new WorldPosition(base.Mission.Scene, _horse.Position);
		_gunnarAgent.SetScriptedPositionAndDirection(ref scriptedPosition, (_horse.Position - _gunnarAgent.Position).AsVec2.RotationInRadians, addHumanLikeDelay: true, Agent.AIScriptedFrameFlags.NeverSlowDown);
	}

	private void SpawnEnemyAgentsOnRoad()
	{
		bool isNight = Campaign.Current.IsNight;
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("enemy_group_parent");
		if (gameEntity != null)
		{
			for (int i = 0; i < gameEntity.ChildCount; i++)
			{
				GameEntity child = gameEntity.GetChild(i);
				_enemyAgentSpawnPoints.Add(new EnemySpawnPoint(child, MBObjectManager.Instance.GetObject<CharacterObject>(_enemyAgentCharacterIds.GetRandomElement()), isNight));
			}
		}
	}

	private void FadeoutEnemyAgents()
	{
		if (_burntShipAgents != null)
		{
			foreach (Agent burntShipAgent in _burntShipAgents)
			{
				if (burntShipAgent.IsActive())
				{
					burntShipAgent.FadeOut(hideInstantly: true, hideMount: true);
				}
			}
		}
		_burntShipAgents = null;
	}

	private void TeleportMainAgent(string spawnPointId)
	{
		MatrixFrame globalFrame = base.Mission.Scene.FindEntityWithTag(spawnPointId).GetGlobalFrame();
		Agent.Main.TeleportToPosition(globalFrame.origin);
		Agent.Main.LookDirection = globalFrame.rotation.f.NormalizedCopy();
	}

	private static void ShowNotification(TextObject text, bool isAnnouncedByGunnar, MBInformationManager.NotificationPriority priority = MBInformationManager.NotificationPriority.Medium)
	{
		if (!isAnnouncedByGunnar)
		{
			MBInformationManager.AddQuickInformation(text);
		}
		else
		{
			CampaignInformationManager.AddDialogLine(text, NavalStorylineData.Gunnar.CharacterObject, null, 0, priority);
		}
	}

	private void DestroyCollidingShips()
	{
		TargetShip.SetShipOrderActive(isOrderActive: false);
		TargetShip.Formation.SetControlledByAI(isControlledByAI: false);
		TargetShip.SetAnchor(isAnchored: false);
		BurningShip.SetShipOrderActive(isOrderActive: false);
		BurningShip.ShipOrder.SetFormation(null);
		TargetShip.ShipOrder.SetFormation(null);
		for (int num = _burntShipAgents.Count - 1; num >= 0; num--)
		{
			Agent agent = _burntShipAgents[num];
			if (!agent.IsInWater() && _navalAgentsLogic.IsAgentOnAnyShip(agent, out var onShip, TeamSideEnum.EnemyTeam) && onShip == TargetShip)
			{
				Blow b = new Blow
				{
					InflictedDamage = 1000,
					DamagedPercentage = 1f
				};
				agent.Die(b);
			}
		}
		BurnSails(TargetShip);
		BurnSails(BurningShip);
	}

	public void OnBurningMachineUsed(BurnShipObject burnShipObject)
	{
		ActivateBurningSystem(burnShipObject, 0.15f);
	}

	private void MakeGunnarEscapeShip()
	{
		if (_gunnarAgent.IsAIControlled && _gunnarAgent.AIMoveToGameObjectIsEnabled())
		{
			_gunnarAgent.AIMoveToGameObjectDisable();
		}
		if (_gunnarAgent.IsUsingGameObject)
		{
			_gunnarAgent.StopUsingGameObject();
		}
		EnableRamp(BurningShip);
		Vec2 asVec = _escapePosition.AsVec2;
		Vec3 escapePosition = GetEscapePosition(BurningShip);
		if (escapePosition.Distance(asVec.ToVec3(escapePosition.z)) > 10f)
		{
			SetEscapePosition(escapePosition);
		}
	}

	private void ShowGunnarEscapeNotification()
	{
		ShowNotification(new TextObject("{=yXOnEQJ6}Our ship is ablaze! Get ready to jump!"), IsGunnarActive());
	}

	private void SetEscapePosition()
	{
		SetEscapePosition(GetEscapePosition(BurningShip));
	}

	private void SetEscapePosition(Vec3 position)
	{
		_escapePosition = position;
		Vec2 targetPosition = _escapePosition.AsVec2;
		_gunnarAgent.SetTargetPositionAndDirection(in targetPosition, (position - _gunnarAgent.Position).NormalizedCopy());
	}

	private Vec3 GetEscapePosition(MissionShip ship)
	{
		return ship.GameEntity.GetGlobalFrame().rotation.f * 10f - ship.GameEntity.GetGlobalFrame().rotation.s * 15f + ship.GameEntity.GlobalPosition;
	}

	public void ActivateAllBurningSystems(float spreadRate)
	{
		for (int i = 0; i < _burningMachines.Count; i++)
		{
			ActivateBurningSystem(_burningMachines[i], spreadRate);
		}
	}

	public void ActivateBurningSystem(BurnShipObject burnShipObject, float spreadRate)
	{
		if (burnShipObject != null)
		{
			(BurningSystem, float) tuple = _playerShipBurningSystems[burnShipObject];
			_playerShipBurningSystems[burnShipObject] = (tuple.Item1, spreadRate);
		}
		IsShipBurning = true;
	}

	private void TickMissionPhase2(float dt)
	{
		if (IsEnding)
		{
			return;
		}
		if (!_startFromCheckPoint)
		{
			if (_checkPointZone.IsPointIn(Agent.Main.Position) && !_checkPointReached && !_enemyAgentSpawnPoints.Any((EnemySpawnPoint x) => x.Agent.IsActive() && x.Agent.Position.Distance(Agent.Main.Position) < 50f))
			{
				OnCheckPointReached();
			}
			if (_checkPointReached)
			{
				if (Agent.Main.HasMount)
				{
					TickHorse(Agent.Main);
				}
			}
			else
			{
				if (Agent.Main.HasMount)
				{
					float stat = Agent.Main.MountAgent.AgentDrivenProperties.GetStat(DrivenProperty.MountSpeed);
					float num = ((IsGunnarActive() && _gunnarAgent.HasMount) ? _gunnarAgent.MountAgent.AgentDrivenProperties.GetStat(DrivenProperty.MountSpeed) : stat);
					if (!stat.ApproximatelyEqualsTo(num) && stat < num)
					{
						Agent.Main.MountAgent.AgentDrivenProperties.SetStat(DrivenProperty.MountSpeed, num);
						Agent.Main.MountAgent.UpdateCustomDrivenProperties();
					}
				}
				bool flag = false;
				float stat2 = Agent.Main.AgentDrivenProperties.GetStat(DrivenProperty.SwingSpeedMultiplier);
				float num2 = TaleWorlds.Library.MathF.Max(stat2, 1f);
				if (!stat2.ApproximatelyEqualsTo(num2))
				{
					flag = true;
					Agent.Main.AgentDrivenProperties.SetStat(DrivenProperty.SwingSpeedMultiplier, num2);
				}
				float stat3 = Agent.Main.AgentDrivenProperties.GetStat(DrivenProperty.ThrustOrRangedReadySpeedMultiplier);
				float num3 = TaleWorlds.Library.MathF.Max(stat3, 1f);
				if (!stat3.ApproximatelyEqualsTo(num3))
				{
					flag = true;
					Agent.Main.AgentDrivenProperties.SetStat(DrivenProperty.ThrustOrRangedReadySpeedMultiplier, num3);
				}
				float stat4 = Agent.Main.AgentDrivenProperties.GetStat(DrivenProperty.OffhandWeaponDefendSpeedMultiplier);
				float num4 = TaleWorlds.Library.MathF.Max(stat4, 1f);
				if (!stat4.ApproximatelyEqualsTo(num4))
				{
					flag = true;
					Agent.Main.AgentDrivenProperties.SetStat(DrivenProperty.OffhandWeaponDefendSpeedMultiplier, num4);
				}
				if (flag)
				{
					Agent.Main.UpdateCustomDrivenProperties();
				}
			}
		}
		if (!_checkPointReached)
		{
			CheckEnemyGroups(dt);
		}
		else if (_playerShip == _mainAgentNavalComponent.SteppedShip && _missionPhaseEndTimer == null)
		{
			_missionPhaseEndTimer = new MissionTimer(1f);
		}
		else if (_gunnarAgent == null && !Agent.Main.HasMount)
		{
			SpawnGunnarOnShip(_playerShip);
		}
		if (_missionPhaseEndTimer != null && _missionPhaseEndTimer.Check() && _talkedToGunnar)
		{
			ProceedToPhase3();
			_missionPhaseEndTimer = null;
		}
	}

	private void TickGunnar(float dt)
	{
		if (IsEnding)
		{
			return;
		}
		bool flag = IsGunnarActive();
		if (CurrentPhase == BattlePhase.Phase1)
		{
			if (flag && !_gunnarAgent.IsUsingGameObject && _initializeGunnarBurningShip && !LastExitZoneReached && !_shouldGunnarEscape)
			{
				BurnShipObject burnShipObject = _burningMachines.FirstOrDefault((BurnShipObject x) => !x.IsDeactivated && !x.HasUser);
				if (burnShipObject != null && !burnShipObject.PilotStandingPoint.HasAIMovingTo)
				{
					_gunnarAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetTarget(burnShipObject);
				}
			}
			else if (flag && _shouldGunnarEscape && !_gunnarAgent.IsInWater())
			{
				MakeGunnarEscapeShip();
			}
		}
		else
		{
			if (CurrentPhase != BattlePhase.Phase2)
			{
				return;
			}
			if (!_checkPointReached && !_talkedToGunnar)
			{
				if (_missionPhaseEndTimer != null && _missionPhaseEndTimer.Check())
				{
					if (flag && _gunnarAgent.HasMount)
					{
						if (Agent.Main.Position.Distance(_gunnarAgent.Position) <= 30f)
						{
							StartConversation();
							_missionPhaseEndTimer = null;
							_talkedToGunnar = true;
						}
						else
						{
							ProceedToRideWithoutTalkingToGunnar();
						}
					}
				}
				else if (!flag && Agent.Main.HasMount)
				{
					ProceedToRideWithoutTalkingToGunnar();
				}
			}
			else
			{
				if (_checkPointReached || !_talkedToGunnar || !_enemyAreaReached || !flag || !_gunnarAgent.HasMount || _missionPhaseEndTimer != null || _gunnarHorsePhaseCheckTimer == null || !_gunnarHorsePhaseCheckTimer.Check())
				{
					return;
				}
				_gunnarHorsePhaseCheckTimer.Reset();
				Vec3 point = _escapePosition;
				float pathDistanceToPoint = _gunnarAgent.GetPathDistanceToPoint(ref point);
				float pathDistanceToPoint2 = Agent.Main.GetPathDistanceToPoint(ref point);
				if (pathDistanceToPoint2 < 150f && pathDistanceToPoint < 150f)
				{
					_gunnarHorsePhaseCheckTimer = null;
					if (!_enemyAgentSpawnPoints.Any((EnemySpawnPoint x) => x.Agent.IsActive() && x.Agent.Position.Distance(Agent.Main.Position) < 50f))
					{
						ShowNotification(new TextObject("{=NHS4NQdS}I think that's the last of them."), isAnnouncedByGunnar: true);
					}
				}
				else if (!_playerLeftBehind && pathDistanceToPoint2 > pathDistanceToPoint + 40f)
				{
					_playerLeftBehind = true;
					ShowNotification(new TextObject("{=AHShYsjD}Don't tarry! Keep up with me!"), isAnnouncedByGunnar: true);
				}
			}
		}
	}

	private void ProceedToRideWithoutTalkingToGunnar()
	{
		if (IsGunnarActive())
		{
			OnTalkedToGunnarPhase2();
		}
		else
		{
			SoundManager.StartOneShotEvent("event:/alerts/horns/attack", _enemyAgentSpawnPoints[0].Position);
		}
		_missionPhaseEndTimer = null;
		_talkedToGunnar = true;
	}

	private void StartConversation()
	{
		Campaign.Current.ConversationManager.SetupAndStartMissionConversation(_gunnarAgent, Agent.Main, setActionsInstantly: false);
		base.Mission.SetMissionMode(MissionMode.Conversation, atStart: true);
	}

	private void SpawnGunnarOnShip(MissionShip ship)
	{
		_navalAgentsLogic.AddReservedTroopToShip(new SimpleAgentOrigin(NavalStorylineData.Gunnar.CharacterObject), ship);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		_gunnarAgent = base.Mission.Agents.First((Agent x) => x.Character == NavalStorylineData.Gunnar.CharacterObject);
		_gunnarAgent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
	}

	private void CheckEnemyGroups(float dt)
	{
		foreach (EnemySpawnPoint enemyAgentSpawnPoint in _enemyAgentSpawnPoints)
		{
			enemyAgentSpawnPoint.Tick(dt, this);
			if (!_enemyAreaReached && enemyAgentSpawnPoint.IsAlerted)
			{
				if (Agent.Main != null && Agent.Main.IsActive() && Agent.Main.HasMount)
				{
					ShowNotification(new TextObject("{=5McrRAZb}There they are! Ride fast! Ride through them!"), isAnnouncedByGunnar: true);
				}
				_enemyAreaReached = true;
			}
		}
	}

	private void ProceedToPhase3()
	{
		if (!_checkPointReached)
		{
			OnCheckPointReached();
		}
		CurrentPhase = BattlePhase.Phase3;
		_playerShip.SetAnchor(isAnchored: false);
		_playerShip.Formation.SetControlledByAI(isControlledByAI: true);
		_playerShip.ShipOrder.FormationJoinShip(_playerShip.Formation);
		_playerShip.SetShipOrderActive(isOrderActive: true);
		if (Agent.Main != null)
		{
			if (_navalAgentsLogic.IsAgentOnAnyShip(Agent.Main, out var _))
			{
				_navalAgentsLogic.TransferAgentToShip(Agent.Main, _playerShip);
			}
			else
			{
				_navalAgentsLogic.AddAgentToShip(Agent.Main, _playerShip);
			}
			if (!_startFromCheckPoint)
			{
				Agent.Main.UseGameObject(_playerShip.ShipControllerMachine.PilotStandingPoint);
				_playerShip.ShipControllerMachine.OnPilotAssignedDuringSpawn();
			}
		}
		_missionObjectiveLogic.StartObjective(new ReachEscapeZoneObjective(base.Mission, _playerShip, _escapeZone.GameEntity.GlobalPosition + new Vec3(0f, 0f, 5f)));
		_gunnarHorsePhaseCheckTimer = null;
		ShowNotification(new TextObject("{=UUexHDKH}Well done! Now, let's run their blockade and reach the open sea."), isAnnouncedByGunnar: true);
	}

	private void ActivateEnemyShips()
	{
		foreach (EnemyShipTrigger trigger in _triggers)
		{
			trigger.SendToDestination();
		}
	}

	private void InitializeShipTriggers()
	{
		for (int i = 0; i < 10; i++)
		{
			int num = i + 2;
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_enemy_ship_" + num);
			VolumeBox volumeBox = Mission.Current.Scene.FindEntityWithTag("sp_enemy_trigger_" + num)?.GetFirstScriptOfType<VolumeBox>();
			if (!(gameEntity == null))
			{
				if (volumeBox == null)
				{
					Debug.FailedAssert("There is no volume box for spawn point: sp_enemy_trigger_" + num, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\MissionControllers\\BlockedEstuaryMissionController.cs", "InitializeShipTriggers", 1429);
					break;
				}
				if (num - 1 > _enemyShipOrigins.Count)
				{
					Debug.FailedAssert("There are not enough ships in party", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\MissionControllers\\BlockedEstuaryMissionController.cs", "InitializeShipTriggers", 1435);
					break;
				}
				if (gameEntity != null)
				{
					_triggers.Add(new EnemyShipTrigger(gameEntity, volumeBox, _enemyShipOrigins[num - 2], "sp_enemy_ship_destination_" + num));
					continue;
				}
				break;
			}
			break;
		}
	}

	private void ClearEnemyGroups()
	{
		for (int num = _enemyAgentSpawnPoints.Count - 1; num >= 0; num--)
		{
			_enemyAgentSpawnPoints[num].Clear();
		}
		_enemyAgentSpawnPoints = null;
	}

	public override void OnAgentMount(Agent agent)
	{
		if (CurrentPhase == BattlePhase.Phase2 && !_checkPointReached && !_startFromCheckPoint && !_talkedToGunnar && !IsEnding && IsGunnarActive() && _gunnarAgent.HasMount && Agent.Main.HasMount)
		{
			_missionPhaseEndTimer = new MissionTimer(1f);
		}
		if (_gunnarAgent == agent)
		{
			_gunnarAgent.SetAlarmState(Agent.AIStateFlag.None);
			_gunnarAgent.SetTargetPosition(_gunnarAgent.Position.AsVec2);
			_gunnarAgent.MountAgent.SetTargetPosition(_gunnarAgent.Position.AsVec2);
		}
	}

	public void OnTalkedToGunnarPhase2()
	{
		_gunnarHorsePhaseCheckTimer = new MissionTimer(3f);
		_gunnarAgent.MountAgent.ClearTargetFrame();
		_gunnarAgent.ClearTargetFrame();
		SoundManager.StartOneShotEvent("event:/alerts/horns/attack", _enemyAgentSpawnPoints[0].Position);
		_horse.SetMortalityState(Agent.MortalityState.Mortal);
		_playerHorse.SetMortalityState(Agent.MortalityState.Mortal);
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: false);
		GetRandomPositionAroundCheckPoint();
		_escapePosition = GetRandomPositionAroundCheckPoint();
		WorldPosition position = new WorldPosition(base.Mission.Scene, _escapePosition);
		_gunnarAgent.SetScriptedPosition(ref position, addHumanLikeDelay: true, Agent.AIScriptedFrameFlags.GoToPosition | Agent.AIScriptedFrameFlags.NeverSlowDown);
		_missionObjectiveLogic.StartObjective(new ReachShipObjective(base.Mission, _gunnarAgent, _playerShip));
		ActivateEnemyShips();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.IsHuman && _enemyAgentSpawnPoints != null)
		{
			for (int num = _enemyAgentSpawnPoints.Count - 1; num >= 0; num--)
			{
				if (_enemyAgentSpawnPoints[num].IsDepleted())
				{
					_enemyAgentSpawnPoints[num].Clear();
					_enemyAgentSpawnPoints.RemoveAt(num);
				}
			}
		}
		if (affectedAgent == _gunnarAgent)
		{
			_gunnarAgent = null;
		}
	}

	private Vec3 GetRandomPositionAroundCheckPoint()
	{
		Vec3 globalPosition = _checkPointZone.GameEntity.GlobalPosition;
		float height = globalPosition.z;
		base.Mission.Scene.GetHeightAtPoint(globalPosition.AsVec2, BodyFlags.None, ref height);
		globalPosition.z = height;
		return base.Mission.GetRandomPositionAroundPoint(globalPosition, 1f, 3f);
	}

	public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
		if (collisionData.MissileGoneUnderWater && _navalShipsLogic.IsMissileFromShipSiegeEngine(collisionData.AffectorWeaponSlotOrMissileIndex))
		{
			_projectileParticles.Add(new BurningProjectile(collisionData.CollisionGlobalPosition, 300f, MBRandom.RandomFloatRanged(0.2f, 1.5f), () => CurrentPhase != BattlePhase.Phase1));
		}
	}

	public override void OnAgentDismount(Agent agent)
	{
		base.OnAgentDismount(agent);
		if (_checkPointReached && agent.IsMainAgent)
		{
			Agent.Main.SetAgentFlags((AgentFlag)((uint)Agent.Main.GetAgentFlags() & 0xFFFFDFFFu));
			if (IsGunnarActive())
			{
				_gunnarAgent.FadeOut(hideInstantly: true, hideMount: true);
			}
		}
	}

	private void TickHorse(Agent rider)
	{
		Vec2 currentVelocity = rider.GetCurrentVelocity();
		float num = ((TaleWorlds.Library.MathF.Abs(currentVelocity.x) <= 0.2f) ? 0f : currentVelocity.x);
		float num2 = ((TaleWorlds.Library.MathF.Abs(currentVelocity.y) <= 0.2f) ? 0f : currentVelocity.y);
		Vec2 movementInputVector = new Vec2(0f - num, 0f - num2);
		rider.MovementInputVector = movementInputVector;
		rider.EventControlFlags |= Agent.EventControlFlag.Dismount;
	}

	private void OnCheckPointReached()
	{
		if (!_startFromCheckPoint)
		{
			GetQuest().OnCheckPointReached();
			ClearEnemyGroups();
		}
		InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=BWSp3Uyj}Checkpoint reached.").ToString(), new Color(0f, 1f, 0f)));
		ShowNotification(new TextObject("{=McvglMqm}Time to get back aboard. Get on the ship."), IsGunnarActive());
		OnCheckPointReachedEvent?.Invoke();
		_checkPointReached = true;
		if (IsGunnarActive())
		{
			_gunnarAgent.SetTeam(base.Mission.PlayerTeam, sync: true);
			_gunnarAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>()?.RemoveBehavior<FollowAgentBehavior>();
		}
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_wind_checkpoint");
		if (gameEntity != null)
		{
			SetWindStrengthAndDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized(), gameEntity.GetGlobalScale().y);
		}
	}

	private void TickMissionPhase3(float dt)
	{
		if (_escapeZone.IsPointIn(_playerShip.GlobalFrame.origin) && !_playerShip.GetIsConnected())
		{
			if (!IsEnding)
			{
				OnPlayerShipReachedDestination();
			}
		}
		else if (GetTroopCountOfShip(_playerShip) == 0 && !IsEnding)
		{
			OnShipCaptured(_playerShip);
		}
		TickEnemyShips(dt);
	}

	private void OnShipCaptured(MissionShip ship)
	{
		ship.SetAnchor(isAnchored: true, anchorInPlace: true);
		ship.ShipOrder.SetShipStopOrder();
		ship.SetShipOrderActive(isOrderActive: false);
		OnFail(new TextObject("{=EydY9CXU}The enemy has captured your ship!"));
	}

	private int GetTroopCountOfShip(MissionShip ship)
	{
		return _navalAgentsLogic.GetActiveAgentCountOfShip(ship) - _navalAgentsLogic.GetActiveHeroCountOfShip(ship);
	}

	private void TickEnemyShips(float dt)
	{
		float num = float.MaxValue;
		foreach (EnemyShipTrigger trigger in _triggers)
		{
			trigger.Tick(_playerShip, dt);
			num = TaleWorlds.Library.MathF.Min(num, trigger.Ship.GameEntity.GlobalPosition.Distance(_playerShip.GameEntity.GlobalPosition));
			if (!_sightedEnemies && Agent.Main != null && CanSeeShip(Agent.Main, trigger.Ship))
			{
				_sightedEnemies = true;
				ShowNotification(new TextObject("{=XSobP84d}There they are! Get ready to evade them…"), isAnnouncedByGunnar: true);
			}
		}
		if (_sightedEnemies)
		{
			if (!_playerShipHasLowHealth && (_playerShip.HitPoints <= _playerShip.MaxHealth * 0.4f || _playerShip.FireHitPoints <= _playerShip.MaxFireHealth * 0.3f))
			{
				_playerShipHasLowHealth = true;
				_shipHitNotificationTimer -= 4f;
				ShowNotification(new TextObject("{=FsT98D3x}We can't take much more!"), isAnnouncedByGunnar: true, MBInformationManager.NotificationPriority.High);
			}
			else if (!_enemyGotClose && num < 40f)
			{
				_enemyGotClose = true;
				ShowNotification(new TextObject("{=SW0y8Rbp}Don't let them catch us! We need to get the silver out of here."), isAnnouncedByGunnar: true, MBInformationManager.NotificationPriority.High);
			}
			_incomingShotNotificationTimer += dt;
			_boardedNotificationTimer += dt;
			_shipHitNotificationTimer += dt;
		}
	}

	private void OnPlayerShipReachedDestination()
	{
		OnSuccess();
		MBMusicManager.Current.ForceStopThemeWithFadeOut();
		ShowNotification(new TextObject("{=7arwZMka}Success! You have run the Sea Hound blockade and reached the sea."), isAnnouncedByGunnar: false);
	}

	public override void OnBehaviorInitialize()
	{
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_shipAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultNavalMissionAgentSpawnLogic>();
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerTeam, NavalShipDeploymentLimit.Max());
		_navalShipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.PlayerAllyTeam, NavalShipDeploymentLimit.Max());
		_navalShipsLogic.SetTeamShipDeploymentLimit(TeamSideEnum.EnemyTeam, NavalShipDeploymentLimit.Max());
		_navalShipsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.ShipCollisionEvent += OnShipCollision;
		_navalShipsLogic.ShipSunkEvent += OnShipSunk;
		_navalShipsLogic.AddShipSiegeEngineMissileEvent += OnBallistaShot;
		_navalShipsLogic.ShipHitEvent += OnShipHit;
		_navalShipsLogic.BridgeConnectedEvent += OnBridgeConnected;
		_escapeZone = base.Mission.Scene.FindEntityWithTag("escape_zone")?.GetFirstScriptOfType<VolumeBox>();
		_jumpingZone = base.Mission.Scene.FindEntityWithTag("jumping_zone")?.GetFirstScriptOfType<VolumeBox>();
		_fire2Zone = base.Mission.Scene.FindEntityWithTag("fire_2_zone")?.GetFirstScriptOfType<VolumeBox>();
		_initialTriggerZone = base.Mission.Scene.FindEntityWithTag("burning_zone")?.GetFirstScriptOfType<VolumeBox>();
		_fire3Zone = base.Mission.Scene.FindEntityWithTag("fire_3_zone")?.GetFirstScriptOfType<VolumeBox>();
		_checkPointZone = base.Mission.Scene.FindEntityWithTag("dismount_zone")?.GetFirstScriptOfType<VolumeBox>();
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
	}

	private void OnShipSunk(MissionShip ship)
	{
		if (CurrentPhase == BattlePhase.Phase1)
		{
			if (ship == BurningShip)
			{
				OnFail(new TextObject("{=Ctrq2rg7}Your ship has sunk!"));
				MatrixFrame globalFrame = BurningShip.GlobalFrame;
				SoundManager.StartOneShotEvent("event:/mission/movement/vessel/ship_sink", in globalFrame.origin);
			}
		}
		else if (ship == _playerShip)
		{
			OnFail(new TextObject("{=Ctrq2rg7}Your ship has sunk!"));
			MatrixFrame globalFrame = _playerShip.GlobalFrame;
			SoundManager.StartOneShotEvent("event:/mission/movement/vessel/ship_sink", in globalFrame.origin);
		}
	}

	private void CacheParticleEntities()
	{
		_playerShipBurningSystems = CreateBurningSystemForPlayerShip(BurningShip);
		_enemyShipBurningSystem = CreateBurningSystem(TargetShip.GameEntity);
	}

	private void OnBridgeConnected(MissionShip source, MissionShip target)
	{
		if (CurrentPhase == BattlePhase.Phase3 && _sightedEnemies && target == _playerShip && !_playerShip.IsSinking && !_playerShip.IsDisabled && Agent.Main != null && Agent.Main.IsActive() && _boardedNotificationTimer >= 15f)
		{
			_boardedNotificationTimer = 0f;
			ShowNotification(new TextObject("{=s3PsXlsG}They've grappled us!"), isAnnouncedByGunnar: true);
		}
	}

	private void OnBallistaShot(Mission.Missile missile)
	{
		if (CurrentPhase == BattlePhase.Phase3 && _sightedEnemies && IsShipActive(_playerShip) && _incomingShotNotificationTimer >= 15f && MBRandom.RandomFloat < 0.2f)
		{
			_incomingShotNotificationTimer = 0f;
			ShowNotification(new TextObject("{=4qEPNXOn}Look out!"), isAnnouncedByGunnar: true, MBInformationManager.NotificationPriority.Low);
		}
	}

	private bool IsShipActive(MissionShip ship)
	{
		if (ship != null && !ship.IsSinking)
		{
			return !ship.IsDisabled;
		}
		return false;
	}

	private void OnShipHit(MissionShip ship, Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex)
	{
		if (ship == _playerShip && CurrentPhase == BattlePhase.Phase3 && _sightedEnemies && IsShipActive(_playerShip) && _navalShipsLogic.IsMissileFromShipSiegeEngine(affectorWeaponSlotOrMissileIndex) && !_playerShipHasLowHealth && _shipHitNotificationTimer >= 15f)
		{
			_shipHitNotificationTimer = 0f;
			ShowNotification(new TextObject("{=xnV0CSK4}Oi! That was a direct hit! Not the end of us yet but let's be careful!"), isAnnouncedByGunnar: true);
		}
	}

	private Dictionary<BurnShipObject, (BurningSystem, float)> CreateBurningSystemForPlayerShip(MissionShip burningShip)
	{
		Dictionary<BurnShipObject, (BurningSystem, float)> dictionary = new Dictionary<BurnShipObject, (BurningSystem, float)>();
		for (int i = 0; i < _burningMachines.Count; i++)
		{
			BurnShipObject burnShipObject = _burningMachines[i];
			WeakGameEntity gameEntity = burnShipObject.GameEntity;
			dictionary[burnShipObject] = (CreateBurningSystem(gameEntity), 0f);
		}
		return dictionary;
	}

	private BurningSystem CreateBurningSystem(WeakGameEntity parent)
	{
		GameEntity gameEntity = GameEntity.CreateFromWeakEntity(parent.GetFirstChildEntityWithTagRecursive("fire_particles"));
		if (gameEntity == null)
		{
			return null;
		}
		gameEntity.SetVisibilityExcludeParents(visible: true);
		List<GameEntity> list = gameEntity.GetChildren().ToList();
		BurningSystem burningSystem = new BurningSystem(gameEntity, 0.15f);
		foreach (GameEntity item in list)
		{
			CreateBurningNode(burningSystem, item);
		}
		burningSystem.SetExternalFlameMultiplier(2f);
		return burningSystem;
	}

	private void CreateBurningNode(BurningSystem system, GameEntity newNode)
	{
		BurningNode firstScriptOfType = newNode.GetFirstScriptOfType<BurningNode>();
		if (firstScriptOfType != null)
		{
			system.AddNewNode(firstScriptOfType);
			if (MBRandom.RandomFloat > 0.9f)
			{
				firstScriptOfType.EnableSparks();
			}
		}
	}

	private void OnShipCollision(MissionShip ship1, WeakGameEntity targetEntity, BodyFlags bodyFlags, Vec3 averageContactPoint, Vec3 totalImpulseOnShip, bool isFirstImpact)
	{
		if (CurrentPhase == BattlePhase.Phase1 && !_shipsCollided && !IsEnding && ((IsShipBurning && targetEntity == TargetShip.GameEntity && ship1 == BurningShip) || (ship1 == TargetShip && targetEntity == BurningShip.GameEntity)))
		{
			ShowNotification(new TextObject("{=LZwFmIOY}You did it! Look at that ship go up in flames! Their whole blockade will be in disarray!"), IsGunnarActive());
			_shipsCollided = true;
			_collisionTimer = null;
			SoundManager.StartOneShotEvent("event:/physics/vessel/ship_ramming", (ship1.GameEntity.GetBodyWorldTransform().origin + targetEntity.GetBodyWorldTransform().origin) * 0.5f, "Force", 1f);
		}
	}

	public override void OnMissionStateFinalized()
	{
		Clear();
		SailWindProfile.FinalizeProfile();
	}

	private void Clear()
	{
		OnCheckPointReachedEvent = null;
		OnLastExitZoneReachedEvent = null;
		OnPhaseEnd = null;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		base.Mission.Scene.SetWaterStrength(1f);
		_missionObjectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
		SpawnEnemyTargetShip();
		SpawnPlayerBurningShip();
		CacheParticleEntities();
		if (_startFromCheckPoint)
		{
			SpawnPlayerTradeShip();
			SpawnPlayerOnShip(_playerShip);
			SpawnGunnarOnShip(_playerShip);
			CurrentPhase = BattlePhase.Phase3;
			_playerShip.SetAnchor(isAnchored: false);
			_playerShip.Formation.SetControlledByAI(isControlledByAI: true);
			_playerShip.ShipOrder.FormationJoinShip(_playerShip.Formation);
			_playerShip.SetShipOrderActive(isOrderActive: true);
			DisableShip(BurningShip);
			DisableShip(TargetShip);
			_shipsCollided = true;
			IsShipBurning = true;
			_missionObjectiveLogic.StartObjective(new ReachEscapeZoneObjective(base.Mission, _playerShip, _escapeZone.GameEntity.GlobalPosition + new Vec3(0f, 0f, 5f)));
		}
		else
		{
			SpawnPlayerOnShip(BurningShip);
			SpawnGunnar("sp_gangradir_burning_ship", noHorses: true);
			_shipAgentSpawnLogic.AllocateAndDeployInitialTroops(BattleSideEnum.Attacker);
			_missionObjectiveLogic.StartObjective(new BurnShipObjective(base.Mission, TargetShip));
		}
		InitializeShipTriggers();
	}

	private void SpawnPlayerOnShip(MissionShip ship)
	{
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, CharacterObject.PlayerCharacter), ship);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		_mainAgentNavalComponent = Agent.Main.GetComponent<AgentNavalComponent>();
		_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(Agent.Main, ship);
		Mission.Current.PlayerTeam.PlayerOrderController.Owner = Agent.Main;
	}

	private bool IsGunnarActive()
	{
		if (_gunnarAgent != null)
		{
			return _gunnarAgent.IsActive();
		}
		return false;
	}

	private void Initialize()
	{
		_initialized = true;
		if (!_startFromCheckPoint)
		{
			InitializeEnemyShip(TargetShip);
		}
		Vec2 direction = base.Mission.Scene.FindEntityWithTag("sp_player_ship").GetGlobalFrame().rotation.f.AsVec2.Normalized();
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag(_startFromCheckPoint ? "sp_wind_checkpoint" : "sp_wind");
		if (gameEntity != null)
		{
			SetWindStrengthAndDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized(), gameEntity.GetGlobalScale().y);
		}
		else
		{
			SetWindStrengthAndDirection(direction, 4f);
		}
		base.Mission.OnInitialSpawnCompleted();
		MBMusicManager.Current.StartThemeWithConstantIntensity(MusicTheme.VikingSeaBattle1);
		MBMusicManager.Current.ChangeCurrentThemeIntensity(0.5f);
		if (!_startFromCheckPoint)
		{
			ShowNotification(new TextObject("{=6ZiKOdbI}Once we get within range, their ballista will pelt us with fiery missiles. Avoid them – even if they just hit the water, the flames will keep burning and can spread to our hull."), isAnnouncedByGunnar: true);
			ShowNotification(new TextObject("{=b1KaR0Hk}When we get close, I will set fire to our ship and then we swim to shore."), isAnnouncedByGunnar: true);
		}
	}

	private void OnFail(TextObject notification)
	{
		PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(BattleState.AttackerVictory);
		_missionEndTimer = new MissionTimer(2f);
		ShowNotification(notification, isAnnouncedByGunnar: false);
	}

	private void OnSuccess(TextObject notification = null)
	{
		PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(BattleState.DefenderVictory);
		_missionEndTimer = new MissionTimer(2f);
		if (!TextObject.IsNullOrEmpty(notification))
		{
			ShowNotification(notification, isAnnouncedByGunnar: false);
		}
	}

	private void OnFinalize()
	{
		_navalShipsLogic.ShipCollisionEvent -= OnShipCollision;
		_navalShipsLogic.ShipSunkEvent -= OnShipSunk;
		_navalShipsLogic.AddShipSiegeEngineMissileEvent -= OnBallistaShot;
		_navalShipsLogic.ShipHitEvent -= OnShipHit;
		_navalShipsLogic.BridgeConnectedEvent -= OnBridgeConnected;
		base.Mission.EndMission();
	}

	private void SpawnPlayerBurningShip()
	{
		Formation formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Ranged);
		string tag = (_startFromCheckPoint ? "sp_player_burning_ship_checkpoint" : "sp_player_burning_ship");
		GameEntity spawnEntity = base.Mission.Scene.FindEntityWithTag(tag);
		BurningShip = CreateShip(_playerBurningShipOrigin, base.Mission.PlayerTeam, formation, spawnEntity);
		formation.SetControlledByAI(isControlledByAI: false);
		BurningShip.SetShipOrderActive(isOrderActive: false);
		InitializeBurningMachines();
	}

	private void InitializeBurningMachines()
	{
		_burningMachines = BurningShip.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<BurnShipObject>();
	}

	private void SpawnPlayerTradeShip()
	{
		Formation formation = base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry);
		GameEntity spawnEntity = base.Mission.Scene.FindEntityWithTag("sp_player_ship");
		_playerShip = CreateShip(_playerShipOrigin, base.Mission.PlayerTeam, formation, spawnEntity);
		if (!_startFromCheckPoint)
		{
			_playerShip.FinalizeDeployment(initializeMachines: true);
		}
		_playerShip.SetAnchor(isAnchored: true, anchorInPlace: true);
		SpawnPlayerTeamAgents();
		_playerShip.ShipOrder.SetShipStopOrder();
		SetTargetPoint(_playerShip, new Vec3(0f, -20f));
	}

	private void SetTargetPoint(MissionShip playerShip, Vec3 localOffset)
	{
		ShipTargetMissionObject firstScriptInFamilyDescending = playerShip.GameEntity.GetFirstScriptInFamilyDescending<ShipTargetMissionObject>();
		firstScriptInFamilyDescending?.GameEntity.SetLocalPosition(localOffset + firstScriptInFamilyDescending.GameEntity.GetLocalFrame().origin);
	}

	private void DisableTargetShipObject(MissionShip ship)
	{
		ship.GameEntity.GetFirstScriptInFamilyDescending<ShipTargetMissionObject>()?.SetDisabled();
	}

	private void SpawnPlayerTeamAgents()
	{
		int num = _playerShip.ShipOrigin.MainDeckCrewCapacity - 2;
		_navalAgentsLogic.SetDesiredTroopCountOfShip(_playerShip, _playerShip.ShipOrigin.MainDeckCrewCapacity);
		int num2 = 0;
		foreach (FlattenedTroopRosterElement item in PartyBase.MainParty.MemberRoster.ToFlattenedRoster())
		{
			if (!item.Troop.IsHero)
			{
				_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, item.Troop), _playerShip);
				num2++;
			}
			if (num2 >= num)
			{
				break;
			}
		}
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
	}

	private void SpawnGunnar(string spawnId, bool noHorses)
	{
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag(spawnId);
		if (gameEntity != null)
		{
			SpawnGunnar(gameEntity.GlobalPosition, noHorses);
		}
		else
		{
			Debug.FailedAssert("Cant find entity.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\MissionControllers\\BlockedEstuaryMissionController.cs", "SpawnGunnar", 2106);
		}
	}

	private void SpawnGunnar(Vec3 position, bool noHorses)
	{
		Vec3 position2 = position;
		Vec2 direction = (Agent.Main.Position - position2).AsVec2.Normalized();
		Equipment equipment = NavalStorylineData.Gunnar.BattleEquipment.Clone();
		if (!noHorses)
		{
			ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("sturgia_horse_tournament");
			equipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(item);
		}
		MissionEquipment missionEquipment = new MissionEquipment(equipment, null);
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Gunnar.CharacterObject).TroopOrigin(new SimpleAgentOrigin(NavalStorylineData.Gunnar.CharacterObject)).Team(base.Mission.PlayerTeam).InitialPosition(in position2)
			.InitialDirection(in direction)
			.NoHorses(noHorses)
			.NoWeapons(noWeapons: true)
			.Equipment(equipment)
			.MissionEquipment(missionEquipment);
		_gunnarAgent = Mission.Current.SpawnAgent(agentBuildData);
		_gunnarAgent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
		_gunnarAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.AddBehaviorGroup<DailyBehaviorGroup>();
		_gunnarAgent.GetComponent<AgentNavalComponent>()?.SetCanDrown(canDrown: false);
	}

	private void TriggerEnemyShip(MissionShip ship, MissionShip target = null)
	{
		ship.SetAnchor(isAnchored: false);
		ship.SetShipOrderActive(isOrderActive: true);
		ship.ShipOrder.SetShipEngageOrder(target);
		ship.ShipOrder.SetBoardingTargetShip(target);
		ToggleShipBallistas(ship, enabled: true);
		ship.ShipOrder.FormationJoinShip(ship.Formation);
	}

	private void InitializeEnemyShip(MissionShip ship)
	{
		ship.ShipOrder.FormationJoinShip(ship.Formation);
		ship.ShipOrder.SetShipStopOrder();
		ship.SetAnchor(isAnchored: true, anchorInPlace: true);
		ship.Formation.SetControlledByAI(isControlledByAI: false);
		ship.SetShipOrderActive(isOrderActive: true);
	}

	private void SpawnEnemyTargetShip()
	{
		Formation formation = base.Mission.PlayerEnemyTeam.GetFormation(FormationClass.Infantry);
		GameEntity spawnEntity = base.Mission.Scene.FindEntityWithTag("sp_enemy_ship_1");
		TargetShip = CreateShip(_enemyBurningShipOrigin, base.Mission.PlayerEnemyTeam, formation, spawnEntity);
		TargetShip.SetCanBeTakenOver(value: false);
	}

	private MissionShip SpawnEnemyChaserShip(GameEntity spawnPoint, IShipOrigin shipOrigin)
	{
		Formation formation = base.Mission.PlayerEnemyTeam.FormationsIncludingEmpty.First((Formation x) => x.CountOfUnits == 0 && x != TargetShip.Formation);
		MissionShip missionShip = CreateShip(shipOrigin, base.Mission.PlayerEnemyTeam, formation, spawnPoint);
		missionShip.SetCanBeTakenOver(value: false);
		int num = MBRandom.RandomInt(12, 14);
		int num2 = MBRandom.RandomInt(8, 10);
		CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>("vlandian_swordsman");
		CharacterObject troop2 = MBObjectManager.Instance.GetObject<CharacterObject>("vlandian_marine_t5");
		_navalAgentsLogic.SetDesiredTroopCountOfShip(missionShip, missionShip.ShipOrigin.MainDeckCrewCapacity);
		for (int num3 = 0; num3 < num; num3++)
		{
			_navalAgentsLogic.AddReservedTroopToShip(new SimpleAgentOrigin(troop), missionShip);
		}
		for (int num4 = 0; num4 < num2; num4++)
		{
			_navalAgentsLogic.AddReservedTroopToShip(new SimpleAgentOrigin(troop2), missionShip);
		}
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.EnemyTeam);
		return missionShip;
	}

	private SpeakToTheSailorsQuest GetQuest()
	{
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest is SpeakToTheSailorsQuest result)
			{
				return result;
			}
		}
		return null;
	}

	private bool CanSeeShip(Agent agent, MissionShip ship)
	{
		if (agent.Position.Distance(ship.GameEntity.GlobalPosition) < 200f || _triggers.Any((EnemyShipTrigger x) => x.Ship.ShipOrder.TargetShip != null && x.Ship.ShipSiegeWeapon != null && x.Ship.ShipSiegeWeapon.State == RangedSiegeWeapon.WeaponState.Shooting))
		{
			return true;
		}
		return false;
	}

	private MissionShip CreateShip(IShipOrigin ship, Team team, Formation formation, GameEntity spawnEntity)
	{
		MatrixFrame shipFrame = spawnEntity.GetGlobalFrame();
		float waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(spawnEntity.GlobalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
		shipFrame.origin = new Vec3(spawnEntity.GlobalPosition.x, spawnEntity.GlobalPosition.y, waterLevelAtPosition);
		return _navalShipsLogic.SpawnShip(ship, in shipFrame, team, formation);
	}

	private Agent SpawnPlayerHorse()
	{
		MatrixFrame globalFrame = base.Mission.Scene.FindEntityWithTag("sp_horse").GetGlobalFrame();
		return SpawnHorse(globalFrame.origin, globalFrame.rotation.f.AsVec2);
	}

	private Agent SpawnHorse(Vec3 position, Vec2 direction)
	{
		ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("sturgia_horse_tournament");
		ItemRosterElement rosterElement = new ItemRosterElement(item, 1);
		ItemObject item2 = MBObjectManager.Instance.GetObject<ItemObject>("light_harness");
		ItemRosterElement harnessRosterElement = new ItemRosterElement(item2);
		Agent agent = Mission.Current.SpawnMonster(rosterElement, harnessRosterElement, in position, direction.Normalized());
		agent.SetTargetPosition(position.AsVec2);
		agent.SetMortalityState(Agent.MortalityState.Invulnerable);
		return agent;
	}

	public static bool WillHitBoundingBox(Vec3 origin, Vec2 velocity2D, Vec3 boxMin, Vec3 boxMax)
	{
		if (velocity2D == Vec2.Zero)
		{
			return false;
		}
		Vec3 vec = velocity2D.ToVec3();
		Vec3 vec2 = new Vec3((vec.X == 0f) ? float.PositiveInfinity : (1f / vec.X), (vec.Y == 0f) ? float.PositiveInfinity : (1f / vec.Y), (vec.Z == 0f) ? float.PositiveInfinity : (1f / vec.Z));
		float val = (boxMin.X - origin.X) * vec2.X;
		float val2 = (boxMax.X - origin.X) * vec2.X;
		float val3 = (boxMin.Y - origin.Y) * vec2.Y;
		float val4 = (boxMax.Y - origin.Y) * vec2.Y;
		float val5 = (boxMin.Z - origin.Z) * vec2.Z;
		float val6 = (boxMax.Z - origin.Z) * vec2.Z;
		float num = Math.Max(Math.Max(Math.Min(val, val2), Math.Min(val3, val4)), Math.Min(val5, val6));
		float num2 = Math.Min(Math.Min(Math.Max(val, val2), Math.Max(val3, val4)), Math.Max(val5, val6));
		if (num2 < 0f)
		{
			return false;
		}
		if (num > num2)
		{
			return false;
		}
		return Math.Max(0f, num) <= Math.Min(1f, num2);
	}

	private Vec2[] GetShipPhysicsBox(MissionShip ship)
	{
		float num = (ship.Physics.PhysicsBoundingBoxWithChildren.max.x - ship.Physics.PhysicsBoundingBoxWithChildren.min.x) / 2f - 6f;
		float num2 = (ship.Physics.PhysicsBoundingBoxWithChildren.max.y - ship.Physics.PhysicsBoundingBoxWithChildren.min.y) / 2f - 2f;
		Vec2 asVec = ship.GameEntity.GetGlobalFrame().rotation.f.AsVec2;
		Vec2 asVec2 = ship.GameEntity.GetGlobalFrame().rotation.s.AsVec2;
		Vec2 asVec3 = ship.GameEntity.GlobalPosition.AsVec2;
		Vec2 vec = asVec2 * num;
		Vec2 vec2 = asVec * num2;
		Vec2 vec3 = asVec3 - vec - vec2;
		Vec2 vec4 = asVec3 + vec - vec2;
		Vec2 vec5 = asVec3 + vec + vec2;
		Vec2 vec6 = asVec3 - vec + vec2;
		return new Vec2[4] { vec3, vec4, vec5, vec6 };
	}

	private bool DoesShipCollideWithProjectile(MissionShip ship, BurningProjectile projectile)
	{
		if (projectile.Initialized)
		{
			return DoesShipCollideWithSphere(ship, projectile.GameEntity.GlobalPosition.AsVec2, 1f);
		}
		return false;
	}

	private bool DoesShipCollideWithSphere(MissionShip ship, Vec2 origin, float radius)
	{
		return PlaneIntersectsCircle(GetShipPhysicsBox(ship), origin, radius);
	}

	private bool PlaneIntersectsCircle(Vec2[] corners, Vec2 circleOrigin, float radius)
	{
		if (IsPointInPolygon(circleOrigin, corners))
		{
			return true;
		}
		float num = radius * radius;
		for (int i = 0; i < corners.Length; i++)
		{
			Vec2 vec = corners[i];
			Vec2 vec2 = corners[(i + 1) % corners.Length];
			float num2 = (vec2.X - vec.X) * (vec2.X - vec.X) + (vec2.Y - vec.Y) * (vec2.Y - vec.Y);
			float num3 = Math.Max(0f, Math.Min(1f, ((circleOrigin.X - vec.X) * (vec2.X - vec.X) + (circleOrigin.Y - vec.Y) * (vec2.Y - vec.Y)) / num2));
			float num4 = vec.X + num3 * (vec2.X - vec.X);
			float num5 = vec.Y + num3 * (vec2.Y - vec.Y);
			if ((circleOrigin.X - num4) * (circleOrigin.X - num4) + (circleOrigin.Y - num5) * (circleOrigin.Y - num5) <= num)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsPointInPolygon(Vec2 point, Vec2[] polygonCorners)
	{
		bool flag = false;
		int num = polygonCorners.Length;
		int num2 = 0;
		int num3 = num - 1;
		while (num2 < num)
		{
			if (polygonCorners[num2].Y > point.Y != polygonCorners[num3].Y > point.Y && point.X < (polygonCorners[num3].X - polygonCorners[num2].X) * (point.Y - polygonCorners[num2].Y) / (polygonCorners[num3].Y - polygonCorners[num2].Y) + polygonCorners[num2].X)
			{
				flag = !flag;
			}
			num3 = num2++;
		}
		return flag;
	}
}
