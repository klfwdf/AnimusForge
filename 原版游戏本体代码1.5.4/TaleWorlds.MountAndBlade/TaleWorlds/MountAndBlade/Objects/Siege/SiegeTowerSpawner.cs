using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class SiegeTowerSpawner : SpawnerBase
{
	private const float _modifierFactorUpperLimit = 1.2f;

	private const float _modifierFactorLowerLimit = 0.8f;

	[SpawnerPermissionField]
	public MatrixFrame wait_pos_ground = MatrixFrame.Zero;

	[EditorVisibleScriptComponentVariable(true)]
	public string SideTag;

	[EditorVisibleScriptComponentVariable(true)]
	public string TargetWallSegmentTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public string PathEntityName = "Path";

	[EditorVisibleScriptComponentVariable(true)]
	public int SoilNavMeshID1 = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public int SoilNavMeshID2 = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public int DitchNavMeshID1 = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public int DitchNavMeshID2 = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public int GroundToSoilNavMeshID1 = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public int GroundToSoilNavMeshID2 = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public int SoilGenericNavMeshID = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public int GroundGenericNavMeshID = -1;

	[EditorVisibleScriptComponentVariable(true)]
	public string AddOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public string RemoveOnDeployTag = "";

	[EditorVisibleScriptComponentVariable(true)]
	public float RampRotationDegree;

	[EditorVisibleScriptComponentVariable(true)]
	public float BarrierLength = 1f;

	[EditorVisibleScriptComponentVariable(true)]
	public float SpeedModifierFactor = 1f;

	public bool EnableAutoGhostMovement;

	[SpawnerPermissionField]
	[RestrictedAccess]
	public MatrixFrame ai_barrier_l = MatrixFrame.Zero;

	[SpawnerPermissionField]
	[RestrictedAccess]
	public MatrixFrame ai_barrier_r = MatrixFrame.Zero;

	[EditorVisibleScriptComponentVariable(true)]
	public string BarrierTagToRemove = string.Empty;

	public float RampRotationRadian => RampRotationDegree * (System.MathF.PI / 180f);

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		_spawnerEditorHelper = new SpawnerEntityEditorHelper(this);
		_spawnerEditorHelper.LockGhostParent = false;
		if (_spawnerEditorHelper.IsValid)
		{
			_spawnerEditorHelper.SetupGhostMovement(PathEntityName);
			_spawnerEditorHelper.GivePermission("ramp", new SpawnerEntityEditorHelper.Permission(SpawnerEntityEditorHelper.PermissionType.rotation, SpawnerEntityEditorHelper.Axis.x), SetRampRotation);
			_spawnerEditorHelper.GivePermission("ai_barrier_r", new SpawnerEntityEditorHelper.Permission(SpawnerEntityEditorHelper.PermissionType.scale, SpawnerEntityEditorHelper.Axis.z), SetAIBarrierRight);
			_spawnerEditorHelper.GivePermission("ai_barrier_l", new SpawnerEntityEditorHelper.Permission(SpawnerEntityEditorHelper.PermissionType.scale, SpawnerEntityEditorHelper.Axis.z), SetAIBarrierLeft);
		}
		OnEditorVariableChanged("RampRotationDegree");
		OnEditorVariableChanged("BarrierLength");
	}

	private void SetRampRotation(float unusedArgument)
	{
		MatrixFrame frame = _spawnerEditorHelper.GetGhostEntityOrChild("ramp").GetFrame();
		Vec3 vec = new Vec3(0f - frame.rotation.u.y, frame.rotation.u.x);
		float z = frame.rotation.u.z;
		float num = TaleWorlds.Library.MathF.Atan2(vec.Length, z);
		if ((double)vec.x < 0.0)
		{
			num = 0f - num;
			num += System.MathF.PI * 2f;
		}
		float num2 = num;
		RampRotationDegree = num2 * 57.29578f;
	}

	private void SetAIBarrierRight(float barrierScale)
	{
		BarrierLength = barrierScale;
		MatrixFrame frame = _spawnerEditorHelper.GetGhostEntityOrChild("ai_barrier_l").GetFrame();
		MatrixFrame frame2 = _spawnerEditorHelper.GetGhostEntityOrChild("ai_barrier_r").GetFrame();
		frame.rotation.u = frame2.rotation.u;
		_spawnerEditorHelper.ChangeStableChildMatrixFrameAndApply("ai_barrier_l", frame, updateTriad: false);
	}

	private void SetAIBarrierLeft(float barrierScale)
	{
		BarrierLength = barrierScale;
		MatrixFrame frame = _spawnerEditorHelper.GetGhostEntityOrChild("ai_barrier_l").GetFrame();
		MatrixFrame frame2 = _spawnerEditorHelper.GetGhostEntityOrChild("ai_barrier_r").GetFrame();
		frame2.rotation.u = frame.rotation.u;
		_spawnerEditorHelper.ChangeStableChildMatrixFrameAndApply("ai_barrier_r", frame2, updateTriad: false);
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
		case "RampRotationDegree":
		{
			MatrixFrame frame3 = _spawnerEditorHelper.GetGhostEntityOrChild("ramp").GetFrame();
			frame3.rotation = Mat3.Identity;
			frame3.rotation.RotateAboutSide(RampRotationRadian);
			_spawnerEditorHelper.ChangeStableChildMatrixFrameAndApply("ramp", frame3);
			break;
		}
		case "BarrierLength":
		{
			MatrixFrame frame = _spawnerEditorHelper.GetGhostEntityOrChild("ai_barrier_l").GetFrame();
			frame.rotation.u.Normalize();
			frame.rotation.u *= TaleWorlds.Library.MathF.Max(0.01f, TaleWorlds.Library.MathF.Abs(BarrierLength));
			MatrixFrame frame2 = _spawnerEditorHelper.GetGhostEntityOrChild("ai_barrier_r").GetFrame();
			frame2.rotation.u = frame.rotation.u;
			_spawnerEditorHelper.ChangeStableChildMatrixFrameAndApply("ai_barrier_l", frame);
			_spawnerEditorHelper.ChangeStableChildMatrixFrameAndApply("ai_barrier_r", frame2);
			break;
		}
		case "SpeedModifierFactor":
			SpeedModifierFactor = TaleWorlds.Library.MathF.Clamp(SpeedModifierFactor, 0.8f, 1.2f);
			break;
		}
	}

	protected internal override void OnPreInit()
	{
		base.OnPreInit();
		_spawnerMissionHelper = new SpawnerEntityMissionHelper(this);
	}

	public override void AssignParameters(SpawnerEntityMissionHelper _spawnerMissionHelper)
	{
		SiegeTower firstScriptOfType = _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<SiegeTower>();
		firstScriptOfType.AddOnDeployTag = AddOnDeployTag;
		firstScriptOfType.RemoveOnDeployTag = RemoveOnDeployTag;
		firstScriptOfType.MaxSpeed *= SpeedModifierFactor;
		firstScriptOfType.MinSpeed *= SpeedModifierFactor;
		Mat3 identity = Mat3.Identity;
		identity.RotateAboutSide(RampRotationRadian);
		firstScriptOfType.AssignParametersFromSpawner(PathEntityName, TargetWallSegmentTag, SideTag, SoilNavMeshID1, SoilNavMeshID2, DitchNavMeshID1, DitchNavMeshID2, GroundToSoilNavMeshID1, GroundToSoilNavMeshID2, SoilGenericNavMeshID, GroundGenericNavMeshID, identity, BarrierTagToRemove);
	}
}
