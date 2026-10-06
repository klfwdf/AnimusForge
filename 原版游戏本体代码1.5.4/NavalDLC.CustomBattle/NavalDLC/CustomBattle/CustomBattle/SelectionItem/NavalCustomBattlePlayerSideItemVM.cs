using TaleWorlds.Core.ViewModelCollection.Selector;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalCustomBattlePlayerSideItemVM : SelectorItemVM
{
	public NavalCustomBattlePlayerSide PlayerSide { get; private set; }

	public NavalCustomBattlePlayerSideItemVM(string playerSideName, NavalCustomBattlePlayerSide playerSide)
		: base(playerSideName)
	{
		PlayerSide = playerSide;
	}
}
