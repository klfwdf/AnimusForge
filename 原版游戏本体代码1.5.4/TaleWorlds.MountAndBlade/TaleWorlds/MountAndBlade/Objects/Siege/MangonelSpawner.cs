using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class MangonelSpawner : SpawnerBase
{
	[SpawnerPermissionField]
	public MatrixFrame projectile_pile = MatrixFrame.Zero;

	[EditorVisibleScriptComponentVariable(true)]
	public string AddOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public string RemoveOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public bool ammo_pos_a_enabled = true;

	[EditorVisibleScriptComponentVariable(true)]
	public bool ammo_pos_b_enabled = true;

	[EditorVisibleScriptComponentVariable(true)]
	public bool ammo_pos_c_enabled = true;

	[EditorVisibleScriptComponentVariable(true)]
	public bool ammo_pos_d_enabled = true;

	[EditorVisibleScriptComponentVariable(true)]
	public bool ammo_pos_e_enabled = true;

	[EditorVisibleScriptComponentVariable(true)]
	public bool ammo_pos_f_enabled = true;

	[EditorVisibleScriptComponentVariable(true)]
	public bool ammo_pos_g_enabled = true;

	[EditorVisibleScriptComponentVariable(true)]
	public bool ammo_pos_h_enabled = true;

	protected internal override void OnPreInit()
	{
		base.OnPreInit();
		_spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
		_spawnerMissionHelperFire = new SpawnerEntityMissionHelper(this, fireVersion: true);
	}

	public override void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
	{
		foreach (GameEntity child in _spawnerMissionHelper.SpawnedEntity.GetChildren())
		{
			if (child.GetFirstScriptOfType<Mangonel>() != null)
			{
				child.GetFirstScriptOfType<Mangonel>().AddOnDeployTag = AddOnDeployTag;
				child.GetFirstScriptOfType<Mangonel>().RemoveOnDeployTag = RemoveOnDeployTag;
				break;
			}
		}
	}
}
