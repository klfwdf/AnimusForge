using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200082E RID: 2094
	[ComVisible(true)]
	public interface IClientResponseChannelSinkStack
	{
		// Token: 0x060059A2 RID: 22946
		[SecurityCritical]
		void AsyncProcessResponse(ITransportHeaders headers, Stream stream);

		// Token: 0x060059A3 RID: 22947
		[SecurityCritical]
		void DispatchReplyMessage(IMessage msg);

		// Token: 0x060059A4 RID: 22948
		[SecurityCritical]
		void DispatchException(Exception e);
	}
}
