using System;
using System.Collections;
using System.Runtime.Remoting.Activation;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000881 RID: 2177
	[Serializable]
	internal class InternalSink
	{
		// Token: 0x06005C78 RID: 23672 RVA: 0x00144758 File Offset: 0x00142958
		[SecurityCritical]
		internal static IMessage ValidateMessage(IMessage reqMsg)
		{
			IMessage result = null;
			if (reqMsg == null)
			{
				result = new ReturnMessage(new ArgumentNullException("reqMsg"), null);
			}
			return result;
		}

		// Token: 0x06005C79 RID: 23673 RVA: 0x0014477C File Offset: 0x0014297C
		[SecurityCritical]
		internal static IMessage DisallowAsyncActivation(IMessage reqMsg)
		{
			if (reqMsg is IConstructionCallMessage)
			{
				return new ReturnMessage(new RemotingException(Environment.GetResourceString("Remoting_Activation_AsyncUnsupported")), null);
			}
			return null;
		}

		// Token: 0x06005C7A RID: 23674 RVA: 0x001447A0 File Offset: 0x001429A0
		[SecurityCritical]
		internal static Identity GetIdentity(IMessage reqMsg)
		{
			Identity identity = null;
			if (reqMsg is IInternalMessage)
			{
				identity = ((IInternalMessage)reqMsg).IdentityObject;
			}
			else if (reqMsg is InternalMessageWrapper)
			{
				identity = (Identity)((InternalMessageWrapper)reqMsg).GetIdentityObject();
			}
			if (identity == null)
			{
				string uri = InternalSink.GetURI(reqMsg);
				identity = IdentityHolder.ResolveIdentity(uri);
				if (identity == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Remoting_ServerObjectNotFound", new object[] { uri }));
				}
			}
			return identity;
		}

		// Token: 0x06005C7B RID: 23675 RVA: 0x00144810 File Offset: 0x00142A10
		[SecurityCritical]
		internal static ServerIdentity GetServerIdentity(IMessage reqMsg)
		{
			ServerIdentity serverIdentity = null;
			bool flag = false;
			IInternalMessage internalMessage = reqMsg as IInternalMessage;
			if (internalMessage != null)
			{
				serverIdentity = ((IInternalMessage)reqMsg).ServerIdentityObject;
				flag = true;
			}
			else if (reqMsg is InternalMessageWrapper)
			{
				serverIdentity = (ServerIdentity)((InternalMessageWrapper)reqMsg).GetServerIdentityObject();
			}
			if (serverIdentity == null)
			{
				string uri = InternalSink.GetURI(reqMsg);
				Identity identity = IdentityHolder.ResolveIdentity(uri);
				if (identity is ServerIdentity)
				{
					serverIdentity = (ServerIdentity)identity;
					if (flag)
					{
						internalMessage.ServerIdentityObject = serverIdentity;
					}
				}
			}
			return serverIdentity;
		}

		// Token: 0x06005C7C RID: 23676 RVA: 0x00144884 File Offset: 0x00142A84
		[SecurityCritical]
		internal static string GetURI(IMessage msg)
		{
			string result = null;
			IMethodMessage methodMessage = msg as IMethodMessage;
			if (methodMessage != null)
			{
				result = methodMessage.Uri;
			}
			else
			{
				IDictionary properties = msg.Properties;
				if (properties != null)
				{
					result = (string)properties["__Uri"];
				}
			}
			return result;
		}
	}
}
