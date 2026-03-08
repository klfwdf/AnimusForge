using System;

namespace Steamworks
{
	// Token: 0x0200014B RID: 331
	public enum EChatRoomEnterResponse
	{
		// Token: 0x04000828 RID: 2088
		k_EChatRoomEnterResponseSuccess = 1,
		// Token: 0x04000829 RID: 2089
		k_EChatRoomEnterResponseDoesntExist,
		// Token: 0x0400082A RID: 2090
		k_EChatRoomEnterResponseNotAllowed,
		// Token: 0x0400082B RID: 2091
		k_EChatRoomEnterResponseFull,
		// Token: 0x0400082C RID: 2092
		k_EChatRoomEnterResponseError,
		// Token: 0x0400082D RID: 2093
		k_EChatRoomEnterResponseBanned,
		// Token: 0x0400082E RID: 2094
		k_EChatRoomEnterResponseLimited,
		// Token: 0x0400082F RID: 2095
		k_EChatRoomEnterResponseClanDisabled,
		// Token: 0x04000830 RID: 2096
		k_EChatRoomEnterResponseCommunityBan,
		// Token: 0x04000831 RID: 2097
		k_EChatRoomEnterResponseMemberBlockedYou,
		// Token: 0x04000832 RID: 2098
		k_EChatRoomEnterResponseYouBlockedMember,
		// Token: 0x04000833 RID: 2099
		k_EChatRoomEnterResponseRatelimitExceeded = 15
	}
}
