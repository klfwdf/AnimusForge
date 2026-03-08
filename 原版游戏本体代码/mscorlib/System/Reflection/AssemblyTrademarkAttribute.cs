using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005B5 RID: 1461
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyTrademarkAttribute : Attribute
	{
		// Token: 0x06004454 RID: 17492 RVA: 0x000FC42B File Offset: 0x000FA62B
		[__DynamicallyInvokable]
		public AssemblyTrademarkAttribute(string trademark)
		{
			this.m_trademark = trademark;
		}

		// Token: 0x17000A10 RID: 2576
		// (get) Token: 0x06004455 RID: 17493 RVA: 0x000FC43A File Offset: 0x000FA63A
		[__DynamicallyInvokable]
		public string Trademark
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_trademark;
			}
		}

		// Token: 0x04001BFF RID: 7167
		private string m_trademark;
	}
}
