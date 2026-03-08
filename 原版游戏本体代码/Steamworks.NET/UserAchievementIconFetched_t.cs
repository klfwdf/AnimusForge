using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000EC RID: 236
	[CallbackIdentity(1109)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct UserAchievementIconFetched_t
	{
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000865 RID: 2149 RVA: 0x0000BF6B File Offset: 0x0000A16B
		// (set) Token: 0x06000866 RID: 2150 RVA: 0x0000BF78 File Offset: 0x0000A178
		public string m_rgchAchievementName
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchAchievementName_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchAchievementName_, 128);
			}
		}

		// Token: 0x040002E0 RID: 736
		public const int k_iCallback = 1109;

		// Token: 0x040002E1 RID: 737
		public CGameID m_nGameID;

		// Token: 0x040002E2 RID: 738
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		private byte[] m_rgchAchievementName_;

		// Token: 0x040002E3 RID: 739
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAchieved;

		// Token: 0x040002E4 RID: 740
		public int m_nIconHandle;
	}
}
