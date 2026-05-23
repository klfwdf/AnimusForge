using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000847 RID: 2119
	[ComVisible(true)]
	public interface IServerChannelSink : IChannelSinkBase
	{
		// Token: 0x06005A1F RID: 23071
		[SecurityCritical]
		ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream);

		// Token: 0x06005A20 RID: 23072
		[SecurityCritical]
		void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream);

		// Token: 0x06005A21 RID: 23073
		[SecurityCritical]
		Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers);

		// Token: 0x17000EF9 RID: 3833
		// (get) Token: 0x06005A22 RID: 23074
		IServerChannelSink NextChannelSink
		{
			[SecurityCritical]
			get;
		}
	}
}
