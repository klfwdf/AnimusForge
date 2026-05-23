using System;
using System.Runtime.Remoting.Contexts;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000887 RID: 2183
	[Serializable]
	internal class ServerObjectTerminatorSink : InternalSink, IMessageSink
	{
		// Token: 0x06005C9D RID: 23709 RVA: 0x00144F07 File Offset: 0x00143107
		internal ServerObjectTerminatorSink(MarshalByRefObject srvObj)
		{
			this._stackBuilderSink = new StackBuilderSink(srvObj);
		}

		// Token: 0x06005C9E RID: 23710 RVA: 0x00144F1C File Offset: 0x0014311C
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage reqMsg)
		{
			IMessage message = InternalSink.ValidateMessage(reqMsg);
			if (message != null)
			{
				return message;
			}
			ServerIdentity serverIdentity = InternalSink.GetServerIdentity(reqMsg);
			ArrayWithSize serverSideDynamicSinks = serverIdentity.ServerSideDynamicSinks;
			if (serverSideDynamicSinks != null)
			{
				DynamicPropertyHolder.NotifyDynamicSinks(reqMsg, serverSideDynamicSinks, false, true, false);
			}
			IMessageSink messageSink = this._stackBuilderSink.ServerObject as IMessageSink;
			IMessage message2;
			if (messageSink != null)
			{
				message2 = messageSink.SyncProcessMessage(reqMsg);
			}
			else
			{
				message2 = this._stackBuilderSink.SyncProcessMessage(reqMsg);
			}
			if (serverSideDynamicSinks != null)
			{
				DynamicPropertyHolder.NotifyDynamicSinks(message2, serverSideDynamicSinks, false, false, false);
			}
			return message2;
		}

		// Token: 0x06005C9F RID: 23711 RVA: 0x00144F8C File Offset: 0x0014318C
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage reqMsg, IMessageSink replySink)
		{
			IMessageCtrl result = null;
			IMessage message = InternalSink.ValidateMessage(reqMsg);
			if (message != null)
			{
				if (replySink != null)
				{
					replySink.SyncProcessMessage(message);
				}
			}
			else
			{
				IMessageSink messageSink = this._stackBuilderSink.ServerObject as IMessageSink;
				if (messageSink != null)
				{
					result = messageSink.AsyncProcessMessage(reqMsg, replySink);
				}
				else
				{
					result = this._stackBuilderSink.AsyncProcessMessage(reqMsg, replySink);
				}
			}
			return result;
		}

		// Token: 0x17000FE9 RID: 4073
		// (get) Token: 0x06005CA0 RID: 23712 RVA: 0x00144FE0 File Offset: 0x001431E0
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x040029D2 RID: 10706
		internal StackBuilderSink _stackBuilderSink;
	}
}
