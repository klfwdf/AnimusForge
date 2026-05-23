using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Serialization.Formatters
{
	// Token: 0x02000762 RID: 1890
	[SecurityCritical]
	[ComVisible(true)]
	public sealed class InternalRM
	{
		// Token: 0x06005309 RID: 21257 RVA: 0x00123C51 File Offset: 0x00121E51
		[Conditional("_LOGGING")]
		public static void InfoSoap(params object[] messages)
		{
		}

		// Token: 0x0600530A RID: 21258 RVA: 0x00123C53 File Offset: 0x00121E53
		public static bool SoapCheckEnabled()
		{
			return BCLDebug.CheckEnabled("SOAP");
		}
	}
}
