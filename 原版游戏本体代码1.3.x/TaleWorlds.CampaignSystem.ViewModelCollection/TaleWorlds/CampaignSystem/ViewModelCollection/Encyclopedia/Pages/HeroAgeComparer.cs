using System;
using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000CF RID: 207
	public class HeroAgeComparer : IComparer<HeroVM>
	{
		// Token: 0x0600139D RID: 5021 RVA: 0x0004E825 File Offset: 0x0004CA25
		public HeroAgeComparer(bool isAscending)
		{
			this._isAscending = isAscending;
		}

		// Token: 0x0600139E RID: 5022 RVA: 0x0004E834 File Offset: 0x0004CA34
		int IComparer<HeroVM>.Compare(HeroVM x, HeroVM y)
		{
			int num = x.Hero.Age.CompareTo(y.Hero.Age) * (this._isAscending ? 1 : (-1));
			if (num == 0)
			{
				num = x.NameText.CompareTo(y.NameText);
			}
			return num;
		}

		// Token: 0x040008FE RID: 2302
		private readonly bool _isAscending;
	}
}
