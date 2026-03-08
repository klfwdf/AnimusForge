using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008C2 RID: 2242
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class RequiredAttributeAttribute : Attribute
	{
		// Token: 0x06005DBB RID: 23995 RVA: 0x00149930 File Offset: 0x00147B30
		public RequiredAttributeAttribute(Type requiredContract)
		{
			this.requiredContract = requiredContract;
		}

		// Token: 0x17001019 RID: 4121
		// (get) Token: 0x06005DBC RID: 23996 RVA: 0x0014993F File Offset: 0x00147B3F
		public Type RequiredContract
		{
			get
			{
				return this.requiredContract;
			}
		}

		// Token: 0x04002A2C RID: 10796
		private Type requiredContract;
	}
}
