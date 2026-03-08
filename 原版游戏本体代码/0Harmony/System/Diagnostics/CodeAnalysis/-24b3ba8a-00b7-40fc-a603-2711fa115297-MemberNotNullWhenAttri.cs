using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000611 RID: 1553
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <24b3ba8a-00b7-40fc-a603-2711fa115297>MemberNotNullWhenAttribute : Attribute
	{
		// Token: 0x1700075B RID: 1883
		// (get) Token: 0x060020E4 RID: 8420 RVA: 0x0006845B File Offset: 0x0006665B
		public bool ReturnValue { get; }

		// Token: 0x1700075C RID: 1884
		// (get) Token: 0x060020E5 RID: 8421 RVA: 0x00068463 File Offset: 0x00066663
		public string[] Members { get; }

		// Token: 0x060020E6 RID: 8422 RVA: 0x0006846B File Offset: 0x0006666B
		public <24b3ba8a-00b7-40fc-a603-2711fa115297>MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			this.ReturnValue = returnValue;
			this.Members = new string[] { member };
		}

		// Token: 0x060020E7 RID: 8423 RVA: 0x0006848A File Offset: 0x0006668A
		public <24b3ba8a-00b7-40fc-a603-2711fa115297>MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			this.ReturnValue = returnValue;
			this.Members = members;
		}
	}
}
