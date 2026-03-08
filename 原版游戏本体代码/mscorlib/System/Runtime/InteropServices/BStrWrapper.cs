using System;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200095C RID: 2396
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class BStrWrapper
	{
		// Token: 0x06006214 RID: 25108 RVA: 0x0014F6F3 File Offset: 0x0014D8F3
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public BStrWrapper(string value)
		{
			this.m_WrappedObject = value;
		}

		// Token: 0x06006215 RID: 25109 RVA: 0x0014F702 File Offset: 0x0014D902
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public BStrWrapper(object value)
		{
			this.m_WrappedObject = (string)value;
		}

		// Token: 0x17001110 RID: 4368
		// (get) Token: 0x06006216 RID: 25110 RVA: 0x0014F716 File Offset: 0x0014D916
		[__DynamicallyInvokable]
		public string WrappedObject
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_WrappedObject;
			}
		}

		// Token: 0x04002B8C RID: 11148
		private string m_WrappedObject;
	}
}
