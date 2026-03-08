using System;

namespace Steamworks
{
	// Token: 0x02000146 RID: 326
	public enum EBeginAuthSessionResult
	{
		// Token: 0x040007F8 RID: 2040
		k_EBeginAuthSessionResultOK,
		// Token: 0x040007F9 RID: 2041
		k_EBeginAuthSessionResultInvalidTicket,
		// Token: 0x040007FA RID: 2042
		k_EBeginAuthSessionResultDuplicateRequest,
		// Token: 0x040007FB RID: 2043
		k_EBeginAuthSessionResultInvalidVersion,
		// Token: 0x040007FC RID: 2044
		k_EBeginAuthSessionResultGameMismatch,
		// Token: 0x040007FD RID: 2045
		k_EBeginAuthSessionResultExpiredTicket
	}
}
