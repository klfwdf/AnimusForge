using System;

namespace TaleWorlds.CampaignSystem.BarterSystem
{
	// Token: 0x02000478 RID: 1144
	public class PrisonerBarterGroup : BarterGroup
	{
		// Token: 0x17000E44 RID: 3652
		// (get) Token: 0x06004813 RID: 18451 RVA: 0x0016CA19 File Offset: 0x0016AC19
		public override float AIDecisionWeight
		{
			get
			{
				return 0.7f;
			}
		}
	}
}
