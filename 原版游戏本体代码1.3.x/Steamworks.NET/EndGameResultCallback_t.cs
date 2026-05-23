using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000086 RID: 134
	[CallbackIdentity(5215)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct EndGameResultCallback_t
	{
		// Token: 0x0400017C RID: 380
		public const int k_iCallback = 5215;

		// Token: 0x0400017D RID: 381
		public EResult m_eResult;

		// Token: 0x0400017E RID: 382
		public ulong ullUniqueGameID;
	}
}
