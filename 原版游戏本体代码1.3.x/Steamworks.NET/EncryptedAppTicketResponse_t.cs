using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000DE RID: 222
	[CallbackIdentity(154)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct EncryptedAppTicketResponse_t
	{
		// Token: 0x040002A8 RID: 680
		public const int k_iCallback = 154;

		// Token: 0x040002A9 RID: 681
		public EResult m_eResult;
	}
}
