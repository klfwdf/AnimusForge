namespace NavalDLC.Missions.Objects;

public class ShipFireBallistaSpawner : ShipBallistaSpawner
{
	protected override void OnPreInit()
	{
		_spawnerMissionHelper = new ShipSpawnerEntityMissionHelper(this, fireVersion: true);
	}
}
