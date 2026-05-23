using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000614 RID: 1556
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <24b3ba8a-00b7-40fc-a603-2711fa115297>NotNullWhenAttribute : Attribute
	{
		// Token: 0x1700075E RID: 1886
		// (get) Token: 0x060020EB RID: 8427 RVA: 0x000684B7 File Offset: 0x000666B7
		public bool ReturnValue { get; }

		// Token: 0x060020EC RID: 8428 RVA: 0x000684BF File Offset: 0x000666BF
		public <24b3ba8a-00b7-40fc-a603-2711fa115297>NotNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
