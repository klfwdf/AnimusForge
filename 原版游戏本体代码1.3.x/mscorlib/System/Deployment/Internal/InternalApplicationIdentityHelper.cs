using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal
{
	// Token: 0x0200066C RID: 1644
	[ComVisible(false)]
	public static class InternalApplicationIdentityHelper
	{
		// Token: 0x06004F15 RID: 20245 RVA: 0x0011C3BC File Offset: 0x0011A5BC
		[SecurityCritical]
		public static object GetInternalAppId(ApplicationIdentity id)
		{
			return id.Identity;
		}
	}
}
