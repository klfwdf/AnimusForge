using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008EB RID: 2283
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class IteratorStateMachineAttribute : StateMachineAttribute
	{
		// Token: 0x06005E07 RID: 24071 RVA: 0x0014A68D File Offset: 0x0014888D
		[__DynamicallyInvokable]
		public IteratorStateMachineAttribute(Type stateMachineType)
			: base(stateMachineType)
		{
		}
	}
}
