using System;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000835 RID: 2101
	internal class AsyncWorkItem : IMessageSink
	{
		// Token: 0x060059CA RID: 22986 RVA: 0x0013CB0D File Offset: 0x0013AD0D
		[SecurityCritical]
		internal AsyncWorkItem(IMessageSink replySink, Context oldCtx)
			: this(null, replySink, oldCtx, null)
		{
		}

		// Token: 0x060059CB RID: 22987 RVA: 0x0013CB19 File Offset: 0x0013AD19
		[SecurityCritical]
		internal AsyncWorkItem(IMessage reqMsg, IMessageSink replySink, Context oldCtx, ServerIdentity srvID)
		{
			this._reqMsg = reqMsg;
			this._replySink = replySink;
			this._oldCtx = oldCtx;
			this._callCtx = Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext;
			this._srvID = srvID;
		}

		// Token: 0x060059CC RID: 22988 RVA: 0x0013CB54 File Offset: 0x0013AD54
		[SecurityCritical]
		internal static object SyncProcessMessageCallback(object[] args)
		{
			IMessageSink messageSink = (IMessageSink)args[0];
			IMessage msg = (IMessage)args[1];
			return messageSink.SyncProcessMessage(msg);
		}

		// Token: 0x060059CD RID: 22989 RVA: 0x0013CB7C File Offset: 0x0013AD7C
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage msg)
		{
			IMessage result = null;
			if (this._replySink != null)
			{
				Thread.CurrentContext.NotifyDynamicSinks(msg, false, false, true, true);
				object[] args = new object[] { this._replySink, msg };
				InternalCrossContextDelegate ftnToCall = new InternalCrossContextDelegate(AsyncWorkItem.SyncProcessMessageCallback);
				result = (IMessage)Thread.CurrentThread.InternalCrossContextCallback(this._oldCtx, ftnToCall, args);
			}
			return result;
		}

		// Token: 0x060059CE RID: 22990 RVA: 0x0013CBDD File Offset: 0x0013ADDD
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
		}

		// Token: 0x17000EE2 RID: 3810
		// (get) Token: 0x060059CF RID: 22991 RVA: 0x0013CBEE File Offset: 0x0013ADEE
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return this._replySink;
			}
		}

		// Token: 0x060059D0 RID: 22992 RVA: 0x0013CBF8 File Offset: 0x0013ADF8
		[SecurityCritical]
		internal static object FinishAsyncWorkCallback(object[] args)
		{
			AsyncWorkItem asyncWorkItem = (AsyncWorkItem)args[0];
			Context serverContext = asyncWorkItem._srvID.ServerContext;
			LogicalCallContext logicalCallContext = CallContext.SetLogicalCallContext(asyncWorkItem._callCtx);
			serverContext.NotifyDynamicSinks(asyncWorkItem._reqMsg, false, true, true, true);
			IMessageCtrl messageCtrl = serverContext.GetServerContextChain().AsyncProcessMessage(asyncWorkItem._reqMsg, asyncWorkItem);
			CallContext.SetLogicalCallContext(logicalCallContext);
			return null;
		}

		// Token: 0x060059D1 RID: 22993 RVA: 0x0013CC54 File Offset: 0x0013AE54
		[SecurityCritical]
		internal virtual void FinishAsyncWork(object stateIgnored)
		{
			InternalCrossContextDelegate ftnToCall = new InternalCrossContextDelegate(AsyncWorkItem.FinishAsyncWorkCallback);
			object[] args = new object[] { this };
			Thread.CurrentThread.InternalCrossContextCallback(this._srvID.ServerContext, ftnToCall, args);
		}

		// Token: 0x040028E1 RID: 10465
		private IMessageSink _replySink;

		// Token: 0x040028E2 RID: 10466
		private ServerIdentity _srvID;

		// Token: 0x040028E3 RID: 10467
		private Context _oldCtx;

		// Token: 0x040028E4 RID: 10468
		[SecurityCritical]
		private LogicalCallContext _callCtx;

		// Token: 0x040028E5 RID: 10469
		private IMessage _reqMsg;
	}
}
