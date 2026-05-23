using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200009E RID: 158
	public struct TWSharedMutexWriteLock : IDisposable
	{
		// Token: 0x0600058F RID: 1423 RVA: 0x00013800 File Offset: 0x00011A00
		public TWSharedMutexWriteLock(TWSharedMutex mtx)
		{
			mtx.EnterWriteLock();
			this._mtx = mtx;
		}

		// Token: 0x06000590 RID: 1424 RVA: 0x0001380F File Offset: 0x00011A0F
		public void Dispose()
		{
			this._mtx.ExitWriteLock();
		}

		// Token: 0x040001B3 RID: 435
		private readonly TWSharedMutex _mtx;
	}
}
