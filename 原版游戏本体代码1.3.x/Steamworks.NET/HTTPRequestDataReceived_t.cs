using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200006B RID: 107
	[CallbackIdentity(2103)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct HTTPRequestDataReceived_t
	{
		// Token: 0x04000106 RID: 262
		public const int k_iCallback = 2103;

		// Token: 0x04000107 RID: 263
		public HTTPRequestHandle m_hRequest;

		// Token: 0x04000108 RID: 264
		public ulong m_ulContextValue;

		// Token: 0x04000109 RID: 265
		public uint m_cOffset;

		// Token: 0x0400010A RID: 266
		public uint m_cBytesReceived;
	}
}
