using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000C9 RID: 201
	[CallbackIdentity(3407)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct UserFavoriteItemsListChanged_t
	{
		// Token: 0x0400025D RID: 605
		public const int k_iCallback = 3407;

		// Token: 0x0400025E RID: 606
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x0400025F RID: 607
		public EResult m_eResult;

		// Token: 0x04000260 RID: 608
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bWasAddRequest;
	}
}
