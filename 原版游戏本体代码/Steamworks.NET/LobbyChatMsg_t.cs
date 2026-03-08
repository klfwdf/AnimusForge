using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200007A RID: 122
	[CallbackIdentity(507)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LobbyChatMsg_t
	{
		// Token: 0x04000143 RID: 323
		public const int k_iCallback = 507;

		// Token: 0x04000144 RID: 324
		public ulong m_ulSteamIDLobby;

		// Token: 0x04000145 RID: 325
		public ulong m_ulSteamIDUser;

		// Token: 0x04000146 RID: 326
		public byte m_eChatEntryType;

		// Token: 0x04000147 RID: 327
		public uint m_iChatID;
	}
}
