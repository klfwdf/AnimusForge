using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000CC RID: 204
	[CallbackIdentity(3410)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct StartPlaytimeTrackingResult_t
	{
		// Token: 0x0400026B RID: 619
		public const int k_iCallback = 3410;

		// Token: 0x0400026C RID: 620
		public EResult m_eResult;
	}
}
