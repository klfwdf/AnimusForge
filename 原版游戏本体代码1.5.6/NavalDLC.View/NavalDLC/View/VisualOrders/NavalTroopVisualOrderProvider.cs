using NavalDLC.View.VisualOrders.Orders;
using NavalDLC.View.VisualOrders.Orders.TroopOrders;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.ToggleOrders;

namespace NavalDLC.View.VisualOrders;

public class NavalTroopVisualOrderProvider : VisualOrderProvider
{
	public override MBReadOnlyList<VisualOrderSet> GetOrders()
	{
		MBList<VisualOrderSet> mBList = new MBList<VisualOrderSet>();
		if (Input.IsGamepadActive)
		{
			GenericVisualOrderSet genericVisualOrderSet = new GenericVisualOrderSet("troop_visual_orders", new TextObject("{=bEmrKaHS}Orders"), useActiveOrderForIconId: true, useActiveOrderForName: false);
			genericVisualOrderSet.AddOrder(new NavalTroopDefendShipOrder("naval_troop_defend_ship_order"));
			genericVisualOrderSet.AddOrder(new FollowMeVisualOrder("order_movement_follow"));
			genericVisualOrderSet.AddOrder(new NavalChargeVisualOrder("order_movement_charge"));
			genericVisualOrderSet.AddOrder(new GenericToggleVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire));
			if (!GameNetwork.IsMultiplayer)
			{
				genericVisualOrderSet.AddOrder(new GenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff));
			}
			genericVisualOrderSet.AddOrder(new ReturnVisualOrder());
			mBList.Add(genericVisualOrderSet);
			mBList.Add(new SingleVisualOrderSet(new ReturnVisualOrder()));
		}
		else
		{
			mBList.Add(new SingleVisualOrderSet(new NavalTroopDefendShipOrder("naval_troop_defend_ship_order")));
			mBList.Add(new SingleVisualOrderSet(new FollowMeVisualOrder("order_movement_follow")));
			mBList.Add(new SingleVisualOrderSet(new NavalChargeVisualOrder("order_movement_charge")));
			mBList.Add(new SingleVisualOrderSet(new GenericToggleVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire)));
			if (!GameNetwork.IsMultiplayer)
			{
				mBList.Add(new SingleVisualOrderSet(new GenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff)));
			}
		}
		return mBList;
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
			return !NavalDLCHelpers.IsShipOrdersAvailable();
		}
		return false;
	}
}
