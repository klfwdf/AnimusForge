using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x0200040D RID: 1037
	[Conditional("CONTRACTS_FULL")]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class ContractInvariantMethodAttribute : Attribute
	{
		// Token: 0x060033F3 RID: 13299 RVA: 0x000C680D File Offset: 0x000C4A0D
		[__DynamicallyInvokable]
		public ContractInvariantMethodAttribute()
		{
		}
	}
}
