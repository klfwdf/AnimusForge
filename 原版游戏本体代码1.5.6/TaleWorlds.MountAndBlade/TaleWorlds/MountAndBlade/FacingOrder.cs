using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct FacingOrder
{
	public enum FacingOrderEnum
	{
		LookAtDirection,
		LookAtEnemy
	}

	public readonly FacingOrderEnum OrderEnum;

	private readonly Vec2 _lookAtDirection;

	public static readonly FacingOrder FacingOrderLookAtEnemy = new FacingOrder(FacingOrderEnum.LookAtEnemy);

	public OrderType OrderType
	{
		get
		{
			if (OrderEnum != FacingOrderEnum.LookAtDirection)
			{
				return OrderType.LookAtEnemy;
			}
			return OrderType.LookAtDirection;
		}
	}

	public static FacingOrder FacingOrderLookAtDirection(Vec2 direction)
	{
		return new FacingOrder(FacingOrderEnum.LookAtDirection, direction);
	}

	private FacingOrder(FacingOrderEnum orderEnum, Vec2 direction)
	{
		OrderEnum = orderEnum;
		_lookAtDirection = direction;
	}

	private FacingOrder(FacingOrderEnum orderEnum)
	{
		OrderEnum = orderEnum;
		_lookAtDirection = Vec2.Invalid;
	}

	private Vec2 GetDirectionAux(Formation f, Agent targetAgent)
	{
		if (f.PhysicalClass.IsMounted() && targetAgent != null && targetAgent.Velocity.LengthSquared > targetAgent.GetMaximumForwardUnlimitedSpeed() * targetAgent.GetMaximumForwardUnlimitedSpeed() * 0.09f)
		{
			return targetAgent.Velocity.AsVec2.Normalized();
		}
		if (OrderEnum == FacingOrderEnum.LookAtDirection)
		{
			return _lookAtDirection;
		}
		if (f.Arrangement is CircularFormation || f.Arrangement is SquareFormation)
		{
			return f.Direction;
		}
		Vec2 currentPosition = f.CurrentPosition;
		Vec2 weightedAverageEnemyPosition = f.QuerySystem.WeightedAverageEnemyPosition;
		if (!weightedAverageEnemyPosition.IsValid)
		{
			return f.Direction;
		}
		Vec2 vec = (weightedAverageEnemyPosition - currentPosition).Normalized();
		float length = (weightedAverageEnemyPosition - currentPosition).Length;
		int enemyUnitCount = f.QuerySystem.Team.EnemyUnitCount;
		int countOfUnits = f.CountOfUnits;
		Vec2 vec2 = f.Direction;
		bool flag = !(length < (float)countOfUnits * 0.2f);
		if (enemyUnitCount == 0 || countOfUnits == 0)
		{
			flag = false;
		}
		float num = ((!flag) ? 1f : (MBMath.ClampFloat((float)countOfUnits * 1f / (float)enemyUnitCount, 1f / 3f, 3f) * MBMath.ClampFloat(length / (float)countOfUnits, 1f / 3f, 3f)));
		if (flag && MathF.Abs(vec.AngleBetween(vec2)) > 0.17453292f * num)
		{
			vec2 = vec;
		}
		return vec2;
	}

	public Vec2 GetDirection(Formation f, Agent targetAgent = null)
	{
		return GetDirectionAux(f, targetAgent);
	}

	public override bool Equals(object obj)
	{
		if (obj is FacingOrder facingOrder)
		{
			return facingOrder == this;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)OrderEnum;
	}

	public static bool operator !=(FacingOrder f1, FacingOrder f2)
	{
		return f1.OrderEnum != f2.OrderEnum;
	}

	public static bool operator ==(FacingOrder f1, FacingOrder f2)
	{
		return f1.OrderEnum == f2.OrderEnum;
	}
}
