using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000ED RID: 237
	[CallbackIdentity(1110)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GlobalAchievementPercentagesReady_t
	{
		// Token: 0x040002E5 RID: 741
		public const int k_iCallback = 1110;

		// Token: 0x040002E6 RID: 742
		public ulong m_nGameID;

		// Token: 0x040002E7 RID: 743
		public EResult m_eResult;
	}
}
