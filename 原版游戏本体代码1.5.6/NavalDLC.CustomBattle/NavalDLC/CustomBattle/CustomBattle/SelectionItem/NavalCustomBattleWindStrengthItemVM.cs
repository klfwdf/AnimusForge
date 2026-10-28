using TaleWorlds.Core.ViewModelCollection.Selector;

namespace NavalDLC.CustomBattle.CustomBattle.SelectionItem;

public class NavalCustomBattleWindStrengthItemVM : SelectorItemVM
{
	public float WindStrength { get; private set; }

	public NavalCustomBattleWindStrengthItemVM(string windStrengthName, float windStrength)
		: base(windStrengthName)
	{
		WindStrength = windStrength;
	}
}
