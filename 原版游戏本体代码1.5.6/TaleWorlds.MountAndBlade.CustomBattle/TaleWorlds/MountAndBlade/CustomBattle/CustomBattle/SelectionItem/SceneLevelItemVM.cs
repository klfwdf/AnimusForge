using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class SceneLevelItemVM : SelectorItemVM
{
	public int Level { get; private set; }

	public SceneLevelItemVM(int level)
		: base(level.ToString())
	{
		Level = level;
	}
}
