using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200009D RID: 157
	public struct TWSharedMutexReadLock : IDisposable
	{
		// Token: 0x0600058D RID: 1421 RVA: 0x000137E4 File Offset: 0x000119E4
		public TWSharedMutexReadLock(TWSharedMutex mtx)
		{
			mtx.EnterReadLock();
			this._mtx = mtx;
		}

		// Token: 0x0600058E RID: 1422 RVA: 0x000137F3 File Offset: 0x000119F3
		public void Dispose()
		{
			this._mtx.ExitReadLock();
		}

		// Token: 0x040001B2 RID: 434
		private readonly TWSharedMutex _mtx;
	}
}
