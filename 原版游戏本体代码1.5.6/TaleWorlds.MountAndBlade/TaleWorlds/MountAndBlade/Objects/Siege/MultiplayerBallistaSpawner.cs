namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MultiplayerBallistaSpawner : BallistaSpawner
{
	protected internal override void OnPreInit()
	{
		_spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
	}
}
