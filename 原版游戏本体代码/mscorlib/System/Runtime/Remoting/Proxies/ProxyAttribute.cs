using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Contexts;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Proxies
{
	// Token: 0x020007FD RID: 2045
	[SecurityCritical]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	public class ProxyAttribute : Attribute, IContextAttribute
	{
		// Token: 0x0600582C RID: 22572 RVA: 0x00136B90 File Offset: 0x00134D90
		[SecurityCritical]
		public virtual MarshalByRefObject CreateInstance(Type serverType)
		{
			if (serverType == null)
			{
				throw new ArgumentNullException("serverType");
			}
			RuntimeType runtimeType = serverType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			if (!serverType.IsContextful)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Activation_MBR_ProxyAttribute"));
			}
			if (serverType.IsAbstract)
			{
				throw new RemotingException(Environment.GetResourceString("Acc_CreateAbst"));
			}
			return this.CreateInstanceInternal(runtimeType);
		}

		// Token: 0x0600582D RID: 22573 RVA: 0x00136C08 File Offset: 0x00134E08
		internal MarshalByRefObject CreateInstanceInternal(RuntimeType serverType)
		{
			return ActivationServices.CreateInstance(serverType);
		}

		// Token: 0x0600582E RID: 22574 RVA: 0x00136C10 File Offset: 0x00134E10
		[SecurityCritical]
		public virtual RealProxy CreateProxy(ObjRef objRef, Type serverType, object serverObject, Context serverContext)
		{
			RemotingProxy remotingProxy = new RemotingProxy(serverType);
			if (serverContext != null)
			{
				RealProxy.SetStubData(remotingProxy, serverContext.InternalContextID);
			}
			if (objRef != null && objRef.GetServerIdentity().IsAllocated)
			{
				remotingProxy.SetSrvInfo(objRef.GetServerIdentity(), objRef.GetDomainID());
			}
			remotingProxy.Initialized = true;
			if (!serverType.IsContextful && !serverType.IsMarshalByRef && serverContext != null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Activation_MBR_ProxyAttribute"));
			}
			return remotingProxy;
		}

		// Token: 0x0600582F RID: 22575 RVA: 0x00136C8D File Offset: 0x00134E8D
		[SecurityCritical]
		[ComVisible(true)]
		public bool IsContextOK(Context ctx, IConstructionCallMessage msg)
		{
			return true;
		}

		// Token: 0x06005830 RID: 22576 RVA: 0x00136C90 File Offset: 0x00134E90
		[SecurityCritical]
		[ComVisible(true)]
		public void GetPropertiesForNewContext(IConstructionCallMessage msg)
		{
		}
	}
}
