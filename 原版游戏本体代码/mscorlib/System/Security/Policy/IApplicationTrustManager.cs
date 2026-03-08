using System;
using System.Runtime.InteropServices;

namespace System.Security.Policy
{
	// Token: 0x0200035B RID: 859
	[ComVisible(true)]
	public interface IApplicationTrustManager : ISecurityEncodable
	{
		// Token: 0x06002A7C RID: 10876
		ApplicationTrust DetermineApplicationTrust(ActivationContext activationContext, TrustManagerContext context);
	}
}
