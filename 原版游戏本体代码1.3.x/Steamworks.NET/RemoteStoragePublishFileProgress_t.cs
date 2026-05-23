using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000BC RID: 188
	[CallbackIdentity(1329)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStoragePublishFileProgress_t
	{
		// Token: 0x04000231 RID: 561
		public const int k_iCallback = 1329;

		// Token: 0x04000232 RID: 562
		public double m_dPercentFile;

		// Token: 0x04000233 RID: 563
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bPreview;
	}
}
