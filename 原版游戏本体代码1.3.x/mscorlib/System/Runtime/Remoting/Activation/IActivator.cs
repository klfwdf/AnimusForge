using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000899 RID: 2201
	[ComVisible(true)]
	public interface IActivator
	{
		// Token: 0x17001006 RID: 4102
		// (get) Token: 0x06005D29 RID: 23849
		// (set) Token: 0x06005D2A RID: 23850
		IActivator NextActivator
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x06005D2B RID: 23851
		[SecurityCritical]
		IConstructionReturnMessage Activate(IConstructionCallMessage msg);

		// Token: 0x17001007 RID: 4103
		// (get) Token: 0x06005D2C RID: 23852
		ActivatorLevel Level
		{
			[SecurityCritical]
			get;
		}
	}
}
