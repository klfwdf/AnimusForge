using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000075 RID: 117
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		// Token: 0x17000117 RID: 279
		// (get) Token: 0x0600049E RID: 1182 RVA: 0x00011470 File Offset: 0x0000F670
		public bool ReturnValue { get; }

		// Token: 0x17000118 RID: 280
		// (get) Token: 0x0600049F RID: 1183 RVA: 0x00011478 File Offset: 0x0000F678
		public string[] Members { get; }

		// Token: 0x060004A0 RID: 1184 RVA: 0x00011480 File Offset: 0x0000F680
		public MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			this.ReturnValue = returnValue;
			this.Members = new string[] { member };
		}

		// Token: 0x060004A1 RID: 1185 RVA: 0x0001149F File Offset: 0x0000F69F
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			this.ReturnValue = returnValue;
			this.Members = members;
		}
	}
}
