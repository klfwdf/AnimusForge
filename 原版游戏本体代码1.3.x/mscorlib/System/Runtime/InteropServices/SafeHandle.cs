using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000957 RID: 2391
	[SecurityCritical]
	[__DynamicallyInvokable]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	public abstract class SafeHandle : CriticalFinalizerObject, IDisposable
	{
		// Token: 0x060061D6 RID: 25046 RVA: 0x0014ED07 File Offset: 0x0014CF07
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		protected SafeHandle(IntPtr invalidHandleValue, bool ownsHandle)
		{
			this.handle = invalidHandleValue;
			this._state = 4;
			this._ownsHandle = ownsHandle;
			if (!ownsHandle)
			{
				GC.SuppressFinalize(this);
			}
			this._fullyInitialized = true;
		}

		// Token: 0x060061D7 RID: 25047 RVA: 0x0014ED34 File Offset: 0x0014CF34
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		~SafeHandle()
		{
			this.Dispose(false);
		}

		// Token: 0x060061D8 RID: 25048
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void InternalFinalize();

		// Token: 0x060061D9 RID: 25049 RVA: 0x0014ED64 File Offset: 0x0014CF64
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		protected void SetHandle(IntPtr handle)
		{
			this.handle = handle;
		}

		// Token: 0x060061DA RID: 25050 RVA: 0x0014ED6D File Offset: 0x0014CF6D
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public IntPtr DangerousGetHandle()
		{
			return this.handle;
		}

		// Token: 0x17001105 RID: 4357
		// (get) Token: 0x060061DB RID: 25051 RVA: 0x0014ED75 File Offset: 0x0014CF75
		[__DynamicallyInvokable]
		public bool IsClosed
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			get
			{
				return (this._state & 1) == 1;
			}
		}

		// Token: 0x17001106 RID: 4358
		// (get) Token: 0x060061DC RID: 25052
		[__DynamicallyInvokable]
		public abstract bool IsInvalid
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x060061DD RID: 25053 RVA: 0x0014ED82 File Offset: 0x0014CF82
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void Close()
		{
			this.Dispose(true);
		}

		// Token: 0x060061DE RID: 25054 RVA: 0x0014ED8B File Offset: 0x0014CF8B
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public void Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x060061DF RID: 25055 RVA: 0x0014ED94 File Offset: 0x0014CF94
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.InternalDispose();
				return;
			}
			this.InternalFinalize();
		}

		// Token: 0x060061E0 RID: 25056
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void InternalDispose();

		// Token: 0x060061E1 RID: 25057
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetHandleAsInvalid();

		// Token: 0x060061E2 RID: 25058
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		protected abstract bool ReleaseHandle();

		// Token: 0x060061E3 RID: 25059
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void DangerousAddRef(ref bool success);

		// Token: 0x060061E4 RID: 25060
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void DangerousRelease();

		// Token: 0x04002B80 RID: 11136
		protected IntPtr handle;

		// Token: 0x04002B81 RID: 11137
		private int _state;

		// Token: 0x04002B82 RID: 11138
		private bool _ownsHandle;

		// Token: 0x04002B83 RID: 11139
		private bool _fullyInitialized;
	}
}
