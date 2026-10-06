using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace TaleWorlds.MountAndBlade.View.VisualOrders.Orders;

public class SingleVisualOrder : VisualOrder
{
	private TextObject _name;

	private OrderType _orderType;

	private bool _useFormationTarget;

	private bool _useWorldPositionTarget;

	public SingleVisualOrder(string stringId, TextObject name, OrderType orderType, bool useFormationTarget, bool useWorldPositionTarget)
		: base(stringId)
	{
		_name = name;
		_orderType = orderType;
		_useFormationTarget = useFormationTarget;
		_useWorldPositionTarget = useWorldPositionTarget;
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (executionParameters.HasFormation && _useFormationTarget)
		{
			orderController.SetOrderWithFormation(_orderType, executionParameters.Formation);
		}
		else if (executionParameters.HasWorldPosition && _useWorldPositionTarget)
		{
			orderController.SetOrderWithPosition(_orderType, executionParameters.WorldPosition);
		}
		else
		{
			orderController.SetOrder(_orderType);
		}
	}

	public override TextObject GetName(OrderController orderController)
	{
		return _name;
	}

	public override bool IsTargeted()
	{
		if (!_useFormationTarget)
		{
			return _useWorldPositionTarget;
		}
		return true;
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return VisualOrderHelper.DoesFormationHaveOrderType(formation, _orderType);
	}
}
