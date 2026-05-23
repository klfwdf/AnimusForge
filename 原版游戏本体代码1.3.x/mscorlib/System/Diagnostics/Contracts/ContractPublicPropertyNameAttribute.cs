using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x02000411 RID: 1041
	[Conditional("CONTRACTS_FULL")]
	[AttributeUsage(AttributeTargets.Field)]
	[__DynamicallyInvokable]
	public sealed class ContractPublicPropertyNameAttribute : Attribute
	{
		// Token: 0x060033F8 RID: 13304 RVA: 0x000C683C File Offset: 0x000C4A3C
		[__DynamicallyInvokable]
		public ContractPublicPropertyNameAttribute(string name)
		{
			this._publicName = name;
		}

		// Token: 0x170007A4 RID: 1956
		// (get) Token: 0x060033F9 RID: 13305 RVA: 0x000C684B File Offset: 0x000C4A4B
		[__DynamicallyInvokable]
		public string Name
		{
			[__DynamicallyInvokable]
			get
			{
				return this._publicName;
			}
		}

		// Token: 0x0400170B RID: 5899
		private string _publicName;
	}
}
