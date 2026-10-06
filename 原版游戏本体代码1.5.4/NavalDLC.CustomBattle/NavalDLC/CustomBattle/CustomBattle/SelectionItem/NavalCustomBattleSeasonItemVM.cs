using TaleWorlds.Core.ViewModelCollection.Selector;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalCustomBattleSeasonItemVM : SelectorItemVM
{
	public string SeasonId { get; private set; }

	public NavalCustomBattleSeasonItemVM(string seasonName, string seasonId)
		: base(seasonName)
	{
		SeasonId = seasonId;
	}
}
