using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000815 RID: 2069
	[ComVisible(true)]
	public interface IContributeObjectSink
	{
		// Token: 0x060058DE RID: 22750
		[SecurityCritical]
		IMessageSink GetObjectSink(MarshalByRefObject obj, IMessageSink nextSink);
	}
}
