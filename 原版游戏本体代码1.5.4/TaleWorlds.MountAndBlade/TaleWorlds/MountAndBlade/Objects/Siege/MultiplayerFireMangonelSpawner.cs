namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MultiplayerFireMangonelSpawner : MangonelSpawner
{
	protected internal override void OnPreInit()
	{
		_spawnerMissionHelperFire = new SpawnerEntityMissionHelper(this, fireVersion: true);
	}
}
