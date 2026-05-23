using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007BE RID: 1982
	internal class ComRedirectionProxy : MarshalByRefObject, IMessageSink
	{
		// Token: 0x060055D0 RID: 21968 RVA: 0x00130D8A File Offset: 0x0012EF8A
		internal ComRedirectionProxy(MarshalByRefObject comObject, Type serverType)
		{
			this._comObject = comObject;
			this._serverType = serverType;
		}

		// Token: 0x060055D1 RID: 21969 RVA: 0x00130DA0 File Offset: 0x0012EFA0
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage msg)
		{
			IMethodCallMessage reqMsg = (IMethodCallMessage)msg;
			IMethodReturnMessage methodReturnMessage = RemotingServices.ExecuteMessage(this._comObject, reqMsg);
			if (methodReturnMessage != null)
			{
				COMException ex = methodReturnMessage.Exception as COMException;
				if (ex != null && (ex._HResult == -2147023174 || ex._HResult == -2147023169))
				{
					this._comObject = (MarshalByRefObject)Activator.CreateInstance(this._serverType, true);
					methodReturnMessage = RemotingServices.ExecuteMessage(this._comObject, reqMsg);
				}
			}
			return methodReturnMessage;
		}

		// Token: 0x060055D2 RID: 21970 RVA: 0x00130E14 File Offset: 0x0012F014
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
		{
			IMessage msg2 = this.SyncProcessMessage(msg);
			if (replySink != null)
			{
				replySink.SyncProcessMessage(msg2);
			}
			return null;
		}

		// Token: 0x17000E24 RID: 3620
		// (get) Token: 0x060055D3 RID: 21971 RVA: 0x00130E37 File Offset: 0x0012F037
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x0400277B RID: 10107
		private MarshalByRefObject _comObject;

		// Token: 0x0400277C RID: 10108
		private Type _serverType;
	}
}
