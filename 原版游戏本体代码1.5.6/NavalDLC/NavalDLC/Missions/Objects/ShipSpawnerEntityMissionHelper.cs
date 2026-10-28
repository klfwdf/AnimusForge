using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace NavalDLC.Missions.Objects;

internal class ShipSpawnerEntityMissionHelper : SpawnerEntityMissionHelper
{
	public ShipSpawnerEntityMissionHelper(SpawnerBase spawner, bool fireVersion = false)
		: base(spawner, fireVersion)
	{
	}

	protected override void InstantiateEntity(GameEntity parent, string entityName)
	{
		SpawnedEntity = GameEntity.Instantiate(parent.Scene, entityName, callScriptCallbacks: true);
	}
}
