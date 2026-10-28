using SandBox.Missions.MissionLogics;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class FightBehavior : AgentBehavior
{
	public FightBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		if (base.OwnerAgent.HumanAIComponent == null)
		{
			base.OwnerAgent.AddComponent(new HumanAIComponent(base.OwnerAgent));
		}
	}

	public override float GetAvailability(bool isSimulation)
	{
		if (!MissionFightHandler.IsAgentAggressive(base.OwnerAgent))
		{
			return 0.1f;
		}
		return 1f;
	}

	protected override void OnActivate()
	{
		TextObject textObject = new TextObject("{=!}{p0} {p1} activate alarmed behavior group.");
		textObject.SetTextVariable("p0", base.OwnerAgent.Name.ToString());
		textObject.SetTextVariable("p1", base.OwnerAgent.Index.ToString());
	}

	protected override void OnDeactivate()
	{
		TextObject textObject = new TextObject("{=!}{p0} {p1} deactivate fight behavior.");
		textObject.SetTextVariable("p0", base.OwnerAgent.Name.ToString());
		textObject.SetTextVariable("p1", base.OwnerAgent.Index.ToString());
	}

	public override string GetDebugInfo()
	{
		return "Fight";
	}
}
