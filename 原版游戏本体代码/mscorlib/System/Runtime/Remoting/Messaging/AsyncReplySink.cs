using System;
using System.Runtime.Remoting.Contexts;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000884 RID: 2180
	internal class AsyncReplySink : IMessageSink
	{
		// Token: 0x06005C8D RID: 23693 RVA: 0x00144C62 File Offset: 0x00142E62
		internal AsyncReplySink(IMessageSink replySink, Context cliCtx)
		{
			this._replySink = replySink;
			this._cliCtx = cliCtx;
		}

		// Token: 0x06005C8E RID: 23694 RVA: 0x00144C78 File Offset: 0x00142E78
		[SecurityCritical]
		internal static object SyncProcessMessageCallback(object[] args)
		{
			IMessage msg = (IMessage)args[0];
			IMessageSink messageSink = (IMessageSink)args[1];
			Thread.CurrentContext.NotifyDynamicSinks(msg, true, false, true, true);
			return messageSink.SyncProcessMessage(msg);
		}

		// Token: 0x06005C8F RID: 23695 RVA: 0x00144CB0 File Offset: 0x00142EB0
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage reqMsg)
		{
			IMessage result = null;
			if (this._replySink != null)
			{
				object[] args = new object[] { reqMsg, this._replySink };
				InternalCrossContextDelegate ftnToCall = new InternalCrossContextDelegate(AsyncReplySink.SyncProcessMessageCallback);
				result = (IMessage)Thread.CurrentThread.InternalCrossContextCallback(this._cliCtx, ftnToCall, args);
			}
			return result;
		}

		// Token: 0x06005C90 RID: 23696 RVA: 0x00144D01 File Offset: 0x00142F01
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage reqMsg, IMessageSink replySink)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000FE5 RID: 4069
		// (get) Token: 0x06005C91 RID: 23697 RVA: 0x00144D08 File Offset: 0x00142F08
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return this._replySink;
			}
		}

		// Token: 0x040029CC RID: 10700
		private IMessageSink _replySink;

		// Token: 0x040029CD RID: 10701
		private Context _cliCtx;
	}
}
