using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000030 RID: 48
	[CallbackIdentity(331)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GameOverlayActivated_t
	{
		// Token: 0x04000021 RID: 33
		public const int k_iCallback = 331;

		// Token: 0x04000022 RID: 34
		public byte m_bActive;
	}
}
