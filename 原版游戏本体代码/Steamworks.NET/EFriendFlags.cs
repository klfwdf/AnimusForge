using System;

namespace Steamworks
{
	// Token: 0x020000FF RID: 255
	[Flags]
	public enum EFriendFlags
	{
		// Token: 0x040003BF RID: 959
		k_EFriendFlagNone = 0,
		// Token: 0x040003C0 RID: 960
		k_EFriendFlagBlocked = 1,
		// Token: 0x040003C1 RID: 961
		k_EFriendFlagFriendshipRequested = 2,
		// Token: 0x040003C2 RID: 962
		k_EFriendFlagImmediate = 4,
		// Token: 0x040003C3 RID: 963
		k_EFriendFlagClanMember = 8,
		// Token: 0x040003C4 RID: 964
		k_EFriendFlagOnGameServer = 16,
		// Token: 0x040003C5 RID: 965
		k_EFriendFlagRequestingFriendship = 128,
		// Token: 0x040003C6 RID: 966
		k_EFriendFlagRequestingInfo = 256,
		// Token: 0x040003C7 RID: 967
		k_EFriendFlagIgnored = 512,
		// Token: 0x040003C8 RID: 968
		k_EFriendFlagIgnoredFriend = 1024,
		// Token: 0x040003C9 RID: 969
		k_EFriendFlagChatMember = 4096,
		// Token: 0x040003CA RID: 970
		k_EFriendFlagAll = 65535
	}
}
