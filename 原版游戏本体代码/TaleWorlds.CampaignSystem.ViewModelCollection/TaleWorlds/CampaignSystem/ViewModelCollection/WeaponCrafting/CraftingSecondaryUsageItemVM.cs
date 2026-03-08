using System;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting
{
	// Token: 0x020000FD RID: 253
	public class CraftingSecondaryUsageItemVM : SelectorItemVM
	{
		// Token: 0x17000788 RID: 1928
		// (get) Token: 0x060016A0 RID: 5792 RVA: 0x000574C2 File Offset: 0x000556C2
		public int UsageIndex { get; }

		// Token: 0x17000789 RID: 1929
		// (get) Token: 0x060016A1 RID: 5793 RVA: 0x000574CA File Offset: 0x000556CA
		public int SelectorIndex { get; }

		// Token: 0x060016A2 RID: 5794 RVA: 0x000574D2 File Offset: 0x000556D2
		public CraftingSecondaryUsageItemVM(TextObject name, int index, int usageIndex, SelectorVM<CraftingSecondaryUsageItemVM> parentSelector)
			: base(name)
		{
			this._parentSelector = parentSelector;
			this.SelectorIndex = index;
			this.UsageIndex = usageIndex;
		}

		// Token: 0x060016A3 RID: 5795 RVA: 0x000574F1 File Offset: 0x000556F1
		public void ExecuteSelect()
		{
			this._parentSelector.SelectedIndex = this.SelectorIndex;
		}

		// Token: 0x04000A5E RID: 2654
		private SelectorVM<CraftingSecondaryUsageItemVM> _parentSelector;
	}
}
