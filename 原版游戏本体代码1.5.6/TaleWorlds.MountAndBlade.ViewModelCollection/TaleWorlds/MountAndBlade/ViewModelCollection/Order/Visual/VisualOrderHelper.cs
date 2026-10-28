namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public static class VisualOrderHelper
{
	public static bool DoesFormationHaveOrderType(Formation formation, OrderType type)
	{
		MovementOrder readonlyMovementOrderReference = formation.GetReadonlyMovementOrderReference();
		switch (type)
		{
		case OrderType.FireAtWill:
			return formation.FiringOrder.OrderEnum == FiringOrder.RangedWeaponUsageOrderEnum.FireAtWill;
		case OrderType.HoldFire:
			return formation.FiringOrder.OrderEnum == FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire;
		case OrderType.Mount:
			return formation.RidingOrder.OrderEnum == RidingOrder.RidingOrderEnum.Mount;
		case OrderType.Dismount:
			return formation.RidingOrder.OrderEnum == RidingOrder.RidingOrderEnum.Dismount;
		case OrderType.AIControlOn:
			return formation.IsAIControlled;
		case OrderType.AIControlOff:
			return !formation.IsAIControlled;
		case OrderType.LookAtDirection:
			return formation.FacingOrder.OrderEnum == FacingOrder.FacingOrderEnum.LookAtDirection;
		case OrderType.LookAtEnemy:
			return formation.FacingOrder.OrderEnum == FacingOrder.FacingOrderEnum.LookAtEnemy;
		default:
			if (readonlyMovementOrderReference.OrderType != type && formation.ArrangementOrder.OrderType != type && formation.FacingOrder.OrderType != type && formation.FiringOrder.OrderType != type && formation.FormOrder.OrderType != type)
			{
				return formation.RidingOrder.OrderType == type;
			}
			return true;
		}
	}
}
