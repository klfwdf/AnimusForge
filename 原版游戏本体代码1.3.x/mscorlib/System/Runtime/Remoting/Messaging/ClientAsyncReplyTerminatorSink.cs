using System;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000888 RID: 2184
	internal class ClientAsyncReplyTerminatorSink : IMessageSink
	{
		// Token: 0x06005CA1 RID: 23713 RVA: 0x00144FE3 File Offset: 0x001431E3
		internal ClientAsyncReplyTerminatorSink(IMessageSink nextSink)
		{
			this._nextSink = nextSink;
		}

		// Token: 0x06005CA2 RID: 23714 RVA: 0x00144FF4 File Offset: 0x001431F4
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage replyMsg)
		{
			Guid id = Guid.Empty;
			if (RemotingServices.CORProfilerTrackRemotingCookie())
			{
				object obj = replyMsg.Properties["CORProfilerCookie"];
				if (obj != null)
				{
					id = (Guid)obj;
				}
			}
			RemotingServices.CORProfilerRemotingClientReceivingReply(id, true);
			return this._nextSink.SyncProcessMessage(replyMsg);
		}

		// Token: 0x06005CA3 RID: 23715 RVA: 0x0014503C File Offset: 0x0014323C
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage replyMsg, IMessageSink replySink)
		{
			return null;
		}

		// Token: 0x17000FEA RID: 4074
		// (get) Token: 0x06005CA4 RID: 23716 RVA: 0x0014503F File Offset: 0x0014323F
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return this._nextSink;
			}
		}

		// Token: 0x040029D3 RID: 10707
		internal IMessageSink _nextSink;
	}
}
