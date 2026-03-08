using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200091D RID: 2333
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	[ComVisible(true)]
	public sealed class ProgIdAttribute : Attribute
	{
		// Token: 0x06006007 RID: 24583 RVA: 0x0014B7CF File Offset: 0x001499CF
		public ProgIdAttribute(string progId)
		{
			this._val = progId;
		}

		// Token: 0x170010D8 RID: 4312
		// (get) Token: 0x06006008 RID: 24584 RVA: 0x0014B7DE File Offset: 0x001499DE
		public string Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A75 RID: 10869
		internal string _val;
	}
}
