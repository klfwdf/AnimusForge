using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.View.VisualOrders.Orders.ToggleOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.FormOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.ToggleOrders;

namespace NavalDLC.View.VisualOrders;

public class NavalRaidVisualOrderProvider : VisualOrderProvider
{
	public override MBReadOnlyList<VisualOrderSet> GetOrders()
	{
		Mission current = Mission.Current;
		bool flag = current != null && current.PlayerTeam?.Side == BattleSideEnum.Attacker;
		MBList<VisualOrderSet> mBList = new MBList<VisualOrderSet>();
		GenericVisualOrderSet genericVisualOrderSet = new GenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement"), useActiveOrderForIconId: true, useActiveOrderForName: true);
		genericVisualOrderSet.AddOrder(new MoveVisualOrder("order_movement_move"));
		genericVisualOrderSet.AddOrder(new FollowMeVisualOrder("order_movement_follow"));
		genericVisualOrderSet.AddOrder(new ChargeVisualOrder("order_movement_charge"));
		genericVisualOrderSet.AddOrder(new AdvanceVisualOrder("order_movement_advance"));
		genericVisualOrderSet.AddOrder(new FallbackVisualOrder("order_movement_fallback"));
		genericVisualOrderSet.AddOrder(new StopVisualOrder("order_movement_stop"));
		if (!flag)
		{
			genericVisualOrderSet.AddOrder(new RetreatVisualOrder("order_movement_retreat"));
		}
		genericVisualOrderSet.AddOrder(new ReturnVisualOrder());
		GenericVisualOrderSet genericVisualOrderSet2 = new GenericVisualOrderSet("order_type_form", new TextObject("{=iBk2wbn3}Form"), useActiveOrderForIconId: true, useActiveOrderForName: true);
		ArrangementVisualOrder order = new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Line, "order_form_line");
		ArrangementVisualOrder order2 = new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.ShieldWall, "order_form_close");
		genericVisualOrderSet2.AddOrder(order);
		genericVisualOrderSet2.AddOrder(order2);
		genericVisualOrderSet2.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Loose, "order_form_loose"));
		genericVisualOrderSet2.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Circle, "order_form_circular"));
		genericVisualOrderSet2.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Square, "order_form_schiltron"));
		genericVisualOrderSet2.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Skein, "order_form_v"));
		genericVisualOrderSet2.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Column, "order_form_column"));
		genericVisualOrderSet2.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Scatter, "order_form_scatter"));
		genericVisualOrderSet2.AddOrder(new ReturnVisualOrder());
		GenericVisualOrderSet genericVisualOrderSet3 = new GenericVisualOrderSet("order_type_toggle", new TextObject("{=0HTNYQz2}Toggle"), useActiveOrderForIconId: false, useActiveOrderForName: false);
		ToggleFacingVisualOrder order3 = new ToggleFacingVisualOrder("order_toggle_facing");
		GenericToggleVisualOrder order4 = new GenericToggleVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire);
		GenericToggleVisualOrder genericToggleVisualOrder = (GameNetwork.IsMultiplayer ? null : new GenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff));
		TransferTroopsVisualOrder transferTroopsVisualOrder = ((GameNetwork.IsMultiplayer || flag) ? null : new TransferTroopsVisualOrder());
		genericVisualOrderSet3.AddOrder(order3);
		genericVisualOrderSet3.AddOrder(order4);
		if (genericToggleVisualOrder != null)
		{
			genericVisualOrderSet3.AddOrder(genericToggleVisualOrder);
		}
		if (transferTroopsVisualOrder != null)
		{
			genericVisualOrderSet3.AddOrder(transferTroopsVisualOrder);
		}
		genericVisualOrderSet3.AddOrder(new ReturnVisualOrder());
		mBList.Add(genericVisualOrderSet);
		mBList.Add(genericVisualOrderSet2);
		mBList.Add(genericVisualOrderSet3);
		if (!Input.IsGamepadActive)
		{
			mBList.Add(new SingleVisualOrderSet(order4));
			if (genericToggleVisualOrder != null)
			{
				mBList.Add(new SingleVisualOrderSet(genericToggleVisualOrder));
			}
			mBList.Add(new SingleVisualOrderSet(order3));
			mBList.Add(new SingleVisualOrderSet(order2));
			mBList.Add(new SingleVisualOrderSet(order));
		}
		return mBList;
	}

	public override bool IsAvailable()
	{
		return NavalDLCHelpers.IsNavalRaidMissionOpen();
	}
}
