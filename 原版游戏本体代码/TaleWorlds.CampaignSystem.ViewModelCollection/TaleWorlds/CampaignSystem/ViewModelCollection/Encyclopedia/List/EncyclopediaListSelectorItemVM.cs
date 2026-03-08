using System;
using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000E1 RID: 225
	public class EncyclopediaListSelectorItemVM : SelectorItemVM
	{
		// Token: 0x0600153C RID: 5436 RVA: 0x00053802 File Offset: 0x00051A02
		public EncyclopediaListSelectorItemVM(EncyclopediaListItemComparer comparer)
			: base(comparer.SortController.Name.ToString())
		{
			this.Comparer = comparer;
		}

		// Token: 0x040009AF RID: 2479
		public EncyclopediaListItemComparer Comparer;
	}
}
