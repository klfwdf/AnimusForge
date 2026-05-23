using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200003E RID: 62
	[CallbackIdentity(345)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FriendsIsFollowing_t
	{
		// Token: 0x04000050 RID: 80
		public const int k_iCallback = 345;

		// Token: 0x04000051 RID: 81
		public EResult m_eResult;

		// Token: 0x04000052 RID: 82
		public CSteamID m_steamID;

		// Token: 0x04000053 RID: 83
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bIsFollowing;
	}
}
