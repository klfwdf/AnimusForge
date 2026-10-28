using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public sealed class ReturnVisualOrder : VisualOrder
{
	public ReturnVisualOrder()
		: base("order_return")
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=EmVbbIUc}Return");
	}

	public override bool IsTargeted()
	{
		return false;
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return false;
	}
}
