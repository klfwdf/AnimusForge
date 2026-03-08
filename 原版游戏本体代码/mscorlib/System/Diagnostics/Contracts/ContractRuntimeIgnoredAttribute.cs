using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x0200040F RID: 1039
	[Conditional("CONTRACTS_FULL")]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	[__DynamicallyInvokable]
	public sealed class ContractRuntimeIgnoredAttribute : Attribute
	{
		// Token: 0x060033F5 RID: 13301 RVA: 0x000C681D File Offset: 0x000C4A1D
		[__DynamicallyInvokable]
		public ContractRuntimeIgnoredAttribute()
		{
		}
	}
}
