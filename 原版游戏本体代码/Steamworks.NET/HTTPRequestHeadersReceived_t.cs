using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200006A RID: 106
	[CallbackIdentity(2102)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTTPRequestHeadersReceived_t
	{
		// Token: 0x04000103 RID: 259
		public const int k_iCallback = 2102;

		// Token: 0x04000104 RID: 260
		public HTTPRequestHandle m_hRequest;

		// Token: 0x04000105 RID: 261
		public ulong m_ulContextValue;
	}
}
