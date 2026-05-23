using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

namespace TaleWorlds.MountAndBlade;

public sealed class Formation : IFormation
{
	private class AgentArrangementData : IFormationUnit
	{
		public IFormationArrangement Formation { get; private set; }

		public int FormationFileIndex { get; set; } = -1;

		public int FormationRankIndex { get; set; } = -1;

		public IFormationUnit FollowedUnit { get; }

		public bool IsShieldUsageEncouraged => true;

		public bool IsPlayerUnit => false;

		public AgentArrangementData(int index, IFormationArrangement arrangement)
		{
			Formation = arrangement;
		}
	}

	public struct FormationIntegrityDataGroup
	{
		public Vec2 AverageVelocityExcludeFarAgents;

		public float DeviationOfPositionsExcludeFarAgents;

		public float MaxDeviationOfPositionExcludeFarAgents;

		public float AverageMaxUnlimitedSpeedExcludeFarAgents;

		public FormationIntegrityDataGroup(Vec2 averageVelocityExcludeFarAgents, float deviationOfPositionsExcludeFarAgents, float maxDeviationOfPositionExcludeFarAgents, float averageMaxUnlimitedSpeedExcludeFarAgents)
		{
			AverageVelocityExcludeFarAgents = averageVelocityExcludeFarAgents;
			DeviationOfPositionsExcludeFarAgents = deviationOfPositionsExcludeFarAgents;
			MaxDeviationOfPositionExcludeFarAgents = maxDeviationOfPositionExcludeFarAgents;
			AverageMaxUnlimitedSpeedExcludeFarAgents = averageMaxUnlimitedSpeedExcludeFarAgents;
		}
	}

	public class RetreatPositionCacheSystem
	{
		private List<(Vec2, WorldPosition)> _retreatPositionDistance;

		public RetreatPositionCacheSystem(int cacheCount)
		{
			_retreatPositionDistance = new List<(Vec2, WorldPosition)>(2);
		}

		public WorldPosition GetRetreatPositionFromCache(Vec2 agentPosition)
		{
			for (int num = _retreatPositionDistance.Count - 1; num >= 0; num--)
			{
				if (_retreatPositionDistance[num].Item1.DistanceSquared(agentPosition) < 400f)
				{
					return _retreatPositionDistance[num].Item2;
				}
			}
			return WorldPosition.Invalid;
		}

		public void AddNewPositionToCache(Vec2 agentPostion, WorldPosition retreatingPosition)
		{
			if (_retreatPositionDistance.Count >= 2)
			{
				_retreatPositionDistance.RemoveAt(0);
			}
			_retreatPositionDistance.Add((agentPostion, retreatingPosition));
		}
	}

	public const float AveragePositionCalculatePeriod = 0.1f;

	public const int MinimumUnitSpacing = 0;

	public const int RetreatPositionDistanceCacheCount = 2;

	public const float RetreatPositionCacheUseDistanceSquared = 400f;

	private static Formation _simulationFormationTemp;

	private static int _simulationFormationUniqueIdentifier;

	public readonly Team Team;

	public readonly int Index;

	public readonly FormationClass FormationIndex;

	public Banner Banner;

	public bool HasBeenPositioned;

	public Vec2? ReferencePosition;

	private bool _logicalClassNeedsUpdate;

	private FormationClass _logicalClass = FormationClass.NumberOfAllFormations;

	private readonly int[] _logicalClassCounts = new int[4];

	private Agent _playerOwner;

	private string _bannerCode;

	private bool _enforceNotSplittableByAI = true;

	private WorldPosition _orderPosition;

	private Vec2 _orderLocalAveragePosition;

	private bool _orderLocalAveragePositionIsDirty = true;

	private int _formationOrderDefensivenessFactor = 2;

	private MovementOrder _movementOrder;

	private Timer _arrangementOrderTickOccasionallyTimer;

	private Timer _arrangementTickOccasionallyTimer;

	private Agent _captain;

	private Vec2 _smoothedAverageUnitPosition = Vec2.Invalid;

	private MBList<IDetachment> _detachments;

	private IFormationArrangement _arrangement;

	private int[] _agentIndicesCache;

	private MBList<Agent> _detachedUnits;

	private int _undetachableNonPlayerUnitCount;

	private MBList<Agent> _looseDetachedUnits;

	private bool? _overridenHasAnyMountedUnit;

	private bool _isArrangementShapeChanged;

	private bool _hasPendingUnitPositions;

	private int _currentSpawnIndex;

	private int _desiredFileCount;

	private Formation _targetFormation;

	private Timer _cachedFormationIntegrityDataUpdateTimer;

	private Timer _cachedPositionAndVelocityUpdateTimer;

	private float _lastAveragePositionCacheTime;

	private Timer _cachedMovementSpeedUpdateTimer;

	private Formation _cachedClosestEnemyFormation;

	private Timer _cachedClosestEnemyFormationUpdateTimer;

	public RetreatPositionCacheSystem RetreatPositionCache { get; private set; } = new RetreatPositionCacheSystem(2);

	public FormationClass RepresentativeClass { get; private set; } = FormationClass.NumberOfAllFormations;

	public bool IsAIControlled { get; private set; } = true;

	public Vec2 Direction { get; private set; }

	public int UnitSpacing { get; private set; }

	public object OrderPositionLock { get; private set; } = new object();

	public object SimulationFormationLock { get; private set; } = new object();

	public int CountOfUnits => Arrangement.UnitCount + _detachedUnits.Count;

	public int CountOfDetachedUnits => _detachedUnits.Count;

	public int CountOfUndetachableNonPlayerUnits => _undetachableNonPlayerUnitCount;

	public int CountOfUnitsWithoutDetachedOnes => Arrangement.UnitCount + _looseDetachedUnits.Count;

	public MBReadOnlyList<IFormationUnit> UnitsWithoutLooseDetachedOnes => Arrangement.GetAllUnits();

	public int CountOfUnitsWithoutLooseDetachedOnes => Arrangement.UnitCount;

	public int CountOfDetachableNonPlayerUnits => Arrangement.UnitCount - ((IsPlayerTroopInFormation || HasPlayerControlledTroop) ? 1 : 0) - CountOfUndetachableNonPlayerUnits;

	public Vec2 OrderPosition => _orderPosition.AsVec2;

	public Vec3 OrderGroundPosition => _orderPosition.GetGroundVec3();

	public bool OrderPositionIsValid => _orderPosition.IsValid;

	public float Depth => Arrangement.Depth;

	public float MinimumWidth => Arrangement.MinimumWidth;

	public float MaximumWidth => Arrangement.MaximumWidth;

	public float UnitDiameter => GetDefaultUnitDiameter(CalculateHasSignificantNumberOfMounted && !(RidingOrder == RidingOrder.RidingOrderDismount));

	public Vec2 CurrentDirection => (QuerySystem.EstimatedDirection * 0.8f + Direction * 0.2f).Normalized();

	public Vec2 SmoothedAverageUnitPosition => _smoothedAverageUnitPosition;

	public MBReadOnlyList<Agent> LooseDetachedUnits => _looseDetachedUnits;

	public MBReadOnlyList<Agent> DetachedUnits => _detachedUnits;

	public AttackEntityOrderSecondaryDetachment AttackEntityOrderSecondaryDetachment { get; private set; }

	public FormationAI AI { get; private set; }

