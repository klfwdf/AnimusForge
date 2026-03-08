using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x020004EA RID: 1258
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public sealed class AutoResetEvent : EventWaitHandle
	{
		// Token: 0x06003B8B RID: 15243 RVA: 0x000E25F6 File Offset: 0x000E07F6
		[__DynamicallyInvokable]
		public AutoResetEvent(bool initialState)
			: base(initialState, EventResetMode.AutoReset)
		{
		}
	}
}
