using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200002A RID: 42
	[CallbackIdentity(1008)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RegisterActivationCodeResponse_t
	{
		// Token: 0x0400000B RID: 11
		public const int k_iCallback = 1008;

		// Token: 0x0400000C RID: 12
		public ERegisterActivationCodeResult m_eResult;

		// Token: 0x0400000D RID: 13
		public uint m_unPackageRegistered;
	}
}
