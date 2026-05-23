using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200091A RID: 2330
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[ComVisible(true)]
	public sealed class LCIDConversionAttribute : Attribute
	{
		// Token: 0x06006003 RID: 24579 RVA: 0x0014B7A8 File Offset: 0x001499A8
		public LCIDConversionAttribute(int lcid)
		{
			this._val = lcid;
		}

		// Token: 0x170010D7 RID: 4311
		// (get) Token: 0x06006004 RID: 24580 RVA: 0x0014B7B7 File Offset: 0x001499B7
		public int Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A74 RID: 10868
		internal int _val;
	}
}
