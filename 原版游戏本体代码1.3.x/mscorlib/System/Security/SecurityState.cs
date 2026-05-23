using System;
using System.Security.Permissions;

namespace System.Security
{
	// Token: 0x020001F0 RID: 496
	[SecurityCritical]
	[PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
	public abstract class SecurityState
	{
		// Token: 0x06001E04 RID: 7684 RVA: 0x00068CE0 File Offset: 0x00066EE0
		[SecurityCritical]
		public bool IsStateAvailable()
		{
			AppDomainManager currentAppDomainManager = AppDomainManager.CurrentAppDomainManager;
			return currentAppDomainManager != null && currentAppDomainManager.CheckSecuritySettings(this);
		}

		// Token: 0x06001E05 RID: 7685
		public abstract void EnsureState();
	}
}
