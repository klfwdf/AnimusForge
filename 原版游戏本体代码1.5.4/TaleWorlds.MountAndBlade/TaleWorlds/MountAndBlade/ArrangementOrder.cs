using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct ArrangementOrder
{
	public enum ArrangementOrderEnum
	{
		Circle,
		Column,
		Line,
		Loose,
		Scatter,
		ShieldWall,
		Skein,
		Square
	}

	private float? _walkRestriction;

	private float? _runRestriction;

	private int _unitSpacing;

	public readonly ArrangementOrderEnum OrderEnum;

	public static readonly ArrangementOrder ArrangementOrderCircle = new ArrangementOrder(ArrangementOrderEnum.Circle);

	public static readonly ArrangementOrder ArrangementOrderColumn = new ArrangementOrder(ArrangementOrderEnum.Column);

	public static readonly ArrangementOrder ArrangementOrderLine = new ArrangementOrder(ArrangementOrderEnum.Line);

	public static readonly ArrangementOrder ArrangementOrderLoose = new ArrangementOrder(ArrangementOrderEnum.Loose);

	public static readonly ArrangementOrder ArrangementOrderScatter = new ArrangementOrder(ArrangementOrderEnum.Scatter);

	public static readonly ArrangementOrder ArrangementOrderShieldWall = new ArrangementOrder(ArrangementOrderEnum.ShieldWall);

	public static readonly ArrangementOrder ArrangementOrderSkein = new ArrangementOrder(ArrangementOrderEnum.Skein);

	public static readonly ArrangementOrder ArrangementOrderSquare = new ArrangementOrder(ArrangementOrderEnum.Square);

	public OrderType OrderType => OrderEnum switch
	{
		ArrangementOrderEnum.Circle => OrderType.ArrangementCircular, 
		ArrangementOrderEnum.Column => OrderType.ArrangementColumn, 
		ArrangementOrderEnum.Line => OrderType.ArrangementLine, 
		ArrangementOrderEnum.Loose => OrderType.ArrangementLoose, 
		ArrangementOrderEnum.Scatter => OrderType.ArrangementScatter, 
		ArrangementOrderEnum.ShieldWall => OrderType.ArrangementCloseOrder, 
		ArrangementOrderEnum.Skein => OrderType.ArrangementVee, 
		ArrangementOrderEnum.Square => OrderType.ArrangementSchiltron, 
		_ => OrderType.ArrangementLine, 
	};

	public static int GetUnitSpacingOf(ArrangementOrderEnum a)
	{
		switch (a)
		{
		case ArrangementOrderEnum.Loose:
			return 6;
		case ArrangementOrderEnum.ShieldWall:
		case ArrangementOrderEnum.Square:
			return 0;
		default:
			return 2;
		}
	}

	public static bool GetUnitLooseness(ArrangementOrderEnum a)
	{
		return a != ArrangementOrderEnum.ShieldWall;
	}

	public ArrangementOrder(ArrangementOrderEnum orderEnum)
	{
		OrderEnum = orderEnum;
		_walkRestriction = null;
		switch (OrderEnum)
		{
		case ArrangementOrderEnum.Line:
			_runRestriction = 0.8f;
			break;
		case ArrangementOrderEnum.Circle:
			_runRestriction = 0.5f;
			break;
		case ArrangementOrderEnum.ShieldWall:
		case ArrangementOrderEnum.Square:
			_runRestriction = 0.3f;
			break;
		case ArrangementOrderEnum.Loose:
		case ArrangementOrderEnum.Scatter:
		case ArrangementOrderEnum.Skein:
			_runRestriction = 0.9f;
			break;
		default:
			_runRestriction = 1f;
			break;
		}
		_unitSpacing = GetUnitSpacingOf(OrderEnum);
	}

	public void GetMovementSpeedRestriction(out float? runRestriction, out float? walkRestriction)
	{
		runRestriction = _runRestriction;
		walkRestriction = _walkRestriction;
	}

	public IFormationArrangement GetArrangement(Formation formation)
	{
		return OrderEnum switch
		{
			ArrangementOrderEnum.Circle => new CircularFormation(formation), 
			ArrangementOrderEnum.Column => new ColumnFormation(formation), 
			ArrangementOrderEnum.Skein => new SkeinFormation(formation), 
			ArrangementOrderEnum.Square => new RectilinearSchiltronFormation(formation), 
			_ => new LineFormation(formation), 
		};
	}

	public void OnApply(Formation formation)
	{
		formation.SetPositioning(null, null, GetUnitSpacing());
		Rearrange(formation);
		if (OrderEnum == ArrangementOrderEnum.Scatter)
		{
			TickOccasionally(formation);
			formation.ResetArrangementOrderTickTimer();
		}
		ArrangementOrderEnum orderEnum = OrderEnum;
		formation.ApplyActionOnEachUnit(delegate(Agent agent)
		{
			if (agent.IsAIControlled)
			{
				Agent.UsageDirection shieldDirectionOfUnit = GetShieldDirectionOfUnit(formation, agent, orderEnum);
				agent.EnforceShieldUsage(shieldDirectionOfUnit);
			}
			agent.UpdateAgentProperties();
			MovementOrder readonlyMovementOrderReference = formation.GetReadonlyMovementOrderReference();
			MovementOrder.MovementOrderEnum movementOrderEnum = readonlyMovementOrderReference.OrderEnum;
			if ((movementOrderEnum == MovementOrder.MovementOrderEnum.Charge || movementOrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget) && readonlyMovementOrderReference.GetPosition(formation).IsValid)
			{
				movementOrderEnum = MovementOrder.MovementOrderEnum.Move;
			}
			agent.RefreshBehaviorValues(movementOrderEnum, orderEnum);
		});
	}

	public void SoftUpdate(Formation formation)
	{
		if (OrderEnum == ArrangementOrderEnum.Scatter)
		{
			TickOccasionally(formation);
			formation.ResetArrangementOrderTickTimer();
		}
	}

	public static Agent.UsageDirection GetShieldDirectionOfUnit(Formation formation, Agent unit, ArrangementOrderEnum orderEnum)
	{
		if (unit.IsDetachedFromFormation)
		{
			return Agent.UsageDirection.None;
		}
		switch (orderEnum)
		{
		case ArrangementOrderEnum.ShieldWall:
			if (((IFormationUnit)unit).FormationRankIndex == 0)
			{
				return Agent.UsageDirection.DefendDown;
			}
			if (formation.Arrangement.GetNeighborUnitOfLeftSide(unit) == null)
			{
				return Agent.UsageDirection.DefendLeft;
			}
			if (formation.Arrangement.GetNeighborUnitOfRightSide(unit) == null)
			{
				return Agent.UsageDirection.DefendRight;
			}
			return Agent.UsageDirection.AttackEnd;
		case ArrangementOrderEnum.Circle:
		case ArrangementOrderEnum.Square:
			if (((IFormationUnit)unit).IsShieldUsageEncouraged)
			{
				if (((IFormationUnit)unit).FormationRankIndex == 0)
				{
					return Agent.UsageDirection.DefendDown;
				}
				return Agent.UsageDirection.AttackEnd;
			}
			return Agent.UsageDirection.None;
		default:
			return Agent.UsageDirection.None;
		}
	}

	public int GetUnitSpacing()
	{
		return _unitSpacing;
	}

	public void Rearrange(Formation formation)
	{
		if (OrderEnum == ArrangementOrderEnum.Column)
		{
			RearrangeAux(formation, isDirectly: false);
		}
		else
		{
			formation.Rearrange(GetArrangement(formation));
		}
	}

	public void RearrangeAux(Formation formation, bool isDirectly)
	{
		if (!isDirectly)
		{
			TransposeLineFormation(formation);
			formation.OnTick += formation.TickForColumnArrangementInitialPositioning;
		}
		else
		{
			formation.OnTick -= formation.TickForColumnArrangementInitialPositioning;
			formation.ReferencePosition = null;
			formation.Rearrange(GetArrangement(formation));
		}
	}

	public static void TransposeLineFormation(Formation formation)
	{
		formation.Rearrange(new TransposedLineFormation(formation));
		formation.SetPositioning(formation.GetReadonlyMovementOrderReference().CreateNewOrderWorldPositionMT(formation, WorldPosition.WorldPositionEnforcedCache.None));
		formation.ReferencePosition = formation.OrderPosition;
	}

	public void OnCancel(Formation formation)
	{
		if (OrderEnum == ArrangementOrderEnum.Scatter && formation.Team?.TeamAI != null)
		{
			MBReadOnlyList<StrategicArea> strategicAreas = formation.Team.TeamAI.StrategicAreas;
			for (int num = formation.Detachments.Count - 1; num >= 0; num--)
			{
				IDetachment detachment = formation.Detachments[num];
				foreach (StrategicArea item in strategicAreas)
				{
					if (detachment == item)
					{
						formation.LeaveDetachment(detachment);
						break;
					}
				}
			}
		}
		if (OrderEnum == ArrangementOrderEnum.ShieldWall || OrderEnum == ArrangementOrderEnum.Circle || OrderEnum == ArrangementOrderEnum.Square)
		{
			formation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				if (agent.IsAIControlled)
				{
					agent.EnforceShieldUsage(Agent.UsageDirection.None);
				}
			});
		}
		if (OrderEnum == ArrangementOrderEnum.Column)
		{
			formation.OnTick -= formation.TickForColumnArrangementInitialPositioning;
		}
	}

	private static StrategicArea CreateStrategicArea(Scene scene, WorldPosition position, Vec2 direction, float width, int capacity, BattleSideEnum side)
	{
		WorldFrame worldFrame = new WorldFrame(new Mat3
		{
			f = direction.ToVec3(),
			u = Vec3.Up
		}, position);
		GameEntity gameEntity = GameEntity.Instantiate(scene, "strategic_area_autogen", worldFrame.ToNavMeshMatrixFrame());
		gameEntity.SetMobility(GameEntity.Mobility.Dynamic);
		StrategicArea firstScriptOfType = gameEntity.GetFirstScriptOfType<StrategicArea>();
		firstScriptOfType.InitializeAutogenerated(width, capacity, side);
		return firstScriptOfType;
	}

	private static IEnumerable<StrategicArea> CreateStrategicAreas(Mission mission, int count, WorldPosition center, float distance, WorldPosition target, float width, int capacity, BattleSideEnum side)
	{
		Scene scene = mission.Scene;
		float distanceMultiplied = distance * 0.7f;
		Func<WorldPosition> func = delegate
		{
			WorldPosition result = center;
			float rotation = MBRandom.RandomFloat * System.MathF.PI * 2f;
			result.SetVec2(center.AsVec2 + Vec2.FromRotation(rotation) * distanceMultiplied);
			return result;
		};
		WorldPosition[] array = ((Func<WorldPosition[]>)delegate
		{
			float num3 = MBRandom.RandomFloat * System.MathF.PI * 2f;
			switch (count)
			{
			case 2:
			{
				WorldPosition worldPosition9 = center;
				worldPosition9.SetVec2(center.AsVec2 + Vec2.FromRotation(num3) * distanceMultiplied);
				WorldPosition worldPosition10 = center;
				worldPosition10.SetVec2(center.AsVec2 + Vec2.FromRotation(num3 + System.MathF.PI) * distanceMultiplied);
				return new WorldPosition[2] { worldPosition9, worldPosition10 };
			}
			case 3:
			{
				WorldPosition worldPosition6 = center;
				worldPosition6.SetVec2(center.AsVec2 + Vec2.FromRotation(num3 + 0f) * distanceMultiplied);
				WorldPosition worldPosition7 = center;
				worldPosition7.SetVec2(center.AsVec2 + Vec2.FromRotation(num3 + System.MathF.PI * 2f / 3f) * distanceMultiplied);
				WorldPosition worldPosition8 = center;
				worldPosition8.SetVec2(center.AsVec2 + Vec2.FromRotation(num3 + 4.1887903f) * distanceMultiplied);
				return new WorldPosition[3] { worldPosition6, worldPosition7, worldPosition8 };
			}
			case 4:
			{
				WorldPosition worldPosition2 = center;
				worldPosition2.SetVec2(center.AsVec2 + Vec2.FromRotation(num3 + 0f) * distanceMultiplied);
				WorldPosition worldPosition3 = center;
				worldPosition3.SetVec2(center.AsVec2 + Vec2.FromRotation(num3 + System.MathF.PI / 2f) * distanceMultiplied);
				WorldPosition worldPosition4 = center;
				worldPosition4.SetVec2(center.AsVec2 + Vec2.FromRotation(num3 + System.MathF.PI) * distanceMultiplied);
				WorldPosition worldPosition5 = center;
				worldPosition5.SetVec2(center.AsVec2 + Vec2.FromRotation(num3 + 4.712389f) * distanceMultiplied);
				return new WorldPosition[4] { worldPosition2, worldPosition3, worldPosition4, worldPosition5 };
			}
			default:
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Orders\\ArrangementOrder.cs", "CreateStrategicAreas", 369);
				return new WorldPosition[0];
			}
		})();
		List<WorldPosition> positions = new List<WorldPosition>();
		WorldPosition[] array2 = array;
		foreach (WorldPosition worldPosition in array2)
		{
			WorldPosition center2 = worldPosition;
			WorldPosition position = mission.FindPositionWithBiggestSlopeTowardsDirectionInSquare(ref center2, distance * 0.25f, ref target);
			Func<WorldPosition, bool> func2 = delegate(WorldPosition p)
			{
				if (!positions.Any((WorldPosition wp) => wp.AsVec2.DistanceSquared(p.AsVec2) < 1f) && scene.GetPathDistanceBetweenPositions(ref center, ref p, 0f, out var pathDistance) && pathDistance < center.AsVec2.Distance(p.AsVec2) * 2f)
				{
					positions.Add(position);
					return true;
				}
				return false;
			};
			if (!func2(position) && !func2(worldPosition))
			{
				int num2 = 0;
				while (num2++ < 10 && !func2(func()))
				{
				}
				if (num2 >= 10)
				{
					positions.Add(center);
				}
			}
		}
		Vec2 direction = (target.AsVec2 - center.AsVec2).Normalized();
		foreach (WorldPosition item in positions)
		{
			yield return CreateStrategicArea(scene, item, direction, width, capacity, side);
		}
	}

	private bool IsStrategicAreaClose(StrategicArea strategicArea, Formation formation)
	{
		if (formation.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Charge && formation.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.ChargeToTarget && strategicArea.IsUsableBy(formation.Team.Side))
		{
			if (strategicArea.IgnoreHeight)
			{
				if (TaleWorlds.Library.MathF.Abs(strategicArea.GameEntity.GlobalPosition.x - formation.OrderPosition.X) <= strategicArea.DistanceToCheck)
				{
					return TaleWorlds.Library.MathF.Abs(strategicArea.GameEntity.GlobalPosition.y - formation.OrderPosition.Y) <= strategicArea.DistanceToCheck;
				}
				return false;
			}
			return formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None).DistanceSquaredWithLimit(strategicArea.GameEntity.GlobalPosition, strategicArea.DistanceToCheck * strategicArea.DistanceToCheck + 1E-05f) < strategicArea.DistanceToCheck * strategicArea.DistanceToCheck;
		}
		return false;
	}

	public void TickOccasionally(Formation formation)
	{
		if (OrderEnum != ArrangementOrderEnum.Scatter || formation.Team?.TeamAI == null)
		{
			return;
		}
		MBReadOnlyList<StrategicArea> strategicAreas = formation.Team.TeamAI.StrategicAreas;
		foreach (StrategicArea item in strategicAreas)
		{
			if (!IsStrategicAreaClose(item, formation))
			{
				continue;
			}
			bool flag = false;
			foreach (IDetachment detachment2 in formation.Detachments)
			{
				if (item == detachment2)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				formation.JoinDetachment(item);
			}
		}
		for (int num = formation.Detachments.Count - 1; num >= 0; num--)
		{
			IDetachment detachment = formation.Detachments[num];
			foreach (StrategicArea item2 in strategicAreas)
			{
				if (detachment == item2 && !IsStrategicAreaClose(item2, formation))
				{
					formation.LeaveDetachment(detachment);
					break;
				}
			}
		}
	}

	public ArrangementOrderEnum GetNativeEnum()
	{
		return OrderEnum;
	}

	public override bool Equals(object obj)
	{
		if (obj is ArrangementOrder)
		{
			return (ArrangementOrder)obj == this;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)OrderEnum;
	}

	public static bool operator !=(ArrangementOrder a1, ArrangementOrder a2)
	{
		return a1.OrderEnum != a2.OrderEnum;
	}

	public static bool operator ==(ArrangementOrder a1, ArrangementOrder a2)
	{
		return a1.OrderEnum == a2.OrderEnum;
	}

	public void OnOrderPositionChanged(Formation formation, Vec2 previousOrderPosition)
	{
		if (OrderEnum == ArrangementOrderEnum.Column && formation.Arrangement is TransposedLineFormation)
		{
			Vec2 direction = formation.Direction;
			Vec2 vector = (formation.OrderPosition - previousOrderPosition).Normalized();
			float num = direction.AngleBetween(vector);
			if ((num > System.MathF.PI / 2f || num < -System.MathF.PI / 2f) && formation.CachedAveragePosition.DistanceSquared(formation.OrderPosition) < formation.Depth * formation.Depth / 10f)
			{
				formation.ReferencePosition = formation.OrderPosition;
			}
		}
	}

	public static int GetArrangementOrderDefensiveness(ArrangementOrderEnum orderEnum)
	{
		if (orderEnum == ArrangementOrderEnum.Circle || orderEnum == ArrangementOrderEnum.ShieldWall || orderEnum == ArrangementOrderEnum.Square)
		{
			return 1;
		}
		return 0;
	}

	public static int GetArrangementOrderDefensivenessChange(ArrangementOrderEnum previousOrderEnum, ArrangementOrderEnum nextOrderEnum)
	{
		if (previousOrderEnum == ArrangementOrderEnum.Circle || previousOrderEnum == ArrangementOrderEnum.ShieldWall || previousOrderEnum == ArrangementOrderEnum.Square)
		{
			if (nextOrderEnum != ArrangementOrderEnum.Circle && nextOrderEnum != ArrangementOrderEnum.ShieldWall && nextOrderEnum != ArrangementOrderEnum.Square)
			{
				return -1;
			}
			return 0;
		}
		if (nextOrderEnum == ArrangementOrderEnum.Circle || nextOrderEnum == ArrangementOrderEnum.ShieldWall || nextOrderEnum == ArrangementOrderEnum.Square)
		{
			return 1;
		}
		return 0;
	}

	public float CalculateFormationDirectionEnforcingFactorForRank(int formationRankIndex, int rankCount)
	{
		if (OrderEnum == ArrangementOrderEnum.Circle || OrderEnum == ArrangementOrderEnum.ShieldWall || OrderEnum == ArrangementOrderEnum.Square)
		{
			return 1f - MBMath.ClampFloat(((float)formationRankIndex + 1f) / ((float)rankCount * 2f), 0f, 1f);
		}
		if (OrderEnum == ArrangementOrderEnum.Column)
		{
			return 0f;
		}
		return 1f - MBMath.ClampFloat(((float)formationRankIndex + 1f) / ((float)rankCount * 0.5f), 0f, 1f);
	}
}
