using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public struct FormationSceneSpawnEntry(FormationClass formationClass, GameEntity spawnEntity, GameEntity reinforcementSpawnEntity)
{
	public readonly FormationClass FormationClass = formationClass;

	public readonly GameEntity SpawnEntity = spawnEntity;

	public readonly GameEntity ReinforcementSpawnEntity = reinforcementSpawnEntity;
}
