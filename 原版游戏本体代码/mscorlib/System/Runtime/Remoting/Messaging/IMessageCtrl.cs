using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000858 RID: 2136
	[ComVisible(true)]
	public interface IMessageCtrl
	{
		// Token: 0x06005A7E RID: 23166
		[SecurityCritical]
		void Cancel(int msToCancel);
	}
}
