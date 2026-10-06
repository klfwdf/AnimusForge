namespace TaleWorlds.MountAndBlade;

public struct RidingOrder
{
	public enum RidingOrderEnum
	{
		Free,
		Mount,
		Dismount
	}

	public readonly RidingOrderEnum OrderEnum;

	public static readonly RidingOrder RidingOrderFree = new RidingOrder(RidingOrderEnum.Free);

	public static readonly RidingOrder RidingOrderMount = new RidingOrder(RidingOrderEnum.Mount);

	public static readonly RidingOrder RidingOrderDismount = new RidingOrder(RidingOrderEnum.Dismount);

	public OrderType OrderType
	{
		get
		{
			if (OrderEnum != RidingOrderEnum.Free)
			{
				if (OrderEnum != RidingOrderEnum.Mount)
				{
					return OrderType.Dismount;
				}
				return OrderType.Mount;
			}
			return OrderType.RideFree;
		}
	}

	private RidingOrder(RidingOrderEnum orderEnum)
	{
		OrderEnum = orderEnum;
	}

	public override bool Equals(object obj)
	{
		if (obj is RidingOrder ridingOrder)
		{
			return ridingOrder == this;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)OrderEnum;
	}

	public static bool operator !=(RidingOrder r1, RidingOrder r2)
	{
		return r1.OrderEnum != r2.OrderEnum;
	}

	public static bool operator ==(RidingOrder r1, RidingOrder r2)
	{
		return r1.OrderEnum == r2.OrderEnum;
	}
}
