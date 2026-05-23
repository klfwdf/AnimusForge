using System;

namespace Steamworks
{
	// Token: 0x02000153 RID: 339
	public enum EGameSearchErrorCode_t
	{
		// Token: 0x0400087F RID: 2175
		k_EGameSearchErrorCode_OK = 1,
		// Token: 0x04000880 RID: 2176
		k_EGameSearchErrorCode_Failed_Search_Already_In_Progress,
		// Token: 0x04000881 RID: 2177
		k_EGameSearchErrorCode_Failed_No_Search_In_Progress,
		// Token: 0x04000882 RID: 2178
		k_EGameSearchErrorCode_Failed_Not_Lobby_Leader,
		// Token: 0x04000883 RID: 2179
		k_EGameSearchErrorCode_Failed_No_Host_Available,
		// Token: 0x04000884 RID: 2180
		k_EGameSearchErrorCode_Failed_Search_Params_Invalid,
		// Token: 0x04000885 RID: 2181
		k_EGameSearchErrorCode_Failed_Offline,
		// Token: 0x04000886 RID: 2182
		k_EGameSearchErrorCode_Failed_NotAuthorized,
		// Token: 0x04000887 RID: 2183
		k_EGameSearchErrorCode_Failed_Unknown_Error
	}
}
