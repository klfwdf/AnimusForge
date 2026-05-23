using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x0200016D RID: 365
	public static class ItemObjectExtensions
	{
		// Token: 0x06001AF9 RID: 6905 RVA: 0x0008AC5E File Offset: 0x00088E5E
		public static ItemCategory GetItemCategory(this ItemObject item)
		{
			return item.ItemCategory;
		}
	}
}
