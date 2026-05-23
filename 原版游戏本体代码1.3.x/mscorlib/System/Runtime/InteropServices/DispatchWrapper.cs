using System;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200095E RID: 2398
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DispatchWrapper
	{
		// Token: 0x0600621A RID: 25114 RVA: 0x0014F768 File Offset: 0x0014D968
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public DispatchWrapper(object obj)
		{
			if (obj != null)
			{
				IntPtr idispatchForObject = Marshal.GetIDispatchForObject(obj);
				Marshal.Release(idispatchForObject);
			}
			this.m_WrappedObject = obj;
		}

		// Token: 0x17001112 RID: 4370
		// (get) Token: 0x0600621B RID: 25115 RVA: 0x0014F793 File Offset: 0x0014D993
		[__DynamicallyInvokable]
		public object WrappedObject
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_WrappedObject;
			}
		}

		// Token: 0x04002B8E RID: 11150
		private object m_WrappedObject;
	}
}
