using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000065 RID: 101
	[CallbackIdentity(4524)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_ShowToolTip_t
	{
		// Token: 0x040000F2 RID: 242
		public const int k_iCallback = 4524;

		// Token: 0x040000F3 RID: 243
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000F4 RID: 244
		public string pchMsg;
	}
}
