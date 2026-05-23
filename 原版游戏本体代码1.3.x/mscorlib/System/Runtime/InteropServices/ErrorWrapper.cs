using System;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200095F RID: 2399
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class ErrorWrapper
	{
		// Token: 0x0600621C RID: 25116 RVA: 0x0014F79B File Offset: 0x0014D99B
		[__DynamicallyInvokable]
		public ErrorWrapper(int errorCode)
		{
			this.m_ErrorCode = errorCode;
		}

		// Token: 0x0600621D RID: 25117 RVA: 0x0014F7AA File Offset: 0x0014D9AA
		[__DynamicallyInvokable]
		public ErrorWrapper(object errorCode)
		{
			if (!(errorCode is int))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeInt32"), "errorCode");
			}
			this.m_ErrorCode = (int)errorCode;
		}

		// Token: 0x0600621E RID: 25118 RVA: 0x0014F7DB File Offset: 0x0014D9DB
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public ErrorWrapper(Exception e)
		{
			this.m_ErrorCode = Marshal.GetHRForException(e);
		}

		// Token: 0x17001113 RID: 4371
		// (get) Token: 0x0600621F RID: 25119 RVA: 0x0014F7EF File Offset: 0x0014D9EF
		[__DynamicallyInvokable]
		public int ErrorCode
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_ErrorCode;
			}
		}

		// Token: 0x04002B8F RID: 11151
		private int m_ErrorCode;
	}
}
