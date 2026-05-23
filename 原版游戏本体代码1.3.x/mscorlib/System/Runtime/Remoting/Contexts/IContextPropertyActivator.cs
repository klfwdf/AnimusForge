using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200080E RID: 2062
	[ComVisible(true)]
	public interface IContextPropertyActivator
	{
		// Token: 0x060058C4 RID: 22724
		[SecurityCritical]
		bool IsOKToActivate(IConstructionCallMessage msg);

		// Token: 0x060058C5 RID: 22725
		[SecurityCritical]
		void CollectFromClientContext(IConstructionCallMessage msg);

		// Token: 0x060058C6 RID: 22726
		[SecurityCritical]
		bool DeliverClientContextToServerContext(IConstructionCallMessage msg);

		// Token: 0x060058C7 RID: 22727
		[SecurityCritical]
		void CollectFromServerContext(IConstructionReturnMessage msg);

		// Token: 0x060058C8 RID: 22728
		[SecurityCritical]
		bool DeliverServerContextToClientContext(IConstructionReturnMessage msg);
	}
}
