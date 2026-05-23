using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000903 RID: 2307
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class AllowNullAttribute : Attribute
	{
	}
}
