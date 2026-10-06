using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public interface IMissionAgentSpawnLogic : IMissionBehavior
{
	BattleSideEnum PlayerSide { get; }

	void StartSpawner(BattleSideEnum side);

	void StopSpawner(BattleSideEnum side);

	bool IsSideSpawnEnabled(BattleSideEnum side);

	bool IsSideDepleted(BattleSideEnum side);

	float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None);

	IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side);

	bool GetSpawnHorses(BattleSideEnum side);

	int GetNumberOfPlayerControllableTroops();
}
