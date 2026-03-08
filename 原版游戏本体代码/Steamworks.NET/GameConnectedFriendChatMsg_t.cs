using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200003C RID: 60
	[CallbackIdentity(343)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GameConnectedFriendChatMsg_t
	{
		// Token: 0x04000049 RID: 73
		public const int k_iCallback = 343;

		// Token: 0x0400004A RID: 74
		public CSteamID m_steamIDUser;

		// Token: 0x0400004B RID: 75
		public int m_iMessageID;
	}
}
