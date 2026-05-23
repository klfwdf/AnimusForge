using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200080C RID: 2060
	[ComVisible(true)]
	public interface IContextAttribute
	{
		// Token: 0x060058BF RID: 22719
		[SecurityCritical]
		bool IsContextOK(Context ctx, IConstructionCallMessage msg);

		// Token: 0x060058C0 RID: 22720
		[SecurityCritical]
		void GetPropertiesForNewContext(IConstructionCallMessage msg);
	}
}
