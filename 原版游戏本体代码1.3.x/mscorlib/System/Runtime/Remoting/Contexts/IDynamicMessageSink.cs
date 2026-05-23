using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000818 RID: 2072
	[ComVisible(true)]
	public interface IDynamicMessageSink
	{
		// Token: 0x060058E1 RID: 22753
		[SecurityCritical]
		void ProcessMessageStart(IMessage reqMsg, bool bCliSide, bool bAsync);

		// Token: 0x060058E2 RID: 22754
		[SecurityCritical]
		void ProcessMessageFinish(IMessage replyMsg, bool bCliSide, bool bAsync);
	}
}
