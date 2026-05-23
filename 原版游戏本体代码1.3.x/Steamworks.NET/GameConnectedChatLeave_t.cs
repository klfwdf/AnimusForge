using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000039 RID: 57
	[CallbackIdentity(340)]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct GameConnectedChatLeave_t
	{
		// Token: 0x0400003F RID: 63
		public const int k_iCallback = 340;

		// Token: 0x04000040 RID: 64
		public CSteamID m_steamIDClanChat;

		// Token: 0x04000041 RID: 65
		public CSteamID m_steamIDUser;

		// Token: 0x04000042 RID: 66
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bKicked;

		// Token: 0x04000043 RID: 67
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bDropped;
	}
}
