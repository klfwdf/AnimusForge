using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200082F RID: 2095
	[SecurityCritical]
	[ComVisible(true)]
	public class ClientChannelSinkStack : IClientChannelSinkStack, IClientResponseChannelSinkStack
	{
		// Token: 0x060059A5 RID: 22949 RVA: 0x0013C155 File Offset: 0x0013A355
		public ClientChannelSinkStack()
		{
		}

		// Token: 0x060059A6 RID: 22950 RVA: 0x0013C15D File Offset: 0x0013A35D
		public ClientChannelSinkStack(IMessageSink replySink)
		{
			this._replySink = replySink;
		}

		// Token: 0x060059A7 RID: 22951 RVA: 0x0013C16C File Offset: 0x0013A36C
		[SecurityCritical]
		public void Push(IClientChannelSink sink, object state)
		{
			this._stack = new ClientChannelSinkStack.SinkStack
			{
				PrevStack = this._stack,
				Sink = sink,
				State = state
			};
		}

		// Token: 0x060059A8 RID: 22952 RVA: 0x0013C1A0 File Offset: 0x0013A3A0
		[SecurityCritical]
		public object Pop(IClientChannelSink sink)
		{
			if (this._stack == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_PopOnEmptySinkStack"));
			}
			while (this._stack.Sink != sink)
			{
				this._stack = this._stack.PrevStack;
				if (this._stack == null)
				{
					break;
				}
			}
			if (this._stack.Sink == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Channel_PopFromSinkStackWithoutPush"));
			}
			object state = this._stack.State;
			this._stack = this._stack.PrevStack;
			return state;
		}

		// Token: 0x060059A9 RID: 22953 RVA: 0x0013C228 File Offset: 0x0013A428
		[SecurityCritical]
		public void AsyncProcessResponse(ITransportHeaders headers, Stream stream)
		{
			if (this._replySink != null)
			{
				if (this._stack == null)
				{
					throw new RemotingException(Environment.GetResourceString("Remoting_Channel_CantCallAPRWhenStackEmpty"));
				}
				IClientChannelSink sink = this._stack.Sink;
				object state = this._stack.State;
				this._stack = this._stack.PrevStack;
				sink.AsyncProcessResponse(this, state, headers, stream);
			}
		}

		// Token: 0x060059AA RID: 22954 RVA: 0x0013C288 File Offset: 0x0013A488
		[SecurityCritical]
		public void DispatchReplyMessage(IMessage msg)
		{
			if (this._replySink != null)
			{
				this._replySink.SyncProcessMessage(msg);
			}
		}

		// Token: 0x060059AB RID: 22955 RVA: 0x0013C29F File Offset: 0x0013A49F
		[SecurityCritical]
		public void DispatchException(Exception e)
		{
			this.DispatchReplyMessage(new ReturnMessage(e, null));
		}

		// Token: 0x040028D4 RID: 10452
		private ClientChannelSinkStack.SinkStack _stack;

		// Token: 0x040028D5 RID: 10453
		private IMessageSink _replySink;

		// Token: 0x02000C77 RID: 3191
		private class SinkStack
		{
			// Token: 0x04003803 RID: 14339
			public ClientChannelSinkStack.SinkStack PrevStack;

			// Token: 0x04003804 RID: 14340
			public IClientChannelSink Sink;

			// Token: 0x04003805 RID: 14341
			public object State;
		}
	}
}
