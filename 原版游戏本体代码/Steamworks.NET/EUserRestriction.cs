using System;

namespace Steamworks
{
	// Token: 0x02000100 RID: 256
	public enum EUserRestriction
	{
		// Token: 0x040003CC RID: 972
		k_nUserRestrictionNone,
		// Token: 0x040003CD RID: 973
		k_nUserRestrictionUnknown,
		// Token: 0x040003CE RID: 974
		k_nUserRestrictionAnyChat,
		// Token: 0x040003CF RID: 975
		k_nUserRestrictionVoiceChat = 4,
		// Token: 0x040003D0 RID: 976
		k_nUserRestrictionGroupChat = 8,
		// Token: 0x040003D1 RID: 977
		k_nUserRestrictionRating = 16,
		// Token: 0x040003D2 RID: 978
		k_nUserRestrictionGameInvites = 32,
		// Token: 0x040003D3 RID: 979
		k_nUserRestrictionTrading = 64
	}
}
