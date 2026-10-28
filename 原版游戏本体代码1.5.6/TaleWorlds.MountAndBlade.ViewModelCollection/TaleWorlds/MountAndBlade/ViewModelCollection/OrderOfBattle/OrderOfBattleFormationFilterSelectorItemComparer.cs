using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

public class OrderOfBattleFormationFilterSelectorItemComparer : IComparer<OrderOfBattleFormationFilterSelectorItemVM>
{
	public int Compare(OrderOfBattleFormationFilterSelectorItemVM x, OrderOfBattleFormationFilterSelectorItemVM y)
	{
		return x.FilterType.CompareTo(y.FilterType);
	}
}
