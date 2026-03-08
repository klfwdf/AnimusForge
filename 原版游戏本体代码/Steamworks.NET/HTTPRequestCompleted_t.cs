using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000069 RID: 105
	[CallbackIdentity(2101)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTTPRequestCompleted_t
	{
		// Token: 0x040000FD RID: 253
		public const int k_iCallback = 2101;

		// Token: 0x040000FE RID: 254
		public HTTPRequestHandle m_hRequest;

		// Token: 0x040000FF RID: 255
		public ulong m_ulContextValue;

		// Token: 0x04000100 RID: 256
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bRequestSuccessful;

		// Token: 0x04000101 RID: 257
		public EHTTPStatusCode m_eStatusCode;

		// Token: 0x04000102 RID: 258
		public uint m_unBodySize;
	}
}
