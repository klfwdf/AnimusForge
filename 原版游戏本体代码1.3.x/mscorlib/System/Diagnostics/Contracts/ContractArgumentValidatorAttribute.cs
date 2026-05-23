using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x02000412 RID: 1042
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	[Conditional("CONTRACTS_FULL")]
	[__DynamicallyInvokable]
	public sealed class ContractArgumentValidatorAttribute : Attribute
	{
		// Token: 0x060033FA RID: 13306 RVA: 0x000C6853 File Offset: 0x000C4A53
		[__DynamicallyInvokable]
		public ContractArgumentValidatorAttribute()
		{
		}
	}
}
