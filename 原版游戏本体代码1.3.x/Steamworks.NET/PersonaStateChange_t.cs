using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200002F RID: 47
	[CallbackIdentity(304)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct PersonaStateChange_t
	{
		// Token: 0x0400001E RID: 30
		public const int k_iCallback = 304;

		// Token: 0x0400001F RID: 31
		public ulong m_ulSteamID;

		// Token: 0x04000020 RID: 32
		public EPersonaChange m_nChangeFlags;
	}
}
