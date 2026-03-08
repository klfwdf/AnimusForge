using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200082D RID: 2093
	[ComVisible(true)]
	public interface IClientChannelSinkStack : IClientResponseChannelSinkStack
	{
		// Token: 0x060059A0 RID: 22944
		[SecurityCritical]
		void Push(IClientChannelSink sink, object state);

		// Token: 0x060059A1 RID: 22945
		[SecurityCritical]
		object Pop(IClientChannelSink sink);
	}
}
