using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;

public class CraftingSecondaryUsageItemVM : SelectorItemVM
{
	private SelectorVM<CraftingSecondaryUsageItemVM> _parentSelector;

	public int UsageIndex { get; }

	public int SelectorIndex { get; }

	public CraftingSecondaryUsageItemVM(TextObject name, int index, int usageIndex, SelectorVM<CraftingSecondaryUsageItemVM> parentSelector)
		: base(name)
	{
		_parentSelector = parentSelector;
		SelectorIndex = index;
		UsageIndex = usageIndex;
	}

	public void ExecuteSelect()
	{
		_parentSelector.SelectedIndex = SelectorIndex;
	}
}
