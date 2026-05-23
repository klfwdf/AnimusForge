using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000B1 RID: 177
	[CallbackIdentity(1318)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageGetPublishedFileDetailsResult_t
	{
		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000853 RID: 2131 RVA: 0x0000BE4B File Offset: 0x0000A04B
		// (set) Token: 0x06000854 RID: 2132 RVA: 0x0000BE58 File Offset: 0x0000A058
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

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000855 RID: 2133 RVA: 0x0000BE6B File Offset: 0x0000A06B
		// (set) Token: 0x06000856 RID: 2134 RVA: 0x0000BE78 File Offset: 0x0000A078
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

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000857 RID: 2135 RVA: 0x0000BE8B File Offset: 0x0000A08B
		// (set) Token: 0x06000858 RID: 2136 RVA: 0x0000BE98 File Offset: 0x0000A098
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

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000859 RID: 2137 RVA: 0x0000BEAB File Offset: 0x0000A0AB
		// (set) Token: 0x0600085A RID: 2138 RVA: 0x0000BEB8 File Offset: 0x0000A0B8
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

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600085B RID: 2139 RVA: 0x0000BECB File Offset: 0x0000A0CB
		// (set) Token: 0x0600085C RID: 2140 RVA: 0x0000BED8 File Offset: 0x0000A0D8
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

		// Token: 0x040001EC RID: 492
		public const int k_iCallback = 1318;

		// Token: 0x040001ED RID: 493
		public EResult m_eResult;

		// Token: 0x040001EE RID: 494
		public PublishedFileId_t m_nPublishedFileId;

		// Token: 0x040001EF RID: 495
		public AppId_t m_nCreatorAppID;

		// Token: 0x040001F0 RID: 496
		public AppId_t m_nConsumerAppID;

		// Token: 0x040001F1 RID: 497
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 129)]
		private byte[] m_rgchTitle_;

		// Token: 0x040001F2 RID: 498
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8000)]
		private byte[] m_rgchDescription_;

		// Token: 0x040001F3 RID: 499
		public UGCHandle_t m_hFile;

		// Token: 0x040001F4 RID: 500
		public UGCHandle_t m_hPreviewFile;

		// Token: 0x040001F5 RID: 501
		public ulong m_ulSteamIDOwner;

		// Token: 0x040001F6 RID: 502
		public uint m_rtimeCreated;

		// Token: 0x040001F7 RID: 503
		public uint m_rtimeUpdated;

		// Token: 0x040001F8 RID: 504
		public ERemoteStoragePublishedFileVisibility m_eVisibility;

		// Token: 0x040001F9 RID: 505
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bBanned;

		// Token: 0x040001FA RID: 506
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1025)]
		private byte[] m_rgchTags_;

		// Token: 0x040001FB RID: 507
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bTagsTruncated;

		// Token: 0x040001FC RID: 508
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
		private byte[] m_pchFileName_;

		// Token: 0x040001FD RID: 509
		public int m_nFileSize;

		// Token: 0x040001FE RID: 510
		public int m_nPreviewFileSize;

		// Token: 0x040001FF RID: 511
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private byte[] m_rgchURL_;

		// Token: 0x04000200 RID: 512
		public EWorkshopFileType m_eFileType;

		// Token: 0x04000201 RID: 513
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAcceptedForUse;
	}
}
