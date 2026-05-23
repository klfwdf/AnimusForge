using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000831 RID: 2097
	[ComVisible(true)]
	public interface IServerResponseChannelSinkStack
	{
		// Token: 0x060059B1 RID: 22961
		[SecurityCritical]
		void AsyncProcessResponse(IMessage msg, ITransportHeaders headers, Stream stream);

		// Token: 0x060059B2 RID: 22962
		[SecurityCritical]
		Stream GetResponseStream(IMessage msg, ITransportHeaders headers);
	}
}
