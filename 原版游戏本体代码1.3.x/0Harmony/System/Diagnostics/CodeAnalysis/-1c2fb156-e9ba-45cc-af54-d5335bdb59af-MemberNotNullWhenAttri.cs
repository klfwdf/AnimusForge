using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004C3 RID: 1219
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <1c2fb156-e9ba-45cc-af54-d5335bdb59af>MemberNotNullWhenAttribute : Attribute
	{
		// Token: 0x170005E8 RID: 1512
		// (get) Token: 0x06001B3A RID: 6970 RVA: 0x0005849B File Offset: 0x0005669B
		public bool ReturnValue { get; }

		// Token: 0x170005E9 RID: 1513
		// (get) Token: 0x06001B3B RID: 6971 RVA: 0x000584A3 File Offset: 0x000566A3
		public string[] Members { get; }

		// Token: 0x06001B3C RID: 6972 RVA: 0x000584AB File Offset: 0x000566AB
		public <1c2fb156-e9ba-45cc-af54-d5335bdb59af>MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			this.ReturnValue = returnValue;
			this.Members = new string[] { member };
		}

		// Token: 0x06001B3D RID: 6973 RVA: 0x000584CA File Offset: 0x000566CA
		public <1c2fb156-e9ba-45cc-af54-d5335bdb59af>MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			this.ReturnValue = returnValue;
			this.Members = members;
		}
	}
}
