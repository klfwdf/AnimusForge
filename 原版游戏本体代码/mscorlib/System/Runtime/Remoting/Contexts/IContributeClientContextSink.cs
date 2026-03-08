using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000812 RID: 2066
	[ComVisible(true)]
	public interface IContributeClientContextSink
	{
		// Token: 0x060058DB RID: 22747
		[SecurityCritical]
		IMessageSink GetClientContextSink(IMessageSink nextSink);
	}
}
