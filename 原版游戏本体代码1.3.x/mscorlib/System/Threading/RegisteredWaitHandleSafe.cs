using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace System.Threading
{
	// Token: 0x0200051D RID: 1309
	internal sealed class RegisteredWaitHandleSafe : CriticalFinalizerObject
	{
		// Token: 0x17000938 RID: 2360
		// (get) Token: 0x06003DCD RID: 15821 RVA: 0x000E7360 File Offset: 0x000E5560
		private static IntPtr InvalidHandle
		{
			[SecuritySafeCritical]
			get
			{
				return Win32Native.INVALID_HANDLE_VALUE;
			}
		}

		// Token: 0x06003DCE RID: 15822 RVA: 0x000E7367 File Offset: 0x000E5567
		internal RegisteredWaitHandleSafe()
		{
			this.registeredWaitHandle = RegisteredWaitHandleSafe.InvalidHandle;
		}

		// Token: 0x06003DCF RID: 15823 RVA: 0x000E737A File Offset: 0x000E557A
		internal IntPtr GetHandle()
		{
			return this.registeredWaitHandle;
		}

		// Token: 0x06003DD0 RID: 15824 RVA: 0x000E7382 File Offset: 0x000E5582
		internal void SetHandle(IntPtr handle)
		{
			this.registeredWaitHandle = handle;
		}

		// Token: 0x06003DD1 RID: 15825 RVA: 0x000E738C File Offset: 0x000E558C
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal void SetWaitObject(WaitHandle waitObject)
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
			}
			finally
			{
				this.m_internalWaitObject = waitObject;
				if (waitObject != null)
				{
					this.m_internalWaitObject.SafeWaitHandle.DangerousAddRef(ref this.bReleaseNeeded);
				}
			}
		}

		// Token: 0x06003DD2 RID: 15826 RVA: 0x000E73D4 File Offset: 0x000E55D4
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal bool Unregister(WaitHandle waitObject)
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
			}
			finally
			{
				bool flag2 = false;
				do
				{
					if (Interlocked.CompareExchange(ref this.m_lock, 1, 0) == 0)
					{
						flag2 = true;
						try
						{
							if (this.ValidHandle())
							{
								flag = RegisteredWaitHandleSafe.UnregisterWaitNative(this.GetHandle(), (waitObject == null) ? null : waitObject.SafeWaitHandle);
								if (flag)
								{
									if (this.bReleaseNeeded)
									{
										this.m_internalWaitObject.SafeWaitHandle.DangerousRelease();
										this.bReleaseNeeded = false;
									}
									this.SetHandle(RegisteredWaitHandleSafe.InvalidHandle);
									this.m_internalWaitObject = null;
									GC.SuppressFinalize(this);
								}
							}
						}
						finally
						{
							this.m_lock = 0;
						}
					}
					Thread.SpinWait(1);
				}
				while (!flag2);
			}
			return flag;
		}

		// Token: 0x06003DD3 RID: 15827 RVA: 0x000E7490 File Offset: 0x000E5690
		private bool ValidHandle()
		{
			return this.registeredWaitHandle != RegisteredWaitHandleSafe.InvalidHandle && this.registeredWaitHandle != IntPtr.Zero;
		}

		// Token: 0x06003DD4 RID: 15828 RVA: 0x000E74B8 File Offset: 0x000E56B8
		[SecuritySafeCritical]
		~RegisteredWaitHandleSafe()
		{
			if (Interlocked.CompareExchange(ref this.m_lock, 1, 0) == 0)
			{
				try
				{
					if (this.ValidHandle())
					{
						RegisteredWaitHandleSafe.WaitHandleCleanupNative(this.registeredWaitHandle);
						if (this.bReleaseNeeded)
						{
							this.m_internalWaitObject.SafeWaitHandle.DangerousRelease();
							this.bReleaseNeeded = false;
						}
						this.SetHandle(RegisteredWaitHandleSafe.InvalidHandle);
						this.m_internalWaitObject = null;
					}
				}
				finally
				{
					this.m_lock = 0;
				}
			}
		}

		// Token: 0x06003DD5 RID: 15829
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void WaitHandleCleanupNative(IntPtr handle);

		// Token: 0x06003DD6 RID: 15830
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool UnregisterWaitNative(IntPtr handle, SafeHandle waitObject);

		// Token: 0x04001A10 RID: 6672
		private IntPtr registeredWaitHandle;

		// Token: 0x04001A11 RID: 6673
		private WaitHandle m_internalWaitObject;

		// Token: 0x04001A12 RID: 6674
		private bool bReleaseNeeded;

		// Token: 0x04001A13 RID: 6675
		private volatile int m_lock;
	}
}
