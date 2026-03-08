using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000855 RID: 2133
	[ComVisible(true)]
	public class AsyncResult : IAsyncResult, IMessageSink
	{
		// Token: 0x06005A68 RID: 23144 RVA: 0x0013DE42 File Offset: 0x0013C042
		[SecurityCritical]
		internal AsyncResult(Message m)
		{
			m.GetAsyncBeginInfo(out this._acbd, out this._asyncState);
			this._asyncDelegate = (Delegate)m.GetThisPtr();
		}

		// Token: 0x17000F1B RID: 3867
		// (get) Token: 0x06005A69 RID: 23145 RVA: 0x0013DE6D File Offset: 0x0013C06D
		public virtual bool IsCompleted
		{
			get
			{
				return this._isCompleted;
			}
		}

		// Token: 0x17000F1C RID: 3868
		// (get) Token: 0x06005A6A RID: 23146 RVA: 0x0013DE75 File Offset: 0x0013C075
		public virtual object AsyncDelegate
		{
			get
			{
				return this._asyncDelegate;
			}
		}

		// Token: 0x17000F1D RID: 3869
		// (get) Token: 0x06005A6B RID: 23147 RVA: 0x0013DE7D File Offset: 0x0013C07D
		public virtual object AsyncState
		{
			get
			{
				return this._asyncState;
			}
		}

		// Token: 0x17000F1E RID: 3870
		// (get) Token: 0x06005A6C RID: 23148 RVA: 0x0013DE85 File Offset: 0x0013C085
		public virtual bool CompletedSynchronously
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000F1F RID: 3871
		// (get) Token: 0x06005A6D RID: 23149 RVA: 0x0013DE88 File Offset: 0x0013C088
		// (set) Token: 0x06005A6E RID: 23150 RVA: 0x0013DE90 File Offset: 0x0013C090
		public bool EndInvokeCalled
		{
			get
			{
				return this._endInvokeCalled;
			}
			set
			{
				this._endInvokeCalled = value;
			}
		}

		// Token: 0x06005A6F RID: 23151 RVA: 0x0013DE9C File Offset: 0x0013C09C
		private void FaultInWaitHandle()
		{
			lock (this)
			{
				if (this._AsyncWaitHandle == null)
				{
					this._AsyncWaitHandle = new ManualResetEvent(false);
				}
			}
		}

		// Token: 0x17000F20 RID: 3872
		// (get) Token: 0x06005A70 RID: 23152 RVA: 0x0013DEE8 File Offset: 0x0013C0E8
		public virtual WaitHandle AsyncWaitHandle
		{
			get
			{
				this.FaultInWaitHandle();
				return this._AsyncWaitHandle;
			}
		}

		// Token: 0x06005A71 RID: 23153 RVA: 0x0013DEF6 File Offset: 0x0013C0F6
		public virtual void SetMessageCtrl(IMessageCtrl mc)
		{
			this._mc = mc;
		}

		// Token: 0x06005A72 RID: 23154 RVA: 0x0013DF00 File Offset: 0x0013C100
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage msg)
		{
			if (msg == null)
			{
				this._replyMsg = new ReturnMessage(new RemotingException(Environment.GetResourceString("Remoting_NullMessage")), new ErrorMessage());
			}
			else if (!(msg is IMethodReturnMessage))
			{
				this._replyMsg = new ReturnMessage(new RemotingException(Environment.GetResourceString("Remoting_Message_BadType")), new ErrorMessage());
			}
			else
			{
				this._replyMsg = msg;
			}
			this._isCompleted = true;
			this.FaultInWaitHandle();
			this._AsyncWaitHandle.Set();
			if (this._acbd != null)
			{
				this._acbd(this);
			}
			return null;
		}

		// Token: 0x06005A73 RID: 23155 RVA: 0x0013DF8F File Offset: 0x0013C18F
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
		}

		// Token: 0x17000F21 RID: 3873
		// (get) Token: 0x06005A74 RID: 23156 RVA: 0x0013DFA0 File Offset: 0x0013C1A0
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x06005A75 RID: 23157 RVA: 0x0013DFA3 File Offset: 0x0013C1A3
		public virtual IMessage GetReplyMessage()
		{
			return this._replyMsg;
		}

		// Token: 0x04002906 RID: 10502
		private IMessageCtrl _mc;

		// Token: 0x04002907 RID: 10503
		private AsyncCallback _acbd;

		// Token: 0x04002908 RID: 10504
		private IMessage _replyMsg;

		// Token: 0x04002909 RID: 10505
		private bool _isCompleted;

		// Token: 0x0400290A RID: 10506
		private bool _endInvokeCalled;

		// Token: 0x0400290B RID: 10507
		private ManualResetEvent _AsyncWaitHandle;

		// Token: 0x0400290C RID: 10508
		private Delegate _asyncDelegate;

		// Token: 0x0400290D RID: 10509
		private object _asyncState;
	}
}
