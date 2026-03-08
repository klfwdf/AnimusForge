using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200092D RID: 2349
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class GuidAttribute : Attribute
	{
		// Token: 0x0600602A RID: 24618 RVA: 0x0014BB46 File Offset: 0x00149D46
		[__DynamicallyInvokable]
		public GuidAttribute(string guid)
		{
			this._val = guid;
		}

		// Token: 0x170010E0 RID: 4320
		// (get) Token: 0x0600602B RID: 24619 RVA: 0x0014BB55 File Offset: 0x00149D55
		[__DynamicallyInvokable]
		public string Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002B09 RID: 11017
		internal string _val;
	}
}
