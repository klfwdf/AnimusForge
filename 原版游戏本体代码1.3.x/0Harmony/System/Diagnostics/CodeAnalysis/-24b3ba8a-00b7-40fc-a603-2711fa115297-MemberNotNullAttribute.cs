using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000610 RID: 1552
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <24b3ba8a-00b7-40fc-a603-2711fa115297>MemberNotNullAttribute : Attribute
	{
		// Token: 0x1700075A RID: 1882
		// (get) Token: 0x060020E1 RID: 8417 RVA: 0x0006842C File Offset: 0x0006662C
		public string[] Members { get; }

		// Token: 0x060020E2 RID: 8418 RVA: 0x00068434 File Offset: 0x00066634
		public <24b3ba8a-00b7-40fc-a603-2711fa115297>MemberNotNullAttribute(string member)
		{
			this.Members = new string[] { member };
		}

		// Token: 0x060020E3 RID: 8419 RVA: 0x0006844C File Offset: 0x0006664C
		public <24b3ba8a-00b7-40fc-a603-2711fa115297>MemberNotNullAttribute(params string[] members)
		{
			this.Members = members;
		}
	}
}
