using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000037 RID: 55
	[CallbackIdentity(338)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GameConnectedClanChatMsg_t
	{
		// Token: 0x04000038 RID: 56
		public const int k_iCallback = 338;

		// Token: 0x04000039 RID: 57
		public CSteamID m_steamIDClanChat;

		// Token: 0x0400003A RID: 58
		public CSteamID m_steamIDUser;

		// Token: 0x0400003B RID: 59
		public int m_iMessageID;
	}
}
