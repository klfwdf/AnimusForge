using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class PlayerSideItemVM : SelectorItemVM
{
	public CustomBattlePlayerSide PlayerSide { get; private set; }

	public PlayerSideItemVM(string playerSideName, CustomBattlePlayerSide playerSide)
		: base(playerSideName)
	{
		PlayerSide = playerSide;
	}
}
