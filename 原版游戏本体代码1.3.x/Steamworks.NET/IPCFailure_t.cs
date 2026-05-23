using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000DA RID: 218
	[CallbackIdentity(117)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct IPCFailure_t
	{
		// Token: 0x0400029D RID: 669
		public const int k_iCallback = 117;

		// Token: 0x0400029E RID: 670
		public byte m_eFailureType;
	}
}
