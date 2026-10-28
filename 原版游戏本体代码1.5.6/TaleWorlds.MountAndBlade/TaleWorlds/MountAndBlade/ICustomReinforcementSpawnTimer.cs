using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public interface ICustomReinforcementSpawnTimer
{
	bool Check(BattleSideEnum side);

	void ResetTimer(BattleSideEnum side);
}
