using System;

namespace MonoMod.Utils
{
	// Token: 0x020008D6 RID: 2262
	internal enum OSKind
	{
		// Token: 0x04003B5D RID: 15197
		Unknown,
		// Token: 0x04003B5E RID: 15198
		Posix,
		// Token: 0x04003B5F RID: 15199
		Linux = 9,
		// Token: 0x04003B60 RID: 15200
		Android = 41,
		// Token: 0x04003B61 RID: 15201
		OSX = 5,
		// Token: 0x04003B62 RID: 15202
		IOS = 37,
		// Token: 0x04003B63 RID: 15203
		BSD = 17,
		// Token: 0x04003B64 RID: 15204
		Windows = 2,
		// Token: 0x04003B65 RID: 15205
		Wine = 34
	}
}
