using System;

namespace Steamworks
{
	// Token: 0x0200014F RID: 335
	[Flags]
	public enum EMarketNotAllowedReasonFlags
	{
		// Token: 0x04000858 RID: 2136
		k_EMarketNotAllowedReason_None = 0,
		// Token: 0x04000859 RID: 2137
		k_EMarketNotAllowedReason_TemporaryFailure = 1,
		// Token: 0x0400085A RID: 2138
		k_EMarketNotAllowedReason_AccountDisabled = 2,
		// Token: 0x0400085B RID: 2139
		k_EMarketNotAllowedReason_AccountLockedDown = 4,
		// Token: 0x0400085C RID: 2140
		k_EMarketNotAllowedReason_AccountLimited = 8,
		// Token: 0x0400085D RID: 2141
		k_EMarketNotAllowedReason_TradeBanned = 16,
		// Token: 0x0400085E RID: 2142
		k_EMarketNotAllowedReason_AccountNotTrusted = 32,
		// Token: 0x0400085F RID: 2143
		k_EMarketNotAllowedReason_SteamGuardNotEnabled = 64,
		// Token: 0x04000860 RID: 2144
		k_EMarketNotAllowedReason_SteamGuardOnlyRecentlyEnabled = 128,
		// Token: 0x04000861 RID: 2145
		k_EMarketNotAllowedReason_RecentPasswordReset = 256,
		// Token: 0x04000862 RID: 2146
		k_EMarketNotAllowedReason_NewPaymentMethod = 512,
		// Token: 0x04000863 RID: 2147
		k_EMarketNotAllowedReason_InvalidCookie = 1024,
		// Token: 0x04000864 RID: 2148
		k_EMarketNotAllowedReason_UsingNewDevice = 2048,
		// Token: 0x04000865 RID: 2149
		k_EMarketNotAllowedReason_RecentSelfRefund = 4096,
		// Token: 0x04000866 RID: 2150
		k_EMarketNotAllowedReason_NewPaymentMethodCannotBeVerified = 8192,
		// Token: 0x04000867 RID: 2151
		k_EMarketNotAllowedReason_NoRecentPurchases = 16384,
		// Token: 0x04000868 RID: 2152
		k_EMarketNotAllowedReason_AcceptedWalletGift = 32768
	}
}
