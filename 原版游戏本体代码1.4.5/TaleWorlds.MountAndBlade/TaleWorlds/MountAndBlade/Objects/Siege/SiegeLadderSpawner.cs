using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class SiegeLadderSpawner : SpawnerBase
{
	[SpawnerPermissionField]
	public MatrixFrame fork_holder = MatrixFrame.Zero;

	[SpawnerPermissionField]
	public MatrixFrame initial_wait_pos = MatrixFrame.Zero;

	[SpawnerPermissionField]
	public MatrixFrame use_push = MatrixFrame.Zero;

	[SpawnerPermissionField]
	public MatrixFrame stand_position_wall_push = MatrixFrame.Zero;

	[SpawnerPermissionField]
	public MatrixFrame distance_holder = MatrixFrame.Zero;

	[SpawnerPermissionField]
	public MatrixFrame stand_position_ground_wait = MatrixFrame.Zero;

	[EditorVisibleScriptComponentVariable(true)]
	public string SideTag;

	[EditorVisibleScriptComponentVariable(true)]
	public string TargetWallSegmentTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public int OnWallNavMeshId = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public string AddOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public string RemoveOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public float UpperStateRotationDegree;

	[EditorVisibleScriptComponentVariable(true)]
	public float DownStateRotationDegree = 90f;

	public float TacticalPositionWidth = 1f;

	[EditorVisibleScriptComponentVariable(true)]
	public string BarrierTagToRemove = string.Empty;

	[EditorVisibleScriptComponentVariable(true)]
	public string IndestructibleMerlonsTag = string.Empty;

	public float UpperStateRotationRadian => UpperStateRotationDegree * (System.MathF.PI / 180f);

	public float DownStateRotationRadian => DownStateRotationDegree * (System.MathF.PI / 180f);

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		_spawnerEditorHelper = new SpawnerEntityEditorHelper(this);
		if (_spawnerEditorHelper.IsValid)
		{
			_spawnerEditorHelper.GivePermission("ladder_up_state", new SpawnerEntityEditorHelper.Permission(SpawnerEntityEditorHelper.PermissionType.rotation, SpawnerEntityEditorHelper.Axis.x), OnLadderUpStateChange);
			_spawnerEditorHelper.GivePermission("ladder_down_state", new SpawnerEntityEditorHelper.Permission(SpawnerEntityEditorHelper.PermissionType.rotation, SpawnerEntityEditorHelper.Axis.x), OnLadderDownStateChange);
		}
		OnEditorVariableChanged("UpperStateRotationDegree");
		OnEditorVariableChanged("DownStateRotationDegree");
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		_spawnerEditorHelper.Tick(dt);
	}

	private void OnLadderUpStateChange(float rotation)
	{
		if (rotation > -0.20135832f)
		{
			rotation = -0.20135832f;
			UpperStateRotationDegree = rotation * 57.29578f;
			OnEditorVariableChanged("UpperStateRotationDegree");
		}
		else
		{
			UpperStateRotationDegree = rotation * 57.29578f;
		}
	}

	private void OnLadderDownStateChange(float unusedArgument)
	{
		GameEntity ghostEntityOrChild = _spawnerEditorHelper.GetGhostEntityOrChild("ladder_down_state");
		DownStateRotationDegree = Vec3.AngleBetweenTwoVectors(Vec3.Up, ghostEntityOrChild.GetFrame().rotation.u) * 57.29578f;
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "UpperStateRotationDegree")
		{
			if (UpperStateRotationDegree > -11.536982f)
			{
				UpperStateRotationDegree = -11.536982f;
			}
			MatrixFrame frame = _spawnerEditorHelper.GetGhostEntityOrChild("ladder_up_state").GetFrame();
			frame.rotation = Mat3.Identity;
			frame.rotation.RotateAboutSide(UpperStateRotationRadian);
			_spawnerEditorHelper.ChangeStableChildMatrixFrameAndApply("ladder_up_state", frame);
		}
		else if (variableName == "DownStateRotationDegree")
		{
			MatrixFrame frame2 = _spawnerEditorHelper.GetGhostEntityOrChild("ladder_down_state").GetFrame();
			frame2.rotation = Mat3.Identity;
			frame2.rotation.RotateAboutUp(System.MathF.PI / 2f);
			frame2.rotation.RotateAboutSide(DownStateRotationRadian);
			_spawnerEditorHelper.ChangeStableChildMatrixFrameAndApply("ladder_down_state", frame2);
		}
	}

	protected internal override bool OnCheckForProblems()
	{
		bool result = base.OnCheckForProblems();
		if (base.Scene.IsMultiplayerScene())
		{
			if (OnWallNavMeshId == 0 || OnWallNavMeshId % 10 == 1)
			{
				MBEditor.AddEntityWarning(base.GameEntity, "OnWallNavMeshId's ones digit cannot be 1 and OnWallNavMeshId cannot be 0 in a multiplayer scene.");
				result = true;
			}
		}
		else if (OnWallNavMeshId == -1 || OnWallNavMeshId == 0 || OnWallNavMeshId % 10 == 1)
		{
			MBEditor.AddEntityWarning(base.GameEntity, "OnWallNavMeshId's ones digit cannot be 1 and OnWallNavMeshId cannot be -1 or 0 in a singleplayer scene.");
			result = true;
		}
		if (OnWallNavMeshId != -1)
		{
			List<GameEntity> entities = new List<GameEntity>();
			base.Scene.GetEntities(ref entities);
			foreach (GameEntity item in entities)
			{
				SiegeLadderSpawner firstScriptOfType = item.GetFirstScriptOfType<SiegeLadderSpawner>();
				if (firstScriptOfType != null && item != base.GameEntity && OnWallNavMeshId == firstScriptOfType.OnWallNavMeshId && base.GameEntity.GetVisibilityLevelMaskIncludingParents() == item.GetVisibilityLevelMaskIncludingParents())
				{
					MBEditor.AddEntityWarning(base.GameEntity, "OnWallNavMeshId must not be shared with any other siege ladder.");
				}
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
		SiegeLadder firstScriptOfType = _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<SiegeLadder>();
		firstScriptOfType.AddOnDeployTag = AddOnDeployTag;
		firstScriptOfType.RemoveOnDeployTag = RemoveOnDeployTag;
		firstScriptOfType.AssignParametersFromSpawner(SideTag, TargetWallSegmentTag, OnWallNavMeshId, DownStateRotationRadian, UpperStateRotationRadian, BarrierTagToRemove, IndestructibleMerlonsTag);
		List<GameEntity> children = new List<GameEntity>();
		_spawnerMissionHelper.SpawnedEntity.GetChildrenRecursive(ref children);
		children.Find((GameEntity x) => x.Name == "initial_wait_pos").GetFirstScriptOfType<TacticalPosition>().SetWidth(TacticalPositionWidth);
	}
}
