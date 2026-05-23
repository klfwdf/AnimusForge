using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000172 RID: 370
	public class EncyclopediaFilterItem
	{
		// Token: 0x06001B14 RID: 6932 RVA: 0x0008B0F4 File Offset: 0x000892F4
		public EncyclopediaFilterItem(TextObject name, Predicate<object> predicate)
		{
			this.Name = name;
			this.Predicate = predicate;
			this.IsActive = false;
		}

		// Token: 0x04000906 RID: 2310
		public readonly TextObject Name;

		// Token: 0x04000907 RID: 2311
		public readonly Predicate<object> Predicate;

		// Token: 0x04000908 RID: 2312
		public bool IsActive;
	}
}
