using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200003F RID: 63
	[CallbackIdentity(346)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FriendsEnumerateFollowingList_t
	{
		// Token: 0x04000054 RID: 84
		public const int k_iCallback = 346;

		// Token: 0x04000055 RID: 85
		public EResult m_eResult;

		// Token: 0x04000056 RID: 86
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
		public CSteamID[] m_rgSteamID;

		// Token: 0x04000057 RID: 87
		public int m_nResultsReturned;

		// Token: 0x04000058 RID: 88
		public int m_nTotalResultCount;
	}
}
