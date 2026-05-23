using System;

namespace TaleWorlds.CampaignSystem.Party
{
	// Token: 0x020002FE RID: 766
	public struct TroopTradeDifference
	{
		// Token: 0x17000AF4 RID: 2804
		// (get) Token: 0x06002CA1 RID: 11425 RVA: 0x000BBAAC File Offset: 0x000B9CAC
		// (set) Token: 0x06002CA2 RID: 11426 RVA: 0x000BBAB4 File Offset: 0x000B9CB4
		public CharacterObject Troop { get; set; }

		// Token: 0x17000AF5 RID: 2805
		// (get) Token: 0x06002CA3 RID: 11427 RVA: 0x000BBABD File Offset: 0x000B9CBD
		// (set) Token: 0x06002CA4 RID: 11428 RVA: 0x000BBAC5 File Offset: 0x000B9CC5
		public bool IsPrisoner { get; set; }

		// Token: 0x17000AF6 RID: 2806
		// (get) Token: 0x06002CA5 RID: 11429 RVA: 0x000BBACE File Offset: 0x000B9CCE
		// (set) Token: 0x06002CA6 RID: 11430 RVA: 0x000BBAD6 File Offset: 0x000B9CD6
		public int FromCount { get; set; }

		// Token: 0x17000AF7 RID: 2807
		// (get) Token: 0x06002CA7 RID: 11431 RVA: 0x000BBADF File Offset: 0x000B9CDF
		// (set) Token: 0x06002CA8 RID: 11432 RVA: 0x000BBAE7 File Offset: 0x000B9CE7
		public int ToCount { get; set; }

		// Token: 0x17000AF8 RID: 2808
		// (get) Token: 0x06002CA9 RID: 11433 RVA: 0x000BBAF0 File Offset: 0x000B9CF0
		public int DifferenceCount
		{
			get
			{
				return this.FromCount - this.ToCount;
			}
		}

		// Token: 0x17000AF9 RID: 2809
		// (get) Token: 0x06002CAA RID: 11434 RVA: 0x000BBAFF File Offset: 0x000B9CFF
		// (set) Token: 0x06002CAB RID: 11435 RVA: 0x000BBB07 File Offset: 0x000B9D07
		public bool IsEmpty { get; private set; }

		// Token: 0x17000AFA RID: 2810
		// (get) Token: 0x06002CAC RID: 11436 RVA: 0x000BBB10 File Offset: 0x000B9D10
		public static TroopTradeDifference Empty
		{
			get
			{
				return new TroopTradeDifference
				{
					IsEmpty = true
				};
			}
		}
	}
}
