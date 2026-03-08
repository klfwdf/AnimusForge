using System;

namespace System.Runtime.Remoting.Proxies
{
	// Token: 0x02000800 RID: 2048
	internal struct MessageData
	{
		// Token: 0x04002839 RID: 10297
		internal IntPtr pFrame;

		// Token: 0x0400283A RID: 10298
		internal IntPtr pMethodDesc;

		// Token: 0x0400283B RID: 10299
		internal IntPtr pDelegateMD;

		// Token: 0x0400283C RID: 10300
		internal IntPtr pSig;

		// Token: 0x0400283D RID: 10301
		internal IntPtr thGoverningType;

		// Token: 0x0400283E RID: 10302
		internal int iFlags;
	}
}
