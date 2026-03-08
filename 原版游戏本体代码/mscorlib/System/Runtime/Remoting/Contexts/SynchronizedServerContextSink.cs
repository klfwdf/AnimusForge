using System;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200081A RID: 2074
	internal class SynchronizedServerContextSink : InternalSink, IMessageSink
	{
		// Token: 0x060058FC RID: 22780 RVA: 0x00139980 File Offset: 0x00137B80
		[SecurityCritical]
		internal SynchronizedServerContextSink(SynchronizationAttribute prop, IMessageSink nextSink)
		{
			this._property = prop;
			this._nextSink = nextSink;
		}

		// Token: 0x060058FD RID: 22781 RVA: 0x00139998 File Offset: 0x00137B98
		[SecuritySafeCritical]
		~SynchronizedServerContextSink()
		{
			this._property.Dispose();
		}

		// Token: 0x060058FE RID: 22782 RVA: 0x001399CC File Offset: 0x00137BCC
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage reqMsg)
		{
			WorkItem workItem = new WorkItem(reqMsg, this._nextSink, null);
			this._property.HandleWorkRequest(workItem);
			return workItem.ReplyMessage;
		}

		// Token: 0x060058FF RID: 22783 RVA: 0x001399FC File Offset: 0x00137BFC
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage reqMsg, IMessageSink replySink)
		{
			WorkItem workItem = new WorkItem(reqMsg, this._nextSink, replySink);
			workItem.SetAsync();
			this._property.HandleWorkRequest(workItem);
			return null;
		}

		// Token: 0x17000EC1 RID: 3777
		// (get) Token: 0x06005900 RID: 22784 RVA: 0x00139A2A File Offset: 0x00137C2A
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return this._nextSink;
			}
		}

		// Token: 0x0400288B RID: 10379
		internal IMessageSink _nextSink;

		// Token: 0x0400288C RID: 10380
		[SecurityCritical]
		internal SynchronizationAttribute _property;
	}
}
