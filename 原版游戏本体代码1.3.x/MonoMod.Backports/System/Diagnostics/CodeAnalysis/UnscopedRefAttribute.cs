using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000055 RID: 85
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class UnscopedRefAttribute : Attribute
	{
	}
}
