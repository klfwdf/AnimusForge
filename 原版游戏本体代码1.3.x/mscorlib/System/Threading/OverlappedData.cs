using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000505 RID: 1285
	internal sealed class OverlappedData
	{
		// Token: 0x06003C90 RID: 15504 RVA: 0x000E4A00 File Offset: 0x000E2C00
		[SecurityCritical]
		internal void ReInitialize()
		{
			this.m_asyncResult = null;
			this.m_iocb = null;
			this.m_iocbHelper = null;
			this.m_overlapped = null;
			this.m_userObject = null;
			this.m_pinSelf = (IntPtr)0;
			this.m_userObjectInternal = (IntPtr)0;
			this.m_AppDomainId = 0;
			this.m_nativeOverlapped.EventHandle = (IntPtr)0;
			this.m_isArray = 0;
			this.m_nativeOverlapped.InternalLow = (IntPtr)0;
			this.m_nativeOverlapped.InternalHigh = (IntPtr)0;
		}

		// Token: 0x06003C91 RID: 15505 RVA: 0x000E4A8C File Offset: 0x000E2C8C
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal unsafe NativeOverlapped* Pack(IOCompletionCallback iocb, object userData)
		{
			if (!this.m_pinSelf.IsNull())
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_Overlapped_Pack"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			if (iocb != null)
			{
				this.m_iocbHelper = new _IOCompletionCallback(iocb, ref stackCrawlMark);
				this.m_iocb = iocb;
			}
			else
			{
				this.m_iocbHelper = null;
				this.m_iocb = null;
			}
			this.m_userObject = userData;
			if (this.m_userObject != null)
			{
				if (this.m_userObject.GetType() == typeof(object[]))
				{
					this.m_isArray = 1;
				}
				else
				{
					this.m_isArray = 0;
				}
			}
			return this.AllocateNativeOverlapped();
		}

		// Token: 0x06003C92 RID: 15506 RVA: 0x000E4B24 File Offset: 0x000E2D24
		[SecurityCritical]
		internal unsafe NativeOverlapped* UnsafePack(IOCompletionCallback iocb, object userData)
		{
			if (!this.m_pinSelf.IsNull())
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_Overlapped_Pack"));
			}
			this.m_userObject = userData;
			if (this.m_userObject != null)
			{
				if (this.m_userObject.GetType() == typeof(object[]))
				{
					this.m_isArray = 1;
				}
				else
				{
					this.m_isArray = 0;
				}
			}
			this.m_iocb = iocb;
			this.m_iocbHelper = null;
			return this.AllocateNativeOverlapped();
		}

		// Token: 0x1700091A RID: 2330
		// (get) Token: 0x06003C93 RID: 15507 RVA: 0x000E4B9D File Offset: 0x000E2D9D
		// (set) Token: 0x06003C94 RID: 15508 RVA: 0x000E4BAA File Offset: 0x000E2DAA
		[ComVisible(false)]
		internal IntPtr UserHandle
		{
			get
			{
				return this.m_nativeOverlapped.EventHandle;
			}
			set
			{
				this.m_nativeOverlapped.EventHandle = value;
			}
		}

		// Token: 0x06003C95 RID: 15509
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe extern NativeOverlapped* AllocateNativeOverlapped();

		// Token: 0x06003C96 RID: 15510
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe static extern void FreeNativeOverlapped(NativeOverlapped* nativeOverlappedPtr);

		// Token: 0x06003C97 RID: 15511
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe static extern OverlappedData GetOverlappedFromNative(NativeOverlapped* nativeOverlappedPtr);

		// Token: 0x06003C98 RID: 15512
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe static extern void CheckVMForIOPacket(out NativeOverlapped* pOVERLAP, out uint errorCode, out uint numBytes);

		// Token: 0x040019B0 RID: 6576
		internal IAsyncResult m_asyncResult;

		// Token: 0x040019B1 RID: 6577
		[SecurityCritical]
		internal IOCompletionCallback m_iocb;

		// Token: 0x040019B2 RID: 6578
		internal _IOCompletionCallback m_iocbHelper;

		// Token: 0x040019B3 RID: 6579
		internal Overlapped m_overlapped;

		// Token: 0x040019B4 RID: 6580
		private object m_userObject;

		// Token: 0x040019B5 RID: 6581
		private IntPtr m_pinSelf;

		// Token: 0x040019B6 RID: 6582
		private IntPtr m_userObjectInternal;

		// Token: 0x040019B7 RID: 6583
		private int m_AppDomainId;

		// Token: 0x040019B8 RID: 6584
		private byte m_isArray;

		// Token: 0x040019B9 RID: 6585
		private byte m_toBeCleaned;

		// Token: 0x040019BA RID: 6586
		internal NativeOverlapped m_nativeOverlapped;
	}
}
