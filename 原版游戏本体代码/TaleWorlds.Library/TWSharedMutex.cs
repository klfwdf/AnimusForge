using System;
using System.Threading;

namespace TaleWorlds.Library
{
	// Token: 0x0200009C RID: 156
	public class TWSharedMutex
	{
		// Token: 0x06000587 RID: 1415 RVA: 0x000136EC File Offset: 0x000118EC
		public void EnterReadLock()
		{
			for (;;)
			{
				if (Volatile.Read(ref this._writerFlag) != 1 && Volatile.Read(ref this._writeRequests) <= 0)
				{
					Interlocked.Increment(ref this._readerCount);
					if (Volatile.Read(ref this._writerFlag) == 0 && Volatile.Read(ref this._writeRequests) == 0)
					{
						break;
					}
					Interlocked.Decrement(ref this._readerCount);
				}
				else
				{
					Thread.SpinWait(4);
				}
			}
		}

		// Token: 0x06000588 RID: 1416 RVA: 0x00013754 File Offset: 0x00011954
		public void EnterWriteLock()
		{
			Interlocked.Increment(ref this._writeRequests);
			while (Interlocked.CompareExchange(ref this._writerFlag, 1, 0) != 0)
			{
				Thread.SpinWait(4);
			}
			while (Volatile.Read(ref this._readerCount) > 0)
			{
				Thread.SpinWait(4);
			}
			Interlocked.Decrement(ref this._writeRequests);
		}

		// Token: 0x06000589 RID: 1417 RVA: 0x000137A8 File Offset: 0x000119A8
		public void ExitReadLock()
		{
			Interlocked.Decrement(ref this._readerCount);
		}

		// Token: 0x0600058A RID: 1418 RVA: 0x000137B6 File Offset: 0x000119B6
		public void ExitWriteLock()
		{
			Volatile.Write(ref this._writerFlag, 0);
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x0600058B RID: 1419 RVA: 0x000137C4 File Offset: 0x000119C4
		public bool IsReadLockHeld
		{
			get
			{
				return Volatile.Read(ref this._readerCount) > 0;
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x0600058C RID: 1420 RVA: 0x000137D4 File Offset: 0x000119D4
		public bool IsWriteLockHeld
		{
			get
			{
				return Volatile.Read(ref this._writerFlag) > 0;
			}
		}

		// Token: 0x040001AF RID: 431
		private int _readerCount;

		// Token: 0x040001B0 RID: 432
		private int _writerFlag;

		// Token: 0x040001B1 RID: 433
		private int _writeRequests;
	}
}
