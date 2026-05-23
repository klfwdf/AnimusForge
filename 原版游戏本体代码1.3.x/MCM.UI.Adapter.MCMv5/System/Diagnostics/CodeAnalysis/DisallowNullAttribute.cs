using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000018 RID: 24
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DisallowNullAttribute : Attribute
	{
	}
}
