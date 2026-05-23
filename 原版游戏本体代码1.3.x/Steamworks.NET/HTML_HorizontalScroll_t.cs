using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200005C RID: 92
	[CallbackIdentity(4511)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_HorizontalScroll_t
	{
		// Token: 0x040000C5 RID: 197
		public const int k_iCallback = 4511;

		// Token: 0x040000C6 RID: 198
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000C7 RID: 199
		public uint unScrollMax;

		// Token: 0x040000C8 RID: 200
		public uint unScrollCurrent;

		// Token: 0x040000C9 RID: 201
		public float flPageScale;

		// Token: 0x040000CA RID: 202
		[MarshalAs(UnmanagedType.I1)]
		public bool bVisible;

		// Token: 0x040000CB RID: 203
		public uint unPageSize;
	}
}
