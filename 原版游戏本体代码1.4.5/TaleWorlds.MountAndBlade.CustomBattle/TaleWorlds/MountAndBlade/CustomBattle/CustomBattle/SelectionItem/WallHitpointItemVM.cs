using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class WallHitpointItemVM : SelectorItemVM
{
	public string WallState { get; private set; }

	public int BreachedWallCount { get; private set; }

	public WallHitpointItemVM(string wallStateName, int breachedWallCount)
		: base(wallStateName)
	{
		WallState = wallStateName;
		BreachedWallCount = breachedWallCount;
	}
}
