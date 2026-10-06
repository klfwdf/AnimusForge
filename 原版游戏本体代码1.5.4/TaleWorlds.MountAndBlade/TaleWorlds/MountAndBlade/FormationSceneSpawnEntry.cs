using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public struct FormationSceneSpawnEntry
{
	public readonly FormationClass FormationClass;

	public readonly GameEntity SpawnEntity;

	public readonly GameEntity ReinforcementSpawnEntity;

	public FormationSceneSpawnEntry(FormationClass formationClass, GameEntity spawnEntity, GameEntity reinforcementSpawnEntity)
	{
		FormationClass = formationClass;
		SpawnEntity = spawnEntity;
		ReinforcementSpawnEntity = reinforcementSpawnEntity;
	}
}
