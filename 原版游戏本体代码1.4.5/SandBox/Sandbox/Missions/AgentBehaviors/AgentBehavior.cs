using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public abstract class AgentBehavior
{
	public float CheckTime = 15f;

	protected readonly AgentBehaviorGroup BehaviorGroup;

	private bool _isActive;

	public AgentNavigator Navigator => BehaviorGroup.Navigator;

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (_isActive != value)
			{
				_isActive = value;
				if (_isActive)
				{
					OnActivate();
				}
				else
				{
					OnDeactivate();
				}
			}
		}
	}

	public Agent OwnerAgent => Navigator.OwnerAgent;

	public Mission Mission { get; private set; }

	protected AgentBehavior(AgentBehaviorGroup behaviorGroup)
	{
		Mission = behaviorGroup.Mission;
		CheckTime = 40f + MBRandom.RandomFloat * 20f;
		BehaviorGroup = behaviorGroup;
		_isActive = false;
	}

	public virtual float GetAvailability(bool isSimulation)
	{
		return 0f;
	}

	public virtual void Tick(float dt, bool isSimulation)
	{
	}

	public virtual void ConversationTick()
	{
	}

	protected virtual void OnActivate()
	{
	}

	protected virtual void OnDeactivate()
	{
	}

	public virtual bool CheckStartWithBehavior()
	{
		return false;
	}

	public virtual void OnSpecialTargetChanged()
	{
	}

	public virtual void SetCustomWanderTarget(UsableMachine customUsableMachine)
	{
	}

	public virtual void OnAgentRemoved(Agent agent)
	{
	}

	public abstract string GetDebugInfo();
}
