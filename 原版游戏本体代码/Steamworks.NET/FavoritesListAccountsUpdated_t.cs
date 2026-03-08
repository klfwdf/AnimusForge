using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200007F RID: 127
	[CallbackIdentity(516)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct FavoritesListAccountsUpdated_t
	{
		// Token: 0x04000156 RID: 342
		public const int k_iCallback = 516;

		// Token: 0x04000157 RID: 343
		public EResult m_eResult;
	}
}
