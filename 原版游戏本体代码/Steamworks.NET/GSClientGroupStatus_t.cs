using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200004B RID: 75
	[CallbackIdentity(208)]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct GSClientGroupStatus_t
	{
		// Token: 0x04000078 RID: 120
		public const int k_iCallback = 208;

		// Token: 0x04000079 RID: 121
		public CSteamID m_SteamIDUser;

		// Token: 0x0400007A RID: 122
		public CSteamID m_SteamIDGroup;

		// Token: 0x0400007B RID: 123
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bMember;

		// Token: 0x0400007C RID: 124
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bOfficer;
	}
}
