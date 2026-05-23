using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000043 RID: 67
	[CallbackIdentity(1701)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GCMessageAvailable_t
	{
		// Token: 0x04000060 RID: 96
		public const int k_iCallback = 1701;

		// Token: 0x04000061 RID: 97
		public uint m_nMessageSize;
	}
}
