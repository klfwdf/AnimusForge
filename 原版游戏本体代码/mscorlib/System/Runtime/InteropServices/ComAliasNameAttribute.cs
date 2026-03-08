using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000937 RID: 2359
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
	[ComVisible(true)]
	public sealed class ComAliasNameAttribute : Attribute
	{
		// Token: 0x06006049 RID: 24649 RVA: 0x0014BF45 File Offset: 0x0014A145
		public ComAliasNameAttribute(string alias)
		{
			this._val = alias;
		}

		// Token: 0x170010E5 RID: 4325
		// (get) Token: 0x0600604A RID: 24650 RVA: 0x0014BF54 File Offset: 0x0014A154
		public string Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002B22 RID: 11042
		internal string _val;
	}
}
