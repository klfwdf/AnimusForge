using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x02000500 RID: 1280
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public sealed class ManualResetEvent : EventWaitHandle
	{
		// Token: 0x06003C60 RID: 15456 RVA: 0x000E438A File Offset: 0x000E258A
		[__DynamicallyInvokable]
		public ManualResetEvent(bool initialState)
			: base(initialState, EventResetMode.ManualReset)
		{
		}
	}
}
