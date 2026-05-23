using System;

namespace Steamworks
{
	// Token: 0x0200011D RID: 285
	public enum EP2PSessionError
	{
		// Token: 0x04000652 RID: 1618
		k_EP2PSessionErrorNone,
		// Token: 0x04000653 RID: 1619
		k_EP2PSessionErrorNoRightsToApp = 2,
		// Token: 0x04000654 RID: 1620
		k_EP2PSessionErrorTimeout = 4,
		// Token: 0x04000655 RID: 1621
		k_EP2PSessionErrorNotRunningApp_DELETED = 1,
		// Token: 0x04000656 RID: 1622
		k_EP2PSessionErrorDestinationNotLoggedIn_DELETED = 3,
		// Token: 0x04000657 RID: 1623
		k_EP2PSessionErrorMax = 5
	}
}
