using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class BatteringRamSpawner : SpawnerBase
{
	private const float _modifierFactorUpperLimit = 1.2f;

	private const float _modifierFactorLowerLimit = 0.8f;

	[SpawnerPermissionField]
	public MatrixFrame wait_pos_ground = MatrixFrame.Zero;

	[EditorVisibleScriptComponentVariable(true)]
	public string SideTag;

	[EditorVisibleScriptComponentVariable(true)]
	public string GateTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public string PathEntityName = "Path";

	[EditorVisibleScriptComponentVariable(true)]
	public int BridgeNavMeshID_1 = 8;

	[EditorVisibleScriptComponentVariable(true)]
	public int BridgeNavMeshID_2 = 8;

	[EditorVisibleScriptComponentVariable(true)]
	public int DitchNavMeshID_1 = 9;

	[EditorVisibleScriptComponentVariable(true)]
	public int DitchNavMeshID_2 = 10;

	[EditorVisibleScriptComponentVariable(true)]
	public int GroundToBridgeNavMeshID_1 = 12;

	[EditorVisibleScriptComponentVariable(true)]
	public int GroundToBridgeNavMeshID_2 = 13;

	[EditorVisibleScriptComponentVariable(true)]
	public string AddOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public string RemoveOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public float SpeedModifierFactor = 1f;

	public bool EnableAutoGhostMovement;

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		_spawnerEditorHelper = new SpawnerEntityEditorHelper(this);
		_spawnerEditorHelper.LockGhostParent = false;
		if (_spawnerEditorHelper.IsValid)
		{
			_spawnerEditorHelper.SetupGhostMovement(PathEntityName);
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		_spawnerEditorHelper.Tick(dt);
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		switch (variableName)
		{
		case "PathEntityName":
			_spawnerEditorHelper.SetupGhostMovement(PathEntityName);
			break;
		case "EnableAutoGhostMovement":
			_spawnerEditorHelper.SetEnableAutoGhostMovement(EnableAutoGhostMovement);
			break;
		case "SpeedModifierFactor":
			SpeedModifierFactor = MathF.Clamp(SpeedModifierFactor, 0.8f, 1.2f);
			break;
		}
	}

	protected internal override bool OnCheckForProblems()
	{
		bool result = base.OnCheckForProblems();
		if (!base.Scene.IsMultiplayerScene() && base.Scene.FindWeakEntitiesWithTag("ditch_filler").FirstOrDefault((WeakGameEntity df) => df.HasTag(SideTag)) != null)
		{
			if (DitchNavMeshID_1 >= 0 && !base.Scene.IsAnyFaceWithId(DitchNavMeshID_1))
			{
				MBEditor.AddEntityWarning(base.GameEntity, "Couldn't find any face with 'DitchNavMeshID_1' id.");
				result = true;
			}
			if (DitchNavMeshID_2 >= 0 && !base.Scene.IsAnyFaceWithId(DitchNavMeshID_2))
			{
				MBEditor.AddEntityWarning(base.GameEntity, "Couldn't find any face with 'DitchNavMeshID_2' id.");
				result = true;
			}
			if (GroundToBridgeNavMeshID_1 >= 0 && !base.Scene.IsAnyFaceWithId(GroundToBridgeNavMeshID_1))
			{
				MBEditor.AddEntityWarning(base.GameEntity, "Couldn't find any face with 'GroundToBridgeNavMeshID_1' id.");
				result = true;
			}
			if (GroundToBridgeNavMeshID_2 >= 0 && !base.Scene.IsAnyFaceWithId(GroundToBridgeNavMeshID_2))
			{
				MBEditor.AddEntityWarning(base.GameEntity, "Couldn't find any face with 'GroundToBridgeNavMeshID_1' id.");
				result = true;
			}
			if (BridgeNavMeshID_1 >= 0 && !base.Scene.IsAnyFaceWithId(BridgeNavMeshID_1))
			{
				MBEditor.AddEntityWarning(base.GameEntity, "Couldn't find any face with 'BridgeNavMeshID_1' id.");
				result = true;
			}
			if (BridgeNavMeshID_2 >= 0 && !base.Scene.IsAnyFaceWithId(BridgeNavMeshID_2))
			{
				MBEditor.AddEntityWarning(base.GameEntity, "Couldn't find any face with 'BridgeNavMeshID_2' id.");
				result = true;
			}
		}
		return result;
	}

	protected internal override void OnPreInit()
	{
		base.OnPreInit();
		_spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
	}

	public override void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
	{
		BatteringRam firstScriptOfType = _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<BatteringRam>();
		firstScriptOfType.AddOnDeployTag = AddOnDeployTag;
		firstScriptOfType.RemoveOnDeployTag = RemoveOnDeployTag;
		firstScriptOfType.MaxSpeed *= SpeedModifierFactor;
		firstScriptOfType.MinSpeed *= SpeedModifierFactor;
		firstScriptOfType.AssignParametersFromSpawner(GateTag, SideTag, BridgeNavMeshID_1, BridgeNavMeshID_2, DitchNavMeshID_1, DitchNavMeshID_2, GroundToBridgeNavMeshID_1, GroundToBridgeNavMeshID_2, PathEntityName);
	}
}
