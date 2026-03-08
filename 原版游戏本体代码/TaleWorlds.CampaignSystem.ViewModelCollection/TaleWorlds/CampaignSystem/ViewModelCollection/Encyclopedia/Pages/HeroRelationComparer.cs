using System;
using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D4 RID: 212
	public class HeroRelationComparer : IComparer<HeroVM>
	{
		// Token: 0x0600145A RID: 5210 RVA: 0x00050E82 File Offset: 0x0004F082
		public HeroRelationComparer(Hero pageHero, bool isAscending, bool showLeadersFirst)
		{
			this._pageHero = pageHero;
			this._isAscending = isAscending;
			this._showLeadersFirst = showLeadersFirst;
		}

		// Token: 0x0600145B RID: 5211 RVA: 0x00050EA0 File Offset: 0x0004F0A0
		int IComparer<HeroVM>.Compare(HeroVM x, HeroVM y)
		{
			int num;
			if (this._showLeadersFirst)
			{
				num = y.IsKingdomLeader.CompareTo(x.IsKingdomLeader);
				if (num != 0)
				{
					return num;
				}
			}
			int relation = this._pageHero.GetRelation(x.Hero);
			int relation2 = this._pageHero.GetRelation(y.Hero);
			num = relation.CompareTo(relation2) * (this._isAscending ? 1 : (-1));
			if (num == 0)
			{
				num = x.NameText.CompareTo(y.NameText);
			}
			return num;
		}

		// Token: 0x04000956 RID: 2390
		private readonly Hero _pageHero;

		// Token: 0x04000957 RID: 2391
		private readonly bool _isAscending;

		// Token: 0x04000958 RID: 2392
		private readonly bool _showLeadersFirst;
	}
}
