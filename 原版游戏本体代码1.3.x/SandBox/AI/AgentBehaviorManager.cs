using System;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SandBox.AI
{
	// Token: 0x02000106 RID: 262
	public class AgentBehaviorManager : IAgentBehaviorManager
	{
		// Token: 0x06000D25 RID: 3365 RVA: 0x000602D0 File Offset: 0x0005E4D0
		public void AddQuestCharacterBehaviors(IAgent agent)
		{
			BehaviorSets.AddQuestCharacterBehaviors(agent);
		}

		// Token: 0x06000D26 RID: 3366 RVA: 0x000602D8 File Offset: 0x0005E4D8
		void IAgentBehaviorManager.AddWandererBehaviors(IAgent agent)
		{
			BehaviorSets.AddWandererBehaviors(agent);
		}

		// Token: 0x06000D27 RID: 3367 RVA: 0x000602E0 File Offset: 0x0005E4E0
		void IAgentBehaviorManager.AddOutdoorWandererBehaviors(IAgent agent)
		{
			BehaviorSets.AddOutdoorWandererBehaviors(agent);
		}

		// Token: 0x06000D28 RID: 3368 RVA: 0x000602E8 File Offset: 0x0005E4E8
		void IAgentBehaviorManager.AddIndoorWandererBehaviors(IAgent agent)
		{
			BehaviorSets.AddIndoorWandererBehaviors(agent);
		}

		// Token: 0x06000D29 RID: 3369 RVA: 0x000602F0 File Offset: 0x0005E4F0
		void IAgentBehaviorManager.AddFixedCharacterBehaviors(IAgent agent)
		{
			BehaviorSets.AddFixedCharacterBehaviors(agent);
		}

		// Token: 0x06000D2A RID: 3370 RVA: 0x000602F8 File Offset: 0x0005E4F8
		void IAgentBehaviorManager.AddPatrollingThugBehaviors(IAgent agent)
		{
			BehaviorSets.AddPatrollingThugBehaviors(agent);
		}

		// Token: 0x06000D2B RID: 3371 RVA: 0x00060300 File Offset: 0x0005E500
		void IAgentBehaviorManager.AddStandGuardBehaviors(IAgent agent)
		{
			BehaviorSets.AddStandGuardBehaviors(agent);
		}

		// Token: 0x06000D2C RID: 3372 RVA: 0x00060308 File Offset: 0x0005E508
		void IAgentBehaviorManager.AddFixedGuardBehaviors(IAgent agent)
		{
			BehaviorSets.AddFixedGuardBehaviors(agent);
		}

		// Token: 0x06000D2D RID: 3373 RVA: 0x00060310 File Offset: 0x0005E510
		void IAgentBehaviorManager.AddStealthAgentBehaviors(IAgent agent)
		{
			BehaviorSets.StealthAgentBehaviors(agent);
		}

		// Token: 0x06000D2E RID: 3374 RVA: 0x00060318 File Offset: 0x0005E518
		void IAgentBehaviorManager.AddPatrollingGuardBehaviors(IAgent agent)
		{
			BehaviorSets.AddPatrollingGuardBehaviors(agent);
		}

		// Token: 0x06000D2F RID: 3375 RVA: 0x00060320 File Offset: 0x0005E520
		void IAgentBehaviorManager.AddCompanionBehaviors(IAgent agent)
		{
			BehaviorSets.AddCompanionBehaviors(agent);
		}

		// Token: 0x06000D30 RID: 3376 RVA: 0x00060328 File Offset: 0x0005E528
		void IAgentBehaviorManager.AddBodyguardBehaviors(IAgent agent)
		{
			BehaviorSets.AddBodyguardBehaviors(agent);
		}

		// Token: 0x06000D31 RID: 3377 RVA: 0x00060330 File Offset: 0x0005E530
		public void AddFirstCompanionBehavior(IAgent agent)
		{
			BehaviorSets.AddFirstCompanionBehavior(agent);
		}
	}
}
