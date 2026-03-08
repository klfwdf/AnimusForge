using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200016D RID: 365
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamUGCDetails_t
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000869 RID: 2153 RVA: 0x0000BFAB File Offset: 0x0000A1AB
		// (set) Token: 0x0600086A RID: 2154 RVA: 0x0000BFB8 File Offset: 0x0000A1B8
		public string m_rgchTitle
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchTitle_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchTitle_, 129);
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600086B RID: 2155 RVA: 0x0000BFCB File Offset: 0x0000A1CB
		// (set) Token: 0x0600086C RID: 2156 RVA: 0x0000BFD8 File Offset: 0x0000A1D8
		public string m_rgchDescription
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchDescription_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchDescription_, 8000);
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600086D RID: 2157 RVA: 0x0000BFEB File Offset: 0x0000A1EB
		// (set) Token: 0x0600086E RID: 2158 RVA: 0x0000BFF8 File Offset: 0x0000A1F8
		public string m_rgchTags
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchTags_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchTags_, 1025);
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600086F RID: 2159 RVA: 0x0000C00B File Offset: 0x0000A20B
		// (set) Token: 0x06000870 RID: 2160 RVA: 0x0000C018 File Offset: 0x0000A218
		public string m_pchFileName
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_pchFileName_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_pchFileName_, 260);
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000871 RID: 2161 RVA: 0x0000C02B File Offset: 0x0000A22B
		// (set) Token: 0x06000872 RID: 2162 RVA: 0x0000C038 File Offset: 0x0000A238
		public string m_rgchURL
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchURL_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchURL_, 256);
			}
		}

		// Token: 0x0400099C RID: 2460
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x0400099D RID: 2461
		public EResult m_eResult;

		// Token: 0x0400099E RID: 2462
		public EWorkshopFileType m_eFileType;

		// Token: 0x0400099F RID: 2463
		public AppId_t m_nCreatorAppID;

		// Token: 0x040009A0 RID: 2464
		public AppId_t m_nConsumerAppID;

		// Token: 0x040009A1 RID: 2465
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 129)]
		private byte[] m_rgchTitle_;

		// Token: 0x040009A2 RID: 2466
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8000)]
		private byte[] m_rgchDescription_;

		// Token: 0x040009A3 RID: 2467
		public ulong m_ulSteamIDOwner;

		// Token: 0x040009A4 RID: 2468
		public uint m_rtimeCreated;

		// Token: 0x040009A5 RID: 2469
		public uint m_rtimeUpdated;

		// Token: 0x040009A6 RID: 2470
		public uint m_rtimeAddedToUserList;

		// Token: 0x040009A7 RID: 2471
		public ERemoteStoragePublishedFileVisibility m_eVisibility;

		// Token: 0x040009A8 RID: 2472
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bBanned;

		// Token: 0x040009A9 RID: 2473
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAcceptedForUse;

		// Token: 0x040009AA RID: 2474
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bTagsTruncated;

		// Token: 0x040009AB RID: 2475
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1025)]
		private byte[] m_rgchTags_;

		// Token: 0x040009AC RID: 2476
		public UGCHandle_t m_hFile;

		// Token: 0x040009AD RID: 2477
		public UGCHandle_t m_hPreviewFile;

		// Token: 0x040009AE RID: 2478
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
		private byte[] m_pchFileName_;

		// Token: 0x040009AF RID: 2479
		public int m_nFileSize;

		// Token: 0x040009B0 RID: 2480
		public int m_nPreviewFileSize;

		// Token: 0x040009B1 RID: 2481
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private byte[] m_rgchURL_;

		// Token: 0x040009B2 RID: 2482
		public uint m_unVotesUp;

		// Token: 0x040009B3 RID: 2483
		public uint m_unVotesDown;

		// Token: 0x040009B4 RID: 2484
		public float m_flScore;

		// Token: 0x040009B5 RID: 2485
		public uint m_unNumChildren;
	}
}
