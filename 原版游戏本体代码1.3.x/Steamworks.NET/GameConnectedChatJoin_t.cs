using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000038 RID: 56
	[CallbackIdentity(339)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GameConnectedChatJoin_t
	{
		// Token: 0x0400003C RID: 60
		public const int k_iCallback = 339;

		// Token: 0x0400003D RID: 61
		public CSteamID m_steamIDClanChat;

		// Token: 0x0400003E RID: 62
		public CSteamID m_steamIDUser;
	}
}
