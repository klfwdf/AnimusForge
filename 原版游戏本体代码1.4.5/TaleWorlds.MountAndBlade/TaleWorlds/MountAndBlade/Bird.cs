using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class Bird : MissionObject
{
	private enum State
	{
		TakingOff,
		Airborne,
		Landing,
		Perched
	}

	[EditableScriptComponentVariable(true, "")]
	private float _flyingSpeedInKph = 14.4f;

	[EditableScriptComponentVariable(true, "")]
	private string _idleAnimation = "anim_bird_idle";

	[EditableScriptComponentVariable(true, "")]
	private string _landingAnimation = "anim_bird_landing";

	[EditableScriptComponentVariable(true, "")]
	private string _takingOffAnimation = "anim_bird_flying";

	[EditableScriptComponentVariable(true, "")]
	private string _flyCycleAnimation = "anim_bird_cycle";

	[EditableScriptComponentVariable(true, "")]
	private bool _canLand = true;

	public bool CanFly;

	private State _state;

	private BasicMissionTimer _timer;

	private State ComputeInitialState()
	{
		if (CanFly && !_canLand)
		{
			return State.Airborne;
		}
		return State.Perched;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		base.GameEntity.SetAnimationSoundActivation(activate: true);
		base.GameEntity.Skeleton.SetAnimationAtChannel(_idleAnimation, 0);
		base.GameEntity.Skeleton.SetAnimationParameterAtChannel(0, MBRandom.RandomFloat * 0.5f);
		_timer = new BasicMissionTimer();
		SetState(ComputeInitialState());
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		base.GameEntity.Skeleton.SetAnimationAtChannel(_idleAnimation, 0);
		base.GameEntity.Skeleton.SetAnimationParameterAtChannel(0, 0f);
		_timer = new BasicMissionTimer();
		SetState(ComputeInitialState());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	private void SetState(State newState)
	{
		_state = newState;
		OnStateChanged();
	}

	private void OnStateChanged()
	{
		switch (_state)
		{
		case State.TakingOff:
			base.GameEntity.Skeleton.SetAnimationAtChannel(_takingOffAnimation, 0);
			_timer.Reset();
			break;
		case State.Airborne:
			base.GameEntity.Skeleton.SetAnimationAtChannel(_flyCycleAnimation, 0);
			_timer.Set(0f - MBRandom.RandomFloatRanged(5f, 15f));
			ApplyAnimationDisplacement();
			break;
		case State.Landing:
			base.GameEntity.Skeleton.SetAnimationAtChannel(_landingAnimation, 0);
			_timer.Reset();
			ApplyAnimationDisplacement();
			break;
		case State.Perched:
			base.GameEntity.Skeleton.SetAnimationAtChannel(_idleAnimation, 0);
			base.GameEntity.Skeleton.SetAnimationParameterAtChannel(0, MBRandom.RandomFloat * 0.5f);
			_timer.Set(0f - MBRandom.RandomFloatRanged(5f, 18f));
			break;
		default:
			Debug.FailedAssert("Unknown state please handle!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Bird.cs", "OnStateChanged", 138);
			break;
		}
	}

	protected internal override void OnTick(float dt)
	{
		switch (_state)
		{
		case State.TakingOff:
			if (base.GameEntity.Skeleton.GetAnimationParameterAtChannel(0) > 0.99f)
			{
				SetState(State.Airborne);
			}
			break;
		case State.Airborne:
			if (_timer.ElapsedTime > 0f)
			{
				if (_canLand)
				{
					SetState(State.Landing);
				}
				else
				{
					base.GameEntity.SetVisibilityExcludeParents(visible: false);
				}
			}
			else
			{
				ApplyFlyingMovement(dt);
			}
			break;
		case State.Landing:
			if (base.GameEntity.Skeleton.GetAnimationParameterAtChannel(0) > 0.99f)
			{
				SetState(State.Perched);
			}
			break;
		case State.Perched:
			if (CanFly && _timer.ElapsedTime > 0f && base.GameEntity.Skeleton.GetAnimationParameterAtChannel(0) > 0.99f)
			{
				SetState(State.TakingOff);
			}
			break;
		}
	}

	private void ApplyFlyingMovement(float dt)
	{
		float num = _flyingSpeedInKph * (5f / 18f) * dt;
		MatrixFrame frame = base.GameEntity.GetGlobalFrame();
		Vec3 vec = frame.rotation.f.NormalizedCopy();
		frame.origin -= vec * num;
		base.GameEntity.SetGlobalFrame(in frame);
	}

	private void ApplyAnimationDisplacement()
	{
		MatrixFrame frame = base.GameEntity.GetGlobalFrame();
		Vec3 vec = frame.rotation.f.NormalizedCopy();
		Vec3 vec2 = frame.rotation.u.NormalizedCopy();
		if (_state == State.Airborne)
		{
			frame.origin -= vec * 20f - vec2 * 6.5f;
		}
		else if (_state == State.Landing)
		{
			frame.origin -= vec * 20f + vec2 * 6.5f;
		}
		base.GameEntity.SetGlobalFrame(in frame);
	}
}
