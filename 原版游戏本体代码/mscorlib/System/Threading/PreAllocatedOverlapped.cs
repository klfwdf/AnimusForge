using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000507 RID: 1287
	public sealed class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
	{
		// Token: 0x06003CB0 RID: 15536 RVA: 0x000E4E0B File Offset: 0x000E300B
		[CLSCompliant(false)]
		[SecuritySafeCritical]
		public PreAllocatedOverlapped(IOCompletionCallback callback, object state, object pinData)
		{
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}
			this._overlapped = new ThreadPoolBoundHandleOverlapped(callback, state, pinData, this);
		}

		// Token: 0x06003CB1 RID: 15537 RVA: 0x000E4E30 File Offset: 0x000E3030
		internal bool AddRef()
		{
			return this._lifetime.AddRef(this);
		}

		// Token: 0x06003CB2 RID: 15538 RVA: 0x000E4E3E File Offset: 0x000E303E
		[SecurityCritical]
		internal void Release()
		{
			this._lifetime.Release(this);
		}

		// Token: 0x06003CB3 RID: 15539 RVA: 0x000E4E4C File Offset: 0x000E304C
		public void Dispose()
		{
			this._lifetime.Dispose(this);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06003CB4 RID: 15540 RVA: 0x000E4E60 File Offset: 0x000E3060
		~PreAllocatedOverlapped()
		{
			if (!Environment.HasShutdownStarted)
			{
				this.Dispose();
			}
		}

		// Token: 0x06003CB5 RID: 15541 RVA: 0x000E4E94 File Offset: 0x000E3094
		[SecurityCritical]
		unsafe void IDeferredDisposable.OnFinalRelease(bool disposed)
		{
			if (this._overlapped != null)
			{
				if (disposed)
				{
					Overlapped.Free(this._overlapped._nativeOverlapped);
					return;
				}
				this._overlapped._boundHandle = null;
				this._overlapped._completed = false;
				*this._overlapped._nativeOverlapped = default(NativeOverlapped);
			}
		}

		// Token: 0x040019BD RID: 6589
		[SecurityCritical]
		internal readonly ThreadPoolBoundHandleOverlapped _overlapped;

		// Token: 0x040019BE RID: 6590
		private DeferredDisposableLifetime<PreAllocatedOverlapped> _lifetime;
	}
}
