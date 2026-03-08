using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004C6 RID: 1222
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <1c2fb156-e9ba-45cc-af54-d5335bdb59af>NotNullWhenAttribute : Attribute
	{
		// Token: 0x170005EB RID: 1515
		// (get) Token: 0x06001B41 RID: 6977 RVA: 0x000584F7 File Offset: 0x000566F7
		public bool ReturnValue { get; }

		// Token: 0x06001B42 RID: 6978 RVA: 0x000584FF File Offset: 0x000566FF
		public <1c2fb156-e9ba-45cc-af54-d5335bdb59af>NotNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
