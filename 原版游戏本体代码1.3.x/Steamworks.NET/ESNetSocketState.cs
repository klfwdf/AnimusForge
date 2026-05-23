using System;

namespace Steamworks
{
	// Token: 0x0200011F RID: 287
	public enum ESNetSocketState
	{
		// Token: 0x0400065E RID: 1630
		k_ESNetSocketStateInvalid,
		// Token: 0x0400065F RID: 1631
		k_ESNetSocketStateConnected,
		// Token: 0x04000660 RID: 1632
		k_ESNetSocketStateInitiated = 10,
		// Token: 0x04000661 RID: 1633
		k_ESNetSocketStateLocalCandidatesFound,
		// Token: 0x04000662 RID: 1634
		k_ESNetSocketStateReceivedRemoteCandidates,
		// Token: 0x04000663 RID: 1635
		k_ESNetSocketStateChallengeHandshake = 15,
		// Token: 0x04000664 RID: 1636
		k_ESNetSocketStateDisconnecting = 21,
		// Token: 0x04000665 RID: 1637
		k_ESNetSocketStateLocalDisconnect,
		// Token: 0x04000666 RID: 1638
		k_ESNetSocketStateTimeoutDuringConnect,
		// Token: 0x04000667 RID: 1639
		k_ESNetSocketStateRemoteEndDisconnected,
		// Token: 0x04000668 RID: 1640
		k_ESNetSocketStateConnectionBroken
	}
}
