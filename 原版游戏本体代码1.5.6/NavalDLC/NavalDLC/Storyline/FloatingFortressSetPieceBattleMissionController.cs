using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.AI.UsableMachineAIs;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using NavalDLC.Missions.ShipInput;
using NavalDLC.Storyline.Objectives.Quest4;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline;

public class FloatingFortressSetPieceBattleMissionController : MissionLogic
{
	private abstract class ConversationLine
	{
		public void TryPlayLine()
		{
			if (CanBePlayed())
			{
				Play();
			}
		}

		protected abstract void Play();

		public abstract void Stop();

		public abstract bool IsPlaying();

		protected abstract bool CanBePlayed();
	}

	private class SimpleConversationLine : ConversationLine
	{
		private readonly TextObject _line;

		private readonly CharacterObject _speaker;

		private readonly float _cooldown;

		private readonly MBInformationManager.NotificationPriority _priority;

		private MBInformationManager.DialogNotificationHandle _handle;

		private float _blockedTime;

		public SimpleConversationLine(CharacterObject speaker, string line, float cooldown, MBInformationManager.NotificationPriority priority)
		{
			_speaker = speaker;
			_cooldown = cooldown;
			_priority = priority;
			_line = new TextObject(line);
			_blockedTime = 0f;
		}

		protected override void Play()
		{
			_handle = CampaignInformationManager.AddDialogLine(_line, _speaker, null, 0, _priority);
			_blockedTime = Mission.Current.CurrentTime + _cooldown;
		}

		public override void Stop()
		{
			CampaignInformationManager.ClearDialogNotification(_handle, fadeOut: false);
		}

		public override bool IsPlaying()
		{
			if (_handle != null)
			{
				return CampaignInformationManager.GetStatusOfDialogNotification(_handle) == MBInformationManager.NotificationStatus.CurrentlyActive;
			}
			return false;
		}

		protected override bool CanBePlayed()
		{
			return _blockedTime <= Mission.Current.CurrentTime;
		}
	}

	private class VariantConversationLine : ConversationLine
	{
		public enum VariationType
		{
			Ordered,
			Random
		}

		private int _current;

		private ConversationLine _active;

		private float _blockedTime;

		private readonly List<ConversationLine> _lines;

		private readonly float _cooldown;

		private readonly VariationType _variationType;

		private readonly bool _canShowEachLineOnce;

		public VariantConversationLine(ConversationLine[] lines, VariationType variationType, float cooldown, bool canShowEachLineOnce = false)
		{
			_lines = lines.ToList();
			_variationType = variationType;
			_cooldown = cooldown;
			_canShowEachLineOnce = canShowEachLineOnce;
			_current = -1;
			_active = null;
			_blockedTime = 0f;
		}

		protected override void Play()
		{
			switch (_variationType)
			{
			case VariationType.Ordered:
				_current = (_current + 1) % _lines.Count;
				break;
			case VariationType.Random:
				_current = MBRandom.RandomInt(0, _lines.Count);
				break;
			default:
				Debug.FailedAssert("Unknown variation type!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\MissionControllers\\FloatingFortressSetPieceBattleMissionController.cs", "Play", 138);
				throw new ArgumentOutOfRangeException();
			}
			_active = _lines[_current];
			_active.TryPlayLine();
			if (_canShowEachLineOnce)
			{
				_lines.RemoveAt(_current);
			}
			_blockedTime = Mission.Current.CurrentTime + _cooldown;
		}

		public override void Stop()
		{
			_active.Stop();
			_active = null;
		}

		public override bool IsPlaying()
		{
			if (_active != null)
			{
				return _active.IsPlaying();
			}
			return false;
		}

		protected override bool CanBePlayed()
		{
			if (_lines.Count > 0)
			{
				return _blockedTime <= Mission.Current.CurrentTime;
			}
			return false;
		}
	}

	private class SequencedConversationLine : ConversationLine
	{
		private float _blockedTime;

		private readonly float _cooldown;

		private readonly ConversationLine[] _lines;

		public SequencedConversationLine(ConversationLine[] lines, float cooldown)
		{
			_lines = lines;
			_cooldown = cooldown;
			_blockedTime = 0f;
		}

		protected override void Play()
		{
			ConversationLine[] lines = _lines;
			for (int i = 0; i < lines.Length; i++)
			{
				lines[i].TryPlayLine();
			}
			_blockedTime = Mission.Current.CurrentTime + _cooldown;
		}

		public override void Stop()
		{
			ConversationLine[] lines = _lines;
			for (int i = 0; i < lines.Length; i++)
			{
				lines[i].Stop();
			}
		}

		public override bool IsPlaying()
		{
			return _lines.Any((ConversationLine x) => x.IsPlaying());
		}

		protected override bool CanBePlayed()
		{
			return _blockedTime <= Mission.Current.CurrentTime;
		}
	}

	private class CircularBuffer<T>
	{
		private readonly T[] _buffer;

		private int _head;

		private int _tail;

		private readonly int _capacity;

		public int Count { get; private set; }

		public T this[int index]
		{
			get
			{
				int num = (_head + index) % _capacity;
				return _buffer[num];
			}
			set
			{
				int num = (_head + index) % _capacity;
				_buffer[num] = value;
			}
		}

		public CircularBuffer(int capacity)
		{
			_capacity = capacity;
			_buffer = new T[capacity];
			_head = 0;
			_tail = 0;
			Count = 0;
		}

		public void Add(T item)
		{
			_buffer[_tail] = item;
			_tail = (_tail + 1) % _capacity;
			if (Count < _capacity)
			{
				Count++;
			}
			else
			{
				_head = (_head + 1) % _capacity;
			}
		}
	}

	private class TrailController
	{
		private readonly CircularBuffer<Vec3> _positions;

		private readonly CircularBuffer<float> _timestamps;

		private readonly float _trailDelay;

		private readonly float _recordInterval;

		private float _lastRecordTime;

		public TrailController(float trailDelay, float recordInterval)
		{
			_trailDelay = trailDelay;
			_recordInterval = recordInterval;
			_lastRecordTime = 0f;
			int val = (int)Math.Ceiling(trailDelay / recordInterval);
			val = Math.Max(val, 10) + 1;
			_positions = new CircularBuffer<Vec3>(val);
			_timestamps = new CircularBuffer<float>(val);
		}

		public void RecordPosition(Vec3 position, float currentTime)
		{
			if (currentTime - _lastRecordTime >= _recordInterval)
			{
				_positions.Add(position);
				_timestamps.Add(currentTime);
				_lastRecordTime = currentTime;
			}
		}

		public Vec3 GetTrailEndPosition(float currentTime)
		{
			if (_positions.Count == 0)
			{
				return default(Vec3);
			}
			float num = currentTime - _trailDelay;
			for (int num2 = _timestamps.Count - 1; num2 >= 1; num2--)
			{
				float num3 = _timestamps[num2 - 1];
				float num4 = _timestamps[num2];
				if (num >= num3 && num <= num4)
				{
					float alpha = (num - num3) / (num4 - num3);
					Vec3 v = _positions[num2 - 1];
					Vec3 v2 = _positions[num2];
					return Vec3.Lerp(v, v2, alpha);
				}
			}
			if (!(num <= _timestamps[0]))
			{
				return _positions[_positions.Count - 1];
			}
			return _positions[0];
		}
	}

