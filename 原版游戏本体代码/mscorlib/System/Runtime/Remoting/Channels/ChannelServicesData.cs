using System;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200082B RID: 2091
	internal class ChannelServicesData
	{
		// Token: 0x040028CF RID: 10447
		internal long remoteCalls;

		// Token: 0x040028D0 RID: 10448
		internal CrossContextChannel xctxmessageSink;

		// Token: 0x040028D1 RID: 10449
		internal CrossAppDomainChannel xadmessageSink;

		// Token: 0x040028D2 RID: 10450
		internal bool fRegisterWellKnownChannels;
	}
}
