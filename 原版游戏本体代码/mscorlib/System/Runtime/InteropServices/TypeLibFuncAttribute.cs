using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000927 RID: 2343
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[ComVisible(true)]
	public sealed class TypeLibFuncAttribute : Attribute
	{
		// Token: 0x06006018 RID: 24600 RVA: 0x0014B953 File Offset: 0x00149B53
		public TypeLibFuncAttribute(TypeLibFuncFlags flags)
		{
			this._val = flags;
		}

		// Token: 0x06006019 RID: 24601 RVA: 0x0014B962 File Offset: 0x00149B62
		public TypeLibFuncAttribute(short flags)
		{
			this._val = (TypeLibFuncFlags)flags;
		}

		// Token: 0x170010DD RID: 4317
		// (get) Token: 0x0600601A RID: 24602 RVA: 0x0014B971 File Offset: 0x00149B71
		public TypeLibFuncFlags Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002AA9 RID: 10921
		internal TypeLibFuncFlags _val;
	}
}
