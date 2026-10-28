using TaleWorlds.Core.ViewModelCollection.Selector;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalCustomBattleTimeOfDayItemVM : SelectorItemVM
{
	public int TimeOfDay { get; private set; }

	public NavalCustomBattleTimeOfDayItemVM(string timeOfDayName, int timeOfDay)
		: base(timeOfDayName)
	{
		TimeOfDay = timeOfDay;
	}
}
