using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;

public class GenericVisualOrderSet : VisualOrderSet
{
	private readonly TextObject _name;

	private readonly string _stringId;

	private readonly bool _useActiveOrderForIconId;

	private readonly bool _useActiveOrderForName;

	public override bool IsSoloOrder => false;

	public override string StringId => _stringId;

	public override string IconId
	{
		get
		{
			if (_useActiveOrderForIconId)
			{
				for (int i = 0; i < base.Orders.Count; i++)
				{
					if (base.Orders[i].GetActiveState(Mission.Current.PlayerTeam.PlayerOrderController) == OrderState.Active)
					{
						return base.Orders[i].IconId;
					}
				}
			}
			return _stringId;
		}
	}

	public override TextObject GetName(OrderController orderController)
	{
		if (_useActiveOrderForName)
		{
			for (int i = 0; i < base.Orders.Count; i++)
			{
				if (base.Orders[i].GetActiveState(orderController) == OrderState.Active)
				{
					return base.Orders[i].GetName(orderController);
				}
			}
		}
		return _name;
	}

	public GenericVisualOrderSet(string stringId, TextObject name, bool useActiveOrderForIconId, bool useActiveOrderForName)
	{
		_stringId = stringId;
		_name = name;
		_useActiveOrderForIconId = useActiveOrderForIconId;
		_useActiveOrderForName = useActiveOrderForName;
	}
}
