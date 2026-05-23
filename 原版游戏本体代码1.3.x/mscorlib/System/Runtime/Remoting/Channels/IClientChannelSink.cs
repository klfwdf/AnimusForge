using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000845 RID: 2117
	[ComVisible(true)]
	public interface IClientChannelSink : IChannelSinkBase
	{
		// Token: 0x06005A1A RID: 23066
		[SecurityCritical]
		void ProcessMessage(IMessage msg, ITransportHeaders requestHeaders, Stream requestStream, out ITransportHeaders responseHeaders, out Stream responseStream);

		// Token: 0x06005A1B RID: 23067
		[SecurityCritical]
		void AsyncProcessRequest(IClientChannelSinkStack sinkStack, IMessage msg, ITransportHeaders headers, Stream stream);

		// Token: 0x06005A1C RID: 23068
		[SecurityCritical]
		void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, Stream stream);

		// Token: 0x06005A1D RID: 23069
		[SecurityCritical]
		Stream GetRequestStream(IMessage msg, ITransportHeaders headers);

		// Token: 0x17000EF8 RID: 3832
		// (get) Token: 0x06005A1E RID: 23070
		IClientChannelSink NextChannelSink
		{
			[SecurityCritical]
			get;
		}
	}
}
