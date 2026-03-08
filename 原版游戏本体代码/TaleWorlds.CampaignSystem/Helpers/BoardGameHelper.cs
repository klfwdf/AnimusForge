using System;

namespace Helpers
{
	// Token: 0x02000018 RID: 24
	public static class BoardGameHelper
	{
		// Token: 0x020004E1 RID: 1249
		public enum AIDifficulty
		{
			// Token: 0x040014B3 RID: 5299
			Easy,
			// Token: 0x040014B4 RID: 5300
			Normal,
			// Token: 0x040014B5 RID: 5301
			Hard,
			// Token: 0x040014B6 RID: 5302
			NumTypes
		}

		// Token: 0x020004E2 RID: 1250
		public enum BoardGameState
		{
			// Token: 0x040014B8 RID: 5304
			None,
			// Token: 0x040014B9 RID: 5305
			Win,
			// Token: 0x040014BA RID: 5306
			Loss,
			// Token: 0x040014BB RID: 5307
			Draw
		}
	}
}
