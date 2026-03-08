using System;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000882 RID: 2178
	[Serializable]
	internal class EnvoyTerminatorSink : InternalSink, IMessageSink
	{
		// Token: 0x17000FE1 RID: 4065
		// (get) Token: 0x06005C7E RID: 23678 RVA: 0x001448CC File Offset: 0x00142ACC
		internal static IMessageSink MessageSink
		{
			get
			{
				if (EnvoyTerminatorSink.messageSink == null)
				{
					EnvoyTerminatorSink envoyTerminatorSink = new EnvoyTerminatorSink();
					object obj = EnvoyTerminatorSink.staticSyncObject;
					lock (obj)
					{
						if (EnvoyTerminatorSink.messageSink == null)
						{
							EnvoyTerminatorSink.messageSink = envoyTerminatorSink;
						}
					}
				}
				return EnvoyTerminatorSink.messageSink;
			}
		}

		// Token: 0x06005C7F RID: 23679 RVA: 0x0014492C File Offset: 0x00142B2C
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage reqMsg)
		{
			IMessage message = InternalSink.ValidateMessage(reqMsg);
			if (message != null)
			{
				return message;
			}
			return Thread.CurrentContext.GetClientContextChain().SyncProcessMessage(reqMsg);
		}

		// Token: 0x06005C80 RID: 23680 RVA: 0x00144958 File Offset: 0x00142B58
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
				result = Thread.CurrentContext.GetClientContextChain().AsyncProcessMessage(reqMsg, replySink);
			}
			return result;
		}

		// Token: 0x17000FE2 RID: 4066
		// (get) Token: 0x06005C81 RID: 23681 RVA: 0x00144991 File Offset: 0x00142B91
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x040029C8 RID: 10696
		private static volatile EnvoyTerminatorSink messageSink;

		// Token: 0x040029C9 RID: 10697
		private static object staticSyncObject = new object();
	}
}
