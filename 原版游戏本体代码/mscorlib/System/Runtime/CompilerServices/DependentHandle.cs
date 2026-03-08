using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008E6 RID: 2278
	[ComVisible(false)]
	internal struct DependentHandle
	{
		// Token: 0x06005DF8 RID: 24056 RVA: 0x0014A5B8 File Offset: 0x001487B8
		[SecurityCritical]
		public DependentHandle(object primary, object secondary)
		{
			IntPtr handle = (IntPtr)0;
			DependentHandle.nInitialize(primary, secondary, out handle);
			this._handle = handle;
		}

		// Token: 0x17001024 RID: 4132
		// (get) Token: 0x06005DF9 RID: 24057 RVA: 0x0014A5DC File Offset: 0x001487DC
		public bool IsAllocated
		{
			get
			{
				return this._handle != (IntPtr)0;
			}
		}

		// Token: 0x06005DFA RID: 24058 RVA: 0x0014A5F0 File Offset: 0x001487F0
		[SecurityCritical]
		public object GetPrimary()
		{
			object result;
			DependentHandle.nGetPrimary(this._handle, out result);
			return result;
		}

		// Token: 0x06005DFB RID: 24059 RVA: 0x0014A60B File Offset: 0x0014880B
		[SecurityCritical]
		public void GetPrimaryAndSecondary(out object primary, out object secondary)
		{
			DependentHandle.nGetPrimaryAndSecondary(this._handle, out primary, out secondary);
		}

		// Token: 0x06005DFC RID: 24060 RVA: 0x0014A61C File Offset: 0x0014881C
		[SecurityCritical]
		public void Free()
		{
			if (this._handle != (IntPtr)0)
			{
				IntPtr handle = this._handle;
				this._handle = (IntPtr)0;
				DependentHandle.nFree(handle);
			}
		}

		// Token: 0x06005DFD RID: 24061
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void nInitialize(object primary, object secondary, out IntPtr dependentHandle);

		// Token: 0x06005DFE RID: 24062
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void nGetPrimary(IntPtr dependentHandle, out object primary);

		// Token: 0x06005DFF RID: 24063
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void nGetPrimaryAndSecondary(IntPtr dependentHandle, out object primary, out object secondary);

		// Token: 0x06005E00 RID: 24064
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void nFree(IntPtr dependentHandle);

		// Token: 0x04002A45 RID: 10821
		private IntPtr _handle;
	}
}
