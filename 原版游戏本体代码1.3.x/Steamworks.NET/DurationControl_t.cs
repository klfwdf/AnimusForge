using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000E3 RID: 227
	[CallbackIdentity(167)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct DurationControl_t
	{
		// Token: 0x040002B7 RID: 695
		public const int k_iCallback = 167;

		// Token: 0x040002B8 RID: 696
		public EResult m_eResult;

		// Token: 0x040002B9 RID: 697
		public AppId_t m_appid;

		// Token: 0x040002BA RID: 698
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bApplicable;

		// Token: 0x040002BB RID: 699
		public int m_csecsLast5h;

		// Token: 0x040002BC RID: 700
		public EDurationControlProgress m_progress;

		// Token: 0x040002BD RID: 701
		public EDurationControlNotification m_notification;

		// Token: 0x040002BE RID: 702
		public int m_csecsToday;

		// Token: 0x040002BF RID: 703
		public int m_csecsRemaining;
	}
}
