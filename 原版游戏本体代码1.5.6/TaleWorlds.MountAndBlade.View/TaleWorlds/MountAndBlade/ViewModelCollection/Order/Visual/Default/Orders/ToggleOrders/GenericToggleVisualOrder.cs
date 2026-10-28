using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.ToggleOrders;

public class GenericToggleVisualOrder : VisualOrder
{
	private readonly TextObject _positiveOrderName;

	private readonly TextObject _negativeOrderName;

	public OrderType PositiveOrder { get; }

	public OrderType NegativeOrder { get; }

	public GenericToggleVisualOrder(string stringId, OrderType positiveOrder, OrderType negativeOrder)
		: base(stringId)
	{
		PositiveOrder = positiveOrder;
		NegativeOrder = negativeOrder;
		_positiveOrderName = GetOrderName(positiveOrder);
		_negativeOrderName = GetOrderName(negativeOrder);
	}

	public override TextObject GetName(OrderController orderController)
	{
		OrderState activeState = GetActiveState(orderController);
		if (activeState == OrderState.Active || activeState == OrderState.PartiallyActive)
		{
			return _positiveOrderName;
		}
		return _negativeOrderName;
	}

	public override bool IsTargeted()
	{
		return false;
	}

	private static TextObject GetOrderName(OrderType orderType)
	{
		return orderType switch
		{
			OrderType.FireAtWill => new TextObject("{=itoYrj8d}Firing at will"), 
			OrderType.HoldFire => new TextObject("{=VyI0rimN}Holding Fire"), 
			OrderType.Mount => new TextObject("{=ubTGIdcv}Mounted"), 
			OrderType.Dismount => new TextObject("{=Ema5Vd6o}Dismounted"), 
			OrderType.AIControlOn => new TextObject("{=zatDiaEI}Delegate Command On"), 
			OrderType.AIControlOff => new TextObject("{=JceqNdWx}Delegate Command Off"), 
			OrderType.LookAtDirection => new TextObject("{=1gC25EMb}Face this Direction"), 
			OrderType.LookAtEnemy => new TextObject("{=u8j8nN5U}Face Enemy"), 
			_ => TextObject.GetEmpty(), 
		};
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (GetActiveState(orderController) == OrderState.Active)
		{
			orderController.SetOrder(NegativeOrder);
		}
		else
		{
			orderController.SetOrder(PositiveOrder);
		}
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		if (VisualOrderHelper.DoesFormationHaveOrderType(formation, PositiveOrder))
		{
			return true;
		}
		return false;
	}

	protected override string GetIconId()
	{
		string iconId = base.GetIconId();
		if (_lastActiveState == OrderState.Active)
		{
			return iconId + "_active";
		}
		return iconId;
	}
}
