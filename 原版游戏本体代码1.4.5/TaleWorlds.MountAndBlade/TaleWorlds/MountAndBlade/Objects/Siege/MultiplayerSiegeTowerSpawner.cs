namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MultiplayerSiegeTowerSpawner : SiegeTowerSpawner
{
	private const float MaxHitPoint = 15000f;

	private const float MinimumSpeed = 0.5f;

	private const float MaximumSpeed = 1f;

	public override void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
	{
		base.AssignParameters(_spawnerMissionHelper);
		_spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<DestructableComponent>().MaxHitPoint = 15000f;
		SiegeTower firstScriptOfType = _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<SiegeTower>();
		firstScriptOfType.MaxSpeed = 1f;
		firstScriptOfType.MinSpeed = 0.5f;
	}
}
