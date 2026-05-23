using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200050B RID: 1291
	internal struct DeferredDisposableLifetime<T> where T : class, IDeferredDisposable
	{
		// Token: 0x06003CC4 RID: 15556 RVA: 0x000E5210 File Offset: 0x000E3410
		public bool AddRef(T obj)
		{
			for (;;)
			{
				int num = Volatile.Read(ref this._count);
				if (num < 0)
				{
					break;
				}
				int value = checked(num + 1);
				if (Interlocked.CompareExchange(ref this._count, value, num) == num)
				{
					return true;
				}
			}
			throw new ObjectDisposedException(typeof(T).ToString());
		}

		// Token: 0x06003CC5 RID: 15557 RVA: 0x000E5258 File Offset: 0x000E3458
		[SecurityCritical]
		public void Release(T obj)
		{
			int num2;
			int num3;
			for (;;)
			{
				int num = Volatile.Read(ref this._count);
				if (num > 0)
				{
					num2 = num - 1;
					if (Interlocked.CompareExchange(ref this._count, num2, num) == num)
					{
						break;
					}
				}
				else
				{
					num3 = num + 1;
					if (Interlocked.CompareExchange(ref this._count, num3, num) == num)
					{
						goto Block_3;
					}
				}
			}
			if (num2 == 0)
			{
				obj.OnFinalRelease(false);
			}
			return;
			Block_3:
			if (num3 == -1)
			{
				obj.OnFinalRelease(true);
			}
		}

		// Token: 0x06003CC6 RID: 15558 RVA: 0x000E52C0 File Offset: 0x000E34C0
		[SecuritySafeCritical]
		public void Dispose(T obj)
		{
			int num2;
			for (;;)
			{
				int num = Volatile.Read(ref this._count);
				if (num < 0)
				{
					break;
				}
				num2 = -1 - num;
				if (Interlocked.CompareExchange(ref this._count, num2, num) == num)
				{
					goto Block_1;
				}
			}
			return;
			Block_1:
			if (num2 == -1)
			{
				obj.OnFinalRelease(true);
			}
		}

		// Token: 0x040019CA RID: 6602
		private int _count;
	}
}
