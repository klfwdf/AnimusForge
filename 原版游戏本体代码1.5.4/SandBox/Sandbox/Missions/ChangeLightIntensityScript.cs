using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.Missions;

public class ChangeLightIntensityScript : ScriptComponentBehavior
{
	private enum State
	{
		None,
		Start,
		WaitBeforeChange,
		ChangingIntensity,
		End
	}

	[EditableScriptComponentVariable(true, "WaitBeforeChangeAsSeconds")]
	private float _waitBeforeChangeAsSeconds;

	[EditableScriptComponentVariable(true, "ChangeAmount")]
	private float _changeAmount = 20f;

	[EditableScriptComponentVariable(true, "ChangeSpeed")]
	private float _changeSpeed = 1f;

	private State _state;

	private float _currentChangeAmount;

	private float _currentTimeDt;

	public SimpleButton Preview;

	public SimpleButton Reset;

	private float _initialIntensityCacheForPreviewButton;

	private Light _lightComponent;

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
		switch (_state)
		{
		case State.Start:
			_lightComponent = base.GameEntity.GetLight();
			_initialIntensityCacheForPreviewButton = _lightComponent.Intensity;
			if (_waitBeforeChangeAsSeconds > 0f)
			{
				_state = State.WaitBeforeChange;
			}
			else
			{
				_state = State.ChangingIntensity;
			}
			break;
		case State.WaitBeforeChange:
			_currentTimeDt += dt;
			if (_currentTimeDt >= _waitBeforeChangeAsSeconds)
			{
				_state = State.ChangingIntensity;
			}
			break;
		case State.ChangingIntensity:
		{
			float num = 1f * _changeSpeed;
			_currentChangeAmount += num;
			_lightComponent.Intensity += num;
			if (_currentChangeAmount >= _changeAmount)
			{
				_state = State.End;
			}
			break;
		}
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "Preview")
		{
			if (_state != State.None && _state != State.End)
			{
				Debug.FailedAssert("The intensity change is already started, please click the \"Reset\" button first!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\ChangeLightIntensityScript.cs", "OnEditorVariableChanged", 108);
			}
			else
			{
				_currentChangeAmount = 0f;
				_currentTimeDt = 0f;
				_state = State.Start;
			}
		}
		if (variableName == "Reset")
		{
			_lightComponent.Intensity = _initialIntensityCacheForPreviewButton;
			_currentChangeAmount = 0f;
			_currentTimeDt = 0f;
			_state = State.None;
		}
	}
}
