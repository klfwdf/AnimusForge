namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MultiplayerTrebuchetSpawner : TrebuchetSpawner
{
	protected internal override void OnPreInit()
	{
		_spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
	}
}
