using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x0200040C RID: 1036
	[Conditional("CONTRACTS_FULL")]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class ContractClassForAttribute : Attribute
	{
		// Token: 0x060033F1 RID: 13297 RVA: 0x000C67F6 File Offset: 0x000C49F6
		[__DynamicallyInvokable]
		public ContractClassForAttribute(Type typeContractsAreFor)
		{
			this._typeIAmAContractFor = typeContractsAreFor;
		}

		// Token: 0x170007A2 RID: 1954
		// (get) Token: 0x060033F2 RID: 13298 RVA: 0x000C6805 File Offset: 0x000C4A05
		[__DynamicallyInvokable]
		public Type TypeContractsAreFor
		{
			[__DynamicallyInvokable]
			get
			{
				return this._typeIAmAContractFor;
			}
		}

		// Token: 0x04001709 RID: 5897
		private Type _typeIAmAContractFor;
	}
}
