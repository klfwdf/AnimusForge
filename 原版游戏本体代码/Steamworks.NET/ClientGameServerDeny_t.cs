using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000D9 RID: 217
	[CallbackIdentity(113)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct ClientGameServerDeny_t
	{
		// Token: 0x04000297 RID: 663
		public const int k_iCallback = 113;

		// Token: 0x04000298 RID: 664
		public uint m_uAppID;

		// Token: 0x04000299 RID: 665
		public uint m_unGameServerIP;

		// Token: 0x0400029A RID: 666
		public ushort m_usGameServerPort;

		// Token: 0x0400029B RID: 667
		public ushort m_bSecure;

		// Token: 0x0400029C RID: 668
		public uint m_uReason;
	}
}
