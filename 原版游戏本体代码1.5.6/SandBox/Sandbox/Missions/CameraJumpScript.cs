using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.Missions;

public class CameraJumpScript : ScriptComponentBehavior
{
	[EditableScriptComponentVariable(true, "WaitBeforeCameraJump")]
	private float _waitBeforeCameraJump = 2f;

	[EditableScriptComponentVariable(true, "CameraJumpPosition")]
	private Vec3 _cameraJumpPosition;

	[EditableScriptComponentVariable(true, "CameraJumpRotation")]
	private Vec3 _cameraJumpRotation;

	public SimpleButton SetCurrentCameraTransform;

	public SimpleButton Preview;

	public SimpleButton Reset;

	private MatrixFrame _initialGlobalFrame;

	private float _elapsedDuration = -1f;

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnInit()
	{
		_elapsedDuration = 0f;
	}

	protected override void OnEditorInit()
	{
		_initialGlobalFrame = base.GameEntity.GetGlobalFrame();
	}

	protected override void OnTick(float dt)
	{
		OnJumpTick(dt);
	}

	protected override void OnEditorTick(float dt)
	{
		OnJumpTick(dt);
	}

	private void OnJumpTick(float dt)
	{
		if (_elapsedDuration >= 0f)
		{
			_elapsedDuration += dt;
			if (_elapsedDuration >= _waitBeforeCameraJump)
			{
				Mat3 rot = Mat3.Identity;
				rot.ApplyEulerAngles(in _cameraJumpRotation);
				base.GameEntity.SetGlobalFrame(new MatrixFrame(in rot, in _cameraJumpPosition));
			}
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		if (variableName == "Preview")
		{
			_elapsedDuration = 0f;
		}
		if (variableName == "Reset")
		{
			base.GameEntity.SetGlobalFrame(in _initialGlobalFrame);
			_elapsedDuration = -1f;
		}
		if (variableName == "SetCurrentCameraTransform")
		{
			MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
			_cameraJumpPosition = globalFrame.origin;
			_cameraJumpRotation = globalFrame.rotation.GetEulerAngles();
		}
	}
}
