using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004C2 RID: 1218
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <1c2fb156-e9ba-45cc-af54-d5335bdb59af>MemberNotNullAttribute : Attribute
	{
		// Token: 0x170005E7 RID: 1511
		// (get) Token: 0x06001B37 RID: 6967 RVA: 0x0005846C File Offset: 0x0005666C
		public string[] Members { get; }

		// Token: 0x06001B38 RID: 6968 RVA: 0x00058474 File Offset: 0x00056674
		public <1c2fb156-e9ba-45cc-af54-d5335bdb59af>MemberNotNullAttribute(string member)
		{
			this.Members = new string[] { member };
		}

		// Token: 0x06001B39 RID: 6969 RVA: 0x0005848C File Offset: 0x0005668C
		public <1c2fb156-e9ba-45cc-af54-d5335bdb59af>MemberNotNullAttribute(params string[] members)
		{
			this.Members = members;
		}
	}
}
