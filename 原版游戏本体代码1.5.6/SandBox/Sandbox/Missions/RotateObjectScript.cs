using System;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.Missions;

public class RotateObjectScript : ScriptComponentBehavior
{
	private enum State
	{
		None,
		Start,
		WaitBeforeRotate,
		Rotating,
		End
	}

	[EditableScriptComponentVariable(true, "RotationAxis")]
	private string _rotationAxis = "X";

	[EditableScriptComponentVariable(true, "WaitBeforeRotateAsSeconds")]
	private float _waitBeforeRotateAsSeconds = 2f;

	[EditableScriptComponentVariable(true, "RotateAngle")]
	private float _rotateAngle = 90f;

	[EditableScriptComponentVariable(true, "RotationSpeed")]
	private float _rotationSpeed = 1f;

	public SimpleButton PreviewRotateObject;

	public SimpleButton StopMovement;

	private MatrixFrame _initialFrameCacheForPreviewRotateObjectButton;

	private State _state;

	private float _currentRotationAngle;

	private float _currentTimeDt;

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnTick(float dt)
	{
		if (_state == State.None)
		{
			_state = State.Start;
		}
		OnTickInternal(dt);
	}

	protected override void OnEditorTick(float dt)
	{
		OnTickInternal(dt);
	}

	private void OnTickInternal(float dt)
	{
		if (_rotationAxis.Equals("X", StringComparison.OrdinalIgnoreCase) || _rotationAxis.Equals("Y", StringComparison.OrdinalIgnoreCase) || _rotationAxis.Equals("Z", StringComparison.OrdinalIgnoreCase))
		{
			_rotationAxis = "X";
		}
		switch (_state)
		{
		case State.Start:
			if (_waitBeforeRotateAsSeconds > 0f)
			{
				_initialFrameCacheForPreviewRotateObjectButton = base.GameEntity.GetFrame();
				_state = State.WaitBeforeRotate;
			}
			else
			{
				_state = State.Rotating;
			}
			break;
		case State.WaitBeforeRotate:
			_currentTimeDt += dt;
			if (_currentTimeDt >= _waitBeforeRotateAsSeconds)
			{
				_state = State.Rotating;
			}
			break;
		case State.Rotating:
		{
			int num = TaleWorlds.Library.MathF.Sign(_rotateAngle);
			MatrixFrame frame = base.GameEntity.GetFrame();
			frame.Rotate(_rotationSpeed * (float)num * dt * (System.MathF.PI / 180f), GetRotationAxis());
			base.GameEntity.SetFrame(ref frame);
			_currentRotationAngle += _rotationSpeed * (float)num * dt;
			if (Math.Abs(_currentRotationAngle) >= Math.Abs(_rotateAngle))
			{
				_state = State.End;
			}
			break;
		}
		}
	}

	private Vec3 GetRotationAxis()
	{
		if (_rotationAxis.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			return Vec3.Side;
		}
		if (_rotationAxis.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return Vec3.Forward;
		}
		if (_rotationAxis.Equals("Z", StringComparison.OrdinalIgnoreCase))
		{
			return Vec3.Up;
		}
		Debug.FailedAssert("Wrong rotation axis!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\RotateObjectScript.cs", "GetRotationAxis", 123);
		return Vec3.Forward;
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "PreviewRotateObject")
		{
			if (_state != State.None && _state != State.End)
			{
				Debug.FailedAssert("The rotation is already started, please click the \"StopMovement\" button first!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\RotateObjectScript.cs", "OnEditorVariableChanged", 135);
			}
			else
			{
				_initialFrameCacheForPreviewRotateObjectButton = base.GameEntity.GetFrame();
				_currentRotationAngle = 0f;
				_currentTimeDt = 0f;
				_state = State.Start;
			}
		}
		if (variableName == "StopMovement")
		{
			base.GameEntity.SetFrame(ref _initialFrameCacheForPreviewRotateObjectButton);
			_currentRotationAngle = 0f;
			_currentTimeDt = 0f;
			_state = State.None;
		}
	}
}
