using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000077 RID: 119
	[CallbackIdentity(504)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyEnter_t
	{
		// Token: 0x04000135 RID: 309
		public const int k_iCallback = 504;

		// Token: 0x04000136 RID: 310
		public ulong m_ulSteamIDLobby;

		// Token: 0x04000137 RID: 311
		public uint m_rgfChatPermissions;

		// Token: 0x04000138 RID: 312
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bLocked;

		// Token: 0x04000139 RID: 313
		public uint m_EChatRoomEnterResponse;
	}
}
