using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
	// Token: 0x020000EC RID: 236
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IAsyncResult
	{
		// Token: 0x170001A5 RID: 421
		// (get) Token: 0x06000EF9 RID: 3833
		[__DynamicallyInvokable]
		bool IsCompleted
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170001A6 RID: 422
		// (get) Token: 0x06000EFA RID: 3834
		[__DynamicallyInvokable]
		WaitHandle AsyncWaitHandle
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170001A7 RID: 423
		// (get) Token: 0x06000EFB RID: 3835
		[__DynamicallyInvokable]
		object AsyncState
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170001A8 RID: 424
		// (get) Token: 0x06000EFC RID: 3836
		[__DynamicallyInvokable]
		bool CompletedSynchronously
		{
			[__DynamicallyInvokable]
			get;
		}
	}
}
