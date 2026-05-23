using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000938 RID: 2360
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
	[ComVisible(true)]
	public sealed class AutomationProxyAttribute : Attribute
	{
		// Token: 0x0600604B RID: 24651 RVA: 0x0014BF5C File Offset: 0x0014A15C
		public AutomationProxyAttribute(bool val)
		{
			this._val = val;
		}

		// Token: 0x170010E6 RID: 4326
		// (get) Token: 0x0600604C RID: 24652 RVA: 0x0014BF6B File Offset: 0x0014A16B
		public bool Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002B23 RID: 11043
		internal bool _val;
	}
}
