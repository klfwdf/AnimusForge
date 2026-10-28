using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class TimeOfDayItemVM : SelectorItemVM
{
	public int TimeOfDay { get; private set; }

	public TimeOfDayItemVM(string timeOfDayName, int timeOfDay)
		: base(timeOfDayName)
	{
		TimeOfDay = timeOfDay;
	}
}
