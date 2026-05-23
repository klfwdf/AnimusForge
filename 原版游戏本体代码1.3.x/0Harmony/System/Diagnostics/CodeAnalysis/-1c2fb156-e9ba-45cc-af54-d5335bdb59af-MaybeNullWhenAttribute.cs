using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004C1 RID: 1217
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <1c2fb156-e9ba-45cc-af54-d5335bdb59af>MaybeNullWhenAttribute : Attribute
	{
		// Token: 0x170005E6 RID: 1510
		// (get) Token: 0x06001B35 RID: 6965 RVA: 0x00058455 File Offset: 0x00056655
		public bool ReturnValue { get; }

		// Token: 0x06001B36 RID: 6966 RVA: 0x0005845D File Offset: 0x0005665D
		public <1c2fb156-e9ba-45cc-af54-d5335bdb59af>MaybeNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
