using System;
using System.Runtime.InteropServices;

namespace System.Security.Policy
{
	// Token: 0x02000355 RID: 853
	[ComVisible(true)]
	public interface IIdentityPermissionFactory
	{
		// Token: 0x06002A70 RID: 10864
		IPermission CreateIdentityPermission(Evidence evidence);
	}
}
