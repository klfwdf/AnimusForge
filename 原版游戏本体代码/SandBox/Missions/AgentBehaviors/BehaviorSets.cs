using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x0200009F RID: 159
	public class BehaviorSets
	{
		// Token: 0x060006B7 RID: 1719 RVA: 0x0002DEE0 File Offset: 0x0002C0E0
		private static void AddBehaviorGroups(IAgent agent)
		{
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.AddBehaviorGroup<DailyBehaviorGroup>();
			agentNavigator.AddBehaviorGroup<InterruptingBehaviorGroup>();
			agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
		}

		// Token: 0x060006B8 RID: 1720 RVA: 0x0002DF06 File Offset: 0x0002C106
		public static void AddQuestCharacterBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>();
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<FleeBehavior>();
			behaviorGroup.AddBehavior<FightBehavior>();
		}

		// Token: 0x060006B9 RID: 1721 RVA: 0x0002DF3C File Offset: 0x0002C13C
		public static void AddWandererBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>();
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<FleeBehavior>();
			behaviorGroup.AddBehavior<FightBehavior>();
		}

		// Token: 0x060006BA RID: 1722 RVA: 0x0002DF74 File Offset: 0x0002C174
		public static void AddOutdoorWandererBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			DailyBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			behaviorGroup.AddBehavior<WalkingBehavior>().SetIndoorWandering(false);
			behaviorGroup.AddBehavior<ChangeLocationBehavior>();
			AlarmedBehaviorGroup behaviorGroup2 = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup2.AddBehavior<FleeBehavior>();
			behaviorGroup2.AddBehavior<FightBehavior>();
		}

		// Token: 0x060006BB RID: 1723 RVA: 0x0002DFC1 File Offset: 0x0002C1C1
		public static void AddIndoorWandererBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>().SetOutdoorWandering(false);
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<FleeBehavior>();
			behaviorGroup.AddBehavior<FightBehavior>();
		}

		// Token: 0x060006BC RID: 1724 RVA: 0x0002DFFC File Offset: 0x0002C1FC
		public static void AddFixedCharacterBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			WalkingBehavior walkingBehavior = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>();
			walkingBehavior.SetIndoorWandering(false);
			walkingBehavior.SetOutdoorWandering(false);
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<FleeBehavior>();
			behaviorGroup.AddBehavior<FightBehavior>();
		}

		// Token: 0x060006BD RID: 1725 RVA: 0x0002E049 File Offset: 0x0002C249
		public static void AddPatrollingThugBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<PatrolAgentBehavior>();
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<FleeBehavior>();
			behaviorGroup.AddBehavior<FightBehavior>();
		}

		// Token: 0x060006BE RID: 1726 RVA: 0x0002E07F File Offset: 0x0002C27F
		public static void AddStandGuardBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<StandGuardBehavior>();
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<FightBehavior>();
			behaviorGroup.DisableCalmDown = true;
		}

		// Token: 0x060006BF RID: 1727 RVA: 0x0002E0B5 File Offset: 0x0002C2B5
		public static void AddFixedGuardBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
		}

		// Token: 0x060006C0 RID: 1728 RVA: 0x0002E0D8 File Offset: 0x0002C2D8
		public static void StealthAgentBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.AddBehaviorGroup<DailyBehaviorGroup>();
			agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<CautiousBehavior>();
			behaviorGroup.AddBehavior<FightBehavior>();
			if (agent.Character.StringId == "disguise_officer_character")
			{
				behaviorGroup.SetCanMoveWhenCautious(false);
			}
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<PatrolAgentBehavior>();
		}

		// Token: 0x060006C1 RID: 1729 RVA: 0x0002E147 File Offset: 0x0002C347
		public static void AddPatrollingGuardBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<PatrollingGuardBehavior>();
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup.AddBehavior<FightBehavior>();
			behaviorGroup.DisableCalmDown = true;
		}

		// Token: 0x060006C2 RID: 1730 RVA: 0x0002E17D File Offset: 0x0002C37D
		public static void AddCompanionBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<WalkingBehavior>().SetIndoorWandering(false);
			agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
		}

		// Token: 0x060006C3 RID: 1731 RVA: 0x0002E1B1 File Offset: 0x0002C3B1
		public static void AddBodyguardBehaviors(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			DailyBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			behaviorGroup.AddBehavior<WalkingBehavior>();
			behaviorGroup.AddBehavior<FollowAgentBehavior>().SetTargetAgent(Agent.Main);
			agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
		}

		// Token: 0x060006C4 RID: 1732 RVA: 0x0002E1F0 File Offset: 0x0002C3F0
		public static void AddFirstCompanionBehavior(IAgent agent)
		{
			BehaviorSets.AddBehaviorGroups(agent);
			AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
			agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<FightBehavior>();
		}
	}
}
