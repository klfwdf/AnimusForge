using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005C1 RID: 1473
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyDelaySignAttribute : Attribute
	{
		// Token: 0x0600446C RID: 17516 RVA: 0x000FC54D File Offset: 0x000FA74D
		[__DynamicallyInvokable]
		public AssemblyDelaySignAttribute(bool delaySign)
		{
			this.m_delaySign = delaySign;
		}

		// Token: 0x17000A1C RID: 2588
		// (get) Token: 0x0600446D RID: 17517 RVA: 0x000FC55C File Offset: 0x000FA75C
		[__DynamicallyInvokable]
		public bool DelaySign
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_delaySign;
			}
		}

		// Token: 0x04001C0B RID: 7179
		private bool m_delaySign;
	}
}
