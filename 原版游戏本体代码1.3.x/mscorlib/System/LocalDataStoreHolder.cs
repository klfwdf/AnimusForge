using System;

namespace System
{
	// Token: 0x02000105 RID: 261
	internal sealed class LocalDataStoreHolder
	{
		// Token: 0x06000FD1 RID: 4049 RVA: 0x000302E6 File Offset: 0x0002E4E6
		public LocalDataStoreHolder(LocalDataStore store)
		{
			this.m_Store = store;
		}

		// Token: 0x06000FD2 RID: 4050 RVA: 0x000302F8 File Offset: 0x0002E4F8
		protected override void Finalize()
		{
			try
			{
				LocalDataStore store = this.m_Store;
				if (store != null)
				{
					store.Dispose();
				}
			}
			finally
			{
				base.Finalize();
			}
		}

		// Token: 0x170001B3 RID: 435
		// (get) Token: 0x06000FD3 RID: 4051 RVA: 0x00030330 File Offset: 0x0002E530
		public LocalDataStore Store
		{
			get
			{
				return this.m_Store;
			}
		}

		// Token: 0x040005AA RID: 1450
		private LocalDataStore m_Store;
	}
}
