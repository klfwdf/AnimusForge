using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000C1 RID: 193
	[CallbackIdentity(2301)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct ScreenshotReady_t
	{
		// Token: 0x04000240 RID: 576
		public const int k_iCallback = 2301;

		// Token: 0x04000241 RID: 577
		public ScreenshotHandle m_hLocal;

		// Token: 0x04000242 RID: 578
		public EResult m_eResult;
	}
}
