using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000040 RID: 64
	[CallbackIdentity(347)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SetPersonaNameResponse_t
	{
		// Token: 0x04000059 RID: 89
		public const int k_iCallback = 347;

		// Token: 0x0400005A RID: 90
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bSuccess;

		// Token: 0x0400005B RID: 91
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bLocalSuccess;

		// Token: 0x0400005C RID: 92
		public EResult m_result;
	}
}
