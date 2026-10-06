using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class TroopSortSelectorItemVM : SelectorItemVM
{
	public PartyScreenLogic.TroopSortType SortType { get; private set; }

	public TroopSortSelectorItemVM(TextObject s, PartyScreenLogic.TroopSortType sortType)
		: base(s)
	{
		SortType = sortType;
	}
}
