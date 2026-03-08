using System;

namespace System.Diagnostics.Contracts
{
	// Token: 0x02000414 RID: 1044
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
	[Conditional("CONTRACTS_FULL")]
	[__DynamicallyInvokable]
	public sealed class ContractOptionAttribute : Attribute
	{
		// Token: 0x060033FC RID: 13308 RVA: 0x000C6863 File Offset: 0x000C4A63
		[__DynamicallyInvokable]
		public ContractOptionAttribute(string category, string setting, bool enabled)
		{
			this._category = category;
			this._setting = setting;
			this._enabled = enabled;
		}

		// Token: 0x060033FD RID: 13309 RVA: 0x000C6880 File Offset: 0x000C4A80
		[__DynamicallyInvokable]
		public ContractOptionAttribute(string category, string setting, string value)
		{
			this._category = category;
			this._setting = setting;
			this._value = value;
		}

		// Token: 0x170007A5 RID: 1957
		// (get) Token: 0x060033FE RID: 13310 RVA: 0x000C689D File Offset: 0x000C4A9D
		[__DynamicallyInvokable]
		public string Category
		{
			[__DynamicallyInvokable]
			get
			{
				return this._category;
			}
		}

		// Token: 0x170007A6 RID: 1958
		// (get) Token: 0x060033FF RID: 13311 RVA: 0x000C68A5 File Offset: 0x000C4AA5
		[__DynamicallyInvokable]
		public string Setting
		{
			[__DynamicallyInvokable]
			get
			{
				return this._setting;
			}
		}

		// Token: 0x170007A7 RID: 1959
		// (get) Token: 0x06003400 RID: 13312 RVA: 0x000C68AD File Offset: 0x000C4AAD
		[__DynamicallyInvokable]
		public bool Enabled
		{
			[__DynamicallyInvokable]
			get
			{
				return this._enabled;
			}
		}

		// Token: 0x170007A8 RID: 1960
		// (get) Token: 0x06003401 RID: 13313 RVA: 0x000C68B5 File Offset: 0x000C4AB5
		[__DynamicallyInvokable]
		public string Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._value;
			}
		}

		// Token: 0x0400170C RID: 5900
		private string _category;

		// Token: 0x0400170D RID: 5901
		private string _setting;

		// Token: 0x0400170E RID: 5902
		private bool _enabled;

		// Token: 0x0400170F RID: 5903
		private string _value;
	}
}
