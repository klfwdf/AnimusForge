using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004C5 RID: 1221
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <1c2fb156-e9ba-45cc-af54-d5335bdb59af>NotNullIfNotNullAttribute : Attribute
	{
		// Token: 0x170005EA RID: 1514
		// (get) Token: 0x06001B3F RID: 6975 RVA: 0x000584E0 File Offset: 0x000566E0
		public string ParameterName { get; }

		// Token: 0x06001B40 RID: 6976 RVA: 0x000584E8 File Offset: 0x000566E8
		public <1c2fb156-e9ba-45cc-af54-d5335bdb59af>NotNullIfNotNullAttribute(string parameterName)
		{
			this.ParameterName = parameterName;
		}
	}
}
