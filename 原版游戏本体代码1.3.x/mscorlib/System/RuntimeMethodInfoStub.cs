using System;
using System.Security;

namespace System
{
	// Token: 0x02000132 RID: 306
	internal class RuntimeMethodInfoStub : IRuntimeMethodInfo
	{
		// Token: 0x06001235 RID: 4661 RVA: 0x000373C4 File Offset: 0x000355C4
		public RuntimeMethodInfoStub(RuntimeMethodHandleInternal methodHandleValue, object keepalive)
		{
			this.m_keepalive = keepalive;
			this.m_value = methodHandleValue;
		}

		// Token: 0x06001236 RID: 4662 RVA: 0x000373DA File Offset: 0x000355DA
		[SecurityCritical]
		public RuntimeMethodInfoStub(IntPtr methodHandleValue, object keepalive)
		{
			this.m_keepalive = keepalive;
			this.m_value = new RuntimeMethodHandleInternal(methodHandleValue);
		}

		// Token: 0x170001F1 RID: 497
		// (get) Token: 0x06001237 RID: 4663 RVA: 0x000373F5 File Offset: 0x000355F5
		RuntimeMethodHandleInternal IRuntimeMethodInfo.Value
		{
			get
			{
				return this.m_value;
			}
		}

		// Token: 0x04000661 RID: 1633
		private object m_keepalive;

		// Token: 0x04000662 RID: 1634
		private object m_a;

		// Token: 0x04000663 RID: 1635
		private object m_b;

		// Token: 0x04000664 RID: 1636
		private object m_c;

		// Token: 0x04000665 RID: 1637
		private object m_d;

		// Token: 0x04000666 RID: 1638
		private object m_e;

		// Token: 0x04000667 RID: 1639
		private object m_f;

		// Token: 0x04000668 RID: 1640
		private object m_g;

		// Token: 0x04000669 RID: 1641
		private object m_h;

		// Token: 0x0400066A RID: 1642
		public RuntimeMethodHandleInternal m_value;
	}
}
