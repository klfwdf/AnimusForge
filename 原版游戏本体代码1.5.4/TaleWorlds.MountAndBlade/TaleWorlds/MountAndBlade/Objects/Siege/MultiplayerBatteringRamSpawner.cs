namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MultiplayerBatteringRamSpawner : BatteringRamSpawner
{
	private const float MaxHitPoint = 12000f;

	private const float SpeedMultiplier = 1f;

	public override void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
	{
		base.AssignParameters(_spawnerMissionHelper);
		_spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<DestructableComponent>().MaxHitPoint = 12000f;
		BatteringRam firstScriptOfType = _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<BatteringRam>();
		firstScriptOfType.MaxSpeed *= 1f;
		firstScriptOfType.MinSpeed *= 1f;
	}
}
