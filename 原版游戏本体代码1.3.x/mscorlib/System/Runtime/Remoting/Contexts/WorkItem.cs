using System;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200081B RID: 2075
	internal class WorkItem
	{
		// Token: 0x06005902 RID: 22786 RVA: 0x00139A48 File Offset: 0x00137C48
		[SecurityCritical]
		internal WorkItem(IMessage reqMsg, IMessageSink nextSink, IMessageSink replySink)
		{
			this._reqMsg = reqMsg;
			this._replyMsg = null;
			this._nextSink = nextSink;
			this._replySink = replySink;
			this._ctx = Thread.CurrentContext;
			this._callCtx = Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext;
		}

		// Token: 0x06005903 RID: 22787 RVA: 0x00139A97 File Offset: 0x00137C97
		internal virtual void SetWaiting()
		{
			this._flags |= 1;
		}

		// Token: 0x06005904 RID: 22788 RVA: 0x00139AA7 File Offset: 0x00137CA7
		internal virtual bool IsWaiting()
		{
			return (this._flags & 1) == 1;
		}

		// Token: 0x06005905 RID: 22789 RVA: 0x00139AB4 File Offset: 0x00137CB4
		internal virtual void SetSignaled()
		{
			this._flags |= 2;
		}

		// Token: 0x06005906 RID: 22790 RVA: 0x00139AC4 File Offset: 0x00137CC4
		internal virtual bool IsSignaled()
		{
			return (this._flags & 2) == 2;
		}

		// Token: 0x06005907 RID: 22791 RVA: 0x00139AD1 File Offset: 0x00137CD1
		internal virtual void SetAsync()
		{
			this._flags |= 4;
		}

		// Token: 0x06005908 RID: 22792 RVA: 0x00139AE1 File Offset: 0x00137CE1
		internal virtual bool IsAsync()
		{
			return (this._flags & 4) == 4;
		}

		// Token: 0x06005909 RID: 22793 RVA: 0x00139AEE File Offset: 0x00137CEE
		internal virtual void SetDummy()
		{
			this._flags |= 8;
		}

		// Token: 0x0600590A RID: 22794 RVA: 0x00139AFE File Offset: 0x00137CFE
		internal virtual bool IsDummy()
		{
			return (this._flags & 8) == 8;
		}

		// Token: 0x0600590B RID: 22795 RVA: 0x00139B0C File Offset: 0x00137D0C
		[SecurityCritical]
		internal static object ExecuteCallback(object[] args)
		{
			WorkItem workItem = (WorkItem)args[0];
			if (workItem.IsAsync())
			{
				workItem._nextSink.AsyncProcessMessage(workItem._reqMsg, workItem._replySink);
			}
			else if (workItem._nextSink != null)
			{
				workItem._replyMsg = workItem._nextSink.SyncProcessMessage(workItem._reqMsg);
			}
			return null;
		}

		// Token: 0x0600590C RID: 22796 RVA: 0x00139B64 File Offset: 0x00137D64
		[SecurityCritical]
		internal virtual void Execute()
		{
			Thread.CurrentThread.InternalCrossContextCallback(this._ctx, WorkItem._xctxDel, new object[] { this });
		}

		// Token: 0x17000EC2 RID: 3778
		// (get) Token: 0x0600590D RID: 22797 RVA: 0x00139B86 File Offset: 0x00137D86
		internal virtual IMessage ReplyMessage
		{
			get
			{
				return this._replyMsg;
			}
		}

		// Token: 0x0400288D RID: 10381
		private const int FLG_WAITING = 1;

		// Token: 0x0400288E RID: 10382
		private const int FLG_SIGNALED = 2;

		// Token: 0x0400288F RID: 10383
		private const int FLG_ASYNC = 4;

		// Token: 0x04002890 RID: 10384
		private const int FLG_DUMMY = 8;

		// Token: 0x04002891 RID: 10385
		internal int _flags;

		// Token: 0x04002892 RID: 10386
		internal IMessage _reqMsg;

		// Token: 0x04002893 RID: 10387
		internal IMessageSink _nextSink;

		// Token: 0x04002894 RID: 10388
		internal IMessageSink _replySink;

		// Token: 0x04002895 RID: 10389
		internal IMessage _replyMsg;

		// Token: 0x04002896 RID: 10390
		internal Context _ctx;

		// Token: 0x04002897 RID: 10391
		[SecurityCritical]
		internal LogicalCallContext _callCtx;

		// Token: 0x04002898 RID: 10392
		internal static InternalCrossContextDelegate _xctxDel = new InternalCrossContextDelegate(WorkItem.ExecuteCallback);
	}
}
