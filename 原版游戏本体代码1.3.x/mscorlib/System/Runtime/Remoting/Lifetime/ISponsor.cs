using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x0200081F RID: 2079
	[ComVisible(true)]
	public interface ISponsor
	{
		// Token: 0x06005929 RID: 22825
		[SecurityCritical]
		TimeSpan Renewal(ILease lease);
	}
}
