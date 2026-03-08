using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000F2 RID: 242
	[CallbackIdentity(703)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamAPICallCompleted_t
	{
		// Token: 0x040002F1 RID: 753
		public const int k_iCallback = 703;

		// Token: 0x040002F2 RID: 754
		public SteamAPICall_t m_hAsyncCall;

		// Token: 0x040002F3 RID: 755
		public int m_iCallback;

		// Token: 0x040002F4 RID: 756
		public uint m_cubParam;
	}
}
