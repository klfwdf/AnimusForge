using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x0200016B RID: 363
	public static class ItemCategories
	{
		// Token: 0x170006E5 RID: 1765
		// (get) Token: 0x06001AF7 RID: 6903 RVA: 0x0008AC46 File Offset: 0x00088E46
		public static MBReadOnlyList<ItemCategory> All
		{
			get
			{
				return Campaign.Current.AllItemCategories;
			}
		}
	}
}
