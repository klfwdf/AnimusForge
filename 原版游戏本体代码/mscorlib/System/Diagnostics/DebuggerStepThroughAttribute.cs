using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	// Token: 0x020003E7 RID: 999
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DebuggerStepThroughAttribute : Attribute
	{
		// Token: 0x06003308 RID: 13064 RVA: 0x000C4C90 File Offset: 0x000C2E90
		[__DynamicallyInvokable]
		public DebuggerStepThroughAttribute()
		{
		}
	}
}
