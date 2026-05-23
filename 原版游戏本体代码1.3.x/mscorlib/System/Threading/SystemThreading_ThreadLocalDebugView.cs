using System;
using System.Collections.Generic;

namespace System.Threading
{
	// Token: 0x02000541 RID: 1345
	internal sealed class SystemThreading_ThreadLocalDebugView<T>
	{
		// Token: 0x06003F00 RID: 16128 RVA: 0x000EA7BB File Offset: 0x000E89BB
		public SystemThreading_ThreadLocalDebugView(ThreadLocal<T> tlocal)
		{
			this.m_tlocal = tlocal;
		}

		// Token: 0x17000950 RID: 2384
		// (get) Token: 0x06003F01 RID: 16129 RVA: 0x000EA7CA File Offset: 0x000E89CA
		public bool IsValueCreated
		{
			get
			{
				return this.m_tlocal.IsValueCreated;
			}
		}

		// Token: 0x17000951 RID: 2385
		// (get) Token: 0x06003F02 RID: 16130 RVA: 0x000EA7D7 File Offset: 0x000E89D7
		public T Value
		{
			get
			{
				return this.m_tlocal.ValueForDebugDisplay;
			}
		}

		// Token: 0x17000952 RID: 2386
		// (get) Token: 0x06003F03 RID: 16131 RVA: 0x000EA7E4 File Offset: 0x000E89E4
		public List<T> Values
		{
			get
			{
				return this.m_tlocal.ValuesForDebugDisplay;
			}
		}

		// Token: 0x04001A80 RID: 6784
		private readonly ThreadLocal<T> m_tlocal;
	}
}
