using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005B6 RID: 1462
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyProductAttribute : Attribute
	{
		// Token: 0x06004456 RID: 17494 RVA: 0x000FC442 File Offset: 0x000FA642
		[__DynamicallyInvokable]
		public AssemblyProductAttribute(string product)
		{
			this.m_product = product;
		}

		// Token: 0x17000A11 RID: 2577
		// (get) Token: 0x06004457 RID: 17495 RVA: 0x000FC451 File Offset: 0x000FA651
		[__DynamicallyInvokable]
		public string Product
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_product;
			}
		}

		// Token: 0x04001C00 RID: 7168
		private string m_product;
	}
}
