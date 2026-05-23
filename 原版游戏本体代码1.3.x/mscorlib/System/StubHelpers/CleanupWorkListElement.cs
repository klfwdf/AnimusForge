using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x020005AA RID: 1450
	[SecurityCritical]
	internal sealed class CleanupWorkListElement
	{
		// Token: 0x0600432E RID: 17198 RVA: 0x000FA3B5 File Offset: 0x000F85B5
		public CleanupWorkListElement(SafeHandle handle)
		{
			this.m_handle = handle;
		}

		// Token: 0x04001BEC RID: 7148
		public SafeHandle m_handle;

		// Token: 0x04001BED RID: 7149
		public bool m_owned;
	}
}
