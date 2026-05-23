using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000098 RID: 152
	[CallbackIdentity(4110)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct MusicPlayerWantsLooped_t
	{
		// Token: 0x0400019B RID: 411
		public const int k_iCallback = 4110;

		// Token: 0x0400019C RID: 412
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bLooped;
	}
}
