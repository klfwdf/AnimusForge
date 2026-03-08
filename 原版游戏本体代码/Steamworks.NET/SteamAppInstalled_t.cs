using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000027 RID: 39
	[CallbackIdentity(3901)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamAppInstalled_t
	{
		// Token: 0x04000003 RID: 3
		public const int k_iCallback = 3901;

		// Token: 0x04000004 RID: 4
		public AppId_t m_nAppID;

		// Token: 0x04000005 RID: 5
		public int m_iInstallFolderIndex;
	}
}
