using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F8 RID: 504
	public abstract class CampaignTimeModel : MBGameModel<CampaignTimeModel>
	{
		// Token: 0x170007BF RID: 1983
		// (get) Token: 0x06001F27 RID: 7975
		public abstract CampaignTime CampaignStartTime { get; }

		// Token: 0x170007C0 RID: 1984
		// (get) Token: 0x06001F28 RID: 7976
		public abstract int SunRise { get; }

		// Token: 0x170007C1 RID: 1985
		// (get) Token: 0x06001F29 RID: 7977
		public abstract int SunSet { get; }

		// Token: 0x170007C2 RID: 1986
		// (get) Token: 0x06001F2A RID: 7978
		public abstract long TimeTicksPerMillisecond { get; }

		// Token: 0x170007C3 RID: 1987
		// (get) Token: 0x06001F2B RID: 7979
		public abstract int MillisecondInSecond { get; }

		// Token: 0x170007C4 RID: 1988
		// (get) Token: 0x06001F2C RID: 7980
		public abstract int SecondsInMinute { get; }

		// Token: 0x170007C5 RID: 1989
		// (get) Token: 0x06001F2D RID: 7981
		public abstract int MinutesInHour { get; }

		// Token: 0x170007C6 RID: 1990
		// (get) Token: 0x06001F2E RID: 7982
		public abstract int HoursInDay { get; }

		// Token: 0x170007C7 RID: 1991
		// (get) Token: 0x06001F2F RID: 7983
		public abstract int DaysInWeek { get; }

		// Token: 0x170007C8 RID: 1992
		// (get) Token: 0x06001F30 RID: 7984
		public abstract int WeeksInSeason { get; }

		// Token: 0x170007C9 RID: 1993
		// (get) Token: 0x06001F31 RID: 7985
		public abstract int SeasonsInYear { get; }
	}
}
