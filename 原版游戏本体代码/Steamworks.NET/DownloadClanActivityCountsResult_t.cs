using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200003A RID: 58
	[CallbackIdentity(341)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct DownloadClanActivityCountsResult_t
	{
		// Token: 0x04000044 RID: 68
		public const int k_iCallback = 341;

		// Token: 0x04000045 RID: 69
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bSuccess;
	}
}
