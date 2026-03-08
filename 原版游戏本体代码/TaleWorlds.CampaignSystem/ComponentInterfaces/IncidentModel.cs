using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001FE RID: 510
	public abstract class IncidentModel : MBGameModel<IncidentModel>
	{
		// Token: 0x06001F4F RID: 8015
		public abstract CampaignTime GetMinGlobalCooldownTime();

		// Token: 0x06001F50 RID: 8016
		public abstract CampaignTime GetMaxGlobalCooldownTime();

		// Token: 0x06001F51 RID: 8017
		public abstract float GetIncidentTriggerGlobalProbability();

		// Token: 0x06001F52 RID: 8018
		public abstract float GetIncidentTriggerProbabilityDuringSiege();

		// Token: 0x06001F53 RID: 8019
		public abstract float GetIncidentTriggerProbabilityDuringWait();
	}
}
