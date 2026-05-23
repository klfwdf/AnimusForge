using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200001C RID: 28
	public static class SteamNetworkingUtils
	{
		// Token: 0x06000355 RID: 853 RVA: 0x00008F86 File Offset: 0x00007186
		public static IntPtr AllocateMessage(int cbAllocateBuffer)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_AllocateMessage(CSteamAPIContext.GetSteamNetworkingUtils(), cbAllocateBuffer);
		}

		// Token: 0x06000356 RID: 854 RVA: 0x00008F98 File Offset: 0x00007198
		public static void InitRelayNetworkAccess()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamNetworkingUtils_InitRelayNetworkAccess(CSteamAPIContext.GetSteamNetworkingUtils());
		}

		// Token: 0x06000357 RID: 855 RVA: 0x00008FA9 File Offset: 0x000071A9
		public static ESteamNetworkingAvailability GetRelayNetworkStatus(out SteamRelayNetworkStatus_t pDetails)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetRelayNetworkStatus(CSteamAPIContext.GetSteamNetworkingUtils(), out pDetails);
		}

		// Token: 0x06000358 RID: 856 RVA: 0x00008FBB File Offset: 0x000071BB
		public static float GetLocalPingLocation(out SteamNetworkPingLocation_t result)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetLocalPingLocation(CSteamAPIContext.GetSteamNetworkingUtils(), out result);
		}

		// Token: 0x06000359 RID: 857 RVA: 0x00008FCD File Offset: 0x000071CD
		public static int EstimatePingTimeBetweenTwoLocations(ref SteamNetworkPingLocation_t location1, ref SteamNetworkPingLocation_t location2)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_EstimatePingTimeBetweenTwoLocations(CSteamAPIContext.GetSteamNetworkingUtils(), ref location1, ref location2);
		}

		// Token: 0x0600035A RID: 858 RVA: 0x00008FE0 File Offset: 0x000071E0
		public static int EstimatePingTimeFromLocalHost(ref SteamNetworkPingLocation_t remoteLocation)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_EstimatePingTimeFromLocalHost(CSteamAPIContext.GetSteamNetworkingUtils(), ref remoteLocation);
		}

		// Token: 0x0600035B RID: 859 RVA: 0x00008FF4 File Offset: 0x000071F4
		public static void ConvertPingLocationToString(ref SteamNetworkPingLocation_t location, out string pszBuf, int cchBufSize)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cchBufSize);
			NativeMethods.ISteamNetworkingUtils_ConvertPingLocationToString(CSteamAPIContext.GetSteamNetworkingUtils(), ref location, intPtr, cchBufSize);
			pszBuf = InteropHelp.PtrToStringUTF8(intPtr);
			Marshal.FreeHGlobal(intPtr);
		}

		// Token: 0x0600035C RID: 860 RVA: 0x00009028 File Offset: 0x00007228
		public static bool ParsePingLocationString(string pszString, out SteamNetworkPingLocation_t result)
		{
			InteropHelp.TestIfAvailableClient();
			bool result2;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszString))
			{
				result2 = NativeMethods.ISteamNetworkingUtils_ParsePingLocationString(CSteamAPIContext.GetSteamNetworkingUtils(), utf8StringHandle, out result);
			}
			return result2;
		}

		// Token: 0x0600035D RID: 861 RVA: 0x0000906C File Offset: 0x0000726C
		public static bool CheckPingDataUpToDate(float flMaxAgeSeconds)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_CheckPingDataUpToDate(CSteamAPIContext.GetSteamNetworkingUtils(), flMaxAgeSeconds);
		}

		// Token: 0x0600035E RID: 862 RVA: 0x0000907E File Offset: 0x0000727E
		public static int GetPingToDataCenter(SteamNetworkingPOPID popID, out SteamNetworkingPOPID pViaRelayPoP)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetPingToDataCenter(CSteamAPIContext.GetSteamNetworkingUtils(), popID, out pViaRelayPoP);
		}

		// Token: 0x0600035F RID: 863 RVA: 0x00009091 File Offset: 0x00007291
		public static int GetDirectPingToPOP(SteamNetworkingPOPID popID)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetDirectPingToPOP(CSteamAPIContext.GetSteamNetworkingUtils(), popID);
		}

		// Token: 0x06000360 RID: 864 RVA: 0x000090A3 File Offset: 0x000072A3
		public static int GetPOPCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetPOPCount(CSteamAPIContext.GetSteamNetworkingUtils());
		}

		// Token: 0x06000361 RID: 865 RVA: 0x000090B4 File Offset: 0x000072B4
		public static int GetPOPList(out SteamNetworkingPOPID list, int nListSz)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetPOPList(CSteamAPIContext.GetSteamNetworkingUtils(), out list, nListSz);
		}

		// Token: 0x06000362 RID: 866 RVA: 0x000090C7 File Offset: 0x000072C7
		public static SteamNetworkingMicroseconds GetLocalTimestamp()
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamNetworkingMicroseconds)NativeMethods.ISteamNetworkingUtils_GetLocalTimestamp(CSteamAPIContext.GetSteamNetworkingUtils());
		}

		// Token: 0x06000363 RID: 867 RVA: 0x000090DD File Offset: 0x000072DD
		public static void SetDebugOutputFunction(ESteamNetworkingSocketsDebugOutputType eDetailLevel, FSteamNetworkingSocketsDebugOutput pfnFunc)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamNetworkingUtils_SetDebugOutputFunction(CSteamAPIContext.GetSteamNetworkingUtils(), eDetailLevel, pfnFunc);
		}

		// Token: 0x06000364 RID: 868 RVA: 0x000090F0 File Offset: 0x000072F0
		public static bool IsFakeIPv4(uint nIPv4)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_IsFakeIPv4(CSteamAPIContext.GetSteamNetworkingUtils(), nIPv4);
		}

		// Token: 0x06000365 RID: 869 RVA: 0x00009102 File Offset: 0x00007302
		public static ESteamNetworkingFakeIPType GetIPv4FakeIPType(uint nIPv4)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetIPv4FakeIPType(CSteamAPIContext.GetSteamNetworkingUtils(), nIPv4);
		}

		// Token: 0x06000366 RID: 870 RVA: 0x00009114 File Offset: 0x00007314
		public static EResult GetRealIdentityForFakeIP(ref SteamNetworkingIPAddr fakeIP, out SteamNetworkingIdentity pOutRealIdentity)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetRealIdentityForFakeIP(CSteamAPIContext.GetSteamNetworkingUtils(), ref fakeIP, out pOutRealIdentity);
		}

		// Token: 0x06000367 RID: 871 RVA: 0x00009127 File Offset: 0x00007327
		public static bool SetConfigValue(ESteamNetworkingConfigValue eValue, ESteamNetworkingConfigScope eScopeType, IntPtr scopeObj, ESteamNetworkingConfigDataType eDataType, IntPtr pArg)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_SetConfigValue(CSteamAPIContext.GetSteamNetworkingUtils(), eValue, eScopeType, scopeObj, eDataType, pArg);
		}

		// Token: 0x06000368 RID: 872 RVA: 0x0000913E File Offset: 0x0000733E
		public static ESteamNetworkingGetConfigValueResult GetConfigValue(ESteamNetworkingConfigValue eValue, ESteamNetworkingConfigScope eScopeType, IntPtr scopeObj, out ESteamNetworkingConfigDataType pOutDataType, IntPtr pResult, ref ulong cbResult)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_GetConfigValue(CSteamAPIContext.GetSteamNetworkingUtils(), eValue, eScopeType, scopeObj, out pOutDataType, pResult, ref cbResult);
		}

		// Token: 0x06000369 RID: 873 RVA: 0x00009157 File Offset: 0x00007357
		public static string GetConfigValueInfo(ESteamNetworkingConfigValue eValue, out ESteamNetworkingConfigDataType pOutDataType, out ESteamNetworkingConfigScope pOutScope)
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamNetworkingUtils_GetConfigValueInfo(CSteamAPIContext.GetSteamNetworkingUtils(), eValue, out pOutDataType, out pOutScope));
		}

		// Token: 0x0600036A RID: 874 RVA: 0x00009170 File Offset: 0x00007370
		public static ESteamNetworkingConfigValue IterateGenericEditableConfigValues(ESteamNetworkingConfigValue eCurrent, bool bEnumerateDevVars)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_IterateGenericEditableConfigValues(CSteamAPIContext.GetSteamNetworkingUtils(), eCurrent, bEnumerateDevVars);
		}

		// Token: 0x0600036B RID: 875 RVA: 0x00009184 File Offset: 0x00007384
		public static void SteamNetworkingIPAddr_ToString(ref SteamNetworkingIPAddr addr, out string buf, uint cbBuf, bool bWithPort)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal((int)cbBuf);
			NativeMethods.ISteamNetworkingUtils_SteamNetworkingIPAddr_ToString(CSteamAPIContext.GetSteamNetworkingUtils(), ref addr, intPtr, cbBuf, bWithPort);
			buf = InteropHelp.PtrToStringUTF8(intPtr);
			Marshal.FreeHGlobal(intPtr);
		}

		// Token: 0x0600036C RID: 876 RVA: 0x000091BC File Offset: 0x000073BC
		public static bool SteamNetworkingIPAddr_ParseString(out SteamNetworkingIPAddr pAddr, string pszStr)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszStr))
			{
				result = NativeMethods.ISteamNetworkingUtils_SteamNetworkingIPAddr_ParseString(CSteamAPIContext.GetSteamNetworkingUtils(), out pAddr, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x0600036D RID: 877 RVA: 0x00009200 File Offset: 0x00007400
		public static ESteamNetworkingFakeIPType SteamNetworkingIPAddr_GetFakeIPType(ref SteamNetworkingIPAddr addr)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamNetworkingUtils_SteamNetworkingIPAddr_GetFakeIPType(CSteamAPIContext.GetSteamNetworkingUtils(), ref addr);
		}

		// Token: 0x0600036E RID: 878 RVA: 0x00009214 File Offset: 0x00007414
		public static void SteamNetworkingIdentity_ToString(ref SteamNetworkingIdentity identity, out string buf, uint cbBuf)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal((int)cbBuf);
			NativeMethods.ISteamNetworkingUtils_SteamNetworkingIdentity_ToString(CSteamAPIContext.GetSteamNetworkingUtils(), ref identity, intPtr, cbBuf);
			buf = InteropHelp.PtrToStringUTF8(intPtr);
			Marshal.FreeHGlobal(intPtr);
		}

		// Token: 0x0600036F RID: 879 RVA: 0x00009248 File Offset: 0x00007448
		public static bool SteamNetworkingIdentity_ParseString(out SteamNetworkingIdentity pIdentity, string pszStr)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszStr))
			{
				result = NativeMethods.ISteamNetworkingUtils_SteamNetworkingIdentity_ParseString(CSteamAPIContext.GetSteamNetworkingUtils(), out pIdentity, utf8StringHandle);
			}
			return result;
		}
	}
}
