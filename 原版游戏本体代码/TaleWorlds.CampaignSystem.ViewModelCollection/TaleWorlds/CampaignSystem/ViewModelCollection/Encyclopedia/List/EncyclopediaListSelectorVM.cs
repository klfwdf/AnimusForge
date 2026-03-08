using System;
using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000E0 RID: 224
	public class EncyclopediaListSelectorVM : SelectorVM<EncyclopediaListSelectorItemVM>
	{
		// Token: 0x0600153A RID: 5434 RVA: 0x000537DF File Offset: 0x000519DF
		public EncyclopediaListSelectorVM(int selectedIndex, Action<SelectorVM<EncyclopediaListSelectorItemVM>> onChange, Action onActivate)
			: base(selectedIndex, onChange)
		{
			this._onActivate = onActivate;
		}

		// Token: 0x0600153B RID: 5435 RVA: 0x000537F0 File Offset: 0x000519F0
		public void ExecuteOnDropdownActivated()
		{
			Action onActivate = this._onActivate;
			if (onActivate == null)
			{
				return;
			}
			onActivate();
		}

		// Token: 0x040009AE RID: 2478
		private Action _onActivate;
	}
}
