using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000FE RID: 254
	public class DefaultCampaignTimeModel : CampaignTimeModel
	{
		// Token: 0x17000624 RID: 1572
		// (get) Token: 0x06001697 RID: 5783 RVA: 0x000674B4 File Offset: 0x000656B4
		public override CampaignTime CampaignStartTime
		{
			get
			{
				return CampaignTime.Years(1084f) + CampaignTime.Weeks((float)CampaignTime.WeeksInSeason) + CampaignTime.Hours(9f);
			}
		}

		// Token: 0x17000625 RID: 1573
		// (get) Token: 0x06001698 RID: 5784 RVA: 0x000674DF File Offset: 0x000656DF
		public override int SunRise
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x17000626 RID: 1574
		// (get) Token: 0x06001699 RID: 5785 RVA: 0x000674E2 File Offset: 0x000656E2
		public override int SunSet
		{
			get
			{
				return 22;
			}
		}

		// Token: 0x17000627 RID: 1575
		// (get) Token: 0x0600169A RID: 5786 RVA: 0x000674E6 File Offset: 0x000656E6
		public override long TimeTicksPerMillisecond
		{
			get
			{
				return 10L;
			}
		}

		// Token: 0x17000628 RID: 1576
		// (get) Token: 0x0600169B RID: 5787 RVA: 0x000674EB File Offset: 0x000656EB
		public override int MillisecondInSecond
		{
			get
			{
				return 1000;
			}
		}

		// Token: 0x17000629 RID: 1577
		// (get) Token: 0x0600169C RID: 5788 RVA: 0x000674F2 File Offset: 0x000656F2
		public override int SecondsInMinute
		{
			get
			{
				return 60;
			}
		}

		// Token: 0x1700062A RID: 1578
		// (get) Token: 0x0600169D RID: 5789 RVA: 0x000674F6 File Offset: 0x000656F6
		public override int MinutesInHour
		{
			get
			{
				return 60;
			}
		}

		// Token: 0x1700062B RID: 1579
		// (get) Token: 0x0600169E RID: 5790 RVA: 0x000674FA File Offset: 0x000656FA
		public override int HoursInDay
		{
			get
			{
				return 24;
			}
		}

		// Token: 0x1700062C RID: 1580
		// (get) Token: 0x0600169F RID: 5791 RVA: 0x000674FE File Offset: 0x000656FE
		public override int DaysInWeek
		{
			get
			{
				if (Campaign.Current.Options.AccelerationMode != GameAccelerationMode.Fast)
				{
					return 7;
				}
				return 3;
			}
		}

		// Token: 0x1700062D RID: 1581
		// (get) Token: 0x060016A0 RID: 5792 RVA: 0x00067515 File Offset: 0x00065715
		public override int WeeksInSeason
		{
			get
			{
				if (Campaign.Current.Options.AccelerationMode != GameAccelerationMode.Fast)
				{
					return 3;
				}
				return 2;
			}
		}

		// Token: 0x1700062E RID: 1582
		// (get) Token: 0x060016A1 RID: 5793 RVA: 0x0006752C File Offset: 0x0006572C
		public override int SeasonsInYear
		{
			get
			{
				return 4;
			}
		}
	}
}
