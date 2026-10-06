using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.View.VisualOrders.Orders;
using TaleWorlds.MountAndBlade.View.VisualOrders.Orders.ToggleOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.FormOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.ToggleOrders;

namespace TaleWorlds.MountAndBlade.View.VisualOrders;

public class DefaultVisualOrderProvider : VisualOrderProvider
{
	public override bool IsAvailable()
	{
		if (Mission.Current != null)
		{
			return !Mission.Current.IsFriendlyMission;
		}
		return false;
	}

	public override MBReadOnlyList<VisualOrderSet> GetOrders()
	{
		if (BannerlordConfig.OrderLayoutType == 1)
		{
			return GetLegacyOrders();
		}
		return GetDefaultOrders();
	}

	private MBReadOnlyList<VisualOrderSet> GetDefaultOrders()
	{
		MBList<VisualOrderSet> mBList = new MBList<VisualOrderSet>();
		GenericVisualOrderSet genericVisualOrderSet = new GenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement"), useActiveOrderForIconId: true, useActiveOrderForName: true);
		genericVisualOrderSet.AddOrder(new MoveVisualOrder("order_movement_move"));
		genericVisualOrderSet.AddOrder(new FollowMeVisualOrder("order_movement_follow"));
		genericVisualOrderSet.AddOrder(new ChargeVisualOrder("order_movement_charge"));
		genericVisualOrderSet.AddOrder(new AdvanceVisualOrder("order_movement_advance"));
		genericVisualOrderSet.AddOrder(new FallbackVisualOrder("order_movement_fallback"));
		genericVisualOrderSet.AddOrder(new StopVisualOrder("order_movement_stop"));
		genericVisualOrderSet.AddOrder(new RetreatVisualOrder("order_movement_retreat"));
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
		GenericToggleVisualOrder order5 = new GenericToggleVisualOrder("order_toggle_mount", OrderType.Mount, OrderType.Dismount);
		GenericToggleVisualOrder genericToggleVisualOrder = (GameNetwork.IsMultiplayer ? null : new GenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff));
		TransferTroopsVisualOrder transferTroopsVisualOrder = (GameNetwork.IsMultiplayer ? null : new TransferTroopsVisualOrder());
		genericVisualOrderSet3.AddOrder(order3);
		genericVisualOrderSet3.AddOrder(order4);
		genericVisualOrderSet3.AddOrder(order5);
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
			mBList.Add(new SingleVisualOrderSet(order5));
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

	private MBList<VisualOrderSet> GetLegacyOrders()
	{
		MBList<VisualOrderSet> mBList = new MBList<VisualOrderSet>();
		GenericVisualOrderSet genericVisualOrderSet = new GenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement"), useActiveOrderForIconId: true, useActiveOrderForName: false);
		genericVisualOrderSet.AddOrder(new MoveVisualOrder("order_movement_move"));
		genericVisualOrderSet.AddOrder(new FollowMeVisualOrder("order_movement_follow"));
		genericVisualOrderSet.AddOrder(new ChargeVisualOrder("order_movement_charge"));
		genericVisualOrderSet.AddOrder(new AdvanceVisualOrder("order_movement_advance"));
		genericVisualOrderSet.AddOrder(new FallbackVisualOrder("order_movement_fallback"));
		genericVisualOrderSet.AddOrder(new StopVisualOrder("order_movement_stop"));
		genericVisualOrderSet.AddOrder(new RetreatVisualOrder("order_movement_retreat"));
		genericVisualOrderSet.AddOrder(new ReturnVisualOrder());
		GenericVisualOrderSet genericVisualOrderSet2 = new GenericVisualOrderSet("order_type_facing", new TextObject("{=psynaDsM}Facing"), useActiveOrderForIconId: true, useActiveOrderForName: false);
		SingleVisualOrder order = new SingleVisualOrder("order_toggle_facing", new TextObject("{=MH9Pi3ao}Face Direction"), OrderType.LookAtDirection, useFormationTarget: false, useWorldPositionTarget: true);
		SingleVisualOrder order2 = new SingleVisualOrder("order_toggle_facing_active", new TextObject("{=u8j8nN5U}Face Enemy"), OrderType.LookAtEnemy, useFormationTarget: false, useWorldPositionTarget: false);
		genericVisualOrderSet2.AddOrder(order);
		genericVisualOrderSet2.AddOrder(order2);
		GenericVisualOrderSet genericVisualOrderSet3 = new GenericVisualOrderSet("order_type_form", new TextObject("{=iBk2wbn3}Form"), useActiveOrderForIconId: true, useActiveOrderForName: true);
		ArrangementVisualOrder order3 = new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Line, "order_form_line");
		ArrangementVisualOrder order4 = new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.ShieldWall, "order_form_close");
		genericVisualOrderSet3.AddOrder(order3);
		genericVisualOrderSet3.AddOrder(order4);
		genericVisualOrderSet3.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Loose, "order_form_loose"));
		genericVisualOrderSet3.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Circle, "order_form_circular"));
		genericVisualOrderSet3.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Square, "order_form_schiltron"));
		genericVisualOrderSet3.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Skein, "order_form_v"));
		genericVisualOrderSet3.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Column, "order_form_column"));
		genericVisualOrderSet3.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Scatter, "order_form_scatter"));
		genericVisualOrderSet3.AddOrder(new ReturnVisualOrder());
		mBList.Add(genericVisualOrderSet);
		mBList.Add(genericVisualOrderSet2);
		mBList.Add(genericVisualOrderSet3);
		GenericToggleVisualOrder order5 = new GenericToggleVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire);
		GenericToggleVisualOrder order6 = new GenericToggleVisualOrder("order_toggle_mount", OrderType.Mount, OrderType.Dismount);
		GenericToggleVisualOrder genericToggleVisualOrder = (GameNetwork.IsMultiplayer ? null : new GenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff));
		TransferTroopsVisualOrder transferTroopsVisualOrder = (GameNetwork.IsMultiplayer ? null : new TransferTroopsVisualOrder());
		if (!Input.IsGamepadActive)
		{
			mBList.Add(new SingleVisualOrderSet(order5));
			mBList.Add(new SingleVisualOrderSet(order6));
			if (genericToggleVisualOrder != null)
			{
				mBList.Add(new SingleVisualOrderSet(genericToggleVisualOrder));
			}
			if (transferTroopsVisualOrder != null)
			{
				mBList.Add(new SingleVisualOrderSet(transferTroopsVisualOrder));
			}
			mBList.Add(new SingleVisualOrderSet(new ReturnVisualOrder()));
		}
		return mBList;
	}
}
