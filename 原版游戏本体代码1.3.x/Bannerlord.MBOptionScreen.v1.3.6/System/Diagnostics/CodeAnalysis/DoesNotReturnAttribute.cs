using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000070 RID: 112
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DoesNotReturnAttribute : Attribute
	{
	}
}
