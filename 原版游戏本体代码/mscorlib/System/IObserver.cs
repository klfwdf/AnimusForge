using System;

namespace System
{
	// Token: 0x020000F3 RID: 243
	[__DynamicallyInvokable]
	public interface IObserver<in T>
	{
		// Token: 0x06000F06 RID: 3846
		[__DynamicallyInvokable]
		void OnNext(T value);

		// Token: 0x06000F07 RID: 3847
		[__DynamicallyInvokable]
		void OnError(Exception error);

		// Token: 0x06000F08 RID: 3848
		[__DynamicallyInvokable]
		void OnCompleted();
	}
}
