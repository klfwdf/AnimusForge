using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000081 RID: 129
	[CallbackIdentity(5202)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SearchForGameResultCallback_t
	{
		// Token: 0x0400015F RID: 351
		public const int k_iCallback = 5202;

		// Token: 0x04000160 RID: 352
		public ulong m_ullSearchID;

		// Token: 0x04000161 RID: 353
		public EResult m_eResult;

		// Token: 0x04000162 RID: 354
		public int m_nCountPlayersInGame;

		// Token: 0x04000163 RID: 355
		public int m_nCountAcceptedGame;

		// Token: 0x04000164 RID: 356
		public CSteamID m_steamIDHost;

		// Token: 0x04000165 RID: 357
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bFinalCallback;
	}
}
