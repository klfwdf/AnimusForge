using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	// Token: 0x020003E9 RID: 1001
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DebuggerHiddenAttribute : Attribute
	{
		// Token: 0x0600330A RID: 13066 RVA: 0x000C4CA0 File Offset: 0x000C2EA0
		[__DynamicallyInvokable]
		public DebuggerHiddenAttribute()
		{
		}
	}
}
