using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000049 RID: 73
	[CallbackIdentity(115)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GSPolicyResponse_t
	{
		// Token: 0x04000071 RID: 113
		public const int k_iCallback = 115;

		// Token: 0x04000072 RID: 114
		public byte m_bSecure;
	}
}
