using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000005 RID: 5
	public static class SteamFriends
	{
		// Token: 0x06000047 RID: 71 RVA: 0x00002C0C File Offset: 0x00000E0C
		public static string GetPersonaName()
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetPersonaName(CSteamAPIContext.GetSteamFriends()));
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00002C24 File Offset: 0x00000E24
		public static SteamAPICall_t SetPersonaName(string pchPersonaName)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchPersonaName))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamFriends_SetPersonaName(CSteamAPIContext.GetSteamFriends(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00002C6C File Offset: 0x00000E6C
		public static EPersonaState GetPersonaState()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetPersonaState(CSteamAPIContext.GetSteamFriends());
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00002C7D File Offset: 0x00000E7D
		public static int GetFriendCount(EFriendFlags iFriendFlags)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendCount(CSteamAPIContext.GetSteamFriends(), iFriendFlags);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00002C8F File Offset: 0x00000E8F
		public static CSteamID GetFriendByIndex(int iFriend, EFriendFlags iFriendFlags)
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamFriends_GetFriendByIndex(CSteamAPIContext.GetSteamFriends(), iFriend, iFriendFlags);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00002CA7 File Offset: 0x00000EA7
		public static EFriendRelationship GetFriendRelationship(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendRelationship(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00002CB9 File Offset: 0x00000EB9
		public static EPersonaState GetFriendPersonaState(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendPersonaState(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00002CCB File Offset: 0x00000ECB
		public static string GetFriendPersonaName(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetFriendPersonaName(CSteamAPIContext.GetSteamFriends(), steamIDFriend));
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00002CE2 File Offset: 0x00000EE2
		public static bool GetFriendGamePlayed(CSteamID steamIDFriend, out FriendGameInfo_t pFriendGameInfo)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendGamePlayed(CSteamAPIContext.GetSteamFriends(), steamIDFriend, out pFriendGameInfo);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00002CF5 File Offset: 0x00000EF5
		public static string GetFriendPersonaNameHistory(CSteamID steamIDFriend, int iPersonaName)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetFriendPersonaNameHistory(CSteamAPIContext.GetSteamFriends(), steamIDFriend, iPersonaName));
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00002D0D File Offset: 0x00000F0D
		public static int GetFriendSteamLevel(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendSteamLevel(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00002D1F File Offset: 0x00000F1F
		public static string GetPlayerNickname(CSteamID steamIDPlayer)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetPlayerNickname(CSteamAPIContext.GetSteamFriends(), steamIDPlayer));
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00002D36 File Offset: 0x00000F36
		public static int GetFriendsGroupCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendsGroupCount(CSteamAPIContext.GetSteamFriends());
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00002D47 File Offset: 0x00000F47
		public static FriendsGroupID_t GetFriendsGroupIDByIndex(int iFG)
		{
			InteropHelp.TestIfAvailableClient();
			return (FriendsGroupID_t)NativeMethods.ISteamFriends_GetFriendsGroupIDByIndex(CSteamAPIContext.GetSteamFriends(), iFG);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00002D5E File Offset: 0x00000F5E
		public static string GetFriendsGroupName(FriendsGroupID_t friendsGroupID)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetFriendsGroupName(CSteamAPIContext.GetSteamFriends(), friendsGroupID));
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00002D75 File Offset: 0x00000F75
		public static int GetFriendsGroupMembersCount(FriendsGroupID_t friendsGroupID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendsGroupMembersCount(CSteamAPIContext.GetSteamFriends(), friendsGroupID);
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00002D87 File Offset: 0x00000F87
		public static void GetFriendsGroupMembersList(FriendsGroupID_t friendsGroupID, CSteamID[] pOutSteamIDMembers, int nMembersCount)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamFriends_GetFriendsGroupMembersList(CSteamAPIContext.GetSteamFriends(), friendsGroupID, pOutSteamIDMembers, nMembersCount);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00002D9B File Offset: 0x00000F9B
		public static bool HasFriend(CSteamID steamIDFriend, EFriendFlags iFriendFlags)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_HasFriend(CSteamAPIContext.GetSteamFriends(), steamIDFriend, iFriendFlags);
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00002DAE File Offset: 0x00000FAE
		public static int GetClanCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetClanCount(CSteamAPIContext.GetSteamFriends());
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00002DBF File Offset: 0x00000FBF
		public static CSteamID GetClanByIndex(int iClan)
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamFriends_GetClanByIndex(CSteamAPIContext.GetSteamFriends(), iClan);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00002DD6 File Offset: 0x00000FD6
		public static string GetClanName(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetClanName(CSteamAPIContext.GetSteamFriends(), steamIDClan));
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002DED File Offset: 0x00000FED
		public static string GetClanTag(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetClanTag(CSteamAPIContext.GetSteamFriends(), steamIDClan));
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002E04 File Offset: 0x00001004
		public static bool GetClanActivityCounts(CSteamID steamIDClan, out int pnOnline, out int pnInGame, out int pnChatting)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetClanActivityCounts(CSteamAPIContext.GetSteamFriends(), steamIDClan, out pnOnline, out pnInGame, out pnChatting);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00002E19 File Offset: 0x00001019
		public static SteamAPICall_t DownloadClanActivityCounts(CSteamID[] psteamIDClans, int cClansToRequest)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamFriends_DownloadClanActivityCounts(CSteamAPIContext.GetSteamFriends(), psteamIDClans, cClansToRequest);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00002E31 File Offset: 0x00001031
		public static int GetFriendCountFromSource(CSteamID steamIDSource)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendCountFromSource(CSteamAPIContext.GetSteamFriends(), steamIDSource);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00002E43 File Offset: 0x00001043
		public static CSteamID GetFriendFromSourceByIndex(CSteamID steamIDSource, int iFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamFriends_GetFriendFromSourceByIndex(CSteamAPIContext.GetSteamFriends(), steamIDSource, iFriend);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00002E5B File Offset: 0x0000105B
		public static bool IsUserInSource(CSteamID steamIDUser, CSteamID steamIDSource)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_IsUserInSource(CSteamAPIContext.GetSteamFriends(), steamIDUser, steamIDSource);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00002E6E File Offset: 0x0000106E
		public static void SetInGameVoiceSpeaking(CSteamID steamIDUser, bool bSpeaking)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamFriends_SetInGameVoiceSpeaking(CSteamAPIContext.GetSteamFriends(), steamIDUser, bSpeaking);
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00002E84 File Offset: 0x00001084
		public static void ActivateGameOverlay(string pchDialog)
		{
			InteropHelp.TestIfAvailableClient();
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchDialog))
			{
				NativeMethods.ISteamFriends_ActivateGameOverlay(CSteamAPIContext.GetSteamFriends(), utf8StringHandle);
			}
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00002EC4 File Offset: 0x000010C4
		public static void ActivateGameOverlayToUser(string pchDialog, CSteamID steamID)
		{
			InteropHelp.TestIfAvailableClient();
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchDialog))
			{
				NativeMethods.ISteamFriends_ActivateGameOverlayToUser(CSteamAPIContext.GetSteamFriends(), utf8StringHandle, steamID);
			}
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00002F08 File Offset: 0x00001108
		public static void ActivateGameOverlayToWebPage(string pchURL, EActivateGameOverlayToWebPageMode eMode = EActivateGameOverlayToWebPageMode.k_EActivateGameOverlayToWebPageMode_Default)
		{
			InteropHelp.TestIfAvailableClient();
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchURL))
			{
				NativeMethods.ISteamFriends_ActivateGameOverlayToWebPage(CSteamAPIContext.GetSteamFriends(), utf8StringHandle, eMode);
			}
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00002F4C File Offset: 0x0000114C
		public static void ActivateGameOverlayToStore(AppId_t nAppID, EOverlayToStoreFlag eFlag)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamFriends_ActivateGameOverlayToStore(CSteamAPIContext.GetSteamFriends(), nAppID, eFlag);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00002F5F File Offset: 0x0000115F
		public static void SetPlayedWith(CSteamID steamIDUserPlayedWith)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamFriends_SetPlayedWith(CSteamAPIContext.GetSteamFriends(), steamIDUserPlayedWith);
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00002F71 File Offset: 0x00001171
		public static void ActivateGameOverlayInviteDialog(CSteamID steamIDLobby)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamFriends_ActivateGameOverlayInviteDialog(CSteamAPIContext.GetSteamFriends(), steamIDLobby);
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00002F83 File Offset: 0x00001183
		public static int GetSmallFriendAvatar(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetSmallFriendAvatar(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00002F95 File Offset: 0x00001195
		public static int GetMediumFriendAvatar(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetMediumFriendAvatar(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00002FA7 File Offset: 0x000011A7
		public static int GetLargeFriendAvatar(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetLargeFriendAvatar(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00002FB9 File Offset: 0x000011B9
		public static bool RequestUserInformation(CSteamID steamIDUser, bool bRequireNameOnly)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_RequestUserInformation(CSteamAPIContext.GetSteamFriends(), steamIDUser, bRequireNameOnly);
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00002FCC File Offset: 0x000011CC
		public static SteamAPICall_t RequestClanOfficerList(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamFriends_RequestClanOfficerList(CSteamAPIContext.GetSteamFriends(), steamIDClan);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00002FE3 File Offset: 0x000011E3
		public static CSteamID GetClanOwner(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamFriends_GetClanOwner(CSteamAPIContext.GetSteamFriends(), steamIDClan);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00002FFA File Offset: 0x000011FA
		public static int GetClanOfficerCount(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetClanOfficerCount(CSteamAPIContext.GetSteamFriends(), steamIDClan);
		}

		// Token: 0x06000070 RID: 112 RVA: 0x0000300C File Offset: 0x0000120C
		public static CSteamID GetClanOfficerByIndex(CSteamID steamIDClan, int iOfficer)
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamFriends_GetClanOfficerByIndex(CSteamAPIContext.GetSteamFriends(), steamIDClan, iOfficer);
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00003024 File Offset: 0x00001224
		public static uint GetUserRestrictions()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetUserRestrictions(CSteamAPIContext.GetSteamFriends());
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00003038 File Offset: 0x00001238
		public static bool SetRichPresence(string pchKey, string pchValue)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchKey))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchValue))
				{
					result = NativeMethods.ISteamFriends_SetRichPresence(CSteamAPIContext.GetSteamFriends(), utf8StringHandle, utf8StringHandle2);
				}
			}
			return result;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00003098 File Offset: 0x00001298
		public static void ClearRichPresence()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamFriends_ClearRichPresence(CSteamAPIContext.GetSteamFriends());
		}

		// Token: 0x06000074 RID: 116 RVA: 0x000030AC File Offset: 0x000012AC
		public static string GetFriendRichPresence(CSteamID steamIDFriend, string pchKey)
		{
			InteropHelp.TestIfAvailableClient();
			string result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchKey))
			{
				result = InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetFriendRichPresence(CSteamAPIContext.GetSteamFriends(), steamIDFriend, utf8StringHandle));
			}
			return result;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000030F4 File Offset: 0x000012F4
		public static int GetFriendRichPresenceKeyCount(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendRichPresenceKeyCount(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00003106 File Offset: 0x00001306
		public static string GetFriendRichPresenceKeyByIndex(CSteamID steamIDFriend, int iKey)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetFriendRichPresenceKeyByIndex(CSteamAPIContext.GetSteamFriends(), steamIDFriend, iKey));
		}

		// Token: 0x06000077 RID: 119 RVA: 0x0000311E File Offset: 0x0000131E
		public static void RequestFriendRichPresence(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamFriends_RequestFriendRichPresence(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00003130 File Offset: 0x00001330
		public static bool InviteUserToGame(CSteamID steamIDFriend, string pchConnectString)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchConnectString))
			{
				result = NativeMethods.ISteamFriends_InviteUserToGame(CSteamAPIContext.GetSteamFriends(), steamIDFriend, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00003174 File Offset: 0x00001374
		public static int GetCoplayFriendCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetCoplayFriendCount(CSteamAPIContext.GetSteamFriends());
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00003185 File Offset: 0x00001385
		public static CSteamID GetCoplayFriend(int iCoplayFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamFriends_GetCoplayFriend(CSteamAPIContext.GetSteamFriends(), iCoplayFriend);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x0000319C File Offset: 0x0000139C
		public static int GetFriendCoplayTime(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetFriendCoplayTime(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x0600007C RID: 124 RVA: 0x000031AE File Offset: 0x000013AE
		public static AppId_t GetFriendCoplayGame(CSteamID steamIDFriend)
		{
			InteropHelp.TestIfAvailableClient();
			return (AppId_t)NativeMethods.ISteamFriends_GetFriendCoplayGame(CSteamAPIContext.GetSteamFriends(), steamIDFriend);
		}

		// Token: 0x0600007D RID: 125 RVA: 0x000031C5 File Offset: 0x000013C5
		public static SteamAPICall_t JoinClanChatRoom(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamFriends_JoinClanChatRoom(CSteamAPIContext.GetSteamFriends(), steamIDClan);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x000031DC File Offset: 0x000013DC
		public static bool LeaveClanChatRoom(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_LeaveClanChatRoom(CSteamAPIContext.GetSteamFriends(), steamIDClan);
		}

		// Token: 0x0600007F RID: 127 RVA: 0x000031EE File Offset: 0x000013EE
		public static int GetClanChatMemberCount(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetClanChatMemberCount(CSteamAPIContext.GetSteamFriends(), steamIDClan);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00003200 File Offset: 0x00001400
		public static CSteamID GetChatMemberByIndex(CSteamID steamIDClan, int iUser)
		{
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamFriends_GetChatMemberByIndex(CSteamAPIContext.GetSteamFriends(), steamIDClan, iUser);
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00003218 File Offset: 0x00001418
		public static bool SendClanChatMessage(CSteamID steamIDClanChat, string pchText)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchText))
			{
				result = NativeMethods.ISteamFriends_SendClanChatMessage(CSteamAPIContext.GetSteamFriends(), steamIDClanChat, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000082 RID: 130 RVA: 0x0000325C File Offset: 0x0000145C
		public static int GetClanChatMessage(CSteamID steamIDClanChat, int iMessage, out string prgchText, int cchTextMax, out EChatEntryType peChatEntryType, out CSteamID psteamidChatter)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cchTextMax);
			int num = NativeMethods.ISteamFriends_GetClanChatMessage(CSteamAPIContext.GetSteamFriends(), steamIDClanChat, iMessage, intPtr, cchTextMax, out peChatEntryType, out psteamidChatter);
			prgchText = ((num != 0) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x0000329D File Offset: 0x0000149D
		public static bool IsClanChatAdmin(CSteamID steamIDClanChat, CSteamID steamIDUser)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_IsClanChatAdmin(CSteamAPIContext.GetSteamFriends(), steamIDClanChat, steamIDUser);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x000032B0 File Offset: 0x000014B0
		public static bool IsClanChatWindowOpenInSteam(CSteamID steamIDClanChat)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_IsClanChatWindowOpenInSteam(CSteamAPIContext.GetSteamFriends(), steamIDClanChat);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000032C2 File Offset: 0x000014C2
		public static bool OpenClanChatWindowInSteam(CSteamID steamIDClanChat)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_OpenClanChatWindowInSteam(CSteamAPIContext.GetSteamFriends(), steamIDClanChat);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000032D4 File Offset: 0x000014D4
		public static bool CloseClanChatWindowInSteam(CSteamID steamIDClanChat)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_CloseClanChatWindowInSteam(CSteamAPIContext.GetSteamFriends(), steamIDClanChat);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x000032E6 File Offset: 0x000014E6
		public static bool SetListenForFriendsMessages(bool bInterceptEnabled)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_SetListenForFriendsMessages(CSteamAPIContext.GetSteamFriends(), bInterceptEnabled);
		}

		// Token: 0x06000088 RID: 136 RVA: 0x000032F8 File Offset: 0x000014F8
		public static bool ReplyToFriendMessage(CSteamID steamIDFriend, string pchMsgToSend)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchMsgToSend))
			{
				result = NativeMethods.ISteamFriends_ReplyToFriendMessage(CSteamAPIContext.GetSteamFriends(), steamIDFriend, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x0000333C File Offset: 0x0000153C
		public static int GetFriendMessage(CSteamID steamIDFriend, int iMessageID, out string pvData, int cubData, out EChatEntryType peChatEntryType)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cubData);
			int num = NativeMethods.ISteamFriends_GetFriendMessage(CSteamAPIContext.GetSteamFriends(), steamIDFriend, iMessageID, intPtr, cubData, out peChatEntryType);
			pvData = ((num != 0) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return num;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x0000337B File Offset: 0x0000157B
		public static SteamAPICall_t GetFollowerCount(CSteamID steamID)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamFriends_GetFollowerCount(CSteamAPIContext.GetSteamFriends(), steamID);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00003392 File Offset: 0x00001592
		public static SteamAPICall_t IsFollowing(CSteamID steamID)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamFriends_IsFollowing(CSteamAPIContext.GetSteamFriends(), steamID);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x000033A9 File Offset: 0x000015A9
		public static SteamAPICall_t EnumerateFollowingList(uint unStartIndex)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamFriends_EnumerateFollowingList(CSteamAPIContext.GetSteamFriends(), unStartIndex);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x000033C0 File Offset: 0x000015C0
		public static bool IsClanPublic(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_IsClanPublic(CSteamAPIContext.GetSteamFriends(), steamIDClan);
		}

		// Token: 0x0600008E RID: 142 RVA: 0x000033D2 File Offset: 0x000015D2
		public static bool IsClanOfficialGameGroup(CSteamID steamIDClan)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_IsClanOfficialGameGroup(CSteamAPIContext.GetSteamFriends(), steamIDClan);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x000033E4 File Offset: 0x000015E4
		public static int GetNumChatsWithUnreadPriorityMessages()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamFriends_GetNumChatsWithUnreadPriorityMessages(CSteamAPIContext.GetSteamFriends());
		}

		// Token: 0x06000090 RID: 144 RVA: 0x000033F5 File Offset: 0x000015F5
		public static void ActivateGameOverlayRemotePlayTogetherInviteDialog(CSteamID steamIDLobby)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamFriends_ActivateGameOverlayRemotePlayTogetherInviteDialog(CSteamAPIContext.GetSteamFriends(), steamIDLobby);
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00003408 File Offset: 0x00001608
		public static bool RegisterProtocolInOverlayBrowser(string pchProtocol)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchProtocol))
			{
				result = NativeMethods.ISteamFriends_RegisterProtocolInOverlayBrowser(CSteamAPIContext.GetSteamFriends(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000092 RID: 146 RVA: 0x0000344C File Offset: 0x0000164C
		public static void ActivateGameOverlayInviteDialogConnectString(string pchConnectString)
		{
			InteropHelp.TestIfAvailableClient();
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchConnectString))
			{
				NativeMethods.ISteamFriends_ActivateGameOverlayInviteDialogConnectString(CSteamAPIContext.GetSteamFriends(), utf8StringHandle);
			}
		}
	}
}
