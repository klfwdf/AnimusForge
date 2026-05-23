using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200009C RID: 156
	[CallbackIdentity(4114)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct MusicPlayerWantsPlayingRepeatStatus_t
	{
		// Token: 0x040001A3 RID: 419
		public const int k_iCallback = 4114;

		// Token: 0x040001A4 RID: 420
		public int m_nPlayingRepeatStatus;
	}
}
