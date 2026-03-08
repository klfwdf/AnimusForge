using System;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Contexts;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000885 RID: 2181
	[Serializable]
	internal class ServerContextTerminatorSink : InternalSink, IMessageSink
	{
		// Token: 0x17000FE6 RID: 4070
		// (get) Token: 0x06005C92 RID: 23698 RVA: 0x00144D10 File Offset: 0x00142F10
		internal static IMessageSink MessageSink
		{
			get
			{
				if (ServerContextTerminatorSink.messageSink == null)
				{
					ServerContextTerminatorSink serverContextTerminatorSink = new ServerContextTerminatorSink();
					object obj = ServerContextTerminatorSink.staticSyncObject;
					lock (obj)
					{
						if (ServerContextTerminatorSink.messageSink == null)
						{
							ServerContextTerminatorSink.messageSink = serverContextTerminatorSink;
						}
					}
				}
				return ServerContextTerminatorSink.messageSink;
			}
		}

		// Token: 0x06005C93 RID: 23699 RVA: 0x00144D70 File Offset: 0x00142F70
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage reqMsg)
		{
			IMessage message = InternalSink.ValidateMessage(reqMsg);
			if (message != null)
			{
				return message;
			}
			Context currentContext = Thread.CurrentContext;
			IMessage message2;
			if (reqMsg is IConstructionCallMessage)
			{
				message = currentContext.NotifyActivatorProperties(reqMsg, true);
				if (message != null)
				{
					return message;
				}
				message2 = ((IConstructionCallMessage)reqMsg).Activator.Activate((IConstructionCallMessage)reqMsg);
				message = currentContext.NotifyActivatorProperties(message2, true);
				if (message != null)
				{
					return message;
				}
			}
			else
			{
				MarshalByRefObject marshalByRefObject = null;
				try
				{
					message2 = this.GetObjectChain(reqMsg, out marshalByRefObject).SyncProcessMessage(reqMsg);
				}
				finally
				{
					IDisposable disposable;
					if (marshalByRefObject != null && (disposable = marshalByRefObject as IDisposable) != null)
					{
						disposable.Dispose();
					}
				}
			}
			return message2;
		}

		// Token: 0x06005C94 RID: 23700 RVA: 0x00144E08 File Offset: 0x00143008
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage reqMsg, IMessageSink replySink)
		{
			IMessageCtrl result = null;
			IMessage message = InternalSink.ValidateMessage(reqMsg);
			if (message == null)
			{
				message = InternalSink.DisallowAsyncActivation(reqMsg);
			}
			if (message != null)
			{
				if (replySink != null)
				{
					replySink.SyncProcessMessage(message);
				}
			}
			else
			{
				MarshalByRefObject marshalByRefObject;
				IMessageSink objectChain = this.GetObjectChain(reqMsg, out marshalByRefObject);
				IDisposable iDis;
				if (marshalByRefObject != null && (iDis = marshalByRefObject as IDisposable) != null)
				{
					DisposeSink disposeSink = new DisposeSink(iDis, replySink);
					replySink = disposeSink;
				}
				result = objectChain.AsyncProcessMessage(reqMsg, replySink);
			}
			return result;
		}

		// Token: 0x17000FE7 RID: 4071
		// (get) Token: 0x06005C95 RID: 23701 RVA: 0x00144E68 File Offset: 0x00143068
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x06005C96 RID: 23702 RVA: 0x00144E6C File Offset: 0x0014306C
		[SecurityCritical]
		internal virtual IMessageSink GetObjectChain(IMessage reqMsg, out MarshalByRefObject obj)
		{
			ServerIdentity serverIdentity = InternalSink.GetServerIdentity(reqMsg);
			return serverIdentity.GetServerObjectChain(out obj);
		}

		// Token: 0x040029CE RID: 10702
		private static volatile ServerContextTerminatorSink messageSink;

		// Token: 0x040029CF RID: 10703
		private static object staticSyncObject = new object();
	}
}
