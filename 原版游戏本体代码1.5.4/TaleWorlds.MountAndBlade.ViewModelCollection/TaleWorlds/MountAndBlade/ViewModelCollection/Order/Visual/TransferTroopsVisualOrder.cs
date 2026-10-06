using System;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public class TransferTroopsVisualOrder : VisualOrder
{
	public static event Action OnTransferStarted;

	public TransferTroopsVisualOrder()
		: base("order_toggle_transfer")
	{
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		TransferTroopsVisualOrder.OnTransferStarted?.Invoke();
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=AmbKQ7LT}Transfer");
	}

	public override bool IsTargeted()
	{
		return false;
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return false;
	}
}
