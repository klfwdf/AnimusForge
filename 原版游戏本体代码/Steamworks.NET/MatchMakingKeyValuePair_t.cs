using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200016F RID: 367
	public struct MatchMakingKeyValuePair_t
	{
		// Token: 0x06000873 RID: 2163 RVA: 0x0000C04B File Offset: 0x0000A24B
		private MatchMakingKeyValuePair_t(string strKey, string strValue)
		{
			this.m_szKey = strKey;
			this.m_szValue = strValue;
		}

		// Token: 0x040009BB RID: 2491
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string m_szKey;

		// Token: 0x040009BC RID: 2492
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string m_szValue;
	}
}
