using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200011C RID: 284
	public class DefaultHideoutModel : HideoutModel
	{
		// Token: 0x17000665 RID: 1637
		// (get) Token: 0x06001803 RID: 6147 RVA: 0x00073477 File Offset: 0x00071677
		public override CampaignTime HideoutHiddenDuration
		{
			get
			{
				return CampaignTime.Days(10f);
			}
		}

		// Token: 0x17000666 RID: 1638
		// (get) Token: 0x06001804 RID: 6148 RVA: 0x00073483 File Offset: 0x00071683
		public override int CanAttackHideoutStartTime
		{
			get
			{
				return CampaignTime.SunSet + 1;
			}
		}

		// Token: 0x17000667 RID: 1639
		// (get) Token: 0x06001805 RID: 6149 RVA: 0x0007348C File Offset: 0x0007168C
		public override int CanAttackHideoutEndTime
		{
			get
			{
				return CampaignTime.SunRise;
			}
		}

		// Token: 0x06001806 RID: 6150 RVA: 0x00073493 File Offset: 0x00071693
		public override float GetRogueryXpGainOnHideoutMissionEnd(bool isSucceeded)
		{
			return (float)(isSucceeded ? MBRandom.RandomInt(700, 1000) : MBRandom.RandomInt(225, 400));
		}
	}
}
