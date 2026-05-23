using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200001F RID: 31
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullAttribute : Attribute
	{
	}
}
