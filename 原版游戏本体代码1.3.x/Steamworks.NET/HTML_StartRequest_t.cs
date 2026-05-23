using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000054 RID: 84
	[CallbackIdentity(4503)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_StartRequest_t
	{
		// Token: 0x040000A4 RID: 164
		public const int k_iCallback = 4503;

		// Token: 0x040000A5 RID: 165
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000A6 RID: 166
		public string pchURL;

		// Token: 0x040000A7 RID: 167
		public string pchTarget;

		// Token: 0x040000A8 RID: 168
		public string pchPostData;

		// Token: 0x040000A9 RID: 169
		[MarshalAs(UnmanagedType.I1)]
		public bool bIsRedirect;
	}
}
