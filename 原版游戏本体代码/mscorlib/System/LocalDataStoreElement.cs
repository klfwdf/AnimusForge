using System;

namespace System
{
	// Token: 0x02000106 RID: 262
	internal sealed class LocalDataStoreElement
	{
		// Token: 0x06000FD4 RID: 4052 RVA: 0x00030338 File Offset: 0x0002E538
		public LocalDataStoreElement(long cookie)
		{
			this.m_cookie = cookie;
		}

		// Token: 0x170001B4 RID: 436
		// (get) Token: 0x06000FD5 RID: 4053 RVA: 0x00030347 File Offset: 0x0002E547
		// (set) Token: 0x06000FD6 RID: 4054 RVA: 0x0003034F File Offset: 0x0002E54F
		public object Value
		{
			get
			{
				return this.m_value;
			}
			set
			{
				this.m_value = value;
			}
		}

		// Token: 0x170001B5 RID: 437
		// (get) Token: 0x06000FD7 RID: 4055 RVA: 0x00030358 File Offset: 0x0002E558
		public long Cookie
		{
			get
			{
				return this.m_cookie;
			}
		}

		// Token: 0x040005AB RID: 1451
		private object m_value;

		// Token: 0x040005AC RID: 1452
		private long m_cookie;
	}
}
