using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000830 RID: 2096
	[ComVisible(true)]
	public interface IServerChannelSinkStack : IServerResponseChannelSinkStack
	{
		// Token: 0x060059AC RID: 22956
		[SecurityCritical]
		void Push(IServerChannelSink sink, object state);

		// Token: 0x060059AD RID: 22957
		[SecurityCritical]
		object Pop(IServerChannelSink sink);

		// Token: 0x060059AE RID: 22958
		[SecurityCritical]
		void Store(IServerChannelSink sink, object state);

		// Token: 0x060059AF RID: 22959
		[SecurityCritical]
		void StoreAndDispatch(IServerChannelSink sink, object state);

		// Token: 0x060059B0 RID: 22960
		[SecurityCritical]
		void ServerCallback(IAsyncResult ar);
	}
}
