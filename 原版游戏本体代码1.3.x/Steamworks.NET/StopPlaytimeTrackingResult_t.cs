using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000CD RID: 205
	[CallbackIdentity(3411)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct StopPlaytimeTrackingResult_t
	{
		// Token: 0x0400026D RID: 621
		public const int k_iCallback = 3411;

		// Token: 0x0400026E RID: 622
		public EResult m_eResult;
	}
}
