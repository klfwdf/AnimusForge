using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200009B RID: 155
	[CallbackIdentity(4013)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct MusicPlayerSelectsPlaylistEntry_t
	{
		// Token: 0x040001A1 RID: 417
		public const int k_iCallback = 4013;

		// Token: 0x040001A2 RID: 418
		public int nID;
	}
}
