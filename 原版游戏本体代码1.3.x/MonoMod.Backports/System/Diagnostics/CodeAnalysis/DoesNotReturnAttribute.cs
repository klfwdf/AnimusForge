using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000049 RID: 73
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DoesNotReturnAttribute : Attribute
	{
	}
}
