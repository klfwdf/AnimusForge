using TaleWorlds.MountAndBlade.GauntletUI;

namespace NavalDLC.GauntletUI;

public class GauntletNavalLoadingWindowManager : GauntletDefaultLoadingWindowManager
{
	protected override string GetSpriteCategoryName()
	{
		return "ui_naval_loading";
	}
}
