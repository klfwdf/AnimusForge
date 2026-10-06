namespace TaleWorlds.MountAndBlade;

public struct FiringOrder
{
	public enum RangedWeaponUsageOrderEnum
	{
		FireAtWill,
		HoldYourFire
	}

	public readonly RangedWeaponUsageOrderEnum OrderEnum;

	public static readonly FiringOrder FiringOrderFireAtWill = new FiringOrder(RangedWeaponUsageOrderEnum.FireAtWill);

	public static readonly FiringOrder FiringOrderHoldYourFire = new FiringOrder(RangedWeaponUsageOrderEnum.HoldYourFire);

	public OrderType OrderType
	{
		get
		{
			if (OrderEnum != RangedWeaponUsageOrderEnum.FireAtWill)
			{
				return OrderType.HoldFire;
			}
			return OrderType.FireAtWill;
		}
	}

	private FiringOrder(RangedWeaponUsageOrderEnum orderEnum)
	{
		OrderEnum = orderEnum;
	}

	public override bool Equals(object obj)
	{
		if (obj is FiringOrder firingOrder)
		{
			return firingOrder == this;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)OrderEnum;
	}

	public static bool operator !=(FiringOrder f1, FiringOrder f2)
	{
		return f1.OrderEnum != f2.OrderEnum;
	}

	public static bool operator ==(FiringOrder f1, FiringOrder f2)
	{
		return f1.OrderEnum == f2.OrderEnum;
	}
}
