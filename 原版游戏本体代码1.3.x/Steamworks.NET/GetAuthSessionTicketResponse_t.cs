using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000DF RID: 223
	[CallbackIdentity(163)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GetAuthSessionTicketResponse_t
	{
		// Token: 0x040002AA RID: 682
		public const int k_iCallback = 163;

		// Token: 0x040002AB RID: 683
		public HAuthTicket m_hAuthTicket;

		// Token: 0x040002AC RID: 684
		public EResult m_eResult;
	}
}