	public Formation TargetFormation
	{
		get
		{
			return _targetFormation;
		}
		private set
		{
			if (_targetFormation != value)
			{
				_targetFormation = value;
				ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.SetTargetFormationIndex(value?.Index ?? (-1));
				});
			}
		}
	}

	public FormationQuerySystem QuerySystem { get; private set; }

	public FormationIntegrityDataGroup CachedFormationIntegrityData { get; private set; }

	public Vec2 CachedAveragePosition { get; private set; }

	public WorldPosition CachedMedianPosition { get; private set; }

	public Vec2 CachedCurrentVelocity { get; private set; }

	public float CachedMovementSpeed { get; private set; } = 1f;

	public float CachedClosestEnemyFormationDistanceSquared { get; private set; }

	public FormationQuerySystem CachedClosestEnemyFormation => _cachedClosestEnemyFormation?.QuerySystem;

	public MBReadOnlyList<IDetachment> Detachments => _detachments;

	public int? OverridenUnitCount { get; private set; }

	public bool IsSpawning { get; private set; }

	public bool IsAITickedAfterSplit { get; set; }

	public bool HasPlayerControlledTroop { get; private set; }

	public bool IsPlayerTroopInFormation { get; private set; }

	public bool ContainsAgentVisuals { get; set; }

	public Agent PlayerOwner
	{
		get
		{
			return _playerOwner;
		}
		set
		{
			_playerOwner = value;
			SetControlledByAI(value == null);
		}
	}

	public string BannerCode
	{
		get
		{
			return _bannerCode;
		}
		set
		{
			_bannerCode = value;
			if (GameNetwork.IsServer)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new InitializeFormation(this, Team.TeamIndex, _bannerCode));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}
	}

	public bool IsSplittableByAI
	{
		get
		{
			if (IsAIOwned)
			{
				return IsConvenientForTransfer;
			}
			return false;
		}
	}

	public bool IsAIOwned
	{
		get
		{
			if (!_enforceNotSplittableByAI)
			{
				if (!IsAIControlled)
				{
					if (!Team.IsPlayerGeneral)
					{
						if (Team.IsPlayerSergeant)
						{
							return PlayerOwner != Agent.Main;
						}
						return true;
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public bool IsConvenientForTransfer
	{
		get
		{
			if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.Siege && Team.Side == BattleSideEnum.Attacker)
			{
				return QuerySystem.InsideCastleUnitCountIncludingUnpositioned == 0;
			}
			return true;
		}
	}

	public Vec2 OrderLocalAveragePosition
	{
		get
		{
			if (_orderLocalAveragePositionIsDirty)
			{
				_orderLocalAveragePositionIsDirty = false;
				_orderLocalAveragePosition = default(Vec2);
				if (UnitsWithoutLooseDetachedOnes.Count > 0)
				{
					int num = 0;
					foreach (IFormationUnit unitsWithoutLooseDetachedOne in UnitsWithoutLooseDetachedOnes)
					{
						Vec2? localPositionOfUnitOrDefault = Arrangement.GetLocalPositionOfUnitOrDefault(unitsWithoutLooseDetachedOne);
						if (localPositionOfUnitOrDefault.HasValue)
						{
							_orderLocalAveragePosition += localPositionOfUnitOrDefault.Value;
							num++;
						}
					}
					if (num > 0)
					{
						_orderLocalAveragePosition *= 1f / (float)num;
					}
				}
			}
			return _orderLocalAveragePosition;
		}
	}

	public FacingOrder FacingOrder { get; private set; }

	public ArrangementOrder ArrangementOrder { get; private set; }

	public FormOrder FormOrder { get; private set; }

	public RidingOrder RidingOrder { get; private set; }

	public FiringOrder FiringOrder { get; private set; }

	private bool IsSimulationFormation => Team == null;

	public bool HasAnyMountedUnit
	{
		get
		{
			if (_overridenHasAnyMountedUnit.HasValue)
			{
				return _overridenHasAnyMountedUnit.Value;
			}
			int num = (int)(QuerySystem.RangedCavalryUnitRatioReadOnly * (float)CountOfUnits + 1E-05f);
			int num2 = (int)(QuerySystem.CavalryUnitRatioReadOnly * (float)CountOfUnits + 1E-05f);
			return num + num2 > 0;
		}
	}

	public float Width
	{
		get
		{
			return Arrangement.Width;
		}
		private set
		{
			Arrangement.Width = value;
		}
	}

	public bool IsDeployment => Mission.Current.Mode == MissionMode.Deployment;

	public FormationClass LogicalClass => _logicalClass;

	public IEnumerable<FormationClass> SecondaryLogicalClasses
	{
		get
		{
			FormationClass primaryLogicalClass = LogicalClass;
			if (primaryLogicalClass == FormationClass.NumberOfAllFormations)
			{
				yield break;
			}
			List<(FormationClass, int)> list = new List<(FormationClass, int)>();
			for (int i = 0; i < _logicalClassCounts.Length; i++)
			{
				if (_logicalClassCounts[i] > 0)
				{
					list.Add(((FormationClass)i, _logicalClassCounts[i]));
				}
			}
			if (list.Count <= 0)
			{
				yield break;
			}
			list.Sort(Comparer<(FormationClass, int)>.Create(((FormationClass fClass, int count) x, (FormationClass fClass, int count) y) => (x.count < y.count) ? 1 : ((x.count > y.count) ? (-1) : 0)));
			foreach (var item in list)
			{
				if (item.Item1 != primaryLogicalClass)
				{
					yield return item.Item1;
				}
			}
		}
	}

	public IFormationArrangement Arrangement
	{
		get
		{
			return _arrangement;
		}
		set
		{
			if (_arrangement != null)
			{
				_arrangement.OnWidthChanged -= Arrangement_OnWidthChanged;
				_arrangement.OnShapeChanged -= Arrangement_OnShapeChanged;
			}
			_arrangement = value;
			if (_arrangement != null)
			{
				_arrangement.OnWidthChanged += Arrangement_OnWidthChanged;
				_arrangement.OnShapeChanged += Arrangement_OnShapeChanged;
			}
			Arrangement_OnWidthChanged();
			Arrangement_OnShapeChanged();
		}
	}

	public FormationClass PhysicalClass => QuerySystem.MainClass;

	public IEnumerable<FormationClass> SecondaryPhysicalClasses
	{
		get
		{
			FormationClass primaryPhysicalClass = PhysicalClass;
			if (primaryPhysicalClass != FormationClass.Infantry && QuerySystem.InfantryUnitRatio > 0f)
			{
				yield return FormationClass.Infantry;
			}
			if (primaryPhysicalClass != FormationClass.Ranged && QuerySystem.RangedUnitRatio > 0f)
			{
				yield return FormationClass.Ranged;
			}
			if (primaryPhysicalClass != FormationClass.Cavalry && QuerySystem.CavalryUnitRatio > 0f)
			{
				yield return FormationClass.Cavalry;
			}
			if (primaryPhysicalClass != FormationClass.HorseArcher && QuerySystem.RangedCavalryUnitRatio > 0f)
			{
				yield return FormationClass.HorseArcher;
			}
		}
	}

	public float Interval
	{
		get
		{
			if (CalculateHasSignificantNumberOfMounted && !(RidingOrder == RidingOrder.RidingOrderDismount))
			{
				return CavalryInterval(UnitSpacing) * Arrangement.IntervalMultiplier;
			}
			return InfantryInterval(UnitSpacing) * Arrangement.IntervalMultiplier;
		}
	}

	public bool CalculateHasSignificantNumberOfMounted
	{
		get
		{
			if (_overridenHasAnyMountedUnit.HasValue)
			{
				return _overridenHasAnyMountedUnit.Value;
			}
			return QuerySystem.CavalryUnitRatio + QuerySystem.RangedCavalryUnitRatio >= 0.1f;
		}
	}

	public float Distance
	{
		get
		{
			if (CalculateHasSignificantNumberOfMounted && !(RidingOrder == RidingOrder.RidingOrderDismount))
			{
				return CavalryDistance(UnitSpacing) * Arrangement.DistanceMultiplier;
			}
			return InfantryDistance(UnitSpacing) * Arrangement.DistanceMultiplier;
		}
	}

	public Vec2 CurrentPosition
	{
		get
		{
			if (Arrangement is ColumnFormation columnFormation)
			{
				if (CountOfUnitsWithoutDetachedOnes <= 0)
				{
					return OrderPosition;
				}
				if ((columnFormation.GetUnit(columnFormation.VanguardFileIndex, 0) ?? columnFormation.Vanguard) is Agent { Position: var position })
				{
					return position.AsVec2;
				}
			}
			return CachedAveragePosition + CurrentDirection.TransformToParentUnitF(-OrderLocalAveragePosition);
		}
	}

	public Agent Captain
	{
		get
		{
			return _captain;
		}
		set
		{
			if (_captain != value)
			{
				_captain = value;
				OnCaptainChanged();
			}
		}
	}

	public float MinimumDistance => GetDefaultMinimumUnitDistance(HasAnyMountedUnit && !(RidingOrder == RidingOrder.RidingOrderDismount));

	public bool IsLoose => ArrangementOrder.GetUnitLooseness(ArrangementOrder.OrderEnum);

	public float MinimumInterval => GetDefaultMinimumUnitInterval(HasAnyMountedUnit && !(RidingOrder == RidingOrder.RidingOrderDismount));

	public float MaximumInterval => GetDefaultUnitInterval(HasAnyMountedUnit && !(RidingOrder == RidingOrder.RidingOrderDismount), ArrangementOrder.GetUnitSpacingOf(ArrangementOrder.OrderEnum));

	public float MaximumDistance => GetDefaultUnitDistance(HasAnyMountedUnit && !(RidingOrder == RidingOrder.RidingOrderDismount), ArrangementOrder.GetUnitSpacingOf(ArrangementOrder.OrderEnum));

	internal bool PostponeCostlyOperations { get; private set; }

	public event Action<Formation, Agent> OnUnitAdded;

	public event Action<Formation, Agent> OnUnitRemoved;

	public event Action<Formation, Agent> OnUnitAttached;

	public event Action<Formation> OnUnitCountChanged;

	public event Action<Formation> OnUnitSpacingChanged;

	public event Action<Formation> OnTick;

	public event Action<Formation> OnWidthChanged;

	public event Action<Formation, MovementOrder.MovementOrderEnum> OnBeforeMovementOrderApplied;

	public event Action<Formation, ArrangementOrder.ArrangementOrderEnum> OnAfterArrangementOrderApplied;

	public Formation(Team team, int index)
	{
		Team = team;
		Index = index;
		FormationIndex = (FormationClass)index;
		IsSpawning = false;
		Reset();
	}

	~Formation()
	{
		if (!IsSimulationFormation)
		{
			_simulationFormationTemp = null;
		}
	}

	bool IFormation.GetIsLocalPositionAvailable(Vec2 localPosition, Vec2? nearestAvailableUnitPositionLocal)
	{
		Vec2 vec = Direction.TransformToParentUnitF(localPosition);
		WorldPosition unitPosition = CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
		unitPosition.SetVec2(OrderPosition + vec);
		WorldPosition nearestAvailableUnitPosition = WorldPosition.Invalid;
		if (nearestAvailableUnitPositionLocal.HasValue)
		{
			vec = Direction.TransformToParentUnitF(nearestAvailableUnitPositionLocal.Value);
			nearestAvailableUnitPosition = CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
			nearestAvailableUnitPosition.SetVec2(OrderPosition + vec);
		}
		float manhattanDistance = TaleWorlds.Library.MathF.Abs(localPosition.x) + TaleWorlds.Library.MathF.Abs(localPosition.y) + (Interval + Distance) * 2f;
		return Mission.Current.IsFormationUnitPositionAvailableMT(ref _orderPosition, ref unitPosition, ref nearestAvailableUnitPosition, manhattanDistance, Team);
	}

	IFormationUnit IFormation.GetClosestUnitTo(Vec2 localPosition, MBList<IFormationUnit> unitsWithSpaces, float? maxDistance)
	{
		Vec2 vec = Direction.TransformToParentUnitF(localPosition);
		Vec2 position = OrderPosition + vec;
		return GetClosestUnitToAux(position, unitsWithSpaces, maxDistance);
	}

	IFormationUnit IFormation.GetClosestUnitTo(IFormationUnit targetUnit, MBList<IFormationUnit> unitsWithSpaces, float? maxDistance)
	{
		return GetClosestUnitToAux(((Agent)targetUnit).Position.AsVec2, unitsWithSpaces, maxDistance);
	}

	void IFormation.SetUnitToFollow(IFormationUnit unit, IFormationUnit toFollow, Vec2 vector)
	{
		Agent obj = unit as Agent;
		Agent followAgent = toFollow as Agent;
		obj.SetColumnwiseFollowAgent(followAgent, ref vector);
	}

	bool IFormation.BatchUnitPositions(MBArrayList<Vec2i> orderedPositionIndices, MBArrayList<Vec2> orderedLocalPositions, MBList2D<int> availabilityTable, MBList2D<WorldPosition> globalPositionTable, int fileCount, int rankCount)
	{
		if (_orderPosition.IsValid && _orderPosition.GetNavMesh() != UIntPtr.Zero)
		{
			Mission.Current.BatchFormationUnitPositions(orderedPositionIndices, orderedLocalPositions, availabilityTable, globalPositionTable, _orderPosition, Direction, fileCount, rankCount, Mission.Current.IsSiegeBattle);
			return true;
		}
		return false;
	}

	public WorldPosition CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache)
	{
		if (!OrderPositionIsValid)
		{
			TaleWorlds.Library.Debug.Print("Formation order position is not valid. Team: " + Team.TeamIndex + ", Formation: " + (int)FormationIndex, 0, TaleWorlds.Library.Debug.DebugColor.Yellow);
		}
		switch (worldPositionEnforcedCache)
		{
		case WorldPosition.WorldPositionEnforcedCache.NavMeshVec3:
			_orderPosition.GetNavMeshVec3();
			break;
		case WorldPosition.WorldPositionEnforcedCache.GroundVec3:
			_orderPosition.GetGroundVec3();
			break;
		}
		return _orderPosition;
	}

	public void SetMovementOrder(MovementOrder input)
	{
		this.OnBeforeMovementOrderApplied?.Invoke(this, input.OrderEnum);
		if (input.OrderEnum == MovementOrder.MovementOrderEnum.Invalid)
		{
			input = MovementOrder.MovementOrderStop;
		}
		bool num = !_movementOrder.AreOrdersPracticallySame(_movementOrder, input, IsAIControlled);
		if (num)
		{
			_movementOrder.OnCancel(this);
		}
		if (num)
		{
			if (MovementOrder.GetMovementOrderDefensivenessChange(_movementOrder.OrderEnum, input.OrderEnum) != 0)
			{
				if (MovementOrder.GetMovementOrderDefensiveness(input.OrderEnum) == 0)
				{
					_formationOrderDefensivenessFactor = 0;
				}
				else
				{
					_formationOrderDefensivenessFactor = MovementOrder.GetMovementOrderDefensiveness(input.OrderEnum) + ArrangementOrder.GetArrangementOrderDefensiveness(ArrangementOrder.OrderEnum);
				}
				UpdateAgentDrivenPropertiesBasedOnOrderDefensiveness();
			}
			_movementOrder = input;
			_movementOrder.OnApply(this);
		}
		SetTargetFormation(null);
	}

	public void SetFacingOrder(FacingOrder order)
	{
		FacingOrder = order;
	}

	public void SetArrangementOrder(ArrangementOrder order)
	{
		if (order.OrderType != ArrangementOrder.OrderType)
		{
			ArrangementOrder.OnCancel(this);
			int arrangementOrderDefensivenessChange = ArrangementOrder.GetArrangementOrderDefensivenessChange(ArrangementOrder.OrderEnum, order.OrderEnum);
			if (arrangementOrderDefensivenessChange != 0 && MovementOrder.GetMovementOrderDefensiveness(_movementOrder.OrderEnum) != 0)
			{
				_formationOrderDefensivenessFactor += arrangementOrderDefensivenessChange;
				UpdateAgentDrivenPropertiesBasedOnOrderDefensiveness();
			}
			ArrangementOrder = order;
			ArrangementOrder.OnApply(this);
			this.OnAfterArrangementOrderApplied?.Invoke(this, ArrangementOrder.OrderEnum);
			if (FormOrder.OrderEnum == FormOrder.FormOrderEnum.Custom && order.OrderEnum != ArrangementOrder.ArrangementOrderEnum.Column)
			{
				SetFormOrder(FormOrder.FormOrderCustom(CalculateDesiredWidth()), updateDesiredFileCount: false);
			}
			QuerySystem.Expire();
			if (CountOfUnitsWithoutLooseDetachedOnes > 0)
			{
				ForceCalculateCaches();
			}
		}
		else
		{
			ArrangementOrder.SoftUpdate(this);
		}
	}

	public void SetFormOrder(FormOrder order, bool updateDesiredFileCount = true)
	{
		if (order.OrderEnum == FormOrder.FormOrderEnum.Custom && updateDesiredFileCount)
		{
			_desiredFileCount = (int)((order.CustomFlankWidth - UnitDiameter) / (UnitDiameter + Interval)) + 1;
		}
		FormOrder = order;
		FormOrder.OnApply(this);
		QuerySystem.Expire();
	}

	public void SetRidingOrder(RidingOrder order)
	{
		if (RidingOrder != order)
		{
			RidingOrder = order;
			ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.SetRidingOrder(order.OrderEnum);
			});
			Arrangement_OnShapeChanged();
		}
	}

	public void SetFiringOrder(FiringOrder order)
	{
		if (FiringOrder != order)
		{
			FiringOrder = order;
			ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.SetFiringOrder(order.OrderEnum);
			});
		}
	}

	public void SetControlledByAI(bool isControlledByAI, bool enforceNotSplittableByAI = false)
	{
		if (IsAIControlled == isControlledByAI)
		{
			return;
		}
		IsAIControlled = isControlledByAI;
		if (IsAIControlled)
		{
			if (AI.ActiveBehavior != null && CountOfUnits > 0)
			{
				bool forceTickOccasionally = Mission.Current.ForceTickOccasionally;
				Mission.Current.ForceTickOccasionally = true;
				BehaviorComponent activeBehavior = AI.ActiveBehavior;
				AI.Tick();
				Mission.Current.ForceTickOccasionally = forceTickOccasionally;
				if (activeBehavior == AI.ActiveBehavior)
				{
					AI.ActiveBehavior.OnBehaviorActivated();
				}
				SetMovementOrder(AI.ActiveBehavior.CurrentOrder);
			}
			_enforceNotSplittableByAI = enforceNotSplittableByAI;
		}
		else
		{
			_enforceNotSplittableByAI = false;
			AI?.ActiveBehavior?.OnLostAIControl();
		}
	}

	public void SetTargetFormation(Formation targetFormation)
	{
		TargetFormation = targetFormation;
	}

	public void OnDeploymentFinished()
	{
		AI?.OnDeploymentFinished();
		OrderController.TryCancelStopOrder(this);
	}

	public void ResetArrangementOrderTickTimer()
	{
		_arrangementOrderTickOccasionallyTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
	}

	public void SetPositioning(WorldPosition? position = null, Vec2? direction = null, int? unitSpacing = null)
	{
		Vec2 orderPosition = OrderPosition;
		Vec2 direction2 = Direction;
		WorldPosition? newPosition = null;
		bool flag = false;
		bool flag2 = false;
		if (position.HasValue && position.Value.IsValid)
		{
			if (!HasBeenPositioned && !IsSimulationFormation)
			{
				HasBeenPositioned = true;
			}
			if (!position.Value.AsVec2.NearlyEquals(OrderPosition, 0.001f))
			{
				if (!Mission.Current.IsPositionInsideBoundaries(position.Value.AsVec2))
				{
					Vec2 closestBoundaryPosition = Mission.Current.GetClosestBoundaryPosition(position.Value.AsVec2);
					if (OrderPosition != closestBoundaryPosition)
					{
						WorldPosition value = position.Value;
						value.SetVec2(closestBoundaryPosition);
						newPosition = value;
					}
				}
				else
				{
					newPosition = position;
				}
				if (!IsSimulationFormation && position.Value.AsVec2.DistanceSquared(OrderPosition) > UnitDiameter * UnitDiameter * 25f)
				{
					Arrangement.UpdateLocalPositionErrors();
				}
			}
		}
		if (direction.HasValue && !Direction.NearlyEquals(direction.Value, 0.01f))
		{
			flag = true;
		}
		if (unitSpacing.HasValue && UnitSpacing != unitSpacing.Value)
		{
			flag2 = true;
			if (!IsSimulationFormation)
			{
				Arrangement.UpdateLocalPositionErrors(recalculateErrors: false);
			}
		}
		if (newPosition.HasValue || flag || flag2)
		{
			Arrangement.BeforeFormationFrameChange();
			if (newPosition.HasValue)
			{
				_orderPosition = newPosition.Value;
			}
			if (flag)
			{
				Direction = direction.Value;
			}
			if (flag2)
			{
				UnitSpacing = unitSpacing.Value;
				this.OnUnitSpacingChanged?.Invoke(this);
				Arrangement_OnShapeChanged();
				Arrangement.AreLocalPositionsDirty = true;
			}
			if (!IsSimulationFormation && Arrangement.IsTurnBackwardsNecessary(orderPosition, newPosition, direction2, flag, direction))
			{
				Arrangement.TurnBackwards();
			}
			Arrangement.OnFormationFrameChanged();
			if (newPosition.HasValue)
			{
				ArrangementOrder.OnOrderPositionChanged(this, orderPosition);
			}
		}
	}

	public int GetCountOfUnitsWithCondition(Func<Agent, bool> function)
	{
		int num = 0;
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			if (function((Agent)allUnit))
			{
				num++;
			}
		}
		foreach (Agent detachedUnit in _detachedUnits)
		{
			if (function(detachedUnit))
			{
				num++;
			}
		}
		return num;
	}

	public ref readonly MovementOrder GetReadonlyMovementOrderReference()
	{
		return ref _movementOrder;
	}

	public Agent GetFirstUnit()
	{
		return GetUnitWithIndex(0);
	}

	public int GetCountOfUnitsBelongingToLogicalClass(FormationClass logicalClass)
	{
		return _logicalClassCounts[(int)logicalClass];
	}

	public int GetCountOfUnitsBelongingToPhysicalClass(FormationClass physicalClass, bool excludeBannerBearers)
	{
		int num = 0;
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			bool flag = false;
			switch (physicalClass)
			{
			case FormationClass.Infantry:
				flag = (excludeBannerBearers ? QueryLibrary.IsInfantryWithoutBanner((Agent)allUnit) : QueryLibrary.IsInfantry((Agent)allUnit));
				break;
			case FormationClass.Ranged:
				flag = (excludeBannerBearers ? QueryLibrary.IsRangedWithoutBanner((Agent)allUnit) : QueryLibrary.IsRanged((Agent)allUnit));
				break;
			case FormationClass.Cavalry:
				flag = (excludeBannerBearers ? QueryLibrary.IsCavalryWithoutBanner((Agent)allUnit) : QueryLibrary.IsCavalry((Agent)allUnit));
				break;
			case FormationClass.HorseArcher:
				flag = (excludeBannerBearers ? QueryLibrary.IsRangedCavalryWithoutBanner((Agent)allUnit) : QueryLibrary.IsRangedCavalry((Agent)allUnit));
				break;
			}
			if (flag)
			{
				num++;
			}
		}
		foreach (Agent detachedUnit in _detachedUnits)
		{
			bool flag2 = false;
			switch (physicalClass)
			{
			case FormationClass.Infantry:
				flag2 = (excludeBannerBearers ? QueryLibrary.IsInfantryWithoutBanner(detachedUnit) : QueryLibrary.IsInfantry(detachedUnit));
				break;
			case FormationClass.Ranged:
				flag2 = (excludeBannerBearers ? QueryLibrary.IsRangedWithoutBanner(detachedUnit) : QueryLibrary.IsRanged(detachedUnit));
				break;
			case FormationClass.Cavalry:
				flag2 = (excludeBannerBearers ? QueryLibrary.IsCavalryWithoutBanner(detachedUnit) : QueryLibrary.IsCavalry(detachedUnit));
				break;
			case FormationClass.HorseArcher:
				flag2 = (excludeBannerBearers ? QueryLibrary.IsRangedCavalryWithoutBanner(detachedUnit) : QueryLibrary.IsRangedCavalry(detachedUnit));
				break;
			}
			if (flag2)
			{
				num++;
			}
		}
		return num;
	}

	public void SetSpawnIndex(int value = 0)
	{
		_currentSpawnIndex = value;
	}

	public int GetNextSpawnIndex()
	{
		int currentSpawnIndex = _currentSpawnIndex;
		_currentSpawnIndex++;
		return currentSpawnIndex;
	}

	public Agent GetUnitWithIndex(int unitIndex)
	{
		if (Arrangement.GetAllUnits().Count > unitIndex)
		{
			return (Agent)Arrangement.GetAllUnits()[unitIndex];
		}
		unitIndex -= Arrangement.GetAllUnits().Count;
		if (_detachedUnits.Count > unitIndex)
		{
			return _detachedUnits[unitIndex];
		}
		return null;
	}

	public Vec2 GetAveragePositionOfUnits(bool excludeDetachedUnits, bool excludePlayer)
	{
		int num = (excludeDetachedUnits ? CountOfUnitsWithoutDetachedOnes : CountOfUnits);
		if (num > 0)
		{
			Vec2 zero = Vec2.Zero;
			foreach (Agent allUnit in Arrangement.GetAllUnits())
			{
				if (!excludePlayer || !allUnit.IsMainAgent)
				{
					zero += allUnit.Position.AsVec2;
				}
				else
				{
					num--;
				}
			}
			if (excludeDetachedUnits)
			{
				foreach (Agent looseDetachedUnit in _looseDetachedUnits)
				{
					zero += looseDetachedUnit.Position.AsVec2;
				}
			}
			else
			{
				foreach (Agent detachedUnit in _detachedUnits)
				{
					zero += detachedUnit.Position.AsVec2;
				}
			}
			if (num > 0)
			{
				return zero * (1f / (float)num);
			}
		}
		return Vec2.Invalid;
	}

	public Agent GetMedianAgent(bool excludeDetachedUnits, bool excludePlayer, Vec2 averagePosition)
	{
		excludeDetachedUnits = excludeDetachedUnits && CountOfUnitsWithoutDetachedOnes > 0;
		excludePlayer = excludePlayer && (CountOfUndetachableNonPlayerUnits > 0 || CountOfDetachableNonPlayerUnits > 0);
		float num = float.MaxValue;
		Agent result = null;
		foreach (Agent allUnit in Arrangement.GetAllUnits())
		{
			if (!excludePlayer || !allUnit.IsMainAgent)
			{
				float num2 = allUnit.Position.AsVec2.DistanceSquared(averagePosition);
				if (num2 <= num)
				{
					result = allUnit;
					num = num2;
				}
			}
		}
		if (excludeDetachedUnits)
		{
			foreach (Agent looseDetachedUnit in _looseDetachedUnits)
			{
				float num3 = looseDetachedUnit.Position.AsVec2.DistanceSquared(averagePosition);
				if (num3 <= num)
				{
					result = looseDetachedUnit;
					num = num3;
				}
			}
		}
		else
		{
			foreach (Agent detachedUnit in _detachedUnits)
			{
				float num4 = detachedUnit.Position.AsVec2.DistanceSquared(averagePosition);
				if (num4 <= num)
				{
					result = detachedUnit;
					num = num4;
				}
			}
		}
		return result;
	}

	public Agent.UnderAttackType GetUnderAttackTypeOfUnits(float timeLimit = 3f)
	{
		float num = float.MinValue;
		float num2 = float.MinValue;
		timeLimit = MBCommon.GetTotalMissionTime() - timeLimit;
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			num = TaleWorlds.Library.MathF.Max(num, ((Agent)allUnit).LastMeleeHitTime);
			num2 = TaleWorlds.Library.MathF.Max(num2, ((Agent)allUnit).LastRangedHitTime);
			if (num2 >= 0f && num2 > timeLimit)
			{
				return Agent.UnderAttackType.UnderRangedAttack;
			}
		}
		for (int i = 0; i < _detachedUnits.Count; i++)
		{
			num = TaleWorlds.Library.MathF.Max(num, _detachedUnits[i].LastMeleeHitTime);
			num2 = TaleWorlds.Library.MathF.Max(num2, _detachedUnits[i].LastRangedHitTime);
			if (num2 >= 0f && num2 > timeLimit)
			{
				return Agent.UnderAttackType.UnderRangedAttack;
			}
		}
		if (num >= 0f && num > timeLimit)
		{
			return Agent.UnderAttackType.UnderMeleeAttack;
		}
		return Agent.UnderAttackType.NotUnderAttack;
	}

	public Agent.MovementBehaviorType GetMovementTypeOfUnits()
	{
		float curMissionTime = MBCommon.GetTotalMissionTime();
		int retreatingCount = 0;
		int attackingCount = 0;
		ApplyActionOnEachUnit(delegate(Agent agent)
		{
			if (agent.IsAIControlled && (agent.IsRetreating() || (agent.Formation != null && agent.Formation._movementOrder.OrderType == OrderType.Retreat)))
			{
				retreatingCount++;
			}
			if (curMissionTime - agent.LastMeleeAttackTime < 3f)
			{
				attackingCount++;
			}
		});
		if (CountOfUnits > 0 && (float)retreatingCount / (float)CountOfUnits > 0.3f)
		{
			return Agent.MovementBehaviorType.Flee;
		}
		if (attackingCount > 0)
		{
			return Agent.MovementBehaviorType.Engaged;
		}
		return Agent.MovementBehaviorType.Idle;
	}

	public IEnumerable<Agent> GetUnitsWithoutDetachedOnes()
	{
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			yield return allUnit as Agent;
		}
		for (int i = 0; i < _looseDetachedUnits.Count; i++)
		{
			yield return _looseDetachedUnits[i];
		}
	}

	public Vec2 GetWallDirectionOfRelativeFormationLocation(Agent unit)
	{
		if (unit.IsDetachedFromFormation)
		{
			return Vec2.Invalid;
		}
		Vec2? localWallDirectionOfRelativeFormationLocation = Arrangement.GetLocalWallDirectionOfRelativeFormationLocation(unit);
		if (localWallDirectionOfRelativeFormationLocation.HasValue)
		{
			return Direction.TransformToParentUnitF(localWallDirectionOfRelativeFormationLocation.Value);
		}
		return Vec2.Invalid;
	}

	public Vec2 GetDirectionOfUnit(Agent unit)
	{
		if (unit.IsDetachedFromFormation)
		{
			return unit.GetMovementDirection();
		}
		Vec2? localDirectionOfUnitOrDefault = Arrangement.GetLocalDirectionOfUnitOrDefault(unit);
		if (localDirectionOfUnitOrDefault.HasValue)
		{
			return Direction.TransformToParentUnitF(localDirectionOfUnitOrDefault.Value);
		}
		return unit.GetMovementDirection();
	}

	private WorldPosition GetOrderPositionOfUnitAux(Agent unit)
	{
		WorldPosition? worldPositionOfUnitOrDefault = Arrangement.GetWorldPositionOfUnitOrDefault(unit);
		if (worldPositionOfUnitOrDefault.HasValue)
		{
			return worldPositionOfUnitOrDefault.Value;
		}
		if (!OrderPositionIsValid)
		{
			WorldPosition worldPosition = unit.GetWorldPosition();
			TaleWorlds.Library.Debug.Print(string.Concat("Formation order position is not valid. Team: ", Team.TeamIndex, ", Formation: ", (int)FormationIndex, "Unit Pos: ", worldPosition.GetGroundVec3(), "Mission Mode: ", Mission.Current.Mode), 0, TaleWorlds.Library.Debug.DebugColor.Yellow);
		}
		WorldPosition unitPosition = _movementOrder.CreateNewOrderWorldPositionMT(this, WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
		if (unit.Mission.IsFormationUnitPositionAvailable(ref unitPosition, Team))
		{
			return unitPosition;
		}
		return unit.GetWorldPosition();
	}

	public MovementOrder.MovementStateEnum GetMovementState()
	{
		return _movementOrder.MovementState;
	}

	public WorldPosition GetOrderPositionOfUnit(Agent unit)
	{
		if (_movementOrder.OrderEnum == MovementOrder.MovementOrderEnum.Follow && _movementOrder._targetAgent != null && _movementOrder._targetAgent == unit)
		{
			return unit.GetWorldPosition();
		}
		if (unit.IsDetachedFromFormation && (_movementOrder.MovementState != MovementOrder.MovementStateEnum.Charge || !unit.Detachment.IsLoose || unit.Mission.Mode == MissionMode.Deployment || _movementOrder.CreateNewOrderWorldPositionMT(this, WorldPosition.WorldPositionEnforcedCache.None).IsValid))
		{
			return GetDetachmentFrame(unit)?.Origin ?? WorldPosition.Invalid;
		}
		if (unit.Mission.IsDeploymentFinished && unit.GetAgentFlags().HasAnyFlag(AgentFlag.UnreachableViaNavMesh))
		{
			return WorldPosition.Invalid;
		}
		switch (_movementOrder.MovementState)
		{
		case MovementOrder.MovementStateEnum.Charge:
			if (unit.Mission.Mode == MissionMode.Deployment)
			{
				return GetOrderPositionOfUnitAux(unit);
			}
			if (!OrderPositionIsValid)
			{
				WorldPosition worldPosition = unit.GetWorldPosition();
				TaleWorlds.Library.Debug.Print("Formation order position is not valid. Team: " + Team.TeamIndex + ", Formation: " + (int)FormationIndex + "Unit Pos: " + worldPosition.GetGroundVec3(), 0, TaleWorlds.Library.Debug.DebugColor.Yellow);
			}
			return _movementOrder.CreateNewOrderWorldPositionMT(this, WorldPosition.WorldPositionEnforcedCache.None);
		case MovementOrder.MovementStateEnum.Hold:
			return GetOrderPositionOfUnitAux(unit);
		case MovementOrder.MovementStateEnum.Retreat:
			return WorldPosition.Invalid;
		case MovementOrder.MovementStateEnum.StandGround:
			return unit.GetWorldPosition();
		default:
			TaleWorlds.Library.Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Formation.cs", "GetOrderPositionOfUnit", 1607);
			return WorldPosition.Invalid;
		}
	}

	public Vec2 GetCurrentGlobalPositionOfUnit(Agent unit, bool blendWithOrderDirection)
	{
		if (unit.IsDetachedFromFormation)
		{
			return unit.Position.AsVec2;
		}
		Vec2? localPositionOfUnitOrDefaultWithAdjustment = Arrangement.GetLocalPositionOfUnitOrDefaultWithAdjustment(unit, blendWithOrderDirection ? ((QuerySystem.EstimatedInterval - Interval) * 0.9f) : 0f);
		if (localPositionOfUnitOrDefaultWithAdjustment.HasValue)
		{
			return (blendWithOrderDirection ? CurrentDirection : QuerySystem.EstimatedDirection).TransformToParentUnitF(localPositionOfUnitOrDefaultWithAdjustment.Value) + CurrentPosition;
		}
		return unit.Position.AsVec2;
	}

	public float GetAverageMaximumMovementSpeedOfUnits()
	{
		if (CountOfUnitsWithoutDetachedOnes == 0)
		{
			return 0.1f;
		}
		float num = 0f;
		foreach (Agent unitsWithoutDetachedOne in GetUnitsWithoutDetachedOnes())
		{
			num += unitsWithoutDetachedOne.GetMaximumForwardUnlimitedSpeed();
		}
		return num / (float)CountOfUnitsWithoutDetachedOnes;
	}

	private void CacheMovementSpeedOfUnits()
	{
		ArrangementOrder.GetMovementSpeedRestriction(out var runRestriction, out var walkRestriction);
		if (!runRestriction.HasValue && !walkRestriction.HasValue)
		{
			runRestriction = 1f;
		}
		if (walkRestriction.HasValue)
		{
			if (CountOfUnits == 0)
			{
				CachedMovementSpeed = 0.1f;
				return;
			}
			IEnumerable<Agent> source;
			if (CountOfUnitsWithoutDetachedOnes != 0)
			{
				source = GetUnitsWithoutDetachedOnes();
			}
			else
			{
				IEnumerable<Agent> detachedUnits = _detachedUnits;
				source = detachedUnits;
			}
			float num = source.Min((Agent u) => u.WalkSpeedCached);
			CachedMovementSpeed = num * walkRestriction.Value;
			return;
		}
		if (CountOfUnits == 0)
		{
			CachedMovementSpeed = 0.1f;
			return;
		}
		IEnumerable<Agent> source2;
		if (CountOfUnitsWithoutDetachedOnes != 0)
		{
			source2 = GetUnitsWithoutDetachedOnes();
		}
		else
		{
			IEnumerable<Agent> detachedUnits = _detachedUnits;
			source2 = detachedUnits;
		}
		float num2 = source2.Average((Agent u) => u.GetMaximumForwardUnlimitedSpeed());
		FormationIntegrityDataGroup cachedFormationIntegrityData = CachedFormationIntegrityData;
		if (cachedFormationIntegrityData.DeviationOfPositionsExcludeFarAgents < cachedFormationIntegrityData.AverageMaxUnlimitedSpeedExcludeFarAgents * 0.5f)
		{
			CachedMovementSpeed = num2 * runRestriction.Value;
		}
		else
		{
			CachedMovementSpeed = num2;
		}
	}

	private void CacheClosestEnemyFormation()
	{
		float num = float.MaxValue;
		_cachedClosestEnemyFormation = null;
		foreach (Team team in Mission.Current.Teams)
		{
			if (!team.IsEnemyOf(Team))
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					float num2 = item.CachedMedianPosition.GetNavMeshVec3().DistanceSquared(new Vec3(CachedAveragePosition, CachedMedianPosition.GetNavMeshZ()));
					if (num2 < num)
					{
						num = num2;
						_cachedClosestEnemyFormation = item;
					}
				}
			}
		}
		CachedClosestEnemyFormationDistanceSquared = num;
	}

	private void CacheFormationIntegrityData()
	{
		bool flag = false;
		if (CountOfUnitsWithoutLooseDetachedOnes > 0)
		{
			float num = 0f;
			MBReadOnlyList<IFormationUnit> allUnits = Arrangement.GetAllUnits();
			int num2 = 0;
			float distanceBetweenAgentsAdjustment = QuerySystem.EstimatedInterval - Interval;
			foreach (Agent item in allUnits)
			{
				Vec2? localPositionOfUnitOrDefaultWithAdjustment = Arrangement.GetLocalPositionOfUnitOrDefaultWithAdjustment(item, distanceBetweenAgentsAdjustment);
				if (localPositionOfUnitOrDefaultWithAdjustment.HasValue)
				{
					Vec2 vec = QuerySystem.EstimatedDirection.TransformToParentUnitF(localPositionOfUnitOrDefaultWithAdjustment.Value) + CurrentPosition;
					num2++;
					num += (vec - item.Position.AsVec2).LengthSquared;
				}
			}
			if (num2 > 0)
			{
				float num3 = num / (float)num2 * 4f;
				float num4 = 0f;
				float num5 = 0f;
				Vec2 zero = Vec2.Zero;
				float num6 = 0f;
				num2 = 0;
				foreach (Agent item2 in allUnits)
				{
					Vec2? localPositionOfUnitOrDefaultWithAdjustment2 = Arrangement.GetLocalPositionOfUnitOrDefaultWithAdjustment(item2, distanceBetweenAgentsAdjustment);
					if (!localPositionOfUnitOrDefaultWithAdjustment2.HasValue)
					{
						continue;
					}
					float lengthSquared = (QuerySystem.EstimatedDirection.TransformToParentUnitF(localPositionOfUnitOrDefaultWithAdjustment2.Value) + CurrentPosition - item2.Position.AsVec2).LengthSquared;
					if (lengthSquared < num3)
					{
						if (lengthSquared > num5)
						{
							num5 = lengthSquared;
						}
						num4 += lengthSquared;
						zero += item2.AverageVelocity.AsVec2;
						num6 += item2.GetMaximumForwardUnlimitedSpeed();
						num2++;
					}
				}
				if (num2 > 0)
				{
					zero *= 1f / (float)num2;
					num4 /= (float)num2;
					num6 /= (float)num2;
					CachedFormationIntegrityData = new FormationIntegrityDataGroup(zero, TaleWorlds.Library.MathF.Sqrt(num4), TaleWorlds.Library.MathF.Sqrt(num5), num6);
					flag = true;
				}
			}
		}
		if (!flag)
		{
			CachedFormationIntegrityData = new FormationIntegrityDataGroup(Vec2.Zero, 0f, 0f, 0f);
		}
	}

	private void CacheAverageAndMedianPositionAndVelocity()
	{
		Vec2 vec = ((CountOfUnitsWithoutDetachedOnes > 1) ? GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: true) : ((CountOfUnitsWithoutDetachedOnes > 0) ? GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false) : OrderPosition));
		float currentTime = Mission.Current.CurrentTime;
		float num = currentTime - _lastAveragePositionCacheTime;
		if (num > 0f)
		{
			CachedCurrentVelocity = (vec - CachedAveragePosition) * (1f / num);
		}
		_lastAveragePositionCacheTime = currentTime;
		CachedAveragePosition = vec;
		CachedMedianPosition = ((CountOfUnitsWithoutDetachedOnes != 0) ? ((CountOfUnitsWithoutDetachedOnes == 1) ? GetMedianAgent(excludeDetachedUnits: true, excludePlayer: false, CachedAveragePosition).GetWorldPosition() : GetMedianAgent(excludeDetachedUnits: true, excludePlayer: true, CachedAveragePosition).GetWorldPosition()) : ((CountOfUnits == 0) ? CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None) : ((CountOfUnits == 1) ? GetFirstUnit().GetWorldPosition() : GetMedianAgent(excludeDetachedUnits: false, excludePlayer: true, CachedAveragePosition).GetWorldPosition())));
	}

	public float GetFormationPower()
	{
		float sum = 0f;
		ApplyActionOnEachUnit(delegate(Agent agent)
		{
			sum += agent.CharacterPowerCached;
		});
		return sum;
	}

	public float GetFormationMeleeFightingPower()
	{
		float sum = 0f;
		ApplyActionOnEachUnit(delegate(Agent agent)
		{
			sum += agent.CharacterPowerCached * ((FormationIndex == FormationClass.Ranged || FormationIndex == FormationClass.HorseArcher) ? 0.4f : 1f);
		});
		return sum;
	}

	internal IDetachment GetDetachmentForDebug(Agent agent)
	{
		return Detachments.FirstOrDefault((IDetachment d) => d.IsAgentUsingOrInterested(agent));
	}

	public WorldFrame? GetDetachmentFrame(Agent agent)
	{
		return agent.Detachment.GetAgentFrame(agent);
	}

	public Vec2 GetMiddleFrontUnitPositionOffset()
	{
		Vec2 localPositionOfReservedUnitPosition = Arrangement.GetLocalPositionOfReservedUnitPosition();
		return Direction.TransformToParentUnitF(localPositionOfReservedUnitPosition);
	}

	public List<IFormationUnit> GetUnitsToPopWithReferencePosition(int count, Vec3 targetPosition)
	{
		int num = TaleWorlds.Library.MathF.Min(count, Arrangement.UnitCount);
		List<IFormationUnit> list = ((num == 0) ? new List<IFormationUnit>() : Arrangement.GetUnitsToPop(num, targetPosition));
		int num2 = count - list.Count;
		if (num2 > 0)
		{
			List<Agent> list2 = _looseDetachedUnits.Take(num2).ToList();
			num2 -= list2.Count;
			list.AddRange(list2);
		}
		if (num2 > 0)
		{
			IEnumerable<Agent> enumerable = _detachedUnits.Take(num2);
			num2 -= enumerable.Count();
			list.AddRange(enumerable);
		}
		return list;
	}

	public List<IFormationUnit> GetUnitsToPop(int count)
	{
		int num = TaleWorlds.Library.MathF.Min(count, Arrangement.UnitCount);
		List<IFormationUnit> list = ((num == 0) ? new List<IFormationUnit>() : Arrangement.GetUnitsToPop(num));
		int num2 = count - list.Count;
		if (num2 > 0)
		{
			List<Agent> list2 = _looseDetachedUnits.Take(num2).ToList();
			num2 -= list2.Count;
			list.AddRange(list2);
		}
		if (num2 > 0)
		{
			IEnumerable<Agent> enumerable = _detachedUnits.Take(num2);
			num2 -= enumerable.Count();
			list.AddRange(enumerable);
		}
		return list;
	}

	public IEnumerable<(WorldPosition, Vec2)> GetUnavailableUnitPositionsAccordingToNewOrder(Formation simulationFormation, in WorldPosition position, in Vec2 direction, float width, int unitSpacing)
	{
		return GetUnavailableUnitPositionsAccordingToNewOrder(this, simulationFormation, position, direction, Arrangement, width, unitSpacing);
	}

	public void GetUnitSpawnFrameWithIndex(int unitIndex, in WorldPosition formationPosition, in Vec2 formationDirection, float width, int unitCount, int unitSpacing, bool isMountedFormation, out WorldPosition? unitSpawnPosition, out Vec2? unitSpawnDirection)
	{
		GetUnitPositionWithIndexAccordingToNewOrder(null, unitIndex, in formationPosition, in formationDirection, Arrangement, width, unitSpacing, unitCount, isMountedFormation, Index, out unitSpawnPosition, out unitSpawnDirection, out var _);
	}

	public void GetUnitPositionWithIndexAccordingToNewOrder(Formation simulationFormation, int unitIndex, in WorldPosition formationPosition, in Vec2 formationDirection, float width, int unitSpacing, out WorldPosition? unitSpawnPosition, out Vec2? unitSpawnDirection)
	{
		GetUnitPositionWithIndexAccordingToNewOrder(simulationFormation, unitIndex, in formationPosition, in formationDirection, Arrangement, width, unitSpacing, Arrangement.UnitCount, HasAnyMountedUnit, Index, out unitSpawnPosition, out unitSpawnDirection, out var _);
	}

	public void GetUnitPositionWithIndexAccordingToNewOrder(Formation simulationFormation, int unitIndex, in WorldPosition formationPosition, in Vec2 formationDirection, float width, int unitSpacing, int overridenUnitCount, out WorldPosition? unitPosition, out Vec2? unitDirection)
	{
		GetUnitPositionWithIndexAccordingToNewOrder(simulationFormation, unitIndex, in formationPosition, in formationDirection, Arrangement, width, unitSpacing, overridenUnitCount, HasAnyMountedUnit, Index, out unitPosition, out unitDirection, out var _);
	}

	public void GetUnitPositionWithIndexAccordingToNewOrder(Formation simulationFormation, int unitIndex, in WorldPosition formationPosition, in Vec2 formationDirection, float width, int unitSpacing, out WorldPosition? unitSpawnPosition, out Vec2? unitSpawnDirection, out float actualWidth)
	{
		GetUnitPositionWithIndexAccordingToNewOrder(simulationFormation, unitIndex, in formationPosition, in formationDirection, Arrangement, width, unitSpacing, Arrangement.UnitCount, HasAnyMountedUnit, Index, out unitSpawnPosition, out unitSpawnDirection, out actualWidth);
	}

	public bool HasUnitsWithCondition(Func<Agent, bool> function)
	{
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			if (function((Agent)allUnit))
			{
				return true;
			}
		}
		for (int i = 0; i < _detachedUnits.Count; i++)
		{
			if (function(_detachedUnits[i]))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasUnitsWithCondition(Func<Agent, bool> function, out Agent result)
	{
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			if (function((Agent)allUnit))
			{
				result = (Agent)allUnit;
				return true;
			}
		}
		for (int i = 0; i < _detachedUnits.Count; i++)
		{
			if (function(_detachedUnits[i]))
			{
				result = _detachedUnits[i];
				return true;
			}
		}
		result = null;
		return false;
	}

	public bool HasAnyEnemyFormationsThatIsNotEmpty()
	{
		foreach (Team team in Mission.Current.Teams)
		{
			if (!team.IsEnemyOf(Team))
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool HasUnitWithConditionLimitedRandom(Func<Agent, bool> function, int startingIndex, int willBeCheckedUnitCount, out Agent resultAgent)
	{
		int unitCount = Arrangement.UnitCount;
		int count = _detachedUnits.Count;
		if (unitCount + count <= willBeCheckedUnitCount)
		{
			return HasUnitsWithCondition(function, out resultAgent);
		}
		for (int i = 0; i < willBeCheckedUnitCount; i++)
		{
			if (startingIndex < unitCount)
			{
				int index = MBRandom.RandomInt(unitCount);
				if (function((Agent)Arrangement.GetAllUnits()[index]))
				{
					resultAgent = (Agent)Arrangement.GetAllUnits()[index];
					return true;
				}
			}
			else if (count > 0)
			{
				int index = MBRandom.RandomInt(count);
				if (function(_detachedUnits[index]))
				{
					resultAgent = _detachedUnits[index];
					return true;
				}
			}
		}
		resultAgent = null;
		return false;
	}

	public int[] CollectUnitIndices()
	{
		if (_agentIndicesCache == null || _agentIndicesCache.Length != CountOfUnits)
		{
			_agentIndicesCache = new int[CountOfUnits];
		}
		int num = 0;
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			_agentIndicesCache[num] = ((Agent)allUnit).Index;
			num++;
		}
		for (int i = 0; i < _detachedUnits.Count; i++)
		{
			_agentIndicesCache[num] = _detachedUnits[i].Index;
			num++;
		}
		return _agentIndicesCache;
	}

	public void ApplyActionOnEachUnit(Action<Agent> action, Agent ignoreAgent = null)
	{
		if (ignoreAgent == null)
		{
			foreach (Agent allUnit in Arrangement.GetAllUnits())
			{
				action(allUnit);
			}
			for (int i = 0; i < _detachedUnits.Count; i++)
			{
				action(_detachedUnits[i]);
			}
			return;
		}
		foreach (Agent allUnit2 in Arrangement.GetAllUnits())
		{
			if (allUnit2 != ignoreAgent)
			{
				action(allUnit2);
			}
		}
		for (int j = 0; j < _detachedUnits.Count; j++)
		{
			Agent agent2 = _detachedUnits[j];
			if (agent2 != ignoreAgent)
			{
				action(agent2);
			}
		}
	}

	public void ApplyActionOnEachAttachedUnit(Action<Agent> action)
	{
		foreach (Agent allUnit in Arrangement.GetAllUnits())
		{
			action(allUnit);
		}
	}

	public void ApplyActionOnEachDetachedUnit(Action<Agent> action)
	{
		for (int i = 0; i < _detachedUnits.Count; i++)
		{
			action(_detachedUnits[i]);
		}
	}

	public void ApplyActionOnEachUnitViaBackupList(Action<Agent> action)
	{
		if (Arrangement.GetAllUnits().Count > 0)
		{
			IFormationUnit[] array = Arrangement.GetAllUnits().ToArray();
			foreach (IFormationUnit formationUnit in array)
			{
				action((Agent)formationUnit);
			}
		}
		if (_detachedUnits.Count > 0)
		{
			Agent[] array2 = _detachedUnits.ToArray();
			foreach (Agent obj in array2)
			{
				action(obj);
			}
		}
	}

	public void ApplyActionOnEachUnit(Action<Agent, List<WorldPosition>> action, List<WorldPosition> list)
	{
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			action((Agent)allUnit, list);
		}
		for (int i = 0; i < _detachedUnits.Count; i++)
		{
			action(_detachedUnits[i], list);
		}
	}

	public int CountUnitsOnNavMeshIDMod10(int navMeshID, bool includeOnlyPositionedUnits)
	{
		int num = 0;
		foreach (IFormationUnit allUnit in Arrangement.GetAllUnits())
		{
			if (((Agent)allUnit).GetCurrentNavigationFaceId() % 10 == navMeshID && (!includeOnlyPositionedUnits || Arrangement.GetUnpositionedUnits() == null || Arrangement.GetUnpositionedUnits().IndexOf(allUnit) < 0))
			{
				num++;
			}
		}
		if (!includeOnlyPositionedUnits)
		{
			foreach (Agent detachedUnit in _detachedUnits)
			{
				if (detachedUnit.GetCurrentNavigationFaceId() % 10 == navMeshID)
				{
					num++;
				}
			}
		}
		return num;
	}

	public void OnAgentControllerChanged(Agent agent, AgentControllerType oldController)
	{
		AgentControllerType controller = agent.Controller;
		if (oldController != AgentControllerType.Player && controller == AgentControllerType.Player)
		{
			HasPlayerControlledTroop = true;
			if (!GameNetwork.IsMultiplayer)
			{
				TryRelocatePlayerUnit();
			}
			if (!agent.IsDetachableFromFormation)
			{
				OnUndetachableNonPlayerUnitRemoved(agent);
			}
		}
		else if (oldController == AgentControllerType.Player && controller != AgentControllerType.Player)
		{
			HasPlayerControlledTroop = false;
			if (!agent.IsDetachableFromFormation)
			{
				OnUndetachableNonPlayerUnitAdded(agent);
			}
		}
	}

	public void OnMassUnitTransferStart()
	{
		PostponeCostlyOperations = true;
	}

	public void OnMassUnitTransferEnd()
	{
		ReapplyFormOrder();
		QuerySystem.Expire();
		Team.QuerySystem.ExpireAfterUnitAddRemove();
		if (_logicalClassNeedsUpdate)
		{
			CalculateLogicalClass();
		}
		if (CountOfUnits == 0)
		{
			RepresentativeClass = FormationClass.NumberOfAllFormations;
		}
		if (Mission.Current.IsTeleportingAgents)
		{
			SetPositioning(_orderPosition);
			Arrangement.OnFormationFrameChanged();
			ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
			});
			SetHasPendingUnitPositions(hasPendingUnitPositions: false);
		}
		PostponeCostlyOperations = false;
	}

	public void OnBatchUnitRemovalStart()
	{
		PostponeCostlyOperations = true;
		Arrangement.OnBatchRemoveStart();
	}

	public void OnBatchUnitRemovalEnd()
	{
		Arrangement.OnBatchRemoveEnd();
		if (PostponeCostlyOperations)
		{
			ReapplyFormOrder();
			QuerySystem.ExpireAfterUnitAddRemove();
			Team.QuerySystem.ExpireAfterUnitAddRemove();
			if (_logicalClassNeedsUpdate)
			{
				CalculateLogicalClass();
			}
			PostponeCostlyOperations = false;
		}
	}

	public void OnUnitAddedOrRemoved()
	{
		if (!PostponeCostlyOperations)
		{
			ReapplyFormOrder();
			QuerySystem.ExpireAfterUnitAddRemove();
			Team?.QuerySystem.ExpireAfterUnitAddRemove();
		}
		CacheAverageAndMedianPositionAndVelocity();
		CacheMovementSpeedOfUnits();
		CacheFormationIntegrityData();
		float currentTime = Mission.Current.CurrentTime;
		_cachedMovementSpeedUpdateTimer.Reset(currentTime);
		_cachedFormationIntegrityDataUpdateTimer.Reset(currentTime);
		_cachedPositionAndVelocityUpdateTimer.Reset(currentTime);
		this.OnUnitCountChanged?.Invoke(this);
	}

	public void OnAgentLostMount(Agent agent)
	{
		if (!agent.IsDetachedFromFormation)
		{
			_arrangement.OnUnitLostMount(agent);
		}
	}

	public void OnFormationDispersed()
	{
		Arrangement.OnFormationDispersed();
		ApplyActionOnEachUnit(delegate(Agent agent)
		{
			agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
		});
		SetHasPendingUnitPositions(hasPendingUnitPositions: false);
	}

	public void OnUnitDetachmentChanged(Agent unit, bool isOldDetachmentLoose, bool isNewDetachmentLoose)
	{
		if (isOldDetachmentLoose && !isNewDetachmentLoose)
		{
			_looseDetachedUnits.Remove(unit);
		}
		else if (!isOldDetachmentLoose && isNewDetachmentLoose)
		{
			_looseDetachedUnits.Add(unit);
		}
	}

	public void OnUndetachableNonPlayerUnitAdded(Agent unit)
	{
		if (unit.Formation == this && !unit.IsPlayerControlled)
		{
			_undetachableNonPlayerUnitCount++;
		}
	}

	public void OnUndetachableNonPlayerUnitRemoved(Agent unit)
	{
		if (unit.Formation == this && !unit.IsPlayerControlled)
		{
			_undetachableNonPlayerUnitCount--;
		}
	}

	public void ResetMovementOrderPositionCache()
	{
		_movementOrder.ResetPositionCache();
	}

	public void Reset()
	{
		Arrangement = new LineFormation(this);
		_arrangementOrderTickOccasionallyTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
		_arrangementTickOccasionallyTimer = new Timer(Mission.Current.CurrentTime, 0.5f);
		_cachedFormationIntegrityDataUpdateTimer = new Timer(Mission.Current.CurrentTime, 0.9f + MBRandom.RandomFloat * 0.2f);
		_cachedPositionAndVelocityUpdateTimer = new Timer(Mission.Current.CurrentTime, 0.075f + MBRandom.RandomFloat * 0.05f);
		_cachedMovementSpeedUpdateTimer = new Timer(Mission.Current.CurrentTime, 1.9f + MBRandom.RandomFloat * 0.2f);
		_cachedClosestEnemyFormationUpdateTimer = new Timer(Mission.Current.CurrentTime, 1.4f + MBRandom.RandomFloat * 0.2f);
		ResetAux();
		FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		_enforceNotSplittableByAI = false;
		ContainsAgentVisuals = false;
		PlayerOwner = null;
	}

	public IEnumerable<Formation> Split(int count = 2)
	{
		foreach (Formation item in Team.FormationsIncludingEmpty)
		{
			item.PostponeCostlyOperations = true;
		}
		IEnumerable<Formation> enumerable = Team.MasterOrderController.SplitFormation(this, count);
		if (enumerable.Count() > 1 && Team != null)
		{
			foreach (Formation item2 in enumerable)
			{
				item2.QuerySystem.Expire();
			}
		}
		foreach (Formation item3 in Team.FormationsIncludingEmpty)
		{
			item3.CalculateLogicalClass();
			item3.PostponeCostlyOperations = false;
		}
		if (CountOfUnits == 0)
		{
			RepresentativeClass = FormationClass.NumberOfAllFormations;
		}
		return enumerable;
	}

	public void TransferUnits(Formation target, int unitCount)
	{
		PostponeCostlyOperations = true;
		target.PostponeCostlyOperations = true;
		Team.MasterOrderController.TransferUnits(this, target, unitCount);
		CalculateLogicalClass();
		target.CalculateLogicalClass();
		if (CountOfUnits == 0)
		{
			RepresentativeClass = FormationClass.NumberOfAllFormations;
		}
		PostponeCostlyOperations = false;
		target.PostponeCostlyOperations = false;
		QuerySystem.Expire();
		target.QuerySystem.Expire();
		Team.QuerySystem.ExpireAfterUnitAddRemove();
		target.Team.QuerySystem.ExpireAfterUnitAddRemove();
	}

	public void TransferUnitsAux(Formation target, int unitCount, bool isPlayerOrder, bool useSelectivePop)
	{
		if (!isPlayerOrder && !IsSplittableByAI)
		{
			return;
		}
		MBDebug.Print(FormationIndex.GetName() + " has " + CountOfUnits + " units, " + target.FormationIndex.GetName() + " has " + target.CountOfUnits + " units");
		MBDebug.Print(string.Concat(Team.Side, " ", FormationIndex.GetName(), " transfers ", unitCount, " units to ", target.FormationIndex.GetName()));
		if (unitCount == 0)
		{
			return;
		}
		if (target.CountOfUnits == 0)
		{
			target.CopyOrdersFrom(this);
			target.SetPositioning(_orderPosition, Direction, UnitSpacing);
		}
		BattleBannerBearersModel battleBannerBearersModel = MissionGameModels.Current.BattleBannerBearersModel;
		List<IFormationUnit> list;
		if (battleBannerBearersModel.GetFormationBanner(this) == null)
		{
			list = (useSelectivePop ? GetUnitsToPopWithReferencePosition(unitCount, target.OrderPositionIsValid ? target.OrderPosition.ToVec3() : target.CachedMedianPosition.GetGroundVec3()) : GetUnitsToPop(unitCount).ToList());
		}
		else
		{
			List<Agent> formationBannerBearers = battleBannerBearersModel.GetFormationBannerBearers(this);
			int count = Math.Min(CountOfUnits, unitCount + formationBannerBearers.Count);
			list = (useSelectivePop ? GetUnitsToPopWithReferencePosition(count, target.OrderPositionIsValid ? target.OrderPosition.ToVec3() : target.CachedMedianPosition.GetGroundVec3()) : GetUnitsToPop(count).ToList());
			foreach (Agent item in formationBannerBearers)
			{
				if (list.Count > unitCount)
				{
					list.Remove(item);
					continue;
				}
				break;
			}
			if (list.Count > unitCount)
			{
				int num = list.Count - unitCount;
				list.RemoveRange(list.Count - num, num);
			}
		}
		if (battleBannerBearersModel.GetFormationBanner(target) != null)
		{
			foreach (Agent formationBannerBearer in battleBannerBearersModel.GetFormationBannerBearers(target))
			{
				if (formationBannerBearer.Formation == this && !list.Contains(formationBannerBearer))
				{
					int num2 = list.FindIndex((IFormationUnit unit) => unit is Agent agent && agent.Banner == null);
					if (num2 < 0)
					{
						break;
					}
					list[num2] = formationBannerBearer;
				}
			}
		}
		foreach (Agent item2 in list)
		{
			item2.Formation = target;
		}
		Team.TriggerOnFormationsChanged(this);
		Team.TriggerOnFormationsChanged(target);
		MBDebug.Print(FormationIndex.GetName() + " has " + CountOfUnits + " units, " + target.FormationIndex.GetName() + " has " + target.CountOfUnits + " units");
	}

	[Conditional("DEBUG")]
	public void DebugArrangements()
	{
		foreach (Team team in Mission.Current.Teams)
		{
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					item.ApplyActionOnEachUnit(delegate(Agent agent)
					{
						agent.AgentVisuals.SetContourColor(null);
					});
				}
			}
		}
		ApplyActionOnEachUnit(delegate(Agent agent)
		{
			agent.AgentVisuals.SetContourColor(4294901760u);
		});
		Vec3 vec = Direction.ToVec3();
		vec.RotateAboutZ(System.MathF.PI / 2f);
		_ = IsSimulationFormation;
		_ = vec * Width * 0.5f;
		_ = Direction.ToVec3() * Depth * 0.5f;
		_ = OrderPositionIsValid;
		CachedMedianPosition.SetVec2(CurrentPosition);
		ApplyActionOnEachUnit(delegate(Agent agent)
		{
			WorldPosition orderPositionOfUnit = GetOrderPositionOfUnit(agent);
			if (orderPositionOfUnit.IsValid)
			{
				Vec2 directionOfUnit = GetDirectionOfUnit(agent);
				directionOfUnit.Normalize();
				directionOfUnit *= 0.1f;
				_ = orderPositionOfUnit.GetGroundVec3() + directionOfUnit.ToVec3();
				_ = orderPositionOfUnit.GetGroundVec3() - directionOfUnit.LeftVec().ToVec3();
				_ = orderPositionOfUnit.GetGroundVec3() + directionOfUnit.LeftVec().ToVec3();
				_ = "(" + ((IFormationUnit)agent).FormationFileIndex + "," + ((IFormationUnit)agent).FormationRankIndex + ")";
			}
		});
		_ = OrderPositionIsValid;
		foreach (IDetachment detachment in Detachments)
		{
			_ = detachment is UsableMachine;
			_ = detachment is RangedSiegeWeapon;
		}
		if (Arrangement is ColumnFormation)
		{
			ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.GetFollowedUnit();
				_ = "(" + ((IFormationUnit)agent).FormationFileIndex + "," + ((IFormationUnit)agent).FormationRankIndex + ")";
			});
		}
	}

	public void AddUnit(Agent unit)
	{
		int countOfUnits = CountOfUnits;
		if (Arrangement.AddUnit(unit) && Mission.Current.HasMissionBehavior<AmmoSupplyLogic>() && Mission.Current.GetMissionBehavior<AmmoSupplyLogic>().IsAgentEligibleForAmmoSupply(unit))
		{
			unit.SetScriptedCombatFlags(unit.GetScriptedCombatFlags() | Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation);
			unit.ResetAiWaitBeforeShootFactor();
			unit.UpdateAgentStats();
		}
		if (unit.IsPlayerControlled)
		{
			HasPlayerControlledTroop = true;
		}
		if (unit.IsPlayerTroop)
		{
			IsPlayerTroopInFormation = true;
		}
		if (!unit.IsDetachableFromFormation && !unit.IsPlayerControlled)
		{
			OnUndetachableNonPlayerUnitAdded(unit);
		}
		if (unit.Character != null)
		{
			FormationClass formationClass = Team.Mission.GetAgentTroopClass(Team.Side, unit.Character).DefaultClass();
			_logicalClassCounts[(int)formationClass]++;
			if (_logicalClass != formationClass)
			{
				if (PostponeCostlyOperations)
				{
					_logicalClassNeedsUpdate = true;
				}
				else
				{
					CalculateLogicalClass();
					_logicalClassNeedsUpdate = false;
				}
			}
		}
		_movementOrder.OnUnitJoinOrLeave(this, unit, isJoining: true);
		unit.SetTargetFormationIndex(TargetFormation?.Index ?? (-1));
		unit.SetFiringOrder(FiringOrder.OrderEnum);
		unit.SetRidingOrder(RidingOrder.OrderEnum);
		OnUnitAddedOrRemoved();
		this.OnUnitAdded?.Invoke(this, unit);
		if (countOfUnits == 0 && CountOfUnits > 0)
		{
			Team.TeamAI?.OnUnitAddedToFormationForTheFirstTime(this);
		}
	}

	public void RemoveUnit(Agent unit)
	{
		if (unit.IsDetachedFromFormation)
		{
			unit.Detachment.RemoveAgent(unit);
			_detachedUnits.Remove(unit);
			_looseDetachedUnits.Remove(unit);
			unit.Detachment = null;
			unit.SetDetachmentWeight(-1f);
		}
		else
		{
			Arrangement.RemoveUnit(unit);
		}
		if (unit.Character != null)
		{
			FormationClass formationClass = Team.Mission.GetAgentTroopClass(Team.Side, unit.Character).DefaultClass();
			_logicalClassCounts[(int)formationClass]--;
			if (_logicalClass == formationClass)
			{
				if (PostponeCostlyOperations)
				{
					_logicalClassNeedsUpdate = true;
				}
				else
				{
					CalculateLogicalClass();
					_logicalClassNeedsUpdate = false;
				}
			}
		}
		if (unit.IsPlayerTroop)
		{
			IsPlayerTroopInFormation = false;
		}
		if (unit.IsPlayerControlled)
		{
			HasPlayerControlledTroop = false;
		}
		if (unit == Captain && !unit.CanLeadFormationsRemotely)
		{
			Captain = null;
		}
		if (!unit.IsDetachableFromFormation && !unit.IsPlayerControlled)
		{
			OnUndetachableNonPlayerUnitRemoved(unit);
		}
		if (Mission.Current.Mode != MissionMode.Deployment && !IsAIControlled && CountOfUnits == 0)
		{
			SetControlledByAI(isControlledByAI: true);
		}
		_movementOrder.OnUnitJoinOrLeave(this, unit, isJoining: false);
		unit.SetTargetFormationIndex(-1);
		unit.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.FireAtWill);
		unit.SetRidingOrder(RidingOrder.RidingOrderEnum.Free);
		OnUnitAddedOrRemoved();
		this.OnUnitRemoved?.Invoke(this, unit);
	}

	public void DetachUnit(Agent unit, bool isLoose)
	{
		Arrangement.RemoveUnit(unit);
		_detachedUnits.Add(unit);
		if (isLoose)
		{
			_looseDetachedUnits.Add(unit);
		}
		unit.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.DefaultDetached);
		OnUnitAttachedOrDetached();
	}

	public void AttachUnit(Agent unit)
	{
		_detachedUnits.Remove(unit);
		_looseDetachedUnits.Remove(unit);
		Arrangement.AddUnit(unit);
		unit.Detachment = null;
		unit.SetDetachmentWeight(-1f);
		_movementOrder.OnUnitJoinOrLeave(this, unit, isJoining: true);
		OnUnitAttachedOrDetached();
		this.OnUnitAttached?.Invoke(this, unit);
	}

	public void SwitchUnitLocations(Agent firstUnit, Agent secondUnit)
	{
		if (!firstUnit.IsDetachedFromFormation && !secondUnit.IsDetachedFromFormation && (((IFormationUnit)firstUnit).FormationFileIndex != -1 || ((IFormationUnit)secondUnit).FormationFileIndex != -1))
		{
			if (((IFormationUnit)firstUnit).FormationFileIndex == -1)
			{
				Arrangement.SwitchUnitLocationsWithUnpositionedUnit(secondUnit, firstUnit);
			}
			else if (((IFormationUnit)secondUnit).FormationFileIndex == -1)
			{
				Arrangement.SwitchUnitLocationsWithUnpositionedUnit(firstUnit, secondUnit);
			}
			else
			{
				Arrangement.SwitchUnitLocations(firstUnit, secondUnit);
			}
		}
	}

	public void ForceCalculateCaches()
	{
		CacheAverageAndMedianPositionAndVelocity();
		CacheClosestEnemyFormation();
		CacheFormationIntegrityData();
		CacheMovementSpeedOfUnits();
	}

	public void Tick(float dt)
	{
		float currentTime = Mission.Current.CurrentTime;
		if (_cachedPositionAndVelocityUpdateTimer.Check(currentTime))
		{
			CacheAverageAndMedianPositionAndVelocity();
		}
		if (_cachedClosestEnemyFormationUpdateTimer.Check(currentTime) || _cachedClosestEnemyFormation == null || _cachedClosestEnemyFormation.CountOfUnits == 0)
		{
			CacheClosestEnemyFormation();
		}
		if (_cachedFormationIntegrityDataUpdateTimer.Check(currentTime))
		{
			CacheFormationIntegrityData();
		}
		if (_cachedMovementSpeedUpdateTimer.Check(currentTime))
		{
			CacheMovementSpeedOfUnits();
		}
		if (Team.HasTeamAi && (IsAIControlled || Team.IsPlayerSergeant))
		{
			AI.Tick();
		}
		else
		{
			IsAITickedAfterSplit = true;
		}
		int num = 0;
		while (!_movementOrder.IsApplicable(this) && num++ < 10)
		{
			SetMovementOrder(_movementOrder.GetSubstituteOrder(this));
		}
		Formation targetFormation = TargetFormation;
		if (targetFormation != null && targetFormation.CountOfUnits <= 0)
		{
			TargetFormation = null;
		}
		if (_arrangementOrderTickOccasionallyTimer.Check(currentTime))
		{
			ArrangementOrder.TickOccasionally(this);
		}
		if (_arrangementTickOccasionallyTimer.Check(currentTime))
		{
			Arrangement.OnTickOccasionally();
		}
		_movementOrder.Tick(this);
		WorldPosition value = _movementOrder.CreateNewOrderWorldPositionMT(this, WorldPosition.WorldPositionEnforcedCache.None);
		Vec2 direction = FacingOrder.GetDirection(this, _movementOrder._targetAgent);
		if (value.IsValid || direction.IsValid)
		{
			SetPositioning(value, direction);
		}
		TickDetachments(dt);
		this.OnTick?.Invoke(this);
		if (_hasPendingUnitPositions)
		{
			ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
			});
			SetHasPendingUnitPositions(hasPendingUnitPositions: false);
		}
		SmoothAverageUnitPosition(dt);
		if (_isArrangementShapeChanged)
		{
			_isArrangementShapeChanged = false;
		}
	}

	public void SetHasPendingUnitPositions(bool hasPendingUnitPositions)
	{
		_hasPendingUnitPositions = hasPendingUnitPositions;
	}

	public void JoinDetachment(IDetachment detachment)
	{
		if (!Team.DetachmentManager.ContainsDetachment(detachment))
		{
			Team.DetachmentManager.MakeDetachment(detachment);
		}
		_detachments.Add(detachment);
		Team.DetachmentManager.OnFormationJoinDetachment(this, detachment);
	}

	public void FormAttackEntityDetachment(GameEntity targetEntity)
	{
		AttackEntityOrderSecondaryDetachment = new AttackEntityOrderSecondaryDetachment(targetEntity);
	}

	public void LeaveDetachment(IDetachment detachment)
	{
		detachment.OnFormationLeave(this);
		_detachments.Remove(detachment);
		Team.DetachmentManager.OnFormationLeaveDetachment(this, detachment);
	}

	public void DisbandAttackEntityDetachment()
	{
		if (AttackEntityOrderSecondaryDetachment != null)
		{
			AttackEntityOrderSecondaryDetachment.Disband(this);
			AttackEntityOrderSecondaryDetachment = null;
		}
	}

	public void Rearrange(IFormationArrangement arrangement)
	{
		if (!(Arrangement.GetType() == arrangement.GetType()))
		{
			IFormationArrangement arrangement2 = Arrangement;
			Arrangement = arrangement;
			arrangement2.RearrangeTo(arrangement);
			arrangement.RearrangeFrom(arrangement2);
			arrangement2.RearrangeTransferUnits(arrangement);
			ReapplyFormOrder();
			_movementOrder.OnArrangementChanged(this);
		}
	}

	public void TickForColumnArrangementInitialPositioning(Formation formation)
	{
		if (!IsDeployment && (CachedFormationIntegrityData.MaxDeviationOfPositionExcludeFarAgents < Interval * 0.75f || OrderPosition.DistanceSquared(CurrentPosition) > Arrangement.RankDepth * Arrangement.RankDepth))
		{
			ArrangementOrder.RearrangeAux(this, isDirectly: true);
		}
	}

	public float CalculateFormationDirectionEnforcingFactorForRank(int rankIndex)
	{
		if (rankIndex == -1)
		{
			return 0f;
		}
		return ArrangementOrder.CalculateFormationDirectionEnforcingFactorForRank(rankIndex, Arrangement.RankCount);
	}

	public void BeginSpawn(int unitCount, bool isMounted)
	{
		IsSpawning = true;
		OverridenUnitCount = unitCount;
		_overridenHasAnyMountedUnit = isMounted;
	}

	public void EndSpawn()
	{
		IsSpawning = false;
		OverridenUnitCount = null;
		_overridenHasAnyMountedUnit = null;
		Arrangement.UpdateLocalPositionErrors();
	}

	public override int GetHashCode()
	{
		return (int)(Team.TeamIndex * 10 + FormationIndex);
	}

	internal bool IsUnitDetachedForDebug(Agent unit)
	{
		return _detachedUnits.Contains(unit);
	}

	internal IEnumerable<IFormationUnit> GetUnitsToPopWithPriorityFunction(int count, Func<Agent, int> priorityFunction, List<Agent> excludedHeroes, bool excludeBannerman)
	{
		List<IFormationUnit> list = new List<IFormationUnit>();
		if (count <= 0)
		{
			return list;
		}
		Func<Agent, bool> selectCondition = (Agent agent) => !excludedHeroes.Contains(agent) && (!excludeBannerman || agent.Banner == null);
		List<Agent> list2 = (from unit in _arrangement.GetAllUnits().Concat(_detachedUnits)
			where unit is Agent arg && selectCondition(arg)
			select unit as Agent).ToList();
		if (list2.IsEmpty())
		{
			return list;
		}
		int num = count;
		int bestFit = int.MaxValue;
		while (num > 0 && bestFit > 0 && list2.Count > 0)
		{
			bestFit = list2.Max((Agent unit) => priorityFunction(unit));
			Func<IFormationUnit, bool> bestFitCondition = (IFormationUnit unit) => unit is Agent arg && selectCondition(arg) && priorityFunction(arg) == bestFit;
			int num2 = Math.Min(num, _arrangement.GetAllUnits().Count((IFormationUnit unit) => bestFitCondition(unit)));
			if (num2 > 0)
			{
				IEnumerable<IFormationUnit> toPop = _arrangement.GetUnitsToPopWithCondition(num2, bestFitCondition);
				if (!toPop.IsEmpty())
				{
					list.AddRange(toPop);
					num -= toPop.Count();
					list2.RemoveAll((Agent unit) => toPop.Contains(unit));
				}
			}
			if (num > 0)
			{
				IEnumerable<Agent> toPop2 = _looseDetachedUnits.Where((Agent agent) => bestFitCondition(agent)).Take(num);
				if (!toPop2.IsEmpty())
				{
					list.AddRange(toPop2);
					num -= toPop2.Count();
					list2.RemoveAll((Agent unit) => toPop2.Contains(unit));
				}
			}
			if (num <= 0)
			{
				continue;
			}
			IEnumerable<Agent> toPop3 = _detachedUnits.Where((Agent agent) => bestFitCondition(agent)).Take(num);
			if (!toPop3.IsEmpty())
			{
				list.AddRange(toPop3);
				num -= toPop3.Count();
				list2.RemoveAll((Agent unit) => toPop3.Contains(unit));
			}
		}
		return list;
	}

	internal void TransferUnitsWithPriorityFunction(Formation target, int unitCount, Func<Agent, int> priorityFunction, bool excludeBannerman, List<Agent> excludedAgents)
	{
		MBDebug.Print(FormationIndex.GetName() + " has " + CountOfUnits + " units, " + target.FormationIndex.GetName() + " has " + target.CountOfUnits + " units");
		MBDebug.Print(Team.Side.ToString() + " " + FormationIndex.GetName() + " transfers " + unitCount + " units to " + target.FormationIndex.GetName());
		if (unitCount == 0)
		{
			return;
		}
		if (target.CountOfUnits == 0)
		{
			target.CopyOrdersFrom(this);
			target.SetPositioning(_orderPosition, Direction, UnitSpacing);
		}
		foreach (Agent item in new List<IFormationUnit>(GetUnitsToPopWithPriorityFunction(unitCount, priorityFunction, excludedAgents, excludeBannerman)))
		{
			item.Formation = target;
		}
		Team.TriggerOnFormationsChanged(this);
		Team.TriggerOnFormationsChanged(target);
		MBDebug.Print(FormationIndex.GetName() + " has " + CountOfUnits + " units, " + target.FormationIndex.GetName() + " has " + target.CountOfUnits + " units");
	}

	private IFormationUnit GetClosestUnitToAux(Vec2 position, MBReadOnlyList<IFormationUnit> unitsWithSpaces, float? maxDistance)
	{
		if (unitsWithSpaces == null)
		{
			unitsWithSpaces = Arrangement.GetAllUnits();
		}
		IFormationUnit result = null;
		float num = (maxDistance.HasValue ? (maxDistance.Value * maxDistance.Value) : float.MaxValue);
		for (int i = 0; i < unitsWithSpaces.Count; i++)
		{
			IFormationUnit formationUnit = unitsWithSpaces[i];
			if (formationUnit != null)
			{
				float num2 = ((Agent)formationUnit).Position.AsVec2.DistanceSquared(position);
				if (num > num2)
				{
					num = num2;
					result = formationUnit;
				}
			}
		}
		return result;
	}

	private void CopyOrdersFrom(Formation target)
	{
		SetMovementOrder(target._movementOrder);
		SetFormOrder(target.FormOrder);
		SetPositioning(null, null, target.UnitSpacing);
		SetRidingOrder(target.RidingOrder);
		SetFiringOrder(target.FiringOrder);
		SetControlledByAI(target.IsAIControlled || !target.Team.IsPlayerGeneral);
		if (target.AI.Side != FormationAI.BehaviorSide.BehaviorSideNotSet)
		{
			AI.Side = target.AI.Side;
		}
		SetMovementOrder(target._movementOrder);
		TargetFormation = target.TargetFormation;
		FacingOrder = target.FacingOrder;
		SetArrangementOrder(target.ArrangementOrder);
	}

	private void TickDetachments(float dt)
	{
		if (IsDeployment)
		{
			return;
		}
		for (int num = _detachments.Count - 1; num >= 0; num--)
		{
			IDetachment detachment = _detachments[num];
			UsableMachine usableMachine = detachment as UsableMachine;
			if (usableMachine?.Ai != null)
			{
				usableMachine.Ai.Tick(null, (usableMachine.UserFormations.Count > 1) ? this : null, Team, dt);
				if (usableMachine.Ai.HasActionCompleted || (usableMachine.IsDisabledForBattleSideAI(Team.Side) && usableMachine.ShouldAutoLeaveDetachmentWhenDisabled(Team.Side)))
				{
					LeaveDetachment(detachment);
				}
			}
		}
	}

	[Conditional("DEBUG")]
	private void TickOrderDebug()
	{
		WorldPosition cachedMedianPosition = CachedMedianPosition;
		WorldPosition worldPosition = CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.GroundVec3);
		cachedMedianPosition.SetVec2(CachedAveragePosition);
		if (worldPosition.IsValid)
		{
			if (!_movementOrder.GetPosition(this).IsValid)
			{
				if (AI != null)
				{
					_ = AI.ActiveBehavior;
				}
			}
			else if (AI != null)
			{
				_ = AI.ActiveBehavior;
			}
		}
		else if (AI != null)
		{
			_ = AI.ActiveBehavior;
		}
	}

	[Conditional("DEBUG")]
	private void TickDebug(float dt)
	{
		if (MBDebug.IsDisplayingHighLevelAI && !IsSimulationFormation && _movementOrder.OrderEnum == MovementOrder.MovementOrderEnum.FollowEntity)
		{
			_ = _movementOrder.TargetEntity.Name;
		}
	}

	private void OnUnitAttachedOrDetached()
	{
		ReapplyFormOrder();
	}

	[Conditional("DEBUG")]
	private void DebugAssertDetachments()
	{
	}

	private void SetOrderPosition(WorldPosition pos)
	{
		_orderPosition = pos;
	}

	private int GetHeroPointForCaptainSelection(Agent agent)
	{
		return agent.Character.Level + 100 * agent.Character.GetSkillValue(DefaultSkills.Charm);
	}

	private void OnCaptainChanged()
	{
		ApplyActionOnEachUnit(delegate(Agent agent)
		{
			agent.UpdateAgentProperties();
		});
		Mission.Current.OnFormationCaptainChanged(this);
	}

	private void UpdateAgentDrivenPropertiesBasedOnOrderDefensiveness()
	{
		ApplyActionOnEachUnit(delegate(Agent agent)
		{
			agent.Defensiveness = _formationOrderDefensivenessFactor;
		});
	}

	private void ResetAux()
	{
		if (_detachments != null)
		{
			for (int num = _detachments.Count - 1; num >= 0; num--)
			{
				LeaveDetachment(_detachments[num]);
			}
		}
		else
		{
			_detachments = new MBList<IDetachment>();
		}
		_detachedUnits = new MBList<Agent>();
		_looseDetachedUnits = new MBList<Agent>();
		AttackEntityOrderSecondaryDetachment = null;
		AI = new FormationAI(this);
		QuerySystem = new FormationQuerySystem(this);
		SetPositioning(null, Vec2.Forward, 1);
		SetMovementOrder(MovementOrder.MovementOrderStop);
		if (_overridenHasAnyMountedUnit.HasValue && _overridenHasAnyMountedUnit == true)
		{
			SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
		}
		else
		{
			SetFormOrder(FormOrder.FormOrderWide);
			SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		}
		SetRidingOrder(RidingOrder.RidingOrderFree);
		SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		Width = 0f * (Interval + UnitDiameter) + UnitDiameter;
		HasBeenPositioned = false;
		_currentSpawnIndex = 0;
		IsPlayerTroopInFormation = false;
		HasPlayerControlledTroop = false;
	}

	private void ResetForSimulation()
	{
		Arrangement.Reset();
		ResetAux();
	}

	private void TryRelocatePlayerUnit()
	{
		if (HasPlayerControlledTroop || IsPlayerTroopInFormation)
		{
			IFormationUnit playerUnit = Arrangement.GetPlayerUnit();
			if (playerUnit != null && playerUnit.FormationFileIndex >= 0 && playerUnit.FormationRankIndex >= 0)
			{
				Arrangement.SwitchUnitLocationsWithBackMostUnit(playerUnit);
			}
		}
	}

	private void ReapplyFormOrder()
	{
		FormOrder formOrder = FormOrder;
		if (FormOrder.OrderEnum == FormOrder.FormOrderEnum.Custom && ArrangementOrder.OrderEnum != ArrangementOrder.ArrangementOrderEnum.Circle)
		{
			formOrder.CustomFlankWidth = Arrangement.FlankWidth;
		}
		SetFormOrder(formOrder, updateDesiredFileCount: false);
	}

	private float CalculateDesiredWidth()
	{
		return TaleWorlds.Library.MathF.Max(0f, (float)(_desiredFileCount - 1) * (UnitDiameter + Interval) + UnitDiameter);
	}

	private void CalculateLogicalClass()
	{
		int num = 0;
		FormationClass logicalClass = FormationClass.NumberOfAllFormations;
		for (int i = 0; i < _logicalClassCounts.Length; i++)
		{
			FormationClass formationClass = (FormationClass)i;
			int num2 = _logicalClassCounts[i];
			if (num2 > num)
			{
				num = num2;
				logicalClass = formationClass;
			}
		}
		_logicalClass = logicalClass;
		if (_logicalClass != FormationClass.NumberOfAllFormations)
		{
			RepresentativeClass = _logicalClass;
		}
	}

	private void SmoothAverageUnitPosition(float dt)
	{
		_smoothedAverageUnitPosition = ((!_smoothedAverageUnitPosition.IsValid) ? CachedAveragePosition : Vec2.Lerp(_smoothedAverageUnitPosition, CachedAveragePosition, dt * 3f));
	}

	private void Arrangement_OnWidthChanged()
	{
		this.OnWidthChanged?.Invoke(this);
	}

	private void Arrangement_OnShapeChanged()
	{
		_orderLocalAveragePositionIsDirty = true;
		_isArrangementShapeChanged = true;
		if (!GameNetwork.IsMultiplayer)
		{
			TryRelocatePlayerUnit();
		}
	}

	public static float GetLastSimulatedFormationsOccupationWidthIfLesserThanActualWidth(Formation simulationFormation)
	{
		float occupationWidth = simulationFormation.Arrangement.GetOccupationWidth(simulationFormation.OverridenUnitCount.GetValueOrDefault());
		if (simulationFormation.Width > occupationWidth)
		{
			return occupationWidth;
		}
		return -1f;
	}

	public static List<WorldFrame> GetFormationFramesForBeforeFormationCreation(float width, int manCount, bool areMounted, WorldPosition spawnOrigin, Mat3 spawnRotation)
	{
		List<AgentArrangementData> list = new List<AgentArrangementData>();
		Formation formation = new Formation(null, -1);
		formation.SetOrderPosition(spawnOrigin);
		formation.Direction = spawnRotation.f.AsVec2.Normalized();
		LineFormation lineFormation = new LineFormation(formation);
		lineFormation.Width = width;
		for (int i = 0; i < manCount; i++)
		{
			list.Add(new AgentArrangementData(i, lineFormation));
		}
		lineFormation.OnFormationFrameChanged();
		foreach (AgentArrangementData item in list)
		{
			lineFormation.AddUnit(item);
		}
		List<WorldFrame> list2 = new List<WorldFrame>();
		int cachedOrderedAndAvailableUnitPositionIndicesCount = lineFormation.GetCachedOrderedAndAvailableUnitPositionIndicesCount();
		for (int j = 0; j < cachedOrderedAndAvailableUnitPositionIndicesCount; j++)
		{
			Vec2i cachedOrderedAndAvailableUnitPositionIndexAt = lineFormation.GetCachedOrderedAndAvailableUnitPositionIndexAt(j);
			WorldPosition globalPositionAtIndex = lineFormation.GetGlobalPositionAtIndex(cachedOrderedAndAvailableUnitPositionIndexAt.X, cachedOrderedAndAvailableUnitPositionIndexAt.Y);
			list2.Add(new WorldFrame(spawnRotation, globalPositionAtIndex));
		}
		return list2;
	}

	public static float GetDefaultUnitDiameter(bool isMounted)
	{
		if (isMounted)
		{
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.QuadrupedalRadius) * 2f;
		}
		return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRadius) * 2f;
	}

	public static float GetDefaultMinimumUnitInterval(bool isMounted)
	{
		if (!isMounted)
		{
			return InfantryInterval(0);
		}
		return CavalryInterval(0);
	}

	public static float GetDefaultUnitInterval(bool isMounted, int unitSpacing)
	{
		if (!isMounted)
		{
			return InfantryInterval(unitSpacing);
		}
		return CavalryInterval(unitSpacing);
	}

	public static float GetDefaultMinimumUnitDistance(bool isMounted)
	{
		if (!isMounted)
		{
			return InfantryDistance(0);
		}
		return CavalryDistance(0);
	}

	public static float GetDefaultUnitDistance(bool isMounted, int unitSpacing)
	{
		if (!isMounted)
		{
			return InfantryDistance(unitSpacing);
		}
		return CavalryDistance(unitSpacing);
	}

	public static float GetDefaultFileWidth(int fileUnitCount, int unitSpacing, bool isMounted)
	{
		float defaultUnitInterval = GetDefaultUnitInterval(isMounted, unitSpacing);
		float defaultUnitDiameter = GetDefaultUnitDiameter(isMounted);
		return (float)(fileUnitCount - 1) * (defaultUnitInterval + defaultUnitDiameter);
	}

	public static float GetDefaultRankDepth(int rankUnitCount, int unitSpacing, bool isMounted)
	{
		float defaultUnitDistance = GetDefaultUnitDistance(isMounted, unitSpacing);
		float defaultUnitDiameter = GetDefaultUnitDiameter(isMounted);
		return (float)(rankUnitCount - 1) * (defaultUnitDistance + defaultUnitDiameter);
	}

	public static float InfantryInterval(int unitSpacing)
	{
		return 0.38f * (float)unitSpacing;
	}

	public static float CavalryInterval(int unitSpacing)
	{
		return 0.18f + 0.32f * (float)unitSpacing;
	}

	public static float InfantryDistance(int unitSpacing)
	{
		return 0.4f * (float)unitSpacing;
	}

	public static float CavalryDistance(int unitSpacing)
	{
		return 1.7f + 0.3f * (float)unitSpacing;
	}

	public static bool IsDefenseRelatedAIDrivenComponent(DrivenProperty drivenProperty)
	{
		if (drivenProperty != DrivenProperty.AIDecideOnAttackChance && drivenProperty != DrivenProperty.AIAttackOnDecideChance && drivenProperty != DrivenProperty.AIAttackOnParryChance && drivenProperty != DrivenProperty.AiUseShieldAgainstEnemyMissileProbability)
		{
			return drivenProperty == DrivenProperty.AiDefendWithShieldDecisionChanceValue;
		}
		return true;
	}

	private static void GetUnitPositionWithIndexAccordingToNewOrder(Formation simulationFormation, int unitIndex, in WorldPosition formationPosition, in Vec2 formationDirection, IFormationArrangement arrangement, float width, int unitSpacing, int unitCount, bool isMounted, int index, out WorldPosition? unitPosition, out Vec2? unitDirection, out float actualWidth)
	{
		unitPosition = null;
		unitDirection = null;
		if (simulationFormation == null)
		{
			if (_simulationFormationTemp == null || _simulationFormationUniqueIdentifier != index)
			{
				_simulationFormationTemp = new Formation(null, -1);
			}
			simulationFormation = _simulationFormationTemp;
		}
		lock (simulationFormation.SimulationFormationLock)
		{
			if (simulationFormation.UnitSpacing != unitSpacing || (TaleWorlds.Library.MathF.Abs(simulationFormation.Width - width + 1E-05f) >= simulationFormation.Interval + simulationFormation.UnitDiameter - 1E-05f && (!(width < simulationFormation.MinimumWidth) || !(TaleWorlds.Library.MathF.Abs(simulationFormation.Width - simulationFormation.MinimumWidth) < 1E-05f))) || !simulationFormation.OrderPositionIsValid || !simulationFormation.OrderGroundPosition.NearlyEquals(formationPosition.GetGroundVec3(), 0.1f) || !simulationFormation.Direction.NearlyEquals(formationDirection, 0.1f) || simulationFormation.Arrangement.GetType() != arrangement.GetType())
			{
				simulationFormation._overridenHasAnyMountedUnit = isMounted;
				simulationFormation.ResetForSimulation();
				simulationFormation.SetPositioning(null, null, unitSpacing);
				simulationFormation.OverridenUnitCount = unitCount;
				simulationFormation.SetPositioning(formationPosition, formationDirection);
				simulationFormation.Rearrange(arrangement.Clone(simulationFormation));
				simulationFormation.Arrangement.DeepCopyFrom(arrangement);
				simulationFormation.Width = width;
				_simulationFormationUniqueIdentifier = index;
				if (arrangement is ColumnFormation columnFormation && arrangement.RankCount > 1)
				{
					Vec2 asVec = ((columnFormation.Vanguard ?? arrangement.GetUnit(columnFormation.VanguardFileIndex, 0)) as Agent).Position.AsVec2;
					Vec2 asVec2 = (arrangement.GetUnit(columnFormation.VanguardFileIndex, 1) as Agent).Position.AsVec2;
					Vec2 previousDirection = (asVec - asVec2).Normalized();
					Vec2 value = (formationPosition.AsVec2 - asVec).Normalized();
					if (arrangement.IsTurnBackwardsNecessary(asVec, formationPosition, previousDirection, hasNewDirection: true, value))
					{
						(simulationFormation.Arrangement as ColumnFormation).UnitPositionsOnVanguardFileIndex.Reverse();
					}
				}
			}
			actualWidth = simulationFormation.Width;
			if (width >= actualWidth)
			{
				Vec2? vec = simulationFormation.Arrangement.GetLocalPositionOfUnitOrDefault(unitIndex);
				if (!vec.HasValue)
				{
					vec = simulationFormation.Arrangement.CreateNewPosition(unitIndex);
				}
				if (vec.HasValue)
				{
					Vec2 vec2 = simulationFormation.Direction.TransformToParentUnitF(vec.Value);
					WorldPosition value2 = simulationFormation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
					value2.SetVec2(value2.AsVec2 + vec2);
					unitPosition = value2;
					unitDirection = formationDirection;
				}
			}
		}
	}

	private static IEnumerable<(WorldPosition, Vec2)> GetUnavailableUnitPositionsAccordingToNewOrder(Formation formation, Formation simulationFormation, WorldPosition position, Vec2 direction, IFormationArrangement arrangement, float width, int unitSpacing)
	{
		if (simulationFormation == null)
		{
			if (_simulationFormationTemp == null || _simulationFormationUniqueIdentifier != formation.Index)
			{
				_simulationFormationTemp = new Formation(null, -1);
			}
			simulationFormation = _simulationFormationTemp;
		}
		lock (simulationFormation.SimulationFormationLock)
		{
			if (simulationFormation.UnitSpacing != unitSpacing || TaleWorlds.Library.MathF.Abs(simulationFormation.Width - width) >= simulationFormation.Interval + simulationFormation.UnitDiameter || !simulationFormation.OrderPositionIsValid || !simulationFormation.OrderGroundPosition.NearlyEquals(position.GetGroundVec3(), 0.1f) || !simulationFormation.Direction.NearlyEquals(direction, 0.1f) || simulationFormation.Arrangement.GetType() != arrangement.GetType())
			{
				simulationFormation._overridenHasAnyMountedUnit = formation.HasAnyMountedUnit;
				simulationFormation.ResetForSimulation();
				simulationFormation.SetPositioning(null, null, unitSpacing);
				simulationFormation.OverridenUnitCount = formation.CountOfUnitsWithoutDetachedOnes;
				simulationFormation.SetPositioning(position, direction);
				simulationFormation.Rearrange(arrangement.Clone(simulationFormation));
				simulationFormation.Arrangement.DeepCopyFrom(arrangement);
				simulationFormation.Width = width;
				_simulationFormationUniqueIdentifier = formation.Index;
			}
			IEnumerable<Vec2> unavailableUnitPositions = simulationFormation.Arrangement.GetUnavailableUnitPositions();
			foreach (Vec2 item2 in unavailableUnitPositions)
			{
				Vec2 vec = simulationFormation.Direction.TransformToParentUnitF(item2);
				WorldPosition item = simulationFormation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
				item.SetVec2(item.AsVec2 + vec);
				yield return (item, direction);
			}
		}
	}
}
