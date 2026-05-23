using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000859 RID: 2137
	[ComVisible(true)]
	public interface IMessageSink
	{
		// Token: 0x06005A7F RID: 23167
		[SecurityCritical]
		IMessage SyncProcessMessage(IMessage msg);

		// Token: 0x06005A80 RID: 23168
		[SecurityCritical]
		IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink);

		// Token: 0x17000F25 RID: 3877
		// (get) Token: 0x06005A81 RID: 23169
		IMessageSink NextSink
		{
			[SecurityCritical]
			get;
		}
	}
}
