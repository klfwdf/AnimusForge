using System;
using System.Security;

namespace System.Runtime.Remoting.Proxies
{
	// Token: 0x02000804 RID: 2052
	internal sealed class __TransparentProxy
	{
		// Token: 0x0600586E RID: 22638 RVA: 0x00137F65 File Offset: 0x00136165
		private __TransparentProxy()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Constructor"));
		}

		// Token: 0x04002852 RID: 10322
		[SecurityCritical]
		private RealProxy _rp;

		// Token: 0x04002853 RID: 10323
		private object _stubData;

		// Token: 0x04002854 RID: 10324
		private IntPtr _pMT;

		// Token: 0x04002855 RID: 10325
		private IntPtr _pInterfaceMT;

		// Token: 0x04002856 RID: 10326
		private IntPtr _stub;
	}
}
