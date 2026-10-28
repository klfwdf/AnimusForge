namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MultiplayerMangonelSpawner : MangonelSpawner
{
	protected internal override void OnPreInit()
	{
		_spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
	}
}
