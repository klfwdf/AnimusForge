using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000108 RID: 264
	[ComVisible(true)]
	public sealed class LocalDataStoreSlot
	{
		// Token: 0x06000FDE RID: 4062 RVA: 0x00030554 File Offset: 0x0002E754
		internal LocalDataStoreSlot(LocalDataStoreMgr mgr, int slot, long cookie)
		{
			this.m_mgr = mgr;
			this.m_slot = slot;
			this.m_cookie = cookie;
		}

		// Token: 0x170001B6 RID: 438
		// (get) Token: 0x06000FDF RID: 4063 RVA: 0x00030571 File Offset: 0x0002E771
		internal LocalDataStoreMgr Manager
		{
			get
			{
				return this.m_mgr;
			}
		}

		// Token: 0x170001B7 RID: 439
		// (get) Token: 0x06000FE0 RID: 4064 RVA: 0x00030579 File Offset: 0x0002E779
		internal int Slot
		{
			get
			{
				return this.m_slot;
			}
		}

		// Token: 0x170001B8 RID: 440
		// (get) Token: 0x06000FE1 RID: 4065 RVA: 0x00030581 File Offset: 0x0002E781
		internal long Cookie
		{
			get
			{
				return this.m_cookie;
			}
		}

		// Token: 0x06000FE2 RID: 4066 RVA: 0x0003058C File Offset: 0x0002E78C
		protected override void Finalize()
		{
			try
			{
				LocalDataStoreMgr mgr = this.m_mgr;
				if (mgr != null)
				{
					int slot = this.m_slot;
					this.m_slot = -1;
					mgr.FreeDataSlot(slot, this.m_cookie);
				}
			}
			finally
			{
				base.Finalize();
			}
		}

		// Token: 0x040005AF RID: 1455
		private LocalDataStoreMgr m_mgr;

		// Token: 0x040005B0 RID: 1456
		private int m_slot;

		// Token: 0x040005B1 RID: 1457
		private long m_cookie;
	}
}
