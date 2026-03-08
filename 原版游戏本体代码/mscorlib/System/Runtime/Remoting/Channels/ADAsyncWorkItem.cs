using System;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000839 RID: 2105
	internal class ADAsyncWorkItem
	{
		// Token: 0x060059F2 RID: 23026 RVA: 0x0013D383 File Offset: 0x0013B583
		[SecurityCritical]
		internal ADAsyncWorkItem(IMessage reqMsg, IMessageSink nextSink, IMessageSink replySink)
		{
			this._reqMsg = reqMsg;
			this._nextSink = nextSink;
			this._replySink = replySink;
			this._callCtx = Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext;
		}

		// Token: 0x060059F3 RID: 23027 RVA: 0x0013D3B8 File Offset: 0x0013B5B8
		[SecurityCritical]
		internal virtual void FinishAsyncWork(object stateIgnored)
		{
			LogicalCallContext logicalCallContext = CallContext.SetLogicalCallContext(this._callCtx);
			IMessage msg = this._nextSink.SyncProcessMessage(this._reqMsg);
			if (this._replySink != null)
			{
				this._replySink.SyncProcessMessage(msg);
			}
			CallContext.SetLogicalCallContext(logicalCallContext);
		}

		// Token: 0x040028F4 RID: 10484
		private IMessageSink _replySink;

		// Token: 0x040028F5 RID: 10485
		private IMessageSink _nextSink;

		// Token: 0x040028F6 RID: 10486
		[SecurityCritical]
		private LogicalCallContext _callCtx;

		// Token: 0x040028F7 RID: 10487
		private IMessage _reqMsg;
	}
}
