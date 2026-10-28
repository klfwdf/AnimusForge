using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View.CustomBattle;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

public class CustomBattleProvider : ICustomBattleProvider
{
	public void StartCustomBattle()
	{
		MBGameManager.StartNewGame(new CustomGameManager());
	}

	public TextObject GetName()
	{
		return new TextObject("{=RZyk1LZy}Land Custom Battle");
	}
}
