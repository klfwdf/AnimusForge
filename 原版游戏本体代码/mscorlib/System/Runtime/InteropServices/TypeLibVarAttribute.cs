using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000928 RID: 2344
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	[ComVisible(true)]
	public sealed class TypeLibVarAttribute : Attribute
	{
		// Token: 0x0600601B RID: 24603 RVA: 0x0014B979 File Offset: 0x00149B79
		public TypeLibVarAttribute(TypeLibVarFlags flags)
		{
			this._val = flags;
		}

		// Token: 0x0600601C RID: 24604 RVA: 0x0014B988 File Offset: 0x00149B88
		public TypeLibVarAttribute(short flags)
		{
			this._val = (TypeLibVarFlags)flags;
		}

		// Token: 0x170010DE RID: 4318
		// (get) Token: 0x0600601D RID: 24605 RVA: 0x0014B997 File Offset: 0x00149B97
		public TypeLibVarFlags Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002AAA RID: 10922
		internal TypeLibVarFlags _val;
	}
}
