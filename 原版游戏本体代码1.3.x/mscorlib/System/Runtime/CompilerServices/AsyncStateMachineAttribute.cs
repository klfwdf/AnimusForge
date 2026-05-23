using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008ED RID: 2285
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class AsyncStateMachineAttribute : StateMachineAttribute
	{
		// Token: 0x06005E0A RID: 24074 RVA: 0x0014A696 File Offset: 0x00148896
		[__DynamicallyInvokable]
		public AsyncStateMachineAttribute(Type stateMachineType)
			: base(stateMachineType)
		{
		}
	}
}
