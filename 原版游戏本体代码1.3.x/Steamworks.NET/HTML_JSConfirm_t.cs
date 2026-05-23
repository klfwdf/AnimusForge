using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000060 RID: 96
	[CallbackIdentity(4515)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_JSConfirm_t
	{
		// Token: 0x040000DD RID: 221
		public const int k_iCallback = 4515;

		// Token: 0x040000DE RID: 222
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000DF RID: 223
		public string pchMessage;
	}
}
