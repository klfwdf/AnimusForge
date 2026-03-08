using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000029 RID: 41
	[CallbackIdentity(1005)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct DlcInstalled_t
	{
		// Token: 0x04000009 RID: 9
		public const int k_iCallback = 1005;

		// Token: 0x0400000A RID: 10
		public AppId_t m_nAppID;
	}
}
