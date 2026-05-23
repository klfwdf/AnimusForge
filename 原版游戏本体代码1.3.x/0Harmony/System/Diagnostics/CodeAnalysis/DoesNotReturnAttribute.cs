using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000905 RID: 2309
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DoesNotReturnAttribute : Attribute
	{
	}
}
