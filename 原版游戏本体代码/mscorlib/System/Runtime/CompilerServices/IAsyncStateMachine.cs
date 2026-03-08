using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008F3 RID: 2291
	[__DynamicallyInvokable]
	public interface IAsyncStateMachine
	{
		// Token: 0x06005E38 RID: 24120
		[__DynamicallyInvokable]
		void MoveNext();

		// Token: 0x06005E39 RID: 24121
		[__DynamicallyInvokable]
		void SetStateMachine(IAsyncStateMachine stateMachine);
	}
}
