using System;

namespace Steamworks
{
	// Token: 0x02000103 RID: 259
	[Flags]
	public enum EPersonaChange
	{
		// Token: 0x040003DC RID: 988
		k_EPersonaChangeName = 1,
		// Token: 0x040003DD RID: 989
		k_EPersonaChangeStatus = 2,
		// Token: 0x040003DE RID: 990
		k_EPersonaChangeComeOnline = 4,
		// Token: 0x040003DF RID: 991
		k_EPersonaChangeGoneOffline = 8,
		// Token: 0x040003E0 RID: 992
		k_EPersonaChangeGamePlayed = 16,
		// Token: 0x040003E1 RID: 993
		k_EPersonaChangeGameServer = 32,
		// Token: 0x040003E2 RID: 994
		k_EPersonaChangeAvatar = 64,
		// Token: 0x040003E3 RID: 995
		k_EPersonaChangeJoinedSource = 128,
		// Token: 0x040003E4 RID: 996
		k_EPersonaChangeLeftSource = 256,
		// Token: 0x040003E5 RID: 997
		k_EPersonaChangeRelationshipChanged = 512,
		// Token: 0x040003E6 RID: 998
		k_EPersonaChangeNameFirstSet = 1024,
		// Token: 0x040003E7 RID: 999
		k_EPersonaChangeBroadcast = 2048,
		// Token: 0x040003E8 RID: 1000
		k_EPersonaChangeNickname = 4096,
		// Token: 0x040003E9 RID: 1001
		k_EPersonaChangeSteamLevel = 8192,
		// Token: 0x040003EA RID: 1002
		k_EPersonaChangeRichPresence = 16384
	}
}
