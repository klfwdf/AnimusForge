namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MultiplayerFireTrebuchetSpawner : TrebuchetSpawner
{
	protected internal override void OnPreInit()
	{
		_spawnerMissionHelperFire = new SpawnerEntityMissionHelper(this, fireVersion: true);
	}
}
