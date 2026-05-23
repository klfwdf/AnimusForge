using System;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007BD RID: 1981
	internal class RedirectionProxy : MarshalByRefObject, IMessageSink
	{
		// Token: 0x060055CB RID: 21963 RVA: 0x00130CA0 File Offset: 0x0012EEA0
		[SecurityCritical]
		internal RedirectionProxy(MarshalByRefObject proxy, Type serverType)
		{
			this._proxy = proxy;
			this._realProxy = RemotingServices.GetRealProxy(this._proxy);
			this._serverType = serverType;
			this._objectMode = WellKnownObjectMode.Singleton;
		}

		// Token: 0x17000E22 RID: 3618
		// (set) Token: 0x060055CC RID: 21964 RVA: 0x00130CCE File Offset: 0x0012EECE
		public WellKnownObjectMode ObjectMode
		{
			set
			{
				this._objectMode = value;
			}
		}

		// Token: 0x060055CD RID: 21965 RVA: 0x00130CD8 File Offset: 0x0012EED8
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage msg)
		{
			IMessage result = null;
			try
			{
				msg.Properties["__Uri"] = this._realProxy.IdentityObject.URI;
				if (this._objectMode == WellKnownObjectMode.Singleton)
				{
					result = this._realProxy.Invoke(msg);
				}
				else
				{
					MarshalByRefObject proxy = (MarshalByRefObject)Activator.CreateInstance(this._serverType, true);
					RealProxy realProxy = RemotingServices.GetRealProxy(proxy);
					result = realProxy.Invoke(msg);
				}
			}
			catch (Exception e)
			{
				result = new ReturnMessage(e, msg as IMethodCallMessage);
			}
			return result;
		}

		// Token: 0x060055CE RID: 21966 RVA: 0x00130D64 File Offset: 0x0012EF64
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

		// Token: 0x17000E23 RID: 3619
		// (get) Token: 0x060055CF RID: 21967 RVA: 0x00130D87 File Offset: 0x0012EF87
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x04002777 RID: 10103
		private MarshalByRefObject _proxy;

		// Token: 0x04002778 RID: 10104
		[SecurityCritical]
		private RealProxy _realProxy;

		// Token: 0x04002779 RID: 10105
		private Type _serverType;

		// Token: 0x0400277A RID: 10106
		private WellKnownObjectMode _objectMode;
	}
}
