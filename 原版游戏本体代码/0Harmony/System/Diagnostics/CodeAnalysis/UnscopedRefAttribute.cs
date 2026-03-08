using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004CA RID: 1226
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	internal sealed class UnscopedRefAttribute : Attribute
	{
	}
}
