using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000509 RID: 1289
	public sealed class ThreadPoolBoundHandle : IDisposable
	{
		// Token: 0x06003CB9 RID: 15545 RVA: 0x000E4FAD File Offset: 0x000E31AD
		[SecurityCritical]
		private ThreadPoolBoundHandle(SafeHandle handle)
		{
			this._handle = handle;
		}

		// Token: 0x17000922 RID: 2338
		// (get) Token: 0x06003CBA RID: 15546 RVA: 0x000E4FBC File Offset: 0x000E31BC
		public SafeHandle Handle
		{
			[SecurityCritical]
			get
			{
				return this._handle;
			}
		}

		// Token: 0x06003CBB RID: 15547 RVA: 0x000E4FC4 File Offset: 0x000E31C4
		[SecurityCritical]
		public static ThreadPoolBoundHandle BindHandle(SafeHandle handle)
		{
			if (handle == null)
			{
				throw new ArgumentNullException("handle");
			}
			if (handle.IsClosed || handle.IsInvalid)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"), "handle");
			}
			try
			{
				bool flag = ThreadPool.BindHandle(handle);
			}
			catch (Exception ex)
			{
				if (ex.HResult == -2147024890)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"), "handle");
				}
				if (ex.HResult == -2147024809)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_AlreadyBoundOrSyncHandle"), "handle");
				}
				throw;
			}
			return new ThreadPoolBoundHandle(handle);
		}

		// Token: 0x06003CBC RID: 15548 RVA: 0x000E506C File Offset: 0x000E326C
		[CLSCompliant(false)]
		[SecurityCritical]
		public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData)
		{
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}
			this.EnsureNotDisposed();
			return new ThreadPoolBoundHandleOverlapped(callback, state, pinData, null)
			{
				_boundHandle = this
			}._nativeOverlapped;
		}

		// Token: 0x06003CBD RID: 15549 RVA: 0x000E50A4 File Offset: 0x000E32A4
		[CLSCompliant(false)]
		[SecurityCritical]
		public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated)
		{
			if (preAllocated == null)
			{
				throw new ArgumentNullException("preAllocated");
			}
			this.EnsureNotDisposed();
			preAllocated.AddRef();
			NativeOverlapped* nativeOverlapped;
			try
			{
				ThreadPoolBoundHandleOverlapped overlapped = preAllocated._overlapped;
				if (overlapped._boundHandle != null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_PreAllocatedAlreadyAllocated"), "preAllocated");
				}
				overlapped._boundHandle = this;
				nativeOverlapped = overlapped._nativeOverlapped;
			}
			catch
			{
				preAllocated.Release();
				throw;
			}
			return nativeOverlapped;
		}

		// Token: 0x06003CBE RID: 15550 RVA: 0x000E511C File Offset: 0x000E331C
		[CLSCompliant(false)]
		[SecurityCritical]
		public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
		{
			if (overlapped == null)
			{
				throw new ArgumentNullException("overlapped");
			}
			ThreadPoolBoundHandleOverlapped overlappedWrapper = ThreadPoolBoundHandle.GetOverlappedWrapper(overlapped, this);
			if (overlappedWrapper._boundHandle != this)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NativeOverlappedWrongBoundHandle"), "overlapped");
			}
			if (overlappedWrapper._preAllocated != null)
			{
				overlappedWrapper._preAllocated.Release();
				return;
			}
			Overlapped.Free(overlapped);
		}

		// Token: 0x06003CBF RID: 15551 RVA: 0x000E517C File Offset: 0x000E337C
		[CLSCompliant(false)]
		[SecurityCritical]
		public unsafe static object GetNativeOverlappedState(NativeOverlapped* overlapped)
		{
			if (overlapped == null)
			{
				throw new ArgumentNullException("overlapped");
			}
			ThreadPoolBoundHandleOverlapped overlappedWrapper = ThreadPoolBoundHandle.GetOverlappedWrapper(overlapped, null);
			return overlappedWrapper._userState;
		}

		// Token: 0x06003CC0 RID: 15552 RVA: 0x000E51A8 File Offset: 0x000E33A8
		[SecurityCritical]
		private unsafe static ThreadPoolBoundHandleOverlapped GetOverlappedWrapper(NativeOverlapped* overlapped, ThreadPoolBoundHandle expectedBoundHandle)
		{
			ThreadPoolBoundHandleOverlapped result;
			try
			{
				result = (ThreadPoolBoundHandleOverlapped)Overlapped.Unpack(overlapped);
			}
			catch (NullReferenceException innerException)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NativeOverlappedAlreadyFree"), "overlapped", innerException);
			}
			return result;
		}

		// Token: 0x06003CC1 RID: 15553 RVA: 0x000E51EC File Offset: 0x000E33EC
		public void Dispose()
		{
			this._isDisposed = true;
		}

		// Token: 0x06003CC2 RID: 15554 RVA: 0x000E51F5 File Offset: 0x000E33F5
		private void EnsureNotDisposed()
		{
			if (this._isDisposed)
			{
				throw new ObjectDisposedException(base.GetType().ToString());
			}
		}

		// Token: 0x040019C6 RID: 6598
		private const int E_HANDLE = -2147024890;

		// Token: 0x040019C7 RID: 6599
		private const int E_INVALIDARG = -2147024809;

		// Token: 0x040019C8 RID: 6600
		[SecurityCritical]
		private readonly SafeHandle _handle;

		// Token: 0x040019C9 RID: 6601
		private bool _isDisposed;
	}
}
