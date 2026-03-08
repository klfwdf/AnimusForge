using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000178 RID: 376
	public class EncyclopediaSortController
	{
		// Token: 0x170006ED RID: 1773
		// (get) Token: 0x06001B42 RID: 6978 RVA: 0x0008B86B File Offset: 0x00089A6B
		public TextObject Name { get; }

		// Token: 0x170006EE RID: 1774
		// (get) Token: 0x06001B43 RID: 6979 RVA: 0x0008B873 File Offset: 0x00089A73
		public EncyclopediaListItemComparerBase Comparer { get; }

		// Token: 0x06001B44 RID: 6980 RVA: 0x0008B87B File Offset: 0x00089A7B
		public EncyclopediaSortController(TextObject name, EncyclopediaListItemComparerBase comparer)
		{
			this.Name = name;
			this.Comparer = comparer;
		}
	}
}
