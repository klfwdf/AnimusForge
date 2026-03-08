using System;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200082C RID: 2092
	internal class ServerAsyncReplyTerminatorSink : IMessageSink
	{
		// Token: 0x0600599C RID: 22940 RVA: 0x0013C0FC File Offset: 0x0013A2FC
		internal ServerAsyncReplyTerminatorSink(IMessageSink nextSink)
		{
			this._nextSink = nextSink;
		}

		// Token: 0x0600599D RID: 22941 RVA: 0x0013C10C File Offset: 0x0013A30C
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage replyMsg)
		{
			Guid guid;
			RemotingServices.CORProfilerRemotingServerSendingReply(out guid, true);
			if (RemotingServices.CORProfilerTrackRemotingCookie())
			{
				replyMsg.Properties["CORProfilerCookie"] = guid;
			}
			return this._nextSink.SyncProcessMessage(replyMsg);
		}

		// Token: 0x0600599E RID: 22942 RVA: 0x0013C14A File Offset: 0x0013A34A
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage replyMsg, IMessageSink replySink)
		{
			return null;
		}

		// Token: 0x17000EDD RID: 3805
		// (get) Token: 0x0600599F RID: 22943 RVA: 0x0013C14D File Offset: 0x0013A34D
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return this._nextSink;
			}
		}

		// Token: 0x040028D3 RID: 10451
		internal IMessageSink _nextSink;
	}
}
