using System;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000176 RID: 374
	internal class EncyclopediaListItemNameComparer : EncyclopediaListItemComparerBase
	{
		// Token: 0x06001B36 RID: 6966 RVA: 0x0008B7E1 File Offset: 0x000899E1
		public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
		{
			return base.ResolveEquality(x, y);
		}

		// Token: 0x06001B37 RID: 6967 RVA: 0x0008B7EB File Offset: 0x000899EB
		public override string GetComparedValueText(EncyclopediaListItem item)
		{
			return "";
		}
	}
}
