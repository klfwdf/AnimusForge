using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000035 RID: 53
	[CallbackIdentity(336)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FriendRichPresenceUpdate_t
	{
		// Token: 0x04000032 RID: 50
		public const int k_iCallback = 336;

		// Token: 0x04000033 RID: 51
		public CSteamID m_steamIDFriend;

		// Token: 0x04000034 RID: 52
		public AppId_t m_nAppID;
	}
}
