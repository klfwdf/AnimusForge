namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MultiplayerFireBallistaSpawner : BallistaSpawner
{
	protected internal override void OnPreInit()
	{
		_spawnerMissionHelperFire = new SpawnerEntityMissionHelper(this, fireVersion: true);
	}
}
