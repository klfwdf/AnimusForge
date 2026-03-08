using System;

namespace TaleWorlds.AchievementSystem
{
	// Token: 0x02000002 RID: 2
	public class Achievement
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		// (set) Token: 0x06000002 RID: 2 RVA: 0x00002050 File Offset: 0x00000250
		public string Id { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002059 File Offset: 0x00000259
		// (set) Token: 0x06000004 RID: 4 RVA: 0x00002061 File Offset: 0x00000261
		public string LockedDisplayName { get; set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000005 RID: 5 RVA: 0x0000206A File Offset: 0x0000026A
		// (set) Token: 0x06000006 RID: 6 RVA: 0x00002072 File Offset: 0x00000272
		public string UnlockedDisplayName { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000007 RID: 7 RVA: 0x0000207B File Offset: 0x0000027B
		// (set) Token: 0x06000008 RID: 8 RVA: 0x00002083 File Offset: 0x00000283
		public string LockedDescription { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000009 RID: 9 RVA: 0x0000208C File Offset: 0x0000028C
		// (set) Token: 0x0600000A RID: 10 RVA: 0x00002094 File Offset: 0x00000294
		public string UnlockedDescription { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600000B RID: 11 RVA: 0x0000209D File Offset: 0x0000029D
		// (set) Token: 0x0600000C RID: 12 RVA: 0x000020A5 File Offset: 0x000002A5
		public int TargetProgress { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600000D RID: 13 RVA: 0x000020AE File Offset: 0x000002AE
		// (set) Token: 0x0600000E RID: 14 RVA: 0x000020B6 File Offset: 0x000002B6
		public bool IsUnlocked { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600000F RID: 15 RVA: 0x000020BF File Offset: 0x000002BF
		// (set) Token: 0x06000010 RID: 16 RVA: 0x000020C7 File Offset: 0x000002C7
		public int CurrentProgress { get; set; }
	}
}
