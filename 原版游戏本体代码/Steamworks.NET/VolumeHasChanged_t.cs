using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200008E RID: 142
	[CallbackIdentity(4002)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct VolumeHasChanged_t
	{
		// Token: 0x0400018F RID: 399
		public const int k_iCallback = 4002;

		// Token: 0x04000190 RID: 400
		public float m_flNewVolume;
	}
}
