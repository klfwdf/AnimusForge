using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public abstract class SiegeWeapon : UsableMachine, ITargetable
{
	private const string TargetingEntityTag = "targeting_entity";

	[EditableScriptComponentVariable(true, "")]
	internal string RemoveOnDeployTag = "";

	[EditableScriptComponentVariable(true, "")]
	internal string AddOnDeployTag = "";

	private List<GameEntity> _addOnDeployEntities;

	protected bool _spawnedFromSpawner;

	private List<GameEntity> _removeOnDeployEntities;

	private List<Formation> _potentialUsingFormations;

	private List<Formation> _forcedUseFormations;

	private bool _needsSingleThreadTickOnce;

	private bool _areMovingAgentsProcessed;

	private bool _isValidated;

	private Vec3? _targetingPositionOffset;

	[EditorVisibleScriptComponentVariable(false)]
	public bool ForcedUse { get; private set; }

	public bool IsUsed
	{
		get
		{
			foreach (Formation userFormation in base.UserFormations)
			{
				if (userFormation.Team.Side == Side)
				{
					return true;
				}
			}
			return false;
		}
	}

	public virtual BattleSideEnum Side => BattleSideEnum.Attacker;

	public override TextObject HitObjectName => GameTexts.FindText("str_siege_engine", GetSiegeEngineType().StringId);

	public override bool HasWaitFrame
	{
		get
		{
			if (base.HasWaitFrame)
			{
				if (this is IPrimarySiegeWeapon)
				{
					return !(this as IPrimarySiegeWeapon).HasCompletedAction();
				}
				return true;
			}
			return false;
		}
	}

	public override bool IsDeactivated
	{
		get
		{
			if (!base.IsDisabled && base.GameEntity.IsValid && base.GameEntity.IsVisibleIncludeParents())
			{
				return base.IsDeactivated;
			}
			return true;
		}
	}

	public void SetForcedUse(bool value)
	{
		ForcedUse = value;
	}

	public abstract SiegeEngineType GetSiegeEngineType();

	protected virtual bool CalculateIsSufficientlyManned(BattleSideEnum battleSide)
	{
		if (GetDetachmentWeightAux(battleSide) < 1f)
		{
			return true;
		}
		foreach (Team team in Mission.Current.Teams)
		{
			if (team.Side != Side)
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0 && IsUsedByFormation(item) && (item.Arrangement.UnitCount > 1 || (item.Arrangement.UnitCount > 0 && !item.HasPlayerControlledTroop)))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool HasNewMovingAgents()
	{
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (standingPoint.HasAIMovingTo && standingPoint.PreviousUserAgent != standingPoint.MovingAgent)
			{
				return true;
			}
		}
		return false;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		ForcedUse = true;
		_potentialUsingFormations = new List<Formation>();
		_forcedUseFormations = new List<Formation>();
		base.GameEntity.SetAnimationSoundActivation(activate: true);
		_removeOnDeployEntities = Mission.Current.Scene.FindEntitiesWithTag(RemoveOnDeployTag).ToList();
		_addOnDeployEntities = Mission.Current.Scene.FindEntitiesWithTag(AddOnDeployTag).ToList();
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (!(standingPoint is StandingPointWithWeaponRequirement))
			{
				standingPoint.AutoEquipWeaponsOnUseStopped = true;
			}
		}
		SetScriptComponentToTick(GetTickRequirement());
		WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("targeting_entity");
		if (firstChildEntityWithTag.IsValid)
		{
			Vec3 vec = base.GameEntity.ComputeGlobalPhysicsBoundingBoxCenter();
			_targetingPositionOffset = firstChildEntityWithTag.GlobalPosition - vec;
		}
		EnemyRangeToStopUsing = 5f;
	}

	public override TickRequirement GetTickRequirement()
	{
		if (base.GameEntity.IsVisibleIncludeParents() && !GameNetwork.IsClientOrReplay)
		{
			return base.GetTickRequirement() | TickRequirement.Tick | TickRequirement.TickParallel;
		}
		return base.GetTickRequirement();
	}

	private void TickAux(bool isParallel)
	{
		if (GameNetwork.IsClientOrReplay || !base.GameEntity.IsVisibleIncludeParents())
		{
			return;
		}
		if (IsDisabledForBattleSide(Side))
		{
			foreach (StandingPoint standingPoint in base.StandingPoints)
			{
				Agent userAgent = standingPoint.UserAgent;
				if (userAgent != null && !userAgent.IsPlayerControlled && userAgent.Formation != null && userAgent.Formation.Team.Side == Side)
				{
					if (isParallel)
					{
						_needsSingleThreadTickOnce = true;
					}
					else
					{
						userAgent.Formation.StopUsingMachine(this);
						_forcedUseFormations.Remove(userAgent.Formation);
						_isValidated = false;
					}
				}
			}
			return;
		}
		if (!ForcedUse)
		{
			return;
		}
		bool flag = false;
		foreach (Team team in Mission.Current.Teams)
		{
			if (team.Side != Side)
			{
				continue;
			}
			if (!CalculateIsSufficientlyManned(team.Side))
			{
				foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
				{
					if (item.CountOfUnits > 0 && item.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Retreat && (item.Arrangement.UnitCount > 1 || (item.Arrangement.UnitCount > 0 && !item.HasPlayerControlledTroop)) && !item.Detachments.Contains(this))
					{
						if (isParallel)
						{
							_needsSingleThreadTickOnce = true;
						}
						else
						{
							_potentialUsingFormations.Add(item);
						}
					}
				}
				_areMovingAgentsProcessed = false;
			}
			else if (HasNewMovingAgents())
			{
				if (!_areMovingAgentsProcessed)
				{
					float num = float.MaxValue;
					Formation formation = null;
					foreach (Formation item2 in team.FormationsIncludingSpecialAndEmpty)
					{
						if (item2.CountOfUnits > 0 && item2.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Retreat && (item2.Arrangement.UnitCount > 1 || (item2.Arrangement.UnitCount > 0 && !item2.HasPlayerControlledTroop)))
						{
							float num2 = item2.CachedMedianPosition.DistanceSquaredWithLimit(base.GameEntity.GlobalPosition, 10000f);
							if (num2 < num)
							{
								num = num2;
								formation = item2;
							}
						}
					}
					if (formation != null && !IsUsedByFormation(formation))
					{
						if (isParallel)
						{
							_needsSingleThreadTickOnce = true;
						}
						else
						{
							_potentialUsingFormations.Clear();
							_potentialUsingFormations.Add(formation);
							flag = true;
							_areMovingAgentsProcessed = true;
						}
					}
					else
					{
						_areMovingAgentsProcessed = true;
					}
				}
			}
			else
			{
				_areMovingAgentsProcessed = false;
			}
			if (flag)
			{
				_potentialUsingFormations[0].StartUsingMachine(this, !_potentialUsingFormations[0].IsAIControlled);
				_forcedUseFormations.Add(_potentialUsingFormations[0]);
				_potentialUsingFormations.Clear();
				_isValidated = false;
				flag = false;
			}
			else if (_potentialUsingFormations.Count > 0)
			{
				float num3 = float.MaxValue;
				Formation formation2 = null;
				foreach (Formation potentialUsingFormation in _potentialUsingFormations)
				{
					float num4 = potentialUsingFormation.CachedAveragePosition.DistanceSquared(base.GameEntity.GlobalPosition.AsVec2);
					if (num4 < num3)
					{
						num3 = num4;
						formation2 = potentialUsingFormation;
					}
				}
				int count = base.StandingPoints.Count;
				int num5 = 0;
				Formation formation3 = null;
				Vec2 zero = Vec2.Zero;
				for (int i = 0; i < count; i++)
				{
					Agent previousUserAgent = base.StandingPoints[i].PreviousUserAgent;
					if (previousUserAgent != null)
					{
						if (!previousUserAgent.IsActive() || previousUserAgent.Formation == null || (formation3 != null && previousUserAgent.Formation != formation3))
						{
							num5 = -1;
							break;
						}
						num5++;
						zero += previousUserAgent.Position.AsVec2;
						formation3 = previousUserAgent.Formation;
					}
				}
				Formation formation4 = formation2;
				if (num5 > 0 && _potentialUsingFormations.Contains(formation3) && (zero * (1f / (float)num5)).DistanceSquared(base.GameEntity.GlobalPosition.AsVec2) < num3)
				{
					formation4 = formation3;
				}
				formation4.StartUsingMachine(this, !formation4.IsAIControlled);
				_forcedUseFormations.Add(formation4);
				_potentialUsingFormations.Clear();
				_isValidated = false;
			}
			else
			{
				if (_isValidated)
				{
					continue;
				}
				if (!HasToBeDefendedByUser(team.Side) && GetDetachmentWeightAux(team.Side) == float.MinValue)
				{
					for (int num6 = _forcedUseFormations.Count - 1; num6 >= 0; num6--)
					{
						Formation formation5 = _forcedUseFormations[num6];
						if (formation5.Team.Side == Side && !IsAnyUserBelongsToFormation(formation5))
						{
							if (isParallel)
							{
								if (IsUsedByFormation(formation5))
								{
									_needsSingleThreadTickOnce = true;
									break;
								}
								_forcedUseFormations.Remove(formation5);
							}
							else
							{
								if (IsUsedByFormation(formation5))
								{
									formation5.StopUsingMachine(this, !formation5.IsAIControlled);
								}
								_forcedUseFormations.Remove(formation5);
							}
						}
					}
					if (isParallel && _needsSingleThreadTickOnce)
					{
						break;
					}
				}
				if (!isParallel)
				{
					_isValidated = true;
				}
			}
		}
	}

	protected virtual bool IsAnyUserBelongsToFormation(Formation formation)
	{
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (standingPoint.UserAgent != null && standingPoint.UserAgent.Formation == formation)
			{
				return true;
			}
		}
		return false;
	}

	protected internal override void OnTickParallel(float dt)
	{
		TickAux(isParallel: true);
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_needsSingleThreadTickOnce)
		{
			_needsSingleThreadTickOnce = false;
			TickAux(isParallel: false);
		}
	}

	public void TickAuxForInit()
	{
		TickAux(isParallel: false);
	}

	protected internal virtual void OnDeploymentStateChanged(bool isDeployed)
	{
		foreach (GameEntity removeOnDeployEntity in _removeOnDeployEntities)
		{
			removeOnDeployEntity.SetVisibilityExcludeParents(!isDeployed);
			StrategicArea firstScriptOfType = removeOnDeployEntity.GetFirstScriptOfType<StrategicArea>();
			if (firstScriptOfType != null)
			{
				firstScriptOfType.OnParentGameEntityVisibilityChanged(!isDeployed);
				continue;
			}
			foreach (StrategicArea item in from c in removeOnDeployEntity.GetChildren()
				where c.HasScriptOfType<StrategicArea>()
				select c.GetFirstScriptOfType<StrategicArea>())
			{
				item.OnParentGameEntityVisibilityChanged(!isDeployed);
			}
		}
		foreach (GameEntity addOnDeployEntity in _addOnDeployEntities)
		{
			addOnDeployEntity.SetVisibilityExcludeParents(isDeployed);
			addOnDeployEntity.GetFirstScriptOfType<MissionObject>()?.SetAbilityOfFaces(isDeployed);
			StrategicArea firstScriptOfType2 = addOnDeployEntity.GetFirstScriptOfType<StrategicArea>();
			if (firstScriptOfType2 != null)
			{
				firstScriptOfType2.OnParentGameEntityVisibilityChanged(isDeployed);
				continue;
			}
			foreach (StrategicArea item2 in from c in addOnDeployEntity.GetChildren()
				where c.HasScriptOfType<StrategicArea>()
				select c.GetFirstScriptOfType<StrategicArea>())
			{
				item2.OnParentGameEntityVisibilityChanged(isDeployed);
			}
		}
		if (_addOnDeployEntities.Count <= 0 && _removeOnDeployEntities.Count <= 0)
		{
			return;
		}
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.RefreshGameEntityWithWorldPosition();
		}
	}

	public override bool ShouldAutoLeaveDetachmentWhenDisabled(BattleSideEnum sideEnum)
	{
		return AutoAttachUserToFormation(sideEnum);
	}

	public override bool AutoAttachUserToFormation(BattleSideEnum sideEnum)
	{
		if (!base.Ai.HasActionCompleted)
		{
			return !IsDisabledDueToEnemyInRange(sideEnum);
		}
		return true;
	}

	public override bool HasToBeDefendedByUser(BattleSideEnum sideEnum)
	{
		if (!base.Ai.HasActionCompleted)
		{
			return IsDisabledDueToEnemyInRange(sideEnum);
		}
		return false;
	}

	protected float GetUserMultiplierOfWeapon()
	{
		int userCountIncludingInStruckAction = base.UserCountIncludingInStruckAction;
		if (userCountIncludingInStruckAction == 0)
		{
			return 0f;
		}
		return 0.7f + 0.3f * (float)userCountIncludingInStruckAction / (float)MaxUserCount;
	}

	protected virtual float GetDistanceMultiplierOfWeapon(Vec3 weaponPos)
	{
		if (GetMinimumDistanceBetweenPositions(weaponPos) > 20f)
		{
			return 0.4f;
		}
		Debug.FailedAssert("Invalid weapon type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\SiegeWeapon.cs", "GetDistanceMultiplierOfWeapon", 549);
		return 1f;
	}

	protected virtual float GetMinimumDistanceBetweenPositions(Vec3 position)
	{
		return base.GameEntity.GlobalPosition.DistanceSquared(position);
	}

	protected float GetHitPointMultiplierOfWeapon()
	{
		if (base.DestructionComponent != null)
		{
			return MathF.Max(1f, 2f - MathF.Log10(base.DestructionComponent.HitPoint / base.DestructionComponent.MaxHitPoint * 10f + 1f));
		}
		return 1f;
	}

	public WeakGameEntity GetTargetEntity()
	{
		return base.GameEntity;
	}

	public Vec3 GetTargetingOffset()
	{
		if (_targetingPositionOffset.HasValue)
		{
			return _targetingPositionOffset.Value;
		}
		return Vec3.Zero;
	}

	public BattleSideEnum GetSide()
	{
		return Side;
	}

	public Vec3 GetTargetGlobalVelocity()
	{
		if (this is IMoveableSiegeWeapon moveableSiegeWeapon)
		{
			return moveableSiegeWeapon.MovementComponent.Velocity;
		}
		return Vec3.Zero;
	}

	public bool IsDestructable()
	{
		return base.GameEntity.HasScriptOfType<DestructableComponent>();
	}

	public WeakGameEntity Entity()
	{
		return base.GameEntity;
	}

	public (Vec3, Vec3) ComputeGlobalPhysicsBoundingBoxMinMax()
	{
		return base.GameEntity.ComputeGlobalPhysicsBoundingBoxMinMax();
	}

	public virtual void OnShipCaptured(BattleSideEnum newDefaultSide)
	{
	}

	public abstract TargetFlags GetTargetFlags();

	public abstract float GetTargetValue(List<Vec3> weaponPos);
}
