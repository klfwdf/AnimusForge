using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public sealed class ActionVisualOrder : VisualOrder
{
	public delegate void OrderActionDelegate(OrderController orderController, VisualOrderExecutionParameters executionParameters);

	private readonly OrderActionDelegate _orderAction;

	private readonly TextObject _name;

	public ActionVisualOrder(string iconId, OrderActionDelegate orderAction, TextObject name)
		: base(iconId)
	{
		_name = name;
		_orderAction = orderAction;
	}

	public override TextObject GetName(OrderController orderController)
	{
		return _name;
	}

	public override bool IsTargeted()
	{
		return false;
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		_orderAction?.Invoke(orderController, executionParameters);
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return false;
	}
}
