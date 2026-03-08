using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200051E RID: 1310
	[ComVisible(true)]
	public sealed class RegisteredWaitHandle : MarshalByRefObject
	{
		// Token: 0x06003DD7 RID: 15831 RVA: 0x000E754C File Offset: 0x000E574C
		internal RegisteredWaitHandle()
		{
			this.internalRegisteredWait = new RegisteredWaitHandleSafe();
		}

		// Token: 0x06003DD8 RID: 15832 RVA: 0x000E755F File Offset: 0x000E575F
		internal void SetHandle(IntPtr handle)
		{
			this.internalRegisteredWait.SetHandle(handle);
		}

		// Token: 0x06003DD9 RID: 15833 RVA: 0x000E756D File Offset: 0x000E576D
		[SecurityCritical]
		internal void SetWaitObject(WaitHandle waitObject)
		{
			this.internalRegisteredWait.SetWaitObject(waitObject);
		}

		// Token: 0x06003DDA RID: 15834 RVA: 0x000E757B File Offset: 0x000E577B
		[SecuritySafeCritical]
		[ComVisible(true)]
		public bool Unregister(WaitHandle waitObject)
		{
			return this.internalRegisteredWait.Unregister(waitObject);
		}

		// Token: 0x04001A14 RID: 6676
		private RegisteredWaitHandleSafe internalRegisteredWait;
	}
}
