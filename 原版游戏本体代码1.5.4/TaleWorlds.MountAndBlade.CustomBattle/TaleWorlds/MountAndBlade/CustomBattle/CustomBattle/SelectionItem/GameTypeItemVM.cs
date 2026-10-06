using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class GameTypeItemVM : SelectorItemVM
{
	public string GameTypeStringId { get; private set; }

	public GameTypeItemVM(string gameTypeName, string gameType)
		: base(gameTypeName)
	{
		GameTypeStringId = gameType;
	}
}
