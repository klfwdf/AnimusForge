using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000085 RID: 133
	[CallbackIdentity(5214)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SubmitPlayerResultResultCallback_t
	{
		// Token: 0x04000178 RID: 376
		public const int k_iCallback = 5214;

		// Token: 0x04000179 RID: 377
		public EResult m_eResult;

		// Token: 0x0400017A RID: 378
		public ulong ullUniqueGameID;

		// Token: 0x0400017B RID: 379
		public CSteamID steamIDPlayer;
	}
}
