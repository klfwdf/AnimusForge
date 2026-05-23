using System;
using System.Security;
using System.Threading;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000959 RID: 2393
	internal sealed class SafeHeapHandleCache : IDisposable
	{
		// Token: 0x060061EA RID: 25066 RVA: 0x0014EF0B File Offset: 0x0014D10B
		[SecuritySafeCritical]
		public SafeHeapHandleCache(ulong minSize = 64UL, ulong maxSize = 2048UL, int maxHandles = 0)
		{
			this._minSize = minSize;
			this._maxSize = maxSize;
			this._handleCache = new SafeHeapHandle[(maxHandles > 0) ? maxHandles : (Environment.ProcessorCount * 4)];
		}

		// Token: 0x060061EB RID: 25067 RVA: 0x0014EF3C File Offset: 0x0014D13C
		[SecurityCritical]
		public SafeHeapHandle Acquire(ulong minSize = 0UL)
		{
			if (minSize < this._minSize)
			{
				minSize = this._minSize;
			}
			SafeHeapHandle safeHeapHandle = null;
			for (int i = 0; i < this._handleCache.Length; i++)
			{
				safeHeapHandle = Interlocked.Exchange<SafeHeapHandle>(ref this._handleCache[i], null);
				if (safeHeapHandle != null)
				{
					break;
				}
			}
			if (safeHeapHandle != null)
			{
				if (safeHeapHandle.ByteLength < minSize)
				{
					safeHeapHandle.Resize(minSize);
				}
			}
			else
			{
				safeHeapHandle = new SafeHeapHandle(minSize);
			}
			return safeHeapHandle;
		}

		// Token: 0x060061EC RID: 25068 RVA: 0x0014EFA4 File Offset: 0x0014D1A4
		[SecurityCritical]
		public void Release(SafeHeapHandle handle)
		{
			if (handle.ByteLength <= this._maxSize)
			{
				for (int i = 0; i < this._handleCache.Length; i++)
				{
					handle = Interlocked.Exchange<SafeHeapHandle>(ref this._handleCache[i], handle);
					if (handle == null)
					{
						return;
					}
				}
			}
			handle.Dispose();
		}

		// Token: 0x060061ED RID: 25069 RVA: 0x0014EFF0 File Offset: 0x0014D1F0
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060061EE RID: 25070 RVA: 0x0014F000 File Offset: 0x0014D200
		[SecuritySafeCritical]
		private void Dispose(bool disposing)
		{
			if (this._handleCache != null)
			{
				for (int i = 0; i < this._handleCache.Length; i++)
				{
					SafeHeapHandle safeHeapHandle = this._handleCache[i];
					this._handleCache[i] = null;
					if (safeHeapHandle != null && disposing)
					{
						safeHeapHandle.Dispose();
					}
				}
			}
		}

		// Token: 0x060061EF RID: 25071 RVA: 0x0014F048 File Offset: 0x0014D248
		~SafeHeapHandleCache()
		{
			this.Dispose(false);
		}

		// Token: 0x04002B84 RID: 11140
		private readonly ulong _minSize;

		// Token: 0x04002B85 RID: 11141
		private readonly ulong _maxSize;

		// Token: 0x04002B86 RID: 11142
		[SecurityCritical]
		internal readonly SafeHeapHandle[] _handleCache;
	}
}
