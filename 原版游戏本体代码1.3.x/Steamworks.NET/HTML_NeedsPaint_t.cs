using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000053 RID: 83
	[CallbackIdentity(4502)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_NeedsPaint_t
	{
		// Token: 0x04000097 RID: 151
		public const int k_iCallback = 4502;

		// Token: 0x04000098 RID: 152
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x04000099 RID: 153
		public IntPtr pBGRA;

		// Token: 0x0400009A RID: 154
		public uint unWide;

		// Token: 0x0400009B RID: 155
		public uint unTall;

		// Token: 0x0400009C RID: 156
		public uint unUpdateX;

		// Token: 0x0400009D RID: 157
		public uint unUpdateY;

		// Token: 0x0400009E RID: 158
		public uint unUpdateWide;

		// Token: 0x0400009F RID: 159
		public uint unUpdateTall;

		// Token: 0x040000A0 RID: 160
		public uint unScrollX;

		// Token: 0x040000A1 RID: 161
		public uint unScrollY;

		// Token: 0x040000A2 RID: 162
		public float flPageScale;

		// Token: 0x040000A3 RID: 163
		public uint unPageSerial;
	}
}
