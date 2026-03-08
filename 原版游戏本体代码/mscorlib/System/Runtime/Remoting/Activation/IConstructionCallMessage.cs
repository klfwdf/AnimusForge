using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x0200089B RID: 2203
	[ComVisible(true)]
	public interface IConstructionCallMessage : IMethodCallMessage, IMethodMessage, IMessage
	{
		// Token: 0x17001008 RID: 4104
		// (get) Token: 0x06005D2D RID: 23853
		// (set) Token: 0x06005D2E RID: 23854
		IActivator Activator
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x17001009 RID: 4105
		// (get) Token: 0x06005D2F RID: 23855
		object[] CallSiteActivationAttributes
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x1700100A RID: 4106
		// (get) Token: 0x06005D30 RID: 23856
		string ActivationTypeName
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x1700100B RID: 4107
		// (get) Token: 0x06005D31 RID: 23857
		Type ActivationType
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x1700100C RID: 4108
		// (get) Token: 0x06005D32 RID: 23858
		IList ContextProperties
		{
			[SecurityCritical]
			get;
		}
	}
}
