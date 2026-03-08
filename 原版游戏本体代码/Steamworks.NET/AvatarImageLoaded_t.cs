using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000033 RID: 51
	[CallbackIdentity(334)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct AvatarImageLoaded_t
	{
		// Token: 0x04000029 RID: 41
		public const int k_iCallback = 334;

		// Token: 0x0400002A RID: 42
		public CSteamID m_steamID;

		// Token: 0x0400002B RID: 43
		public int m_iImage;

		// Token: 0x0400002C RID: 44
		public int m_iWide;

		// Token: 0x0400002D RID: 45
		public int m_iTall;
	}
}
