using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions;

public static class Items
{
	public static MBReadOnlyList<ItemObject> All => Campaign.Current.AllItems;

	public static IEnumerable<ItemObject> AllTradeGoods
	{
		get
		{
			MBReadOnlyList<ItemObject> all = All;
			foreach (ItemObject item in all)
			{
				if (item.IsTradeGood)
				{
					yield return item;
				}
			}
		}
	}
}
