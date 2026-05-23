using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000DD RID: 221
	[CallbackIdentity(152)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct MicroTxnAuthorizationResponse_t
	{
		// Token: 0x040002A4 RID: 676
		public const int k_iCallback = 152;

		// Token: 0x040002A5 RID: 677
		public uint m_unAppID;

		// Token: 0x040002A6 RID: 678
		public ulong m_ulOrderID;

		// Token: 0x040002A7 RID: 679
		public byte m_bAuthorized;
	}
}
