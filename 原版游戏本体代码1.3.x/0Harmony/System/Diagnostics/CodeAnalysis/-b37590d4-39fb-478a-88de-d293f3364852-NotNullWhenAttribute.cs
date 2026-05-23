using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020007F2 RID: 2034
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class <b37590d4-39fb-478a-88de-d293f3364852>NotNullWhenAttribute : Attribute
	{
		// Token: 0x060026FD RID: 9981 RVA: 0x00087423 File Offset: 0x00085623
		public <b37590d4-39fb-478a-88de-d293f3364852>NotNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}

		// Token: 0x17000802 RID: 2050
		// (get) Token: 0x060026FE RID: 9982 RVA: 0x00087432 File Offset: 0x00085632
		public bool ReturnValue { get; }
	}
}
