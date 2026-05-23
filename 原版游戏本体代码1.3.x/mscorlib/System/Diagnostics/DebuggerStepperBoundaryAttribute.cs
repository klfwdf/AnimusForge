using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	// Token: 0x020003E8 RID: 1000
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class DebuggerStepperBoundaryAttribute : Attribute
	{
	}
}
