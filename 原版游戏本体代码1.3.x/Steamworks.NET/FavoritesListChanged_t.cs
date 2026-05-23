using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000075 RID: 117
	[CallbackIdentity(502)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct FavoritesListChanged_t
	{
		// Token: 0x04000129 RID: 297
		public const int k_iCallback = 502;

		// Token: 0x0400012A RID: 298
		public uint m_nIP;

		// Token: 0x0400012B RID: 299
		public uint m_nQueryPort;

		// Token: 0x0400012C RID: 300
		public uint m_nConnPort;

		// Token: 0x0400012D RID: 301
		public uint m_nAppID;

		// Token: 0x0400012E RID: 302
		public uint m_nFlags;

		// Token: 0x0400012F RID: 303
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAdd;

		// Token: 0x04000130 RID: 304
		public AccountID_t m_unAccountId;
	}
}
