using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000177 RID: 375
	public abstract class EncyclopediaListItemComparerBase : IComparer<EncyclopediaListItem>
	{
		// Token: 0x170006EC RID: 1772
		// (get) Token: 0x06001B39 RID: 6969 RVA: 0x0008B7FA File Offset: 0x000899FA
		// (set) Token: 0x06001B3A RID: 6970 RVA: 0x0008B802 File Offset: 0x00089A02
		public bool IsAscending { get; private set; }

		// Token: 0x06001B3B RID: 6971 RVA: 0x0008B80B File Offset: 0x00089A0B
		public void SetSortOrder(bool isAscending)
		{
			this.IsAscending = isAscending;
		}

		// Token: 0x06001B3C RID: 6972 RVA: 0x0008B814 File Offset: 0x00089A14
		public void SwitchSortOrder()
		{
			this.IsAscending = !this.IsAscending;
		}

		// Token: 0x06001B3D RID: 6973 RVA: 0x0008B825 File Offset: 0x00089A25
		public void SetDefaultSortOrder()
		{
			this.IsAscending = false;
		}

		// Token: 0x06001B3E RID: 6974
		public abstract int Compare(EncyclopediaListItem x, EncyclopediaListItem y);

		// Token: 0x06001B3F RID: 6975
		public abstract string GetComparedValueText(EncyclopediaListItem item);

		// Token: 0x06001B40 RID: 6976 RVA: 0x0008B82E File Offset: 0x00089A2E
		protected int ResolveEquality(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return x.Name.CompareTo(y.Name);
		}

		// Token: 0x0400091E RID: 2334
		protected readonly TextObject _emptyValue = new TextObject("{=4NaOKslb}-", null);

		// Token: 0x0400091F RID: 2335
		protected readonly TextObject _missingValue = new TextObject("{=keqS2dGa}???", null);
	}
}
