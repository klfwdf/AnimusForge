using System;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200081C RID: 2076
	internal class SynchronizedClientContextSink : InternalSink, IMessageSink
	{
		// Token: 0x0600590E RID: 22798 RVA: 0x00139B8E File Offset: 0x00137D8E
		[SecurityCritical]
		internal SynchronizedClientContextSink(SynchronizationAttribute prop, IMessageSink nextSink)
		{
			this._property = prop;
			this._nextSink = nextSink;
		}

		// Token: 0x0600590F RID: 22799 RVA: 0x00139BA4 File Offset: 0x00137DA4
		[SecuritySafeCritical]
		~SynchronizedClientContextSink()
		{
			this._property.Dispose();
		}

		// Token: 0x06005910 RID: 22800 RVA: 0x00139BD8 File Offset: 0x00137DD8
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage reqMsg)
		{
			IMessage message;
			if (this._property.IsReEntrant)
			{
				this._property.HandleThreadExit();
				message = this._nextSink.SyncProcessMessage(reqMsg);
				this._property.HandleThreadReEntry();
			}
			else
			{
				LogicalCallContext logicalCallContext = (LogicalCallContext)reqMsg.Properties[Message.CallContextKey];
				string text = logicalCallContext.RemotingData.LogicalCallID;
				bool flag = false;
				if (text == null)
				{
					text = Identity.GetNewLogicalCallID();
					logicalCallContext.RemotingData.LogicalCallID = text;
					flag = true;
				}
				bool flag2 = false;
				if (this._property.SyncCallOutLCID == null)
				{
					this._property.SyncCallOutLCID = text;
					flag2 = true;
				}
				message = this._nextSink.SyncProcessMessage(reqMsg);
				if (flag2)
				{
					this._property.SyncCallOutLCID = null;
					if (flag)
					{
						LogicalCallContext logicalCallContext2 = (LogicalCallContext)message.Properties[Message.CallContextKey];
						logicalCallContext2.RemotingData.LogicalCallID = null;
					}
				}
			}
			return message;
		}

		// Token: 0x06005911 RID: 22801 RVA: 0x00139CBC File Offset: 0x00137EBC
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage reqMsg, IMessageSink replySink)
		{
			if (!this._property.IsReEntrant)
			{
				LogicalCallContext logicalCallContext = (LogicalCallContext)reqMsg.Properties[Message.CallContextKey];
				string newLogicalCallID = Identity.GetNewLogicalCallID();
				logicalCallContext.RemotingData.LogicalCallID = newLogicalCallID;
				this._property.AsyncCallOutLCIDList.Add(newLogicalCallID);
			}
			SynchronizedClientContextSink.AsyncReplySink replySink2 = new SynchronizedClientContextSink.AsyncReplySink(replySink, this._property);
			return this._nextSink.AsyncProcessMessage(reqMsg, replySink2);
		}

		// Token: 0x17000EC3 RID: 3779
		// (get) Token: 0x06005912 RID: 22802 RVA: 0x00139D2E File Offset: 0x00137F2E
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return this._nextSink;
			}
		}

		// Token: 0x04002899 RID: 10393
		internal IMessageSink _nextSink;

		// Token: 0x0400289A RID: 10394
		[SecurityCritical]
		internal SynchronizationAttribute _property;

		// Token: 0x02000C72 RID: 3186
		internal class AsyncReplySink : IMessageSink
		{
			// Token: 0x060070AE RID: 28846 RVA: 0x00184979 File Offset: 0x00182B79
			[SecurityCritical]
			internal AsyncReplySink(IMessageSink nextSink, SynchronizationAttribute prop)
			{
				this._nextSink = nextSink;
				this._property = prop;
			}

			// Token: 0x060070AF RID: 28847 RVA: 0x00184990 File Offset: 0x00182B90
			[SecurityCritical]
			public virtual IMessage SyncProcessMessage(IMessage reqMsg)
			{
				WorkItem workItem = new WorkItem(reqMsg, this._nextSink, null);
				this._property.HandleWorkRequest(workItem);
				if (!this._property.IsReEntrant)
				{
					this._property.AsyncCallOutLCIDList.Remove(((LogicalCallContext)reqMsg.Properties[Message.CallContextKey]).RemotingData.LogicalCallID);
				}
				return workItem.ReplyMessage;
			}

			// Token: 0x060070B0 RID: 28848 RVA: 0x001849F9 File Offset: 0x00182BF9
			[SecurityCritical]
			public virtual IMessageCtrl AsyncProcessMessage(IMessage reqMsg, IMessageSink replySink)
			{
				throw new NotSupportedException();
			}

			// Token: 0x17001352 RID: 4946
			// (get) Token: 0x060070B1 RID: 28849 RVA: 0x00184A00 File Offset: 0x00182C00
			public IMessageSink NextSink
			{
				[SecurityCritical]
				get
				{
					return this._nextSink;
				}
			}

			// Token: 0x040037F8 RID: 14328
			internal IMessageSink _nextSink;

			// Token: 0x040037F9 RID: 14329
			[SecurityCritical]
			internal SynchronizationAttribute _property;
		}
	}
}
