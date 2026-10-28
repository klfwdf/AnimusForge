using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class BehaviorSets
{
	private static void AddBehaviorGroups(IAgent agent)
	{
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.AddBehaviorGroup<DailyBehaviorGroup>();
		agentNavigator.AddBehaviorGroup<InterruptingBehaviorGroup>();
		agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
	}

	public static void AddQuestCharacterBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>();
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<FleeBehavior>();
		behaviorGroup.AddBehavior<FightBehavior>();
	}

	public static void AddWandererBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>();
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<FleeBehavior>();
		behaviorGroup.AddBehavior<FightBehavior>();
	}

	public static void AddOutdoorWandererBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		DailyBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		behaviorGroup.AddBehavior<WalkingBehavior>().SetIndoorWandering(isActive: false);
		behaviorGroup.AddBehavior<ChangeLocationBehavior>();
		AlarmedBehaviorGroup behaviorGroup2 = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup2.AddBehavior<FleeBehavior>();
		behaviorGroup2.AddBehavior<FightBehavior>();
	}

	public static void AddIndoorWandererBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>().SetOutdoorWandering(isActive: false);
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<FleeBehavior>();
		behaviorGroup.AddBehavior<FightBehavior>();
	}

	public static void AddFixedCharacterBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		WalkingBehavior walkingBehavior = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>();
		walkingBehavior.SetIndoorWandering(isActive: false);
		walkingBehavior.SetOutdoorWandering(isActive: false);
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<FleeBehavior>();
		behaviorGroup.AddBehavior<FightBehavior>();
	}

	public static void AddPatrollingThugBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<PatrolAgentBehavior>();
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<FleeBehavior>();
		behaviorGroup.AddBehavior<FightBehavior>();
	}

	public static void AddStandGuardBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<StandGuardBehavior>();
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<FightBehavior>();
		behaviorGroup.DisableCalmDown = true;
	}

	public static void AddFixedGuardBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
	}

	public static void StealthAgentBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.AddBehaviorGroup<DailyBehaviorGroup>();
		agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<CautiousBehavior>();
		behaviorGroup.AddBehavior<FightBehavior>();
		if (agent.Character.StringId == "disguise_officer_character")
		{
			behaviorGroup.SetCanMoveWhenCautious(value: false);
		}
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<PatrolAgentBehavior>();
	}

	public static void AddPatrollingGuardBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<PatrollingGuardBehavior>();
		AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
		behaviorGroup.AddBehavior<FightBehavior>();
		behaviorGroup.DisableCalmDown = true;
	}

	public static void AddCompanionBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>().SetIndoorWandering(isActive: false);
		agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
	}

	public static void AddBodyguardBehaviors(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		DailyBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		behaviorGroup.AddBehavior<WalkingBehavior>();
		behaviorGroup.AddBehavior<FollowAgentBehavior>().SetTargetAgent(Agent.Main);
		agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
	}

	public static void AddFirstCompanionBehavior(IAgent agent)
	{
		AddBehaviorGroups(agent);
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
	}
}
