using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Security;

namespace System.Runtime.Remoting.Services
{
	// Token: 0x02000805 RID: 2053
	[SecurityCritical]
	[ComVisible(true)]
	public sealed class EnterpriseServicesHelper
	{
		// Token: 0x0600586F RID: 22639 RVA: 0x00137F7C File Offset: 0x0013617C
		[SecurityCritical]
		public static object WrapIUnknownWithComObject(IntPtr punk)
		{
			return Marshal.InternalWrapIUnknownWithComObject(punk);
		}

		// Token: 0x06005870 RID: 22640 RVA: 0x00137F84 File Offset: 0x00136184
		[ComVisible(true)]
		public static IConstructionReturnMessage CreateConstructionReturnMessage(IConstructionCallMessage ctorMsg, MarshalByRefObject retObj)
		{
			return new ConstructorReturnMessage(retObj, null, 0, null, ctorMsg);
		}

		// Token: 0x06005871 RID: 22641 RVA: 0x00137FA0 File Offset: 0x001361A0
		[SecurityCritical]
		public static void SwitchWrappers(RealProxy oldcp, RealProxy newcp)
		{
			object transparentProxy = oldcp.GetTransparentProxy();
			object transparentProxy2 = newcp.GetTransparentProxy();
			IntPtr serverContextForProxy = RemotingServices.GetServerContextForProxy(transparentProxy);
			IntPtr serverContextForProxy2 = RemotingServices.GetServerContextForProxy(transparentProxy2);
			Marshal.InternalSwitchCCW(transparentProxy, transparentProxy2);
		}
	}
}
