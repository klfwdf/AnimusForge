using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000063 RID: 99
	[CallbackIdentity(4522)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTML_SetCursor_t
	{
		// Token: 0x040000EC RID: 236
		public const int k_iCallback = 4522;

		// Token: 0x040000ED RID: 237
		public HHTMLBrowser unBrowserHandle;

		// Token: 0x040000EE RID: 238
		public uint eMouseCursor;
	}
}
