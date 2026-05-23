using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x02000168 RID: 360
	public static class Items
	{
		// Token: 0x170006E1 RID: 1761
		// (get) Token: 0x06001AF2 RID: 6898 RVA: 0x0008AC0C File Offset: 0x00088E0C
		public static MBReadOnlyList<ItemObject> All
		{
			get
			{
				return Campaign.Current.AllItems;
			}
		}

		// Token: 0x170006E2 RID: 1762
		// (get) Token: 0x06001AF3 RID: 6899 RVA: 0x0008AC18 File Offset: 0x00088E18
		public static IEnumerable<ItemObject> AllTradeGoods
		{
			get
			{
				MBReadOnlyList<ItemObject> all = Items.All;
				foreach (ItemObject itemObject in all)
				{
					if (itemObject.IsTradeGood)
					{
						yield return itemObject;
					}
				}
				List<ItemObject>.Enumerator enumerator = default(List<ItemObject>.Enumerator);
				yield break;
				yield break;
			}
		}
	}
}
