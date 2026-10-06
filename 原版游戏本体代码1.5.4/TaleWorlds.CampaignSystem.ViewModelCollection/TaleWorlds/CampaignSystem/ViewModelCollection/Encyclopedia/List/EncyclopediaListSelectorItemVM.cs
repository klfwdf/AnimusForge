using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

public class EncyclopediaListSelectorItemVM : SelectorItemVM
{
	public EncyclopediaListItemComparer Comparer;

	public EncyclopediaListSelectorItemVM(EncyclopediaListItemComparer comparer)
		: base(comparer.SortController.Name.ToString())
	{
		Comparer = comparer;
	}
}
