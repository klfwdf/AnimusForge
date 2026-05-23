using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200004D RID: 77
	[CallbackIdentity(210)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct AssociateWithClanResult_t
	{
		// Token: 0x04000085 RID: 133
		public const int k_iCallback = 210;

		// Token: 0x04000086 RID: 134
		public EResult m_eResult;
	}
}
