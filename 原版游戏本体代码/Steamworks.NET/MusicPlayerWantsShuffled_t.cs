using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000097 RID: 151
	[CallbackIdentity(4109)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct MusicPlayerWantsShuffled_t
	{
		// Token: 0x04000199 RID: 409
		public const int k_iCallback = 4109;

		// Token: 0x0400019A RID: 410
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bShuffled;
	}
}
