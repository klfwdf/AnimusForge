using System;

namespace System
{
	// Token: 0x02000099 RID: 153
	internal struct AppDomainHandle
	{
		// Token: 0x0600088C RID: 2188 RVA: 0x0001D482 File Offset: 0x0001B682
		internal AppDomainHandle(IntPtr domainHandle)
		{
			this.m_appDomainHandle = domainHandle;
		}

		// Token: 0x0400039C RID: 924
		private IntPtr m_appDomainHandle;
	}
}
