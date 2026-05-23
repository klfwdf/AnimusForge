using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200003B RID: 59
	[CallbackIdentity(342)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct JoinClanChatRoomCompletionResult_t
	{
		// Token: 0x04000046 RID: 70
		public const int k_iCallback = 342;

		// Token: 0x04000047 RID: 71
		public CSteamID m_steamIDClanChat;

		// Token: 0x04000048 RID: 72
		public EChatRoomEnterResponse m_eChatRoomEnterResponse;
	}
}
