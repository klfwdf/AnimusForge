using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.View.CustomBattle;

public interface ICustomBattleProvider
{
	void StartCustomBattle();

	TextObject GetName();
}
