using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encyclopedia;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000DF RID: 223
	public class EncyclopediaListItemComparer : IComparer<EncyclopediaListItemVM>
	{
		// Token: 0x17000707 RID: 1799
		// (get) Token: 0x06001536 RID: 5430 RVA: 0x0005376B File Offset: 0x0005196B
		public EncyclopediaSortController SortController { get; }

		// Token: 0x06001537 RID: 5431 RVA: 0x00053773 File Offset: 0x00051973
		public EncyclopediaListItemComparer(EncyclopediaSortController sortController)
		{
			this.SortController = sortController;
		}

		// Token: 0x06001538 RID: 5432 RVA: 0x00053784 File Offset: 0x00051984
		private int GetBookmarkComparison(EncyclopediaListItemVM x, EncyclopediaListItemVM y)
		{
			return -x.IsBookmarked.CompareTo(y.IsBookmarked);
		}

		// Token: 0x06001539 RID: 5433 RVA: 0x000537A8 File Offset: 0x000519A8
		public int Compare(EncyclopediaListItemVM x, EncyclopediaListItemVM y)
		{
			int bookmarkComparison = this.GetBookmarkComparison(x, y);
			if (bookmarkComparison != 0)
			{
				return bookmarkComparison;
			}
			return this.SortController.Comparer.Compare(x.ListItem, y.ListItem);
		}
	}
}
