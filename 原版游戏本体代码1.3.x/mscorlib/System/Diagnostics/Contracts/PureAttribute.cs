using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x0200040A RID: 1034
	[Conditional("CONTRACTS_FULL")]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.Delegate, AllowMultiple = false, Inherited = true)]
	[__DynamicallyInvokable]
	public sealed class PureAttribute : Attribute
	{
		// Token: 0x060033EE RID: 13294 RVA: 0x000C67D7 File Offset: 0x000C49D7
		[__DynamicallyInvokable]
		public PureAttribute()
		{
		}
	}
}