	private const float PlayerShipTargetingWarningDistance = 15f;

	private const float TimeToLoseAfterLastBallistaShot = 5f;

	private const float BallistaRandomAttackRadius = 15f;

	private const float BallistaRandomAttackPointSelectionTime = 1f;

	private const string PlayerPhaseOneSpawnPointTag = "sp_player_ship";

	private const string PlayerPhaseTwoSpawnPointTag = "sp_player_phase_two_start";

	private const float PlayerShipTooCloseThresholdDistanceSquared = 10000f;

	private const float PlayerShipLowHpThresholdRatio = 0.65f;

	private const float PlayerRemainingAmmoThresholdRatio = 3f;

	private const float AllyShipAnchorFrameConnectionDistanceSquared = 1600f;

	private const string PlayerStartingShipHull = "naval_storyline_quest_4_player_medit_ship";

	private const float AllyShipDistanceToSelfAnchor = 200f;

	private const int PlayerBallistaStartingAmmo = 30;

	private const float AllyShipForceConnectDelay = 5f;

	private const float AllyShipForceConnectMaxDistanceSquared = 2500f;

	private static readonly List<(string, string)[]> AllyShipUpgrades = new List<(string, string)[]>
	{
		new(string, string)[2]
		{
			("sail", "sails_lvl2"),
			("side", "side_northern_shields_lvl1")
		},
		new(string, string)[2]
		{
			("sail", "sails_lvl3"),
			("side", "side_northern_shields_lvl2")
		},
		new(string, string)[2]
		{
			("sail", "sails_lvl2"),
			("side", "side_northern_shields_lvl2")
		},
		new(string, string)[2]
		{
			("sail", "sails_lvl3"),
			("side", "side_northern_shields_lvl3")
		}
	};

	private const int BridgesBetweenEnemyShips = 1;

	private readonly List<Figurehead> _allyShipFigureheads = new List<Figurehead>
	{
		DefaultFigureheads.Raven,
		DefaultFigureheads.Turtle,
		DefaultFigureheads.Boar,
		DefaultFigureheads.Dragon
	};

	private readonly Dictionary<string, string> _playerShipUpgradePieces = new Dictionary<string, string>
	{
		{ "fore", "fore_heavy_ballista_stone" },
		{ "aft", "" },
		{ "hull", "" },
		{ "deck", "" },
		{ "oars", "" },
		{ "sail", "sails_lvl3" },
		{ "roof", "roof_8" }
	};

	private readonly List<string> _allyShipHulls = new List<string> { "northern_medium_ship", "northern_medium_ship", "northern_light_ship", "northern_medium_ship" };

	private readonly List<(string, int)> _playerShipTroops = new List<(string, int)>
	{
		("skolderbrotva_tier_2", 2),
		("skolderbrotva_tier_3", 28)
	};

	private readonly List<(string, int)[]> _allyShipAgents = new List<(string, int)[]>
	{
		new(string, int)[1] { ("skolderbrotva_tier_3", 40) },
		new(string, int)[1] { ("skolderbrotva_tier_3", 39) },
		new(string, int)[2]
		{
			("skolderbrotva_tier_3", 16),
			("skolderbrotva_tier_2", 3)
		},
		new(string, int)[2]
		{
			("gangradirs_kin_melee", 20),
			("gangradirs_kin_ranged", 20)
		}
	};

	private readonly (string, string)[] _enemyShipHulls = new(string, string)[8]
	{
		("naval_storyline_quest_4_boss_light_ship", "sp_enemy_ship_1"),
		("ship_storyline_quest_4_boss_cog_ship", "sp_enemy_ship_2"),
		("naval_storyline_quest_4_boss_light_ship", "sp_enemy_ship_3"),
		("naval_storyline_quest_4_boss_round_ship", "sp_enemy_ship_4"),
		("naval_storyline_quest_4_boss_light_ship", "sp_enemy_ship_5"),
		("ship_storyline_quest_4_boss_cog_ship", "sp_enemy_ship_7"),
		("naval_storyline_quest_4_boss_light_ship", "sp_enemy_ship_6"),
		("ship_storyline_quest_4_boss_cog_ship", "sp_enemy_ship_8")
	};

