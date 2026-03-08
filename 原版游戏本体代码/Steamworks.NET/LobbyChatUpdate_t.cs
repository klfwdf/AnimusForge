using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000079 RID: 121
	[CallbackIdentity(506)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyChatUpdate_t
	{
		// Token: 0x0400013E RID: 318
		public const int k_iCallback = 506;

		// Token: 0x0400013F RID: 319
		public ulong m_ulSteamIDLobby;

		// Token: 0x04000140 RID: 320
		public ulong m_ulSteamIDUserChanged;

		// Token: 0x04000141 RID: 321
		public ulong m_ulSteamIDMakingChange;

		// Token: 0x04000142 RID: 322
		public uint m_rgfChatMemberStateChange;
	}
}
