using System;
using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000092 RID: 146
	public interface ICampaignBehaviorManager
	{
		// Token: 0x06001266 RID: 4710
		void RegisterEvents();

		// Token: 0x06001267 RID: 4711
		T GetBehavior<T>();

		// Token: 0x06001268 RID: 4712
		IEnumerable<T> GetBehaviors<T>();

		// Token: 0x06001269 RID: 4713
		void AddBehavior(CampaignBehaviorBase campaignBehavior);

		// Token: 0x0600126A RID: 4714
		void RemoveBehavior<T>() where T : CampaignBehaviorBase;

		// Token: 0x0600126B RID: 4715
		void ClearBehaviors();

		// Token: 0x0600126C RID: 4716
		void LoadBehaviorData();

		// Token: 0x0600126D RID: 4717
		void InitializeCampaignBehaviors(IEnumerable<CampaignBehaviorBase> inputComponents);
	}
}
