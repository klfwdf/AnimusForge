using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public abstract class AgentBehaviorGroup
{
	public AgentNavigator Navigator;

	public List<AgentBehavior> Behaviors;

	protected float CheckBehaviorTime = 5f;

	protected Timer CheckBehaviorTimer;

	private bool _isActive;

	public Agent OwnerAgent => Navigator.OwnerAgent;

	public AgentBehavior ScriptedBehavior { get; private set; }

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

	public Mission Mission { get; private set; }

	protected AgentBehaviorGroup(AgentNavigator navigator, Mission mission)
	{
		Mission = mission;
		Behaviors = new List<AgentBehavior>();
		Navigator = navigator;
		_isActive = false;
		ScriptedBehavior = null;
	}

	public T AddBehavior<T>() where T : AgentBehavior
	{
		T val = Activator.CreateInstance(typeof(T), this) as T;
		if (val != null)
		{
			foreach (AgentBehavior behavior in Behaviors)
			{
				if (behavior.GetType() == val.GetType())
				{
					return behavior as T;
				}
			}
			Behaviors.Add(val);
		}
		return val;
	}

	public T GetBehavior<T>() where T : AgentBehavior
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior is T)
			{
				return (T)behavior;
			}
		}
		return null;
	}

	public bool HasBehavior<T>() where T : AgentBehavior
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior is T)
			{
				return true;
			}
		}
		return false;
	}

	public void RemoveBehavior<T>() where T : AgentBehavior
	{
		for (int i = 0; i < Behaviors.Count; i++)
		{
			if (Behaviors[i] is T)
			{
				bool isActive = Behaviors[i].IsActive;
				Behaviors[i].IsActive = false;
				if (ScriptedBehavior == Behaviors[i])
				{
					ScriptedBehavior = null;
				}
				Behaviors.RemoveAt(i);
				if (isActive)
				{
					ForceThink(0f);
				}
			}
		}
	}

	public void SetScriptedBehavior<T>() where T : AgentBehavior
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior is T)
			{
				ScriptedBehavior = behavior;
				ForceThink(0f);
				break;
			}
		}
		foreach (AgentBehavior behavior2 in Behaviors)
		{
			if (behavior2 != ScriptedBehavior)
			{
				behavior2.IsActive = false;
			}
		}
	}

	public void DisableScriptedBehavior()
	{
		if (ScriptedBehavior != null)
		{
			ScriptedBehavior.IsActive = false;
			ScriptedBehavior = null;
			ForceThink(0f);
		}
	}

	public void DisableAllBehaviors()
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			behavior.IsActive = false;
		}
	}

	public AgentBehavior GetActiveBehavior()
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior.IsActive)
			{
				return behavior;
			}
		}
		return null;
	}

	public virtual void Tick(float dt, bool isSimulation)
	{
	}

	public virtual void ConversationTick()
	{
	}

	public virtual void OnAgentRemoved(Agent agent)
	{
	}

	protected virtual void OnActivate()
	{
	}

	protected virtual void OnDeactivate()
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			behavior.IsActive = false;
		}
	}

	public virtual float GetScore(bool isSimulation)
	{
		return 0f;
	}

	public virtual void ForceThink(float inSeconds)
	{
	}
}
