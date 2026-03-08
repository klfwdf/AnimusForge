using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x0200040B RID: 1035
	[Conditional("CONTRACTS_FULL")]
	[Conditional("DEBUG")]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class ContractClassAttribute : Attribute
	{
		// Token: 0x060033EF RID: 13295 RVA: 0x000C67DF File Offset: 0x000C49DF
		[__DynamicallyInvokable]
		public ContractClassAttribute(Type typeContainingContracts)
		{
			this._typeWithContracts = typeContainingContracts;
		}

		// Token: 0x170007A1 RID: 1953
		// (get) Token: 0x060033F0 RID: 13296 RVA: 0x000C67EE File Offset: 0x000C49EE
		[__DynamicallyInvokable]
		public Type TypeContainingContracts
		{
			[__DynamicallyInvokable]
			get
			{
				return this._typeWithContracts;
			}
		}

		// Token: 0x04001708 RID: 5896
		private Type _typeWithContracts;
	}
}