	private readonly List<(string, int)[]> _initialEnemyShipAgents = new List<(string, int)[]>
	{
		new(string, int)[2]
		{
			("sea_hounds_marksman", 5),
			("sea_hounds", 10)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 2),
			("sea_hounds_pups", 9)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 6),
			("sea_hounds_pups", 9)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 9),
			("sea_hounds_pups", 14)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 4),
			("sea_hounds", 11)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 2),
			("sea_hounds_pups", 4)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 3),
			("sea_hounds", 9)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 3),
			("sea_hounds_pups", 6)
		}
	};

	private readonly List<(string, int)[]> _reinforcementEnemyShipAgents = new List<(string, int)[]>
	{
		new(string, int)[2]
		{
			("sea_hounds_marksman", 2),
			("sea_hounds", 2)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 5),
			("sea_hounds_pups", 2)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 1),
			("sea_hounds_pups", 3)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 2),
			("sea_hounds_pups", 4)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 6),
			("sea_hounds", 2)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 1),
			("sea_hounds_pups", 2)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 2),
			("sea_hounds", 4)
		},
		new(string, int)[2]
		{
			("sea_hounds_marksman", 2),
			("sea_hounds_pups", 4)
		}
	};

	private readonly Dictionary<int, string> _enemyShipsToAddBallista = new Dictionary<int, string>
	{
		{ 2, "fore_mangonel" },
		{ 4, "fore_mangonel" },
		{ 6, "fore_mangonel" },
		{ 8, "fore_mangonel" }
	};

	private MissionShip _playerShip;

	private GameEntity _trailingTargetObject;

	private ShipTargetMissionObject _playerShipTargetObject;

	private readonly TrailController _playerShipTargetObjectTrailController = new TrailController(6f, 0.25f);

	private MBList<MissionShip> _enemyMissionShipsOrdered;

	private bool _isPhaseOneInitialized;

	private int _currentPhaseOneInitializationTick;

	private float _playerLoseRemainingTime = 5f;

	private float _lastRandomAttackPointPickTime;

	private Vec3 _randomAttackPoint;

	private bool _shouldStartPhaseTwo;

	private bool _isPhaseTwoInitialized;

	private int _currentPhaseTwoInitializationTick;

	private bool _isMissionSuccessful;

	private bool _isMissionFailed;

	private List<GameEntity> _entities = new List<GameEntity>();

	private readonly MBList<MissionShip> _playerAllyMissionShips = new MBList<MissionShip>();

	private readonly MBList<(MissionShip, bool)> _playerAllyShipAnchorState = new MBList<(MissionShip, bool)>();

	private readonly Dictionary<MissionShip, float> _allyShipStuckDurations = new Dictionary<MissionShip, float>();

	private readonly MBList<DestructableComponent> _enemySiegeWeaponDestructables = new MBList<DestructableComponent>();

	private readonly Dictionary<DestructableComponent, DestructableComponent> _enemySiegeWeaponByCover = new Dictionary<DestructableComponent, DestructableComponent>();

	private readonly Dictionary<RangedSiegeWeapon, Agent> _cachedMangonelAgents = new Dictionary<RangedSiegeWeapon, Agent>();

	private NavalAgentsLogic _navalAgentsLogic;

	private NavalShipsLogic _navalShipsLogic;

	private readonly ConversationLine _playerShipTooCloseLine;

	private readonly ConversationLine _playerShipLowHpLine;

	private readonly ConversationLine _playerShipRemainingAmmoLine;

	private readonly ConversationLine _playerShipStandingStillLine;

	private readonly ConversationLine _playerShipHitLine;

	private readonly ConversationLine _playerShipSailDestroyedLine;

	private readonly ConversationLine _playerTookMangonelDownLine;

	private readonly ConversationLine _playerTookAllMangonelsDownLine;

	private MissionObjectiveLogic _missionObjectiveLogic;

	private BoardFloatingFortressObjective _boardFloatingFortressObjective;

	private DefeatTheEnemyCrewObjective _defeatTheEnemyCrewObjective;

	public bool IsPhaseOneCompleted { get; private set; }

	public bool IsStartedFromCheckpoint { get; }

	public MBReadOnlyList<MissionShip> EnemyShipsOrdered => _enemyMissionShipsOrdered;

	public FloatingFortressSetPieceBattleMissionController(bool startFromCheckpoint)
	{
		IsStartedFromCheckpoint = startFromCheckpoint;
		_playerShipStandingStillLine = new VariantConversationLine(new ConversationLine[4]
		{
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=PRzT0o1t}Keep rowing! The next hit might punch right through our deck!", 0f, MBInformationManager.NotificationPriority.Medium),
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=3067dlpE}Keep moving! That last hit made our timbers groan!", 0f, MBInformationManager.NotificationPriority.Medium),
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=jaKW2HIJ}Unless you want to swim, I suggest you keep moving!", 0f, MBInformationManager.NotificationPriority.Medium),
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=BV06pwuU}Standing still? You planning to go down with the mast?", 0f, MBInformationManager.NotificationPriority.Medium)
		}, VariantConversationLine.VariationType.Ordered, 10f);
		_playerShipHitLine = new VariantConversationLine(new ConversationLine[2]
		{
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=qA4pYH6z}That hit us! We're still afloat, but the next time we might not be so lucky", 0f, MBInformationManager.NotificationPriority.High),
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=Yv3BQ7cT}Stamp out those sparks, lads! Let's not get hit again.", 0f, MBInformationManager.NotificationPriority.High)
		}, VariantConversationLine.VariationType.Ordered, 15f);
		_playerTookMangonelDownLine = new VariantConversationLine(new ConversationLine[2]
		{
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=bdpsa5CC}One mangonel down!", 0f, MBInformationManager.NotificationPriority.Highest),
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=k5NjdC48}You smashed that mangonel! Look at it, like a broken toy!", 0f, MBInformationManager.NotificationPriority.Highest)
		}, VariantConversationLine.VariationType.Ordered, 0f, canShowEachLineOnce: true);
		_playerTookAllMangonelsDownLine = new SequencedConversationLine(new ConversationLine[2]
		{
			new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=75khXDaR}You silenced those mangonels! Now let's all move in and board them!", 0f, MBInformationManager.NotificationPriority.Medium),
			new SimpleConversationLine(NavalStorylineData.Gunnar.CharacterObject, "{=4r2IhSCi}We're right behind you! Row, lads, row!", 0f, MBInformationManager.NotificationPriority.Medium)
		}, 10000f);
		_playerShipTooCloseLine = new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=tl473Yje}Let's keep our distance! Their decks are packed with bowmen!", 15f, MBInformationManager.NotificationPriority.Medium);
		_playerShipLowHpLine = new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=eAabzGkE}Our timbers are groaning like a sick man.", 10000f, MBInformationManager.NotificationPriority.Medium);
		_playerShipSailDestroyedLine = new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=gzvtND1s}Our sail is down!", 10000f, MBInformationManager.NotificationPriority.High);
		_playerShipRemainingAmmoLine = new SimpleConversationLine(NavalStorylineData.Bjolgur.CharacterObject, "{=O4oqNTAl}Choose your targets! Take out the mangonels before we run out of bolts!", 10000f, MBInformationManager.NotificationPriority.High);
	}

	public override void AfterStart()
	{
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_missionObjectiveLogic = base.Mission.GetMissionBehavior<MissionObjectiveLogic>();
		base.Mission.Scene.SetAtmosphereWithName("TOD_naval_09_00_Overcast");
		_navalShipsLogic.ShipHitEvent += OnShipHit;
		base.Mission.Teams.Add(BattleSideEnum.Attacker, base.Mission.PlayerTeam.Color, base.Mission.PlayerTeam.Color2, base.Mission.PlayerTeam.Banner);
		_navalAgentsLogic.UpdateTeamAgentsData();
		MBMusicManager.Current.StartTheme(MusicTheme.MediterraneanSeaBattle1, 0.5f);
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isPhaseOneInitialized)
		{
			TickPhaseOneInitialization();
		}
		if (_shouldStartPhaseTwo && !_isPhaseTwoInitialized)
		{
			TickPhaseTwoInitialization();
		}
		if (_isPhaseOneInitialized && !_isPhaseTwoInitialized)
		{
			TickPhaseOneLogic(dt);
		}
		if (_isPhaseTwoInitialized)
		{
			TickPhaseTwoLogic(dt);
		}
		if (_isPhaseOneInitialized && IsStartedFromCheckpoint && !_isPhaseTwoInitialized)
		{
			Agent.Main.Controller = AgentControllerType.Player;
			_shouldStartPhaseTwo = true;
		}
	}

	private void TickPhaseOneInitialization()
	{
		_currentPhaseOneInitializationTick++;
		if (_currentPhaseOneInitializationTick == 1)
		{
			UpdateEntityReferences();
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_wind");
			if (gameEntity != null)
			{
				SetWindStrengthAndDirection(gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized(), gameEntity.GetGlobalScale().z);
			}
			base.Mission.Scene.SetWaterStrength(2f);
			SpawnPlayerShip();
			SpawnEnemyShips();
			ConnectEnemyShips();
			foreach (MissionShip item in _enemyMissionShipsOrdered)
			{
				if (item.ShipOrigin is Ship ship)
				{
					ship.IsInvulnerable = true;
				}
			}
			base.Mission.PlayerTeam.SetPlayerRole(isPlayerGeneral: true, isPlayerSergeant: true);
			UpdateEntityReferences();
		}
		if (_currentPhaseOneInitializationTick != 2)
		{
			return;
		}
		SpawnPlayerShipAgents();
		SpawnPlayer();
		for (int i = 0; i < _enemyMissionShipsOrdered.Count; i++)
		{
			(string, int)[] source = _initialEnemyShipAgents[i];
			SpawnEnemyShipAgents(_enemyMissionShipsOrdered[i], source);
		}
		_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(Agent.Main, _playerShip, _playerShip);
		_navalShipsLogic.SetDeploymentMode(value: true);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.PlayerTeam);
		_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.EnemyTeam);
		if (Agent.Main != null && Agent.Main.IsUsingGameObject)
		{
			Agent.Main.StopUsingGameObject();
		}
		_navalShipsLogic.SetDeploymentMode(value: false);
		base.Mission.OnInitialSpawnCompleted();
		foreach (MissionShip item2 in _enemyMissionShipsOrdered)
		{
			item2.SetAnchor(isAnchored: true, anchorInPlace: true);
			item2.BlockConnection();
			if (item2.ShipSiegeWeapon != null)
			{
				_cachedMangonelAgents[item2.ShipSiegeWeapon] = item2.ShipSiegeWeapon.PilotAgent;
				item2.ShipSiegeWeapon.PilotAgent.StopUsingGameObject();
				item2.ShipSiegeWeapon.SetIsDisabledForAI(isDisabledForAI: true);
			}
		}
		_playerShip.OnSetRangedWeaponControlMode(value: true);
		_isPhaseOneInitialized = true;
	}

	private void TickPhaseOneLogic(float dt)
	{
		if (_playerShip.IsSinking)
		{
			OnMissionFailed();
			return;
		}
		if (_playerShip.SailHitPoints <= 0f)
		{
			_playerShipSailDestroyedLine.TryPlayLine();
		}
		if (_playerShip.HitPoints <= _playerShip.MaxHealth * 0.65f)
		{
			_playerShipLowHpLine.TryPlayLine();
		}
		if (_enemySiegeWeaponDestructables.Count == 0)
		{
			return;
		}
		if (_playerShip.ShipSiegeWeapon.DestructionComponent.IsDestroyed || _playerShip.ShipSiegeWeapon.AmmoCount == 0)
		{
			_playerLoseRemainingTime -= dt;
			if (_playerLoseRemainingTime <= 0f)
			{
				OnMissionFailed();
				return;
			}
		}
		bool flag = _playerShip.GameEntity.GlobalPosition.Distance(_trailingTargetObject.GlobalPosition) < 15f;
		foreach (MissionShip item in _enemyMissionShipsOrdered)
		{
			if (Agent.Main != null && item.GetIsAgentOnShip(Agent.Main))
			{
				OnMissionFailed();
			}
			if (_playerShip.GameEntity.GlobalPosition.DistanceSquared(item.GameEntity.GlobalPosition) <= 10000f)
			{
				_playerShipTooCloseLine.TryPlayLine();
			}
			if (item.ShipSiegeWeapon == null || item.ShipSiegeWeapon.IsDisabledForAI)
			{
				continue;
			}
			RangedSiegeWeapon shipSiegeWeapon = item.ShipSiegeWeapon;
			if (!shipSiegeWeapon.IsDestroyed)
			{
				shipSiegeWeapon.GameEntity.SetContourColor(new Color(1f, 0.68f, 0.44f, (TaleWorlds.Library.MathF.Sin(base.Mission.CurrentTime * 2f) + 1f) / 2f).ToUnsignedInteger());
			}
			if (flag && !shipSiegeWeapon.PilotStandingPoint.IsDisabled && shipSiegeWeapon.PilotAgent != null && shipSiegeWeapon.CanShootAtPoint(_trailingTargetObject.GlobalPosition))
			{
				_playerShipStandingStillLine.TryPlayLine();
			}
			if (shipSiegeWeapon.IsDestroyed || shipSiegeWeapon.PilotStandingPoint.IsDisabled || (shipSiegeWeapon.PilotStandingPoint.UserAgent != null && shipSiegeWeapon.PilotStandingPoint.UserAgent.IsActive()) || shipSiegeWeapon.PilotStandingPoint.HasAIMovingTo || shipSiegeWeapon.State != RangedSiegeWeapon.WeaponState.Idle)
			{
				continue;
			}
			float num = 1000000f;
			Agent agent = null;
			foreach (Agent item2 in _navalAgentsLogic.GetActiveAgentsOfShip(item))
			{
				if (!item2.IsHero && item2.Detachment == null)
				{
					float num2 = item2.Position.DistanceSquared(shipSiegeWeapon.GameEntity.GlobalPosition);
					if (num2 < num)
					{
						num = num2;
						agent = item2;
					}
				}
			}
			if (agent != null)
			{
				shipSiegeWeapon.AddAgentAtSlotIndex(agent, shipSiegeWeapon.PilotStandingPointSlotIndex);
			}
		}
		RangedSiegeWeapon shipSiegeWeapon2 = _playerShip.ShipSiegeWeapon;
		if (shipSiegeWeapon2 != null)
		{
			if ((float)shipSiegeWeapon2.AmmoCount <= (float)_enemySiegeWeaponDestructables.Count * 3f)
			{
				_playerShipRemainingAmmoLine.TryPlayLine();
			}
			if (shipSiegeWeapon2.AmmoCount == 0)
			{
				OnMissionFailed();
			}
			if (!shipSiegeWeapon2.IsDestroyed && (shipSiegeWeapon2.PilotStandingPoint.UserAgent == null || !shipSiegeWeapon2.PilotStandingPoint.UserAgent.IsActive()) && !shipSiegeWeapon2.PilotStandingPoint.HasAIMovingTo && shipSiegeWeapon2.State == RangedSiegeWeapon.WeaponState.Idle)
			{
				float num3 = 1000000f;
				Agent agent2 = null;
				foreach (Agent item3 in _navalAgentsLogic.GetActiveAgentsOfShip(_playerShip))
				{
					if (!item3.IsHero && item3.Detachment == null)
					{
						float num4 = item3.Position.DistanceSquared(shipSiegeWeapon2.GameEntity.GlobalPosition);
						if (num4 < num3)
						{
							num3 = num4;
							agent2 = item3;
						}
					}
				}
				if (agent2 != null)
				{
					shipSiegeWeapon2.AddAgentAtSlotIndex(agent2, shipSiegeWeapon2.PilotStandingPointSlotIndex);
				}
			}
		}
		_playerShipTargetObjectTrailController.RecordPosition(_playerShip.GameEntity.GlobalPosition, base.Mission.CurrentTime);
		_trailingTargetObject.WeakEntity.SetGlobalPosition(_playerShipTargetObjectTrailController.GetTrailEndPosition(base.Mission.CurrentTime));
		if (flag)
		{
			_playerShipTargetObject.GameEntity.SetGlobalPosition(_playerShip.GameEntity.GlobalPosition);
			return;
		}
		if (_lastRandomAttackPointPickTime + 1f < base.Mission.CurrentTime)
		{
			_randomAttackPoint = GetRandomPointOnCircle(Vec3.Zero, 15f);
			_lastRandomAttackPointPickTime = base.Mission.CurrentTime;
		}
		Vec3 globalPosition = _playerShip.GameEntity.GlobalPosition + _randomAttackPoint;
		_playerShipTargetObject.GameEntity.SetGlobalPosition(globalPosition);
	}

	private void TickPhaseTwoLogic(float dt)
	{
		if (_boardFloatingFortressObjective.IsCompleted && _defeatTheEnemyCrewObjective == null)
		{
			_defeatTheEnemyCrewObjective = new DefeatTheEnemyCrewObjective(base.Mission);
			_missionObjectiveLogic.StartObjective(_defeatTheEnemyCrewObjective);
		}
		for (int i = 0; i < _playerAllyShipAnchorState.Count; i++)
		{
			(MissionShip, bool) valueTuple = _playerAllyShipAnchorState[i];
			Vec3 globalPosition = valueTuple.Item1.GameEntity.GlobalPosition;
			if (valueTuple.Item2)
			{
				if (valueTuple.Item1.GetIsConnectedToEnemy())
				{
					_allyShipStuckDurations.Remove(valueTuple.Item1);
					if (valueTuple.Item1.Physics.IsAnchored)
					{
						valueTuple.Item1.SetAnchor(isAnchored: false);
					}
				}
				else
				{
					TickAllyShipForceConnect(valueTuple.Item1, dt);
				}
				continue;
			}
			if (valueTuple.Item1.Physics.IsAnchored)
			{
				if (valueTuple.Item1.Physics.AnchorGlobalFrame.origin.DistanceSquared(globalPosition) < 200f)
				{
					valueTuple.Item1.SetAnchor(isAnchored: true, anchorInPlace: true);
					valueTuple.Item2 = true;
					_playerAllyShipAnchorState[i] = valueTuple;
				}
				continue;
			}
			if (valueTuple.Item1.ShipOrder.TargetShip == null)
			{
				MissionShip missionShip = _enemyMissionShipsOrdered.MinBy((MissionShip x) => x.GameEntity.GlobalPosition.DistanceSquared(valueTuple.Item1.GameEntity.GlobalPosition));
				valueTuple.Item1.ShipOrder.SetShipEngageOrder(missionShip);
				valueTuple.Item1.ShipOrder.SetBoardingTargetShip(missionShip);
			}
			Vec3 globalPosition2 = valueTuple.Item1.ShipOrder.TargetShip.GameEntity.GlobalPosition;
			if (globalPosition.DistanceSquared(globalPosition2) < 1600f)
			{
				Vec3 vec = (globalPosition2 - globalPosition).NormalizedCopy();
				valueTuple.Item1.SetAnchor(isAnchored: true);
				valueTuple.Item1.SetAnchorFrame(globalPosition2.AsVec2, vec.AsVec2, 0.2f);
			}
		}
	}

	private void TickAllyShipForceConnect(MissionShip allyShip, float dt)
	{
		if (allyShip.IsSinking)
		{
			_allyShipStuckDurations.Remove(allyShip);
			return;
		}
		_allyShipStuckDurations.TryGetValue(allyShip, out var value);
		value += dt;
		_allyShipStuckDurations[allyShip] = value;
		if (value < 5f)
		{
			return;
		}
		MissionShip missionShip = allyShip.ShipOrder.TargetShip;
		if (missionShip != null && !missionShip.IsSinking)
		{
			Team team = missionShip.Team;
			if (team != null && team.IsEnemyOf(allyShip.Team))
			{
				goto IL_00e9;
			}
		}
		missionShip = null;
		float num = float.MaxValue;
		foreach (MissionShip item in _enemyMissionShipsOrdered)
		{
			if (!item.IsSinking)
			{
				float num2 = item.GameEntity.GlobalPosition.DistanceSquared(allyShip.GameEntity.GlobalPosition);
				if (num2 < num)
				{
					num = num2;
					missionShip = item;
				}
			}
		}
		goto IL_00e9;
		IL_00e9:
		if (missionShip != null && !(missionShip.GameEntity.GlobalPosition.DistanceSquared(allyShip.GameEntity.GlobalPosition) > 2500f) && TryForceConnectShips(allyShip, missionShip))
		{
			_allyShipStuckDurations.Remove(allyShip);
		}
	}

	private static bool TryForceConnectShips(MissionShip ship, MissionShip otherShip)
	{
		foreach (ShipAttachmentMachine attachmentMachine in ship.AttachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment == null && attachmentMachine.LinkedAttachmentPointMachine.CurrentAttachment == null)
			{
				attachmentMachine.SetPreferredTargetShip(otherShip);
				ShipAttachmentPointMachine bestEnemyAttachment = attachmentMachine.GetBestEnemyAttachment(checkAttachmentAlreadyExists: true, checkInteractionDistance: false);
				if (bestEnemyAttachment != null)
				{
					attachmentMachine.ConnectWithAttachmentPointMachine(bestEnemyAttachment, forceBridge: true);
					return true;
				}
			}
		}
		ShipAttachmentMachine shipAttachmentMachine = null;
		ShipAttachmentPointMachine attachmentPointMachine = null;
		float num = 2500f;
		foreach (ShipAttachmentMachine attachmentMachine2 in ship.AttachmentMachines)
		{
			if (attachmentMachine2.CurrentAttachment != null || attachmentMachine2.LinkedAttachmentPointMachine.CurrentAttachment != null)
			{
				continue;
			}
			foreach (ShipAttachmentPointMachine attachmentPointMachine2 in otherShip.AttachmentPointMachines)
			{
				if (attachmentPointMachine2.CurrentAttachment == null && attachmentPointMachine2.LinkedAttachmentMachine?.CurrentAttachment == null)
				{
					float num2 = attachmentMachine2.GameEntity.GlobalPosition.DistanceSquared(attachmentPointMachine2.GameEntity.GlobalPosition);
					if (num2 < num)
					{
						num = num2;
						shipAttachmentMachine = attachmentMachine2;
						attachmentPointMachine = attachmentPointMachine2;
					}
				}
			}
		}
		if (shipAttachmentMachine != null)
		{
			shipAttachmentMachine.SetPreferredTargetShip(otherShip);
			shipAttachmentMachine.ConnectWithAttachmentPointMachine(attachmentPointMachine, forceBridge: true);
			return true;
		}
		return false;
	}

	private void SpawnPlayerShip()
	{
		Formation formation = Mission.GetTeam(TeamSideEnum.PlayerTeam).GetFormation(FormationClass.Infantry);
		ShipHull playerShipHull = Campaign.Current.ObjectManager.GetObject<ShipHull>("naval_storyline_quest_4_player_medit_ship");
		Ship ship = PartyBase.MainParty.Ships.FirstOrDefault((Ship x) => x.ShipHull == playerShipHull) ?? new Ship(playerShipHull)
		{
			IsTradeable = false,
			IsUsedByQuest = true,
			Owner = PartyBase.MainParty,
			CanHaveUnlockedPieces = true
		};
		ship.HitPoints = ship.MaxHitPoints;
		foreach (KeyValuePair<string, string> playerShipUpgradePiece in _playerShipUpgradePieces)
		{
			ship.EquipUpgradePiece(playerShipUpgradePiece.Key, Campaign.Current.ObjectManager.GetObject<ShipUpgradePiece>(playerShipUpgradePiece.Value));
		}
		_playerShip = CreateMissionShip(ship, IsStartedFromCheckpoint ? "sp_player_phase_two_start" : "sp_player_ship", formation);
		_playerShip.SetShipOrderActive(isOrderActive: false);
		_trailingTargetObject = GameEntity.CreateEmpty(base.Mission.Scene);
		_playerShipTargetObject = _playerShip.GameEntity.GetFirstScriptInFamilyDescending<ShipTargetMissionObject>();
		((ShipBallistaAI)_playerShip.ShipSiegeWeapon.Ai).SetCanAiUpdateAim(canAiUpdateAim: false);
		_playerShip.ShipSiegeWeapon.SetStartAmmo(30);
	}

	private void TickPhaseTwoInitialization()
	{
		_currentPhaseTwoInitializationTick++;
		if (_currentPhaseTwoInitializationTick == 1)
		{
			if (!IsStartedFromCheckpoint)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=BWSp3Uyj}Checkpoint reached.").ToString(), new Color(0f, 1f, 0f)));
				GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("sp_player_phase_two_start"));
				_navalShipsLogic.TeleportShip(_playerShip, gameEntity.GetGlobalFrame(), checkFreeArea: false);
			}
			((ShipBallistaAI)_playerShip.ShipSiegeWeapon.Ai).SetCanAiUpdateAim(canAiUpdateAim: true);
			foreach (MissionShip item in _enemyMissionShipsOrdered)
			{
				if (item.ShipOrigin is Ship ship)
				{
					ship.IsInvulnerable = false;
				}
			}
			SpawnAllyShips();
			if (Agent.Main.CurrentlyUsedGameObject != null)
			{
				Agent.Main.StopUsingGameObject();
			}
			_playerShip.SetShipOrderActive(isOrderActive: true);
		}
		if (_currentPhaseTwoInitializationTick == 2)
		{
			for (int num = 0; num < _playerAllyMissionShips.Count; num++)
			{
				SpawnAllyShipAgents(_playerAllyMissionShips[num], _allyShipAgents[num]);
			}
			for (int num2 = 0; num2 < _enemyMissionShipsOrdered.Count; num2++)
			{
				(string, int)[] source = _reinforcementEnemyShipAgents[num2];
				SpawnEnemyShipAgents(_enemyMissionShipsOrdered[num2], source);
			}
			foreach (MissionShip item2 in _enemyMissionShipsOrdered)
			{
				item2.ResetConnectionBlock();
				item2.ShipOrder.SetOrderOarsmenLevel(0);
				item2.ShipOrder.SetCutLoose(enable: false);
			}
			List<MissionShip> list = _enemyMissionShipsOrdered.ToList();
			foreach (MissionShip playerAllyMissionShip in _playerAllyMissionShips)
			{
				MissionShip missionShip = list.MinBy((MissionShip x) => x.GameEntity.GlobalPosition.DistanceSquared(playerAllyMissionShip.GameEntity.GlobalPosition));
				list.Remove(missionShip);
				playerAllyMissionShip.ShipOrder.SetShipEngageOrder(missionShip);
				playerAllyMissionShip.ShipOrder.SetBoardingTargetShip(missionShip);
			}
			_navalAgentsLogic.SetDeploymentMode(value: true);
			_navalShipsLogic.SetDeploymentMode(value: true);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.PlayerTeam);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.EnemyTeam);
			_navalAgentsLogic.AssignAndTeleportCrewToShipMachines(TeamSideEnum.PlayerAllyTeam);
			_navalAgentsLogic.SetDeploymentMode(value: false);
			_navalShipsLogic.SetDeploymentMode(value: false);
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: false);
			_playerTookAllMangonelsDownLine.TryPlayLine();
			_boardFloatingFortressObjective = new BoardFloatingFortressObjective(base.Mission, _playerShip, _enemyMissionShipsOrdered);
			_missionObjectiveLogic.StartObjective(_boardFloatingFortressObjective);
			_isPhaseTwoInitialized = true;
		}
		Agent.Main.Health = Agent.Main.HealthLimit;
	}

	private void SpawnAllyShips()
	{
		List<Formation> list = Mission.GetTeam(TeamSideEnum.PlayerAllyTeam).FormationsIncludingEmpty.Where((Formation x) => x != _playerShip.Formation).ToList();
		for (int num = 0; num < _allyShipHulls.Count; num++)
		{
			ShipHull hull = Campaign.Current.ObjectManager.GetObject<ShipHull>(_allyShipHulls[num]);
			Ship ship = PartyBase.MainParty.Ships.FirstOrDefault((Ship x) => x.ShipHull == hull);
			if (ship == null || PartyBase.MainParty.Ships.CountQ((Ship x) => x.ShipHull == hull) < _allyShipHulls.CountQ((string x) => x == hull.StringId))
			{
				ship = new Ship(hull)
				{
					IsTradeable = false,
					IsUsedByQuest = true,
					Owner = PartyBase.MainParty,
					CanHaveUnlockedPieces = true
				};
			}
			ship.HitPoints = ship.MaxHitPoints;
			(string, string)[] array = AllyShipUpgrades[num];
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				var (slotTag, objectName) = array[num2];
				if (ship.HasSlot(slotTag))
				{
					ship.EquipUpgradePiece(slotTag, MBObjectManager.Instance.GetObject<ShipUpgradePiece>(objectName));
				}
			}
			ship.ChangeFigurehead(_allyShipFigureheads[num]);
			string allySpawnPoint = GetAllySpawnPoint(num);
			MissionShip missionShip = CreateMissionShip(ship, allySpawnPoint, list[num]);
			GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(allySpawnPoint);
			_navalShipsLogic.TeleportShip(missionShip, gameEntity.GetGlobalFrame(), checkFreeArea: false);
			_playerAllyMissionShips.Add(missionShip);
			_playerAllyShipAnchorState.Add((missionShip, false));
		}
		foreach (MissionShip playerAllyMissionShip in _playerAllyMissionShips)
		{
			playerAllyMissionShip.FinalizeDeployment(initializeMachines: true);
		}
	}

	private void OnEnemyShipBallistaDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		if (!IsPhaseOneCompleted)
		{
			_enemySiegeWeaponDestructables.Remove(target);
			_playerTookMangonelDownLine.TryPlayLine();
			target.GameEntity.SetContourColor(null);
			if (_enemySiegeWeaponDestructables.Count == 0)
			{
				IsPhaseOneCompleted = true;
			}
		}
	}

	private void OnEnemyShipBallistaCoverDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		DestructableComponent destructableComponent = _enemySiegeWeaponByCover[target];
		if (!destructableComponent.IsDestroyed)
		{
			int internalValue = (int)Game.Current.ObjectManager.GetObject<ItemObject>("ballista_projectile").Id.InternalValue;
			destructableComponent.TriggerOnHit(Agent.Main, 10000, destructableComponent.GameEntity.GlobalPosition, Vec3.One, new MissionWeapon(ItemObject.GetItemFromWeaponKind(internalValue), null, null), -1, null);
		}
	}

	public override void OnBehaviorInitialize()
	{
		if (!SailWindProfile.IsSailWindProfileInitialized)
		{
			SailWindProfile.InitializeProfile();
		}
		Team team = Mission.GetTeam(TeamSideEnum.PlayerTeam);
		base.Mission.Teams.Add(team.Side, team.Color, team.Color2, team.Banner);
	}

	private void UpdateEntityReferences()
	{
		base.Mission.Scene.GetEntities(ref _entities);
	}

	private MissionShip CreateMissionShip(Ship ship, string spawnPointId, Formation formation)
	{
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag(spawnPointId));
		MatrixFrame shipFrame = gameEntity.GetGlobalFrame();
		shipFrame.origin = new Vec3(z: Mission.Current.Scene.GetWaterLevelAtPosition(gameEntity.GlobalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false), x: gameEntity.GlobalPosition.x, y: gameEntity.GlobalPosition.y);
		MissionShip missionShip = missionBehavior.SpawnShip(ship, in shipFrame, formation.Team, formation);
		missionShip.ShipOrder.FormationJoinShip(formation);
		return missionShip;
	}

	private void SpawnEnemyShips()
	{
		MBList<Formation> formationsIncludingEmpty = Mission.GetTeam(TeamSideEnum.EnemyTeam).FormationsIncludingEmpty;
		_enemyMissionShipsOrdered = new MBList<MissionShip>();
		for (int i = 0; i < _enemyShipHulls.Length; i++)
		{
			(string, string) tuple = _enemyShipHulls[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			ShipHull shipHullObject = Campaign.Current.ObjectManager.GetObject<ShipHull>(item);
			Ship ship = MapEvent.PlayerMapEvent.GetLeaderParty(Mission.Current.PlayerEnemyTeam.Side).Ships.FirstOrDefault((Ship x) => x.ShipHull == shipHullObject) ?? new Ship(shipHullObject)
			{
				IsTradeable = false,
				IsUsedByQuest = true,
				Owner = MapEvent.PlayerMapEvent.GetLeaderParty(Mission.Current.PlayerEnemyTeam.Side)
			};
			if (ship.HasSlot("fore"))
			{
				bool flag = !IsStartedFromCheckpoint && _enemyShipsToAddBallista.ContainsKey(i + 1);
				ship.EquipUpgradePiece("fore", flag ? Campaign.Current.ObjectManager.GetObject<ShipUpgradePiece>(_enemyShipsToAddBallista[i + 1]) : null);
			}
			MissionShip missionShip = CreateMissionShip(ship, item2, formationsIncludingEmpty[i]);
			GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(item2);
			missionShip.SetShipOrderActive(isOrderActive: false);
			missionShip.ShipOrder.SetOrderOarsmenLevel(0);
			missionShip.SetCustomSailSetting(enableCustomSailSetting: true, SailInput.Raised);
			missionShip.SetController(ShipControllerType.None, autoUpdateController: false);
			missionShip.ShipControllerMachine.PilotStandingPoint.SetDisabled();
			missionShip.SetCanBeTakenOver(value: false);
			_navalShipsLogic.TeleportShip(missionShip, gameEntity.GetGlobalFrame(), checkFreeArea: false, anchorShip: true);
			if (missionShip.ShipSiegeWeapon != null)
			{
				_enemySiegeWeaponDestructables.Add(missionShip.ShipSiegeWeapon.DestructionComponent);
			}
			_enemyMissionShipsOrdered.Add(missionShip);
		}
		foreach (DestructableComponent enemySiegeWeaponDestructable in _enemySiegeWeaponDestructables)
		{
			enemySiegeWeaponDestructable.OnDestroyed += OnEnemyShipBallistaDestroyed;
			DestructableComponent firstScriptOfType = enemySiegeWeaponDestructable.GameEntity.GetFirstChildEntityWithTag("ballista_cover").GetFirstScriptOfType<DestructableComponent>();
			if (firstScriptOfType != null)
			{
				_enemySiegeWeaponByCover.Add(firstScriptOfType, enemySiegeWeaponDestructable);
				firstScriptOfType.OnDestroyed += OnEnemyShipBallistaCoverDestroyed;
			}
		}
	}

	private void ConnectEnemyShips()
	{
		for (int i = 0; i < _enemyMissionShipsOrdered.Count; i++)
		{
			int index = i + 1;
			if (i == _enemyMissionShipsOrdered.Count - 1)
			{
				index = 0;
			}
			TryMaintainConnection(_enemyMissionShipsOrdered[i], _enemyMissionShipsOrdered[index]);
		}
	}

	private void TryMaintainConnection(MissionShip ship, MissionShip otherShip)
	{
		int num = 0;
		foreach (ShipAttachmentMachine attachmentMachine in ship.AttachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null && attachmentMachine.CurrentAttachment.AttachmentTarget.OwnerShip == otherShip)
			{
				num++;
			}
		}
		if (num >= 1)
		{
			return;
		}
		Vec3 fortressCenter = Vec3.Zero;
		foreach (MissionShip item in _enemyMissionShipsOrdered)
		{
			fortressCenter += item.GameEntity.GlobalPosition;
		}
		fortressCenter /= (float)_enemyMissionShipsOrdered.Count;
		foreach (ShipAttachmentMachine item2 in ship.AttachmentMachines.OrderBy((ShipAttachmentMachine x) => x.GameEntity.GlobalPosition.DistanceSquared(fortressCenter)))
		{
			if (item2.CurrentAttachment != null)
			{
				continue;
			}
			item2.SetPreferredTargetShip(otherShip);
			if (item2.LinkedAttachmentPointMachine.CurrentAttachment != null)
			{
				continue;
			}
			item2.SetCanConnectToFriends(canConnectToFriends: true);
			ShipAttachmentPointMachine bestEnemyAttachment = item2.GetBestEnemyAttachment(checkAttachmentAlreadyExists: true);
			if (bestEnemyAttachment != null)
			{
				item2.ConnectWithAttachmentPointMachine(bestEnemyAttachment, forceBridge: true, unbreakableBridge: true);
				num++;
				if (num >= 1)
				{
					break;
				}
			}
		}
	}

	private void SpawnPlayer()
	{
		_navalAgentsLogic.AddReservedTroopToShip(new PartyAgentOrigin(PartyBase.MainParty, CharacterObject.PlayerCharacter), _playerShip);
		_navalAgentsLogic.SpawnNextBatch(TeamSideEnum.PlayerTeam);
		Agent main = Agent.Main;
		_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(main, _playerShip);
		Mission.Current.PlayerTeam.PlayerOrderController.Owner = main;
		base.Mission.PlayerTeam.GetFormation(FormationClass.Infantry).PlayerOwner = main;
		main.OnAgentHealthChanged += OnMainAgentHealthChanged;
	}

	private void SpawnPlayerShipAgents()
	{
		List<CharacterObject> list = new List<CharacterObject>();
		foreach (var (objectName, count) in _playerShipTroops)
		{
			list.AddRange(Enumerable.Repeat(Campaign.Current.ObjectManager.GetObject<CharacterObject>(objectName), count));
		}
		NavalAgentsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		missionBehavior.SetDesiredTroopCountOfShip(_playerShip, list.Count + 1);
		int deckFrameCount = _playerShip.DeckFrameCount;
		list.Shuffle();
		for (int i = 0; i < deckFrameCount && i < list.Count; i++)
		{
			MatrixFrame nextOuterInnerSpawnGlobalFrame = _playerShip.GetNextOuterInnerSpawnGlobalFrame();
			CharacterObject characterObject = list.ElementAtOrDefault(i);
			if (characterObject == null)
			{
				break;
			}
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).Team(base.Mission.PlayerTeam).Formation(_playerShip.Formation)
				.InitialPosition(in nextOuterInnerSpawnGlobalFrame.origin)
				.InitialDirection(nextOuterInnerSpawnGlobalFrame.rotation.f.AsVec2.Normalized())
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			missionBehavior.AddAgentToShip(agent, _playerShip);
		}
	}

	private void SetWindStrengthAndDirection(Vec2 direction, float strength)
	{
		Mission.Current.Scene.SetGlobalWindVelocity(strength * direction);
	}

	private void SpawnEnemyShipAgents(MissionShip ship, (string, int)[] source)
	{
		NavalAgentsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		missionBehavior.SetDesiredTroopCountOfShip(ship, source.Sum(((string, int) x) => x.Item2));
		List<CharacterObject> list = new List<CharacterObject>();
		for (int num = 0; num < source.Length; num++)
		{
			var (objectName, count) = source[num];
			list.AddRange(Enumerable.Repeat(Campaign.Current.ObjectManager.GetObject<CharacterObject>(objectName), count));
		}
		list.Shuffle();
		int deckFrameCount = ship.DeckFrameCount;
		for (int num2 = 0; num2 < deckFrameCount && num2 < list.Count; num2++)
		{
			CharacterObject characterObject = list[num2];
			if (characterObject == null)
			{
				break;
			}
			MatrixFrame nextOuterInnerSpawnGlobalFrame = ship.GetNextOuterInnerSpawnGlobalFrame();
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).Team(base.Mission.PlayerEnemyTeam).InitialPosition(in nextOuterInnerSpawnGlobalFrame.origin)
				.InitialDirection(nextOuterInnerSpawnGlobalFrame.rotation.f.AsVec2.Normalized())
				.Formation(ship.Formation)
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			missionBehavior.AddAgentToShip(agent, ship);
		}
	}

	private void SpawnAllyShipAgents(MissionShip ship, (string, int)[] source)
	{
		NavalAgentsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		missionBehavior.SetDesiredTroopCountOfShip(ship, source.Sum(((string, int) x) => x.Item2));
		List<CharacterObject> list = new List<CharacterObject>();
		for (int num = 0; num < source.Length; num++)
		{
			var (objectName, count) = source[num];
			list.AddRange(Enumerable.Repeat(Campaign.Current.ObjectManager.GetObject<CharacterObject>(objectName), count));
		}
		list.Shuffle();
		int deckFrameCount = ship.DeckFrameCount;
		for (int num2 = 0; num2 < deckFrameCount && num2 < list.Count; num2++)
		{
			MatrixFrame nextOuterInnerSpawnGlobalFrame = ship.GetNextOuterInnerSpawnGlobalFrame();
			CharacterObject characterObject = list[num2];
			if (characterObject == null)
			{
				break;
			}
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).Team(base.Mission.PlayerAllyTeam).InitialPosition(in nextOuterInnerSpawnGlobalFrame.origin)
				.InitialDirection(nextOuterInnerSpawnGlobalFrame.rotation.f.AsVec2.Normalized())
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			missionBehavior.AddAgentToShip(agent, ship);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if ((base.Mission.PlayerTeam.ActiveAgents.IsEmpty() || (affectedAgent.IsMainAgent && !_shouldStartPhaseTwo)) && !_isMissionSuccessful)
		{
			OnMissionFailed();
		}
		else if (base.Mission.PlayerEnemyTeam.ActiveAgents.IsEmpty() && !_isMissionFailed && !_isMissionSuccessful)
		{
			OnMissionSucceeded();
		}
	}

	private void OnMainAgentHealthChanged(Agent agent, float oldHealth, float newHealth)
	{
		if (!_shouldStartPhaseTwo && newHealth <= 0f)
		{
			OnMissionFailed();
		}
	}

	private void OnMissionSucceeded()
	{
		if (!_isMissionSuccessful && !_isMissionFailed && Mission.Current.CurrentState != Mission.State.EndingNextFrame)
		{
			_isMissionSuccessful = true;
			PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(BattleState.AttackerVictory);
		}
	}

	private void OnMissionFailed()
	{
		if (!_isMissionFailed && !_isMissionSuccessful && Mission.Current.CurrentState != Mission.State.EndingNextFrame)
		{
			_isMissionFailed = true;
			PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(BattleState.DefenderVictory);
			base.Mission.EndMission();
		}
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
		MBMusicManager.Current.ForceStopThemeWithFadeOut();
		return result;
	}

	public void OnViewFadeOut(int reason)
	{
		switch (reason)
		{
		case 1:
		{
			_playerShip.SetShipOrderActive(isOrderActive: true);
			MBList<ShipMangonel> mBList = new MBList<ShipMangonel>();
			foreach (MissionShip item in _enemyMissionShipsOrdered)
			{
				if (item.ShipSiegeWeapon != null)
				{
					mBList.Add(item.ShipSiegeWeapon as ShipMangonel);
					item.ShipSiegeWeapon.SetIsDisabledForAI(isDisabledForAI: false);
					if (_cachedMangonelAgents.TryGetValue(item.ShipSiegeWeapon, out var value))
					{
						value.Formation.StartUsingMachine(item.ShipSiegeWeapon);
						item.ShipSiegeWeapon.AddAgentAtSlotIndex(value, item.ShipSiegeWeapon.PilotStandingPointSlotIndex);
					}
				}
			}
			_missionObjectiveLogic.StartObjective(new DestroyMangonelsObjective(base.Mission, mBList));
			Agent.Main.Controller = AgentControllerType.Player;
			break;
		}
		case 2:
			(Campaign.Current.QuestManager.Quests.FirstOrDefault((QuestBase x) => x is CaptureTheImperialMerchantPrusas) as CaptureTheImperialMerchantPrusas)?.OnCheckPointReached();
			_shouldStartPhaseTwo = true;
			break;
		case 0:
			break;
		}
	}

	public override void OnRetreatMission()
	{
		_isMissionFailed = true;
		PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(BattleState.DefenderVictory);
	}

	private void OnShipHit(MissionShip ship, Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex)
	{
		if (weapon.CurrentUsageItem != null && weapon.CurrentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.CanPenetrateShield) && ship == _playerShip && !_isPhaseTwoInitialized)
		{
			_playerShipHitLine.TryPlayLine();
		}
	}

	private void DestroyEnemyBallistas()
	{
		int internalValue = (int)Game.Current.ObjectManager.GetObject<ItemObject>("ballista_projectile").Id.InternalValue;
		for (int num = _enemySiegeWeaponDestructables.Count - 1; num >= 0; num--)
		{
			_enemySiegeWeaponDestructables[num].TriggerOnHit(Agent.Main, 1000, _enemySiegeWeaponDestructables[num].GameEntity.GlobalPosition, Vec3.One, new MissionWeapon(ItemObject.GetItemFromWeaponKind(internalValue), null, null), -1, null);
		}
	}

	private static string GetAllySpawnPoint(int i)
	{
		return $"sp_player_reinforcement_{i + 1}";
	}

	private Vec3 GetRandomPointOnCircle(Vec3 center, float radius)
	{
		float x = MBRandom.RandomFloat * System.MathF.PI * 2f;
		float x2 = center.x + radius * TaleWorlds.Library.MathF.Cos(x);
		float y = center.y + radius * TaleWorlds.Library.MathF.Sin(x);
		return new Vec3(x2, y, center.z);
	}
}
