using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class SeasonItemVM : SelectorItemVM
{
	public string SeasonId { get; private set; }

	public SeasonItemVM(string seasonName, string seasonId)
		: base(seasonName)
	{
		SeasonId = seasonId;
	}
}
