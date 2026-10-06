using TaleWorlds.Core.ViewModelCollection.Selector;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalGameTypeItemVM : SelectorItemVM
{
	public string GameTypeStringId { get; private set; }

	public NavalGameTypeItemVM(string gameTypeName, string gameType)
		: base(gameTypeName)
	{
		GameTypeStringId = gameType;
	}
}
