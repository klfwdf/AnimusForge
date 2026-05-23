using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Services
{
	// Token: 0x02000806 RID: 2054
	[ComVisible(true)]
	public interface ITrackingHandler
	{
		// Token: 0x06005873 RID: 22643
		[SecurityCritical]
		void MarshaledObject(object obj, ObjRef or);

		// Token: 0x06005874 RID: 22644
		[SecurityCritical]
		void UnmarshaledObject(object obj, ObjRef or);

		// Token: 0x06005875 RID: 22645
		[SecurityCritical]
		void DisconnectedObject(object obj);
	}
}
