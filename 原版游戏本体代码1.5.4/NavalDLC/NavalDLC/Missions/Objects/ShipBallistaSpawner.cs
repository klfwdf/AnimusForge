using NavalDLC.Missions.AI.UsableMachineAIs;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace NavalDLC.Missions.Objects;

public class ShipBallistaSpawner : BallistaSpawner
{
	protected override void OnPreInit()
	{
		_spawnerMissionHelper = new ShipSpawnerEntityMissionHelper(this);
	}

	public override void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
	{
		base.AssignParameters(_spawnerMissionHelper);
		if (Mission.Current != null)
		{
			Ballista firstScriptOfType = _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>();
			firstScriptOfType.SetAI(new ShipBallistaAI(firstScriptOfType));
		}
	}
}
