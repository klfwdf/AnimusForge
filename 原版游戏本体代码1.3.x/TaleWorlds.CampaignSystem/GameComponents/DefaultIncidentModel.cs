using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200011D RID: 285
	public class DefaultIncidentModel : IncidentModel
	{
		// Token: 0x06001808 RID: 6152 RVA: 0x000734C1 File Offset: 0x000716C1
		public override CampaignTime GetMinGlobalCooldownTime()
		{
			return CampaignTime.Days(8f);
		}

		// Token: 0x06001809 RID: 6153 RVA: 0x000734CD File Offset: 0x000716CD
		public override CampaignTime GetMaxGlobalCooldownTime()
		{
			return CampaignTime.Days(15f);
		}

		// Token: 0x0600180A RID: 6154 RVA: 0x000734D9 File Offset: 0x000716D9
		public override float GetIncidentTriggerGlobalProbability()
		{
			return 0.5f;
		}

		// Token: 0x0600180B RID: 6155 RVA: 0x000734E0 File Offset: 0x000716E0
		public override float GetIncidentTriggerProbabilityDuringSiege()
		{
			return 0.143f;
		}

		// Token: 0x0600180C RID: 6156 RVA: 0x000734E7 File Offset: 0x000716E7
		public override float GetIncidentTriggerProbabilityDuringWait()
		{
			return 0.143f;
		}
	}
}
