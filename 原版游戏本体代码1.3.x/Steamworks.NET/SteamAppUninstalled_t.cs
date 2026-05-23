using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000028 RID: 40
	[CallbackIdentity(3902)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamAppUninstalled_t
	{
		// Token: 0x04000006 RID: 6
		public const int k_iCallback = 3902;

		// Token: 0x04000007 RID: 7
		public AppId_t m_nAppID;

		// Token: 0x04000008 RID: 8
		public int m_iInstallFolderIndex;
	}
}
