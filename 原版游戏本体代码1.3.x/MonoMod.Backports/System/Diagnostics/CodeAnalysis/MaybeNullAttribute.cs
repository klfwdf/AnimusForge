using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200004B RID: 75
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MaybeNullAttribute : Attribute
	{
	}
}
