using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000BF RID: 191
	[CallbackIdentity(1332)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageFileReadAsyncComplete_t
	{
		// Token: 0x0400023A RID: 570
		public const int k_iCallback = 1332;

		// Token: 0x0400023B RID: 571
		public SteamAPICall_t m_hFileReadAsync;

		// Token: 0x0400023C RID: 572
		public EResult m_eResult;

		// Token: 0x0400023D RID: 573
		public uint m_nOffset;

		// Token: 0x0400023E RID: 574
		public uint m_cubRead;
	}
}
