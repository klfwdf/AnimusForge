using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200016C RID: 364
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamParamStringArray_t
	{
		// Token: 0x0400099A RID: 2458
		public IntPtr m_ppStrings;

		// Token: 0x0400099B RID: 2459
		public int m_nNumStrings;
	}
}
