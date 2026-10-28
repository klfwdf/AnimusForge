using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.CustomBattle;

namespace NavalDLC.CustomBattle;

public class NavalCustomBattleProvider : ICustomBattleProvider
{
	public void StartCustomBattle()
	{
		MBGameManager.StartNewGame(new NavalCustomGameManager());
	}

	public TextObject GetName()
	{
		return new TextObject("{=Q8gbZIiM}Naval Custom Battle");
	}
}
