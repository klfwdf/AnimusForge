using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core.ViewModelCollection.Selector;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalCustomBattleWindDirectionItemVM : SelectorItemVM
{
	public NavalCustomBattleWindConfig.Direction WindDirection { get; private set; }

	public NavalCustomBattleWindDirectionItemVM(string windDirectionName, NavalCustomBattleWindConfig.Direction windDirection)
		: base(windDirectionName)
	{
		WindDirection = windDirection;
	}
}
