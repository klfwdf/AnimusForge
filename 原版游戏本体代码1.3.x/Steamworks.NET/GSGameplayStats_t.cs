using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200004A RID: 74
	[CallbackIdentity(207)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GSGameplayStats_t
	{
		// Token: 0x04000073 RID: 115
		public const int k_iCallback = 207;

		// Token: 0x04000074 RID: 116
		public EResult m_eResult;

		// Token: 0x04000075 RID: 117
		public int m_nRank;

		// Token: 0x04000076 RID: 118
		public uint m_unTotalConnects;

		// Token: 0x04000077 RID: 119
		public uint m_unTotalMinutesPlayed;
	}
}
