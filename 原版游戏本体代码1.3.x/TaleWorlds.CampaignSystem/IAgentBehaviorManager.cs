using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200008F RID: 143
	public interface IAgentBehaviorManager
	{
		// Token: 0x06001248 RID: 4680
		void AddQuestCharacterBehaviors(IAgent agent);

		// Token: 0x06001249 RID: 4681
		void AddWandererBehaviors(IAgent agent);

		// Token: 0x0600124A RID: 4682
		void AddOutdoorWandererBehaviors(IAgent agent);

		// Token: 0x0600124B RID: 4683
		void AddIndoorWandererBehaviors(IAgent agent);

		// Token: 0x0600124C RID: 4684
		void AddFixedCharacterBehaviors(IAgent agent);

		// Token: 0x0600124D RID: 4685
		void AddPatrollingThugBehaviors(IAgent agent);

		// Token: 0x0600124E RID: 4686
		void AddStandGuardBehaviors(IAgent agent);

		// Token: 0x0600124F RID: 4687
		void AddFixedGuardBehaviors(IAgent agent);

		// Token: 0x06001250 RID: 4688
		void AddStealthAgentBehaviors(IAgent agent);

		// Token: 0x06001251 RID: 4689
		void AddPatrollingGuardBehaviors(IAgent agent);

		// Token: 0x06001252 RID: 4690
		void AddCompanionBehaviors(IAgent agent);

		// Token: 0x06001253 RID: 4691
		void AddBodyguardBehaviors(IAgent agent);

		// Token: 0x06001254 RID: 4692
		void AddFirstCompanionBehavior(IAgent agent);
	}
}
