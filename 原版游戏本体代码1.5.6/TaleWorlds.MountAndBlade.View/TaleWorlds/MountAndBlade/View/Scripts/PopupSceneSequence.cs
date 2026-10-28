using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class PopupSceneSequence : ScriptComponentBehavior
{
	public float InitialActivationTime;

	public float PositiveActivationTime;

	public float NegativeActivationTime;

	protected AgentVisuals _agentVisuals;

	protected float _time;

	protected bool _triggered;

	protected int _state;

	public void InitializeWithAgentVisuals(AgentVisuals visuals)
	{
		_agentVisuals = visuals;
		_time = 0f;
		_triggered = false;
		_state = 0;
	}

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected override void OnTick(float dt)
	{
		_time += dt;
		if (!_triggered)
		{
			if (_state == 0 && _time >= InitialActivationTime)
			{
				_triggered = true;
				OnInitialState();
			}
			if (_state == 1 && _time >= PositiveActivationTime)
			{
				_triggered = true;
				OnPositiveState();
			}
			if (_state == 2 && _time >= NegativeActivationTime)
			{
				_triggered = true;
				OnNegativeState();
			}
		}
	}

	public virtual void OnInitialState()
	{
	}

	public virtual void OnPositiveState()
	{
	}

	public virtual void OnNegativeState()
	{
	}

	public void SetInitialState()
	{
		_triggered = false;
		_state = 0;
		_time = 0f;
	}

	public void SetPositiveState()
	{
		_triggered = false;
		_state = 1;
		_time = 0f;
	}

	public void SetNegativeState()
	{
		_triggered = false;
		_state = 2;
		_time = 0f;
	}
}
