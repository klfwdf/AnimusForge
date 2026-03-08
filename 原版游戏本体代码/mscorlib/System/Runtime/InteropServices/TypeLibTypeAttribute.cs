using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000926 RID: 2342
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = false)]
	[ComVisible(true)]
	public sealed class TypeLibTypeAttribute : Attribute
	{
		// Token: 0x06006015 RID: 24597 RVA: 0x0014B92D File Offset: 0x00149B2D
		public TypeLibTypeAttribute(TypeLibTypeFlags flags)
		{
			this._val = flags;
		}

		// Token: 0x06006016 RID: 24598 RVA: 0x0014B93C File Offset: 0x00149B3C
		public TypeLibTypeAttribute(short flags)
		{
			this._val = (TypeLibTypeFlags)flags;
		}

		// Token: 0x170010DC RID: 4316
		// (get) Token: 0x06006017 RID: 24599 RVA: 0x0014B94B File Offset: 0x00149B4B
		public TypeLibTypeFlags Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002AA8 RID: 10920
		internal TypeLibTypeFlags _val;
	}
}
