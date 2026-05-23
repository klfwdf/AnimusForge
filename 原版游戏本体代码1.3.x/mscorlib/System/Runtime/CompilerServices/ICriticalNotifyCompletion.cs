using System;
using System.Security;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008F5 RID: 2293
	[__DynamicallyInvokable]
	public interface ICriticalNotifyCompletion : INotifyCompletion
	{
		// Token: 0x06005E3B RID: 24123
		[SecurityCritical]
		[__DynamicallyInvokable]
		void UnsafeOnCompleted(Action continuation);
	}
}
