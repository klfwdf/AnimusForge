using System;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class BallistaSpawner : SpawnerBase
{
	[EditorVisibleScriptComponentVariable(true)]
	public string AddOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public string RemoveOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public float DirectionRestrictionDegree = 90f;

	protected internal override void OnPreInit()
	{
		base.OnPreInit();
		_spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
		_spawnerMissionHelperFire = new SpawnerEntityMissionHelper(this, fireVersion: true);
	}

	public override void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
	{
		_spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().AddOnDeployTag = AddOnDeployTag;
		_spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().RemoveOnDeployTag = RemoveOnDeployTag;
		_spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<Ballista>().HorizontalDirectionRestriction = DirectionRestrictionDegree * (MathF.PI / 180f);
	}
}
