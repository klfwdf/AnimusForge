using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TrajectoryVisualizer : ScriptComponentBehavior
{
	private struct TrajectoryParams
	{
		public Vec3 MissileShootingPositionOffset;

		public float MissileSpeed;

		public float VerticalAngleMinInDegrees;

		public float VerticalAngleMaxInDegrees;

		public float HorizontalAngleRangeInDegrees;

		public float AirFrictionConstant;

		public bool IsValid;
	}

	public bool ShowTrajectory;

	private GameEntity _trajectoryMeshHolder;

	private TrajectoryParams _trajectoryParams;

	public void SetTrajectoryParams(Vec3 missileShootingPositionOffset, float missileSpeed, float verticalAngleMinInDegrees, float verticalAngleMaxInDegrees, float horizontalAngleRangeInDegrees, float airFrictionConstant)
	{
		_trajectoryParams.MissileShootingPositionOffset = missileShootingPositionOffset;
		_trajectoryParams.MissileSpeed = missileSpeed;
		_trajectoryParams.VerticalAngleMinInDegrees = verticalAngleMinInDegrees;
		_trajectoryParams.VerticalAngleMaxInDegrees = verticalAngleMaxInDegrees;
		_trajectoryParams.HorizontalAngleRangeInDegrees = horizontalAngleRangeInDegrees;
		_trajectoryParams.AirFrictionConstant = airFrictionConstant;
		_trajectoryParams.IsValid = true;
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		if (!(variableName == "ShowTrajectory"))
		{
			return;
		}
		if (ShowTrajectory && _trajectoryMeshHolder == null && !base.GameEntity.IsGhostObject() && _trajectoryParams.IsValid)
		{
			_trajectoryMeshHolder = TaleWorlds.Engine.GameEntity.CreateEmpty(base.Scene, isModifiableFromEditor: false);
			if (_trajectoryMeshHolder != null)
			{
				_trajectoryMeshHolder.EntityFlags |= EntityFlags.DontSaveToScene;
				MatrixFrame frame = base.GameEntity.GetGlobalFrame();
				Vec3 origin = frame.origin + (frame.rotation.s * _trajectoryParams.MissileShootingPositionOffset.x + frame.rotation.f * _trajectoryParams.MissileShootingPositionOffset.y + frame.rotation.u * _trajectoryParams.MissileShootingPositionOffset.z);
				frame.origin = origin;
				_trajectoryMeshHolder.SetGlobalFrame(in frame);
				_trajectoryMeshHolder.ComputeTrajectoryVolume(_trajectoryParams.MissileSpeed, _trajectoryParams.VerticalAngleMaxInDegrees, _trajectoryParams.VerticalAngleMinInDegrees, _trajectoryParams.HorizontalAngleRangeInDegrees, _trajectoryParams.AirFrictionConstant);
				base.GameEntity.AddChild(_trajectoryMeshHolder.WeakEntity, autoLocalizeFrame: true);
				_trajectoryMeshHolder.SetVisibilityExcludeParents(visible: false);
			}
		}
		if (_trajectoryMeshHolder != null)
		{
			_trajectoryMeshHolder.SetVisibilityExcludeParents(ShowTrajectory);
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		if (_trajectoryMeshHolder != null)
		{
			_trajectoryMeshHolder.Remove(removeReason);
		}
	}
}
