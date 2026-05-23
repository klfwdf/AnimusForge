using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000019 RID: 25
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DoesNotReturnAttribute : Attribute
	{
	}
}
