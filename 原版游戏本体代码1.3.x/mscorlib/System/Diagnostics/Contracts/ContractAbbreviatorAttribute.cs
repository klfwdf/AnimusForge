using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x02000413 RID: 1043
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	[Conditional("CONTRACTS_FULL")]
	[__DynamicallyInvokable]
	public sealed class ContractAbbreviatorAttribute : Attribute
	{
		// Token: 0x060033FB RID: 13307 RVA: 0x000C685B File Offset: 0x000C4A5B
		[__DynamicallyInvokable]
		public ContractAbbreviatorAttribute()
		{
		}
	}
}
