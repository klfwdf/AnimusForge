using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000816 RID: 2070
	[ComVisible(true)]
	public interface IContributeServerContextSink
	{
		// Token: 0x060058DF RID: 22751
		[SecurityCritical]
		IMessageSink GetServerContextSink(IMessageSink nextSink);
	}
}
