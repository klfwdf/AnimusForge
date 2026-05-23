using System;

namespace HarmonyLib
{
	/// <summary>A patch priority</summary>
	// Token: 0x0200009E RID: 158
	public static class Priority
	{
		/// <summary>Patch last</summary>
		// Token: 0x04000219 RID: 537
		public const int Last = 0;

		/// <summary>Patch with very low priority</summary>
		// Token: 0x0400021A RID: 538
		public const int VeryLow = 100;

		/// <summary>Patch with low priority</summary>
		// Token: 0x0400021B RID: 539
		public const int Low = 200;

		/// <summary>Patch with lower than normal priority</summary>
		// Token: 0x0400021C RID: 540
		public const int LowerThanNormal = 300;

		/// <summary>Patch with normal priority</summary>
		// Token: 0x0400021D RID: 541
		public const int Normal = 400;

		/// <summary>Patch with higher than normal priority</summary>
		// Token: 0x0400021E RID: 542
		public const int HigherThanNormal = 500;

		/// <summary>Patch with high priority</summary>
		// Token: 0x0400021F RID: 543
		public const int High = 600;

		/// <summary>Patch with very high priority</summary>
		// Token: 0x04000220 RID: 544
		public const int VeryHigh = 700;

		/// <summary>Patch first</summary>
		// Token: 0x04000221 RID: 545
		public const int First = 800;
	}
}
