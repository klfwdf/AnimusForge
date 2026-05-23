using System;
using System.Collections;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200083C RID: 2108
	internal class DispatchChannelSink : IServerChannelSink, IChannelSinkBase
	{
		// Token: 0x06005A01 RID: 23041 RVA: 0x0013D5B1 File Offset: 0x0013B7B1
		internal DispatchChannelSink()
		{
		}

		// Token: 0x06005A02 RID: 23042 RVA: 0x0013D5B9 File Offset: 0x0013B7B9
		[SecurityCritical]
		public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
		{
			if (requestMsg == null)
			{
				throw new ArgumentNullException("requestMsg", Environment.GetResourceString("Remoting_Channel_DispatchSinkMessageMissing"));
			}
			if (requestStream != null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_DispatchSinkWantsNullRequestStream"));
			}
			responseHeaders = null;
			responseStream = null;
			return ChannelServices.DispatchMessage(sinkStack, requestMsg, out responseMsg);
		}

		// Token: 0x06005A03 RID: 23043 RVA: 0x0013D5F8 File Offset: 0x0013B7F8
		[SecurityCritical]
		public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers, Stream stream)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06005A04 RID: 23044 RVA: 0x0013D5FF File Offset: 0x0013B7FF
		[SecurityCritical]
		public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg, ITransportHeaders headers)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000EEE RID: 3822
		// (get) Token: 0x06005A05 RID: 23045 RVA: 0x0013D606 File Offset: 0x0013B806
		public IServerChannelSink NextChannelSink
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x17000EEF RID: 3823
		// (get) Token: 0x06005A06 RID: 23046 RVA: 0x0013D609 File Offset: 0x0013B809
		public IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}
	}
}
