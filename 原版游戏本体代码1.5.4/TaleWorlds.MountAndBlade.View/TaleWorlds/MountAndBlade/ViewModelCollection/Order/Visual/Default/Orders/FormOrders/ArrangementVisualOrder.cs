using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.FormOrders;

public class ArrangementVisualOrder : VisualOrder
{
	public ArrangementOrder.ArrangementOrderEnum ArrangementOrder { get; }

	public ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum arrangementOrder, string iconId)
		: base(iconId)
	{
		ArrangementOrder = arrangementOrder;
	}

	public override TextObject GetName(OrderController orderController)
	{
		return ArrangementOrder switch
		{
			TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Circle => new TextObject("{=9TGLirQf}Circle"), 
			TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Column => new TextObject("{=WsmZzaOq}Column"), 
			TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Line => new TextObject("{=9aboazgu}Line"), 
			TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Loose => new TextObject("{=iJXH3841}Loose"), 
			TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Scatter => new TextObject("{=eEf7hE4r}Scatter"), 
			TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.ShieldWall => new TextObject("{=rTPnyeJ3}Shield Wall"), 
			TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Skein => new TextObject("{=uCyQNvq1}Skein"), 
			TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Square => new TextObject("{=squareOrder}Square"), 
			_ => TextObject.GetEmpty(), 
		};
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		orderController.SetOrder(new ArrangementOrder(ArrangementOrder).OrderType);
	}

	public override bool IsTargeted()
	{
		return false;
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return formation.ArrangementOrder.OrderEnum == ArrangementOrder;
	}

	private static OrderType GetArrangementOrderType(ArrangementOrder.ArrangementOrderEnum arrangementOrderEnum)
	{
		switch (arrangementOrderEnum)
		{
		case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Circle:
			return OrderType.ArrangementCircular;
		case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Column:
			return OrderType.ArrangementColumn;
		case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Line:
			return OrderType.ArrangementLine;
		case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Loose:
			return OrderType.ArrangementLine;
		default:
			Debug.FailedAssert("Failed to find arrangement order type: " + arrangementOrderEnum, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\VisualOrders\\Orders\\FormOrders\\ArrangementVisualOrder.cs", "GetArrangementOrderType", 78);
			return OrderType.None;
		}
	}
}
