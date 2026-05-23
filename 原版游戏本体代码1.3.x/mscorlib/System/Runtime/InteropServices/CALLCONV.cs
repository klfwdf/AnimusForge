using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009A1 RID: 2465
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.CALLCONV instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Serializable]
	public enum CALLCONV
	{
		// Token: 0x04002C71 RID: 11377
		CC_CDECL = 1,
		// Token: 0x04002C72 RID: 11378
		CC_MSCPASCAL,
		// Token: 0x04002C73 RID: 11379
		CC_PASCAL = 2,
		// Token: 0x04002C74 RID: 11380
		CC_MACPASCAL,
		// Token: 0x04002C75 RID: 11381
		CC_STDCALL,
		// Token: 0x04002C76 RID: 11382
		CC_RESERVED,
		// Token: 0x04002C77 RID: 11383
		CC_SYSCALL,
		// Token: 0x04002C78 RID: 11384
		CC_MPWCDECL,
		// Token: 0x04002C79 RID: 11385
		CC_MPWPASCAL,
		// Token: 0x04002C7A RID: 11386
		CC_MAX
	}
}
