using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008F4 RID: 2292
	[__DynamicallyInvokable]
	public interface INotifyCompletion
	{
		// Token: 0x06005E3A RID: 24122
		[__DynamicallyInvokable]
		void OnCompleted(Action continuation);
	}
}
