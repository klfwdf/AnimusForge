using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class InterruptingBehaviorGroup : AgentBehaviorGroup
{
	public InterruptingBehaviorGroup(AgentNavigator navigator, Mission mission)
		: base(navigator, mission)
	{
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (!base.IsActive)
		{
			return;
		}
		if (base.ScriptedBehavior != null)
		{
			if (!base.ScriptedBehavior.IsActive)
			{
				DisableAllBehaviors();
				base.ScriptedBehavior.IsActive = true;
			}
		}
		else
		{
			int bestBehaviorIndex = GetBestBehaviorIndex(isSimulation);
			if (bestBehaviorIndex != -1 && !Behaviors[bestBehaviorIndex].IsActive)
			{
				DisableAllBehaviors();
				Behaviors[bestBehaviorIndex].IsActive = true;
			}
		}
		TickActiveBehaviors(dt, isSimulation);
	}

	private void TickActiveBehaviors(float dt, bool isSimulation)
	{
		for (int num = Behaviors.Count - 1; num >= 0; num--)
		{
			AgentBehavior agentBehavior = Behaviors[num];
			if (agentBehavior.IsActive)
			{
				agentBehavior.Tick(dt, isSimulation);
			}
		}
	}

	public override float GetScore(bool isSimulation)
	{
		if (GetBestBehaviorIndex(isSimulation) != -1)
		{
			return 0.75f;
		}
		return 0f;
	}

	private int GetBestBehaviorIndex(bool isSimulation)
	{
		float num = 0f;
		int result = -1;
		for (int i = 0; i < Behaviors.Count; i++)
		{
			float availability = Behaviors[i].GetAvailability(isSimulation);
			if (availability > num)
			{
				num = availability;
				result = i;
			}
		}
		return result;
	}

	public override void ForceThink(float inSeconds)
	{
		Navigator.RefreshBehaviorGroups(isSimulation: false);
	}

	public override void ConversationTick()
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior.IsActive)
			{
				behavior.ConversationTick();
			}
		}
	}
}
