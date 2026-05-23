using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200060F RID: 1551
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <24b3ba8a-00b7-40fc-a603-2711fa115297>MaybeNullWhenAttribute : Attribute
	{
		// Token: 0x17000759 RID: 1881
		// (get) Token: 0x060020DF RID: 8415 RVA: 0x00068415 File Offset: 0x00066615
		public bool ReturnValue { get; }

		// Token: 0x060020E0 RID: 8416 RVA: 0x0006841D File Offset: 0x0006661D
		public <24b3ba8a-00b7-40fc-a603-2711fa115297>MaybeNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
