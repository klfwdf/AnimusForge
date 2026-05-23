using System;
using System.Collections.Generic;

namespace Steamworks
{
	// Token: 0x0200001F RID: 31
	public static class SteamRemoteStorage
	{
		// Token: 0x0600037D RID: 893 RVA: 0x00009384 File Offset: 0x00007584
		public static bool FileWrite(string pchFile, byte[] pvData, int cubData)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_FileWrite(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle, pvData, cubData);
			}
			return result;
		}

		// Token: 0x0600037E RID: 894 RVA: 0x000093C8 File Offset: 0x000075C8
		public static int FileRead(string pchFile, byte[] pvData, int cubDataToRead)
		{
			InteropHelp.TestIfAvailableClient();
			int result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_FileRead(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle, pvData, cubDataToRead);
			}
			return result;
		}

		// Token: 0x0600037F RID: 895 RVA: 0x0000940C File Offset: 0x0000760C
		public static SteamAPICall_t FileWriteAsync(string pchFile, byte[] pvData, uint cubData)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_FileWriteAsync(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle, pvData, cubData);
			}
			return result;
		}

		// Token: 0x06000380 RID: 896 RVA: 0x00009458 File Offset: 0x00007658
		public static SteamAPICall_t FileReadAsync(string pchFile, uint nOffset, uint cubToRead)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_FileReadAsync(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle, nOffset, cubToRead);
			}
			return result;
		}

		// Token: 0x06000381 RID: 897 RVA: 0x000094A4 File Offset: 0x000076A4
		public static bool FileReadAsyncComplete(SteamAPICall_t hReadCall, byte[] pvBuffer, uint cubToRead)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_FileReadAsyncComplete(CSteamAPIContext.GetSteamRemoteStorage(), hReadCall, pvBuffer, cubToRead);
		}

		// Token: 0x06000382 RID: 898 RVA: 0x000094B8 File Offset: 0x000076B8
		public static bool FileForget(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_FileForget(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000383 RID: 899 RVA: 0x000094FC File Offset: 0x000076FC
		public static bool FileDelete(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_FileDelete(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000384 RID: 900 RVA: 0x00009540 File Offset: 0x00007740
		public static SteamAPICall_t FileShare(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_FileShare(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000385 RID: 901 RVA: 0x00009588 File Offset: 0x00007788
		public static bool SetSyncPlatforms(string pchFile, ERemoteStoragePlatform eRemoteStoragePlatform)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_SetSyncPlatforms(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle, eRemoteStoragePlatform);
			}
			return result;
		}

		// Token: 0x06000386 RID: 902 RVA: 0x000095CC File Offset: 0x000077CC
		public static UGCFileWriteStreamHandle_t FileWriteStreamOpen(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			UGCFileWriteStreamHandle_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = (UGCFileWriteStreamHandle_t)NativeMethods.ISteamRemoteStorage_FileWriteStreamOpen(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000387 RID: 903 RVA: 0x00009614 File Offset: 0x00007814
		public static bool FileWriteStreamWriteChunk(UGCFileWriteStreamHandle_t writeHandle, byte[] pvData, int cubData)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_FileWriteStreamWriteChunk(CSteamAPIContext.GetSteamRemoteStorage(), writeHandle, pvData, cubData);
		}

		// Token: 0x06000388 RID: 904 RVA: 0x00009628 File Offset: 0x00007828
		public static bool FileWriteStreamClose(UGCFileWriteStreamHandle_t writeHandle)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_FileWriteStreamClose(CSteamAPIContext.GetSteamRemoteStorage(), writeHandle);
		}

		// Token: 0x06000389 RID: 905 RVA: 0x0000963A File Offset: 0x0000783A
		public static bool FileWriteStreamCancel(UGCFileWriteStreamHandle_t writeHandle)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_FileWriteStreamCancel(CSteamAPIContext.GetSteamRemoteStorage(), writeHandle);
		}

		// Token: 0x0600038A RID: 906 RVA: 0x0000964C File Offset: 0x0000784C
		public static bool FileExists(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_FileExists(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600038B RID: 907 RVA: 0x00009690 File Offset: 0x00007890
		public static bool FilePersisted(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_FilePersisted(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600038C RID: 908 RVA: 0x000096D4 File Offset: 0x000078D4
		public static int GetFileSize(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			int result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_GetFileSize(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600038D RID: 909 RVA: 0x00009718 File Offset: 0x00007918
		public static long GetFileTimestamp(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			long result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_GetFileTimestamp(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600038E RID: 910 RVA: 0x0000975C File Offset: 0x0000795C
		public static ERemoteStoragePlatform GetSyncPlatforms(string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			ERemoteStoragePlatform result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_GetSyncPlatforms(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600038F RID: 911 RVA: 0x000097A0 File Offset: 0x000079A0
		public static int GetFileCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_GetFileCount(CSteamAPIContext.GetSteamRemoteStorage());
		}

		// Token: 0x06000390 RID: 912 RVA: 0x000097B1 File Offset: 0x000079B1
		public static string GetFileNameAndSize(int iFile, out int pnFileSizeInBytes)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamRemoteStorage_GetFileNameAndSize(CSteamAPIContext.GetSteamRemoteStorage(), iFile, out pnFileSizeInBytes));
		}

		// Token: 0x06000391 RID: 913 RVA: 0x000097C9 File Offset: 0x000079C9
		public static bool GetQuota(out ulong pnTotalBytes, out ulong puAvailableBytes)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_GetQuota(CSteamAPIContext.GetSteamRemoteStorage(), out pnTotalBytes, out puAvailableBytes);
		}

		// Token: 0x06000392 RID: 914 RVA: 0x000097DC File Offset: 0x000079DC
		public static bool IsCloudEnabledForAccount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_IsCloudEnabledForAccount(CSteamAPIContext.GetSteamRemoteStorage());
		}

		// Token: 0x06000393 RID: 915 RVA: 0x000097ED File Offset: 0x000079ED
		public static bool IsCloudEnabledForApp()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_IsCloudEnabledForApp(CSteamAPIContext.GetSteamRemoteStorage());
		}

		// Token: 0x06000394 RID: 916 RVA: 0x000097FE File Offset: 0x000079FE
		public static void SetCloudEnabledForApp(bool bEnabled)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamRemoteStorage_SetCloudEnabledForApp(CSteamAPIContext.GetSteamRemoteStorage(), bEnabled);
		}

		// Token: 0x06000395 RID: 917 RVA: 0x00009810 File Offset: 0x00007A10
		public static SteamAPICall_t UGCDownload(UGCHandle_t hContent, uint unPriority)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_UGCDownload(CSteamAPIContext.GetSteamRemoteStorage(), hContent, unPriority);
		}

		// Token: 0x06000396 RID: 918 RVA: 0x00009828 File Offset: 0x00007A28
		public static bool GetUGCDownloadProgress(UGCHandle_t hContent, out int pnBytesDownloaded, out int pnBytesExpected)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_GetUGCDownloadProgress(CSteamAPIContext.GetSteamRemoteStorage(), hContent, out pnBytesDownloaded, out pnBytesExpected);
		}

		// Token: 0x06000397 RID: 919 RVA: 0x0000983C File Offset: 0x00007A3C
		public static bool GetUGCDetails(UGCHandle_t hContent, out AppId_t pnAppID, out string ppchName, out int pnFileSizeInBytes, out CSteamID pSteamIDOwner)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr nativeUtf;
			bool flag = NativeMethods.ISteamRemoteStorage_GetUGCDetails(CSteamAPIContext.GetSteamRemoteStorage(), hContent, out pnAppID, out nativeUtf, out pnFileSizeInBytes, out pSteamIDOwner);
			ppchName = (flag ? InteropHelp.PtrToStringUTF8(nativeUtf) : null);
			return flag;
		}

		// Token: 0x06000398 RID: 920 RVA: 0x0000986F File Offset: 0x00007A6F
		public static int UGCRead(UGCHandle_t hContent, byte[] pvData, int cubDataToRead, uint cOffset, EUGCReadAction eAction)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_UGCRead(CSteamAPIContext.GetSteamRemoteStorage(), hContent, pvData, cubDataToRead, cOffset, eAction);
		}

		// Token: 0x06000399 RID: 921 RVA: 0x00009886 File Offset: 0x00007A86
		public static int GetCachedUGCCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_GetCachedUGCCount(CSteamAPIContext.GetSteamRemoteStorage());
		}

		// Token: 0x0600039A RID: 922 RVA: 0x00009897 File Offset: 0x00007A97
		public static UGCHandle_t GetCachedUGCHandle(int iCachedContent)
		{
			InteropHelp.TestIfAvailableClient();
			return (UGCHandle_t)NativeMethods.ISteamRemoteStorage_GetCachedUGCHandle(CSteamAPIContext.GetSteamRemoteStorage(), iCachedContent);
		}

		// Token: 0x0600039B RID: 923 RVA: 0x000098B0 File Offset: 0x00007AB0
		public static SteamAPICall_t PublishWorkshopFile(string pchFile, string pchPreviewFile, AppId_t nConsumerAppId, string pchTitle, string pchDescription, ERemoteStoragePublishedFileVisibility eVisibility, IList<string> pTags, EWorkshopFileType eWorkshopFileType)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchPreviewFile))
				{
					using (InteropHelp.UTF8StringHandle utf8StringHandle3 = new InteropHelp.UTF8StringHandle(pchTitle))
					{
						using (InteropHelp.UTF8StringHandle utf8StringHandle4 = new InteropHelp.UTF8StringHandle(pchDescription))
						{
							result = (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_PublishWorkshopFile(CSteamAPIContext.GetSteamRemoteStorage(), utf8StringHandle, utf8StringHandle2, nConsumerAppId, utf8StringHandle3, utf8StringHandle4, eVisibility, new InteropHelp.SteamParamStringArray(pTags), eWorkshopFileType);
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600039C RID: 924 RVA: 0x00009968 File Offset: 0x00007B68
		public static PublishedFileUpdateHandle_t CreatePublishedFileUpdateRequest(PublishedFileId_t unPublishedFileId)
		{
			InteropHelp.TestIfAvailableClient();
			return (PublishedFileUpdateHandle_t)NativeMethods.ISteamRemoteStorage_CreatePublishedFileUpdateRequest(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId);
		}

		// Token: 0x0600039D RID: 925 RVA: 0x00009980 File Offset: 0x00007B80
		public static bool UpdatePublishedFileFile(PublishedFileUpdateHandle_t updateHandle, string pchFile)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchFile))
			{
				result = NativeMethods.ISteamRemoteStorage_UpdatePublishedFileFile(CSteamAPIContext.GetSteamRemoteStorage(), updateHandle, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600039E RID: 926 RVA: 0x000099C4 File Offset: 0x00007BC4
		public static bool UpdatePublishedFilePreviewFile(PublishedFileUpdateHandle_t updateHandle, string pchPreviewFile)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchPreviewFile))
			{
				result = NativeMethods.ISteamRemoteStorage_UpdatePublishedFilePreviewFile(CSteamAPIContext.GetSteamRemoteStorage(), updateHandle, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600039F RID: 927 RVA: 0x00009A08 File Offset: 0x00007C08
		public static bool UpdatePublishedFileTitle(PublishedFileUpdateHandle_t updateHandle, string pchTitle)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchTitle))
			{
				result = NativeMethods.ISteamRemoteStorage_UpdatePublishedFileTitle(CSteamAPIContext.GetSteamRemoteStorage(), updateHandle, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x060003A0 RID: 928 RVA: 0x00009A4C File Offset: 0x00007C4C
		public static bool UpdatePublishedFileDescription(PublishedFileUpdateHandle_t updateHandle, string pchDescription)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchDescription))
			{
				result = NativeMethods.ISteamRemoteStorage_UpdatePublishedFileDescription(CSteamAPIContext.GetSteamRemoteStorage(), updateHandle, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x060003A1 RID: 929 RVA: 0x00009A90 File Offset: 0x00007C90
		public static bool UpdatePublishedFileVisibility(PublishedFileUpdateHandle_t updateHandle, ERemoteStoragePublishedFileVisibility eVisibility)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_UpdatePublishedFileVisibility(CSteamAPIContext.GetSteamRemoteStorage(), updateHandle, eVisibility);
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x00009AA3 File Offset: 0x00007CA3
		public static bool UpdatePublishedFileTags(PublishedFileUpdateHandle_t updateHandle, IList<string> pTags)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_UpdatePublishedFileTags(CSteamAPIContext.GetSteamRemoteStorage(), updateHandle, new InteropHelp.SteamParamStringArray(pTags));
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x00009AC0 File Offset: 0x00007CC0
		public static SteamAPICall_t CommitPublishedFileUpdate(PublishedFileUpdateHandle_t updateHandle)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_CommitPublishedFileUpdate(CSteamAPIContext.GetSteamRemoteStorage(), updateHandle);
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x00009AD7 File Offset: 0x00007CD7
		public static SteamAPICall_t GetPublishedFileDetails(PublishedFileId_t unPublishedFileId, uint unMaxSecondsOld)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_GetPublishedFileDetails(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId, unMaxSecondsOld);
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x00009AEF File Offset: 0x00007CEF
		public static SteamAPICall_t DeletePublishedFile(PublishedFileId_t unPublishedFileId)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_DeletePublishedFile(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId);
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x00009B06 File Offset: 0x00007D06
		public static SteamAPICall_t EnumerateUserPublishedFiles(uint unStartIndex)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_EnumerateUserPublishedFiles(CSteamAPIContext.GetSteamRemoteStorage(), unStartIndex);
		}

		// Token: 0x060003A7 RID: 935 RVA: 0x00009B1D File Offset: 0x00007D1D
		public static SteamAPICall_t SubscribePublishedFile(PublishedFileId_t unPublishedFileId)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_SubscribePublishedFile(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId);
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x00009B34 File Offset: 0x00007D34
		public static SteamAPICall_t EnumerateUserSubscribedFiles(uint unStartIndex)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_EnumerateUserSubscribedFiles(CSteamAPIContext.GetSteamRemoteStorage(), unStartIndex);
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x00009B4B File Offset: 0x00007D4B
		public static SteamAPICall_t UnsubscribePublishedFile(PublishedFileId_t unPublishedFileId)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_UnsubscribePublishedFile(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId);
		}

		// Token: 0x060003AA RID: 938 RVA: 0x00009B64 File Offset: 0x00007D64
		public static bool UpdatePublishedFileSetChangeDescription(PublishedFileUpdateHandle_t updateHandle, string pchChangeDescription)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchChangeDescription))
			{
				result = NativeMethods.ISteamRemoteStorage_UpdatePublishedFileSetChangeDescription(CSteamAPIContext.GetSteamRemoteStorage(), updateHandle, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x060003AB RID: 939 RVA: 0x00009BA8 File Offset: 0x00007DA8
		public static SteamAPICall_t GetPublishedItemVoteDetails(PublishedFileId_t unPublishedFileId)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_GetPublishedItemVoteDetails(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId);
		}

		// Token: 0x060003AC RID: 940 RVA: 0x00009BBF File Offset: 0x00007DBF
		public static SteamAPICall_t UpdateUserPublishedItemVote(PublishedFileId_t unPublishedFileId, bool bVoteUp)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_UpdateUserPublishedItemVote(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId, bVoteUp);
		}

		// Token: 0x060003AD RID: 941 RVA: 0x00009BD7 File Offset: 0x00007DD7
		public static SteamAPICall_t GetUserPublishedItemVoteDetails(PublishedFileId_t unPublishedFileId)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_GetUserPublishedItemVoteDetails(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId);
		}

		// Token: 0x060003AE RID: 942 RVA: 0x00009BEE File Offset: 0x00007DEE
		public static SteamAPICall_t EnumerateUserSharedWorkshopFiles(CSteamID steamId, uint unStartIndex, IList<string> pRequiredTags, IList<string> pExcludedTags)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_EnumerateUserSharedWorkshopFiles(CSteamAPIContext.GetSteamRemoteStorage(), steamId, unStartIndex, new InteropHelp.SteamParamStringArray(pRequiredTags), new InteropHelp.SteamParamStringArray(pExcludedTags));
		}

		// Token: 0x060003AF RID: 943 RVA: 0x00009C1C File Offset: 0x00007E1C
		public static SteamAPICall_t PublishVideo(EWorkshopVideoProvider eVideoProvider, string pchVideoAccount, string pchVideoIdentifier, string pchPreviewFile, AppId_t nConsumerAppId, string pchTitle, string pchDescription, ERemoteStoragePublishedFileVisibility eVisibility, IList<string> pTags)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchVideoAccount))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchVideoIdentifier))
				{
					using (InteropHelp.UTF8StringHandle utf8StringHandle3 = new InteropHelp.UTF8StringHandle(pchPreviewFile))
					{
						using (InteropHelp.UTF8StringHandle utf8StringHandle4 = new InteropHelp.UTF8StringHandle(pchTitle))
						{
							using (InteropHelp.UTF8StringHandle utf8StringHandle5 = new InteropHelp.UTF8StringHandle(pchDescription))
							{
								result = (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_PublishVideo(CSteamAPIContext.GetSteamRemoteStorage(), eVideoProvider, utf8StringHandle, utf8StringHandle2, utf8StringHandle3, nConsumerAppId, utf8StringHandle4, utf8StringHandle5, eVisibility, new InteropHelp.SteamParamStringArray(pTags));
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00009CF4 File Offset: 0x00007EF4
		public static SteamAPICall_t SetUserPublishedFileAction(PublishedFileId_t unPublishedFileId, EWorkshopFileAction eAction)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_SetUserPublishedFileAction(CSteamAPIContext.GetSteamRemoteStorage(), unPublishedFileId, eAction);
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x00009D0C File Offset: 0x00007F0C
		public static SteamAPICall_t EnumeratePublishedFilesByUserAction(EWorkshopFileAction eAction, uint unStartIndex)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_EnumeratePublishedFilesByUserAction(CSteamAPIContext.GetSteamRemoteStorage(), eAction, unStartIndex);
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x00009D24 File Offset: 0x00007F24
		public static SteamAPICall_t EnumeratePublishedWorkshopFiles(EWorkshopEnumerationType eEnumerationType, uint unStartIndex, uint unCount, uint unDays, IList<string> pTags, IList<string> pUserTags)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_EnumeratePublishedWorkshopFiles(CSteamAPIContext.GetSteamRemoteStorage(), eEnumerationType, unStartIndex, unCount, unDays, new InteropHelp.SteamParamStringArray(pTags), new InteropHelp.SteamParamStringArray(pUserTags));
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x00009D58 File Offset: 0x00007F58
		public static SteamAPICall_t UGCDownloadToLocation(UGCHandle_t hContent, string pchLocation, uint unPriority)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchLocation))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamRemoteStorage_UGCDownloadToLocation(CSteamAPIContext.GetSteamRemoteStorage(), hContent, utf8StringHandle, unPriority);
			}
			return result;
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x00009DA4 File Offset: 0x00007FA4
		public static int GetLocalFileChangeCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_GetLocalFileChangeCount(CSteamAPIContext.GetSteamRemoteStorage());
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x00009DB5 File Offset: 0x00007FB5
		public static string GetLocalFileChange(int iFile, out ERemoteStorageLocalFileChange pEChangeType, out ERemoteStorageFilePathType pEFilePathType)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamRemoteStorage_GetLocalFileChange(CSteamAPIContext.GetSteamRemoteStorage(), iFile, out pEChangeType, out pEFilePathType));
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x00009DCE File Offset: 0x00007FCE
		public static bool BeginFileWriteBatch()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_BeginFileWriteBatch(CSteamAPIContext.GetSteamRemoteStorage());
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x00009DDF File Offset: 0x00007FDF
		public static bool EndFileWriteBatch()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamRemoteStorage_EndFileWriteBatch(CSteamAPIContext.GetSteamRemoteStorage());
		}
	}
}
