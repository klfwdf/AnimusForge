using NavalDLC.View.VisualOrders.Orders;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.ToggleOrders;

namespace NavalDLC.View.VisualOrders;

public class NavalShipVisualOrderProvider : VisualOrderProvider
{
	public override MBReadOnlyList<VisualOrderSet> GetOrders()
	{
		MBList<VisualOrderSet> mBList = new MBList<VisualOrderSet>();
		if (Input.IsGamepadActive)
		{
			GenericVisualOrderSet genericVisualOrderSet = new GenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement"), useActiveOrderForIconId: true, useActiveOrderForName: true);
			genericVisualOrderSet.AddOrder(new NavalMovementOrder("order_movement_move", OrderType.Move, new TextObject("{=F7JGCr9s}Move"), useWorldPosition: true));
			genericVisualOrderSet.AddOrder(new NavalMovementOrder("order_movement_follow", OrderType.FollowMe, new TextObject("{=5LpufKs7}Follow Me")));
			genericVisualOrderSet.AddOrder(new NavalSkirmishOrder("order_movement_skirmish"));
			genericVisualOrderSet.AddOrder(new NavalMovementOrder("order_movement_advance", OrderType.Advance, new TextObject("{=A38xbjqm}Engage"), useWorldPosition: false, isTargeted: true));
			genericVisualOrderSet.AddOrder(new NavalMovementOrder("order_movement_stop", OrderType.StandYourGround, new TextObject("{=QTr6UDAa}Stop")));
			genericVisualOrderSet.AddOrder(new NavalMovementOrder("order_movement_retreat", OrderType.Retreat, new TextObject("{=VbeHEAsa}Retreat")));
			genericVisualOrderSet.AddOrder(new ReturnVisualOrder());
			GenericVisualOrderSet genericVisualOrderSet2 = new GenericVisualOrderSet("order_type_toggle", new TextObject("{=0HTNYQz2}Toggle"), useActiveOrderForIconId: false, useActiveOrderForName: false);
			GenericToggleVisualOrder order = new GenericToggleVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire);
			GenericToggleVisualOrder genericToggleVisualOrder = (GameNetwork.IsMultiplayer ? null : new GenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff));
			genericVisualOrderSet2.AddOrder(order);
			if (genericToggleVisualOrder != null)
			{
				genericVisualOrderSet2.AddOrder(genericToggleVisualOrder);
			}
			genericVisualOrderSet2.AddOrder(new ReturnVisualOrder());
			mBList.Add(genericVisualOrderSet);
			mBList.Add(genericVisualOrderSet2);
			if (genericToggleVisualOrder != null)
			{
				mBList.Add(new SingleVisualOrderSet(genericToggleVisualOrder));
			}
			mBList.Add(new SingleVisualOrderSet(new ReturnVisualOrder()));
		}
		else
		{
			mBList.Add(CreateSingleOrderSetFor(new NavalMovementOrder("order_movement_move", OrderType.Move, new TextObject("{=F7JGCr9s}Move"), useWorldPosition: true)));
			mBList.Add(CreateSingleOrderSetFor(new NavalMovementOrder("order_movement_follow", OrderType.FollowMe, new TextObject("{=5LpufKs7}Follow Me"))));
			mBList.Add(CreateSingleOrderSetFor(new NavalSkirmishOrder("order_movement_skirmish")));
			mBList.Add(CreateSingleOrderSetFor(new NavalMovementOrder("order_movement_advance", OrderType.Advance, new TextObject("{=A38xbjqm}Engage"), useWorldPosition: false, isTargeted: true)));
			mBList.Add(CreateSingleOrderSetFor(new NavalMovementOrder("order_movement_stop", OrderType.StandYourGround, new TextObject("{=QTr6UDAa}Stop"))));
			mBList.Add(CreateSingleOrderSetFor(new NavalMovementOrder("order_movement_retreat", OrderType.Retreat, new TextObject("{=VbeHEAsa}Retreat"))));
			mBList.Add(CreateSingleOrderSetFor(new GenericToggleVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire)));
			GenericToggleVisualOrder genericToggleVisualOrder2 = (GameNetwork.IsMultiplayer ? null : new GenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff));
			if (genericToggleVisualOrder2 != null)
			{
				mBList.Add(CreateSingleOrderSetFor(genericToggleVisualOrder2));
			}
		}
		return mBList;
	}

	private SingleVisualOrderSet CreateSingleOrderSetFor(VisualOrder order)
	{
		return new SingleVisualOrderSet(order);
	}

	public override bool IsAvailable()
	{
		if (NavalDLCHelpers.IsNavalRaidMissionOpen())
		{
			return false;
		}
		Mission current = Mission.Current;
		if (current != null && current.IsNavalBattle)
		{
			return NavalDLCHelpers.IsShipOrdersAvailable();
		}
		return false;
	}
}
