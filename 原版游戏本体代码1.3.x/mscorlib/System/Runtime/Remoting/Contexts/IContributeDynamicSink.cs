using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000813 RID: 2067
	[ComVisible(true)]
	public interface IContributeDynamicSink
	{
		// Token: 0x060058DC RID: 22748
		[SecurityCritical]
		IDynamicMessageSink GetDynamicSink();
	}
}
