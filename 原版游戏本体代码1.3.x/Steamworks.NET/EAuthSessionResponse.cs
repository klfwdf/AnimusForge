using System;

namespace Steamworks
{
	// Token: 0x02000147 RID: 327
	public enum EAuthSessionResponse
	{
		// Token: 0x040007FF RID: 2047
		k_EAuthSessionResponseOK,
		// Token: 0x04000800 RID: 2048
		k_EAuthSessionResponseUserNotConnectedToSteam,
		// Token: 0x04000801 RID: 2049
		k_EAuthSessionResponseNoLicenseOrExpired,
		// Token: 0x04000802 RID: 2050
		k_EAuthSessionResponseVACBanned,
		// Token: 0x04000803 RID: 2051
		k_EAuthSessionResponseLoggedInElseWhere,
		// Token: 0x04000804 RID: 2052
		k_EAuthSessionResponseVACCheckTimedOut,
		// Token: 0x04000805 RID: 2053
		k_EAuthSessionResponseAuthTicketCanceled,
		// Token: 0x04000806 RID: 2054
		k_EAuthSessionResponseAuthTicketInvalidAlreadyUsed,
		// Token: 0x04000807 RID: 2055
		k_EAuthSessionResponseAuthTicketInvalid,
		// Token: 0x04000808 RID: 2056
		k_EAuthSessionResponsePublisherIssuedBan
	}
}
