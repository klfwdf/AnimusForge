using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000061 RID: 97
	[CallbackIdentity(4516)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_FileOpenDialog_t
	{
		// Token: 0x040000E0 RID: 224
		public const int k_iCallback = 4516;

		// Token: 0x040000E1 RID: 225
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000E2 RID: 226
		public string pchTitle;

		// Token: 0x040000E3 RID: 227
		public string pchInitialFile;
	}
}
