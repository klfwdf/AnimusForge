using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000024 RID: 36
	public static class SteamUtils
	{
		// Token: 0x06000464 RID: 1124 RVA: 0x0000B8A0 File Offset: 0x00009AA0
		public static uint GetSecondsSinceAppActive()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetSecondsSinceAppActive(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000465 RID: 1125 RVA: 0x0000B8B1 File Offset: 0x00009AB1
		public static uint GetSecondsSinceComputerActive()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetSecondsSinceComputerActive(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x0000B8C2 File Offset: 0x00009AC2
		public static EUniverse GetConnectedUniverse()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetConnectedUniverse(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x0000B8D3 File Offset: 0x00009AD3
		public static uint GetServerRealTime()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetServerRealTime(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000468 RID: 1128 RVA: 0x0000B8E4 File Offset: 0x00009AE4
		public static string GetIPCountry()
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamUtils_GetIPCountry(CSteamAPIContext.GetSteamUtils()));
		}

		// Token: 0x06000469 RID: 1129 RVA: 0x0000B8FA File Offset: 0x00009AFA
		public static bool GetImageSize(int iImage, out uint pnWidth, out uint pnHeight)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetImageSize(CSteamAPIContext.GetSteamUtils(), iImage, out pnWidth, out pnHeight);
		}

		// Token: 0x0600046A RID: 1130 RVA: 0x0000B90E File Offset: 0x00009B0E
		public static bool GetImageRGBA(int iImage, byte[] pubDest, int nDestBufferSize)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetImageRGBA(CSteamAPIContext.GetSteamUtils(), iImage, pubDest, nDestBufferSize);
		}

		// Token: 0x0600046B RID: 1131 RVA: 0x0000B922 File Offset: 0x00009B22
		public static byte GetCurrentBatteryPower()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetCurrentBatteryPower(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x0600046C RID: 1132 RVA: 0x0000B933 File Offset: 0x00009B33
		public static AppId_t GetAppID()
		{
			InteropHelp.TestIfAvailableClient();
			return (AppId_t)NativeMethods.ISteamUtils_GetAppID(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x0600046D RID: 1133 RVA: 0x0000B949 File Offset: 0x00009B49
		public static void SetOverlayNotificationPosition(ENotificationPosition eNotificationPosition)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUtils_SetOverlayNotificationPosition(CSteamAPIContext.GetSteamUtils(), eNotificationPosition);
		}

		// Token: 0x0600046E RID: 1134 RVA: 0x0000B95B File Offset: 0x00009B5B
		public static bool IsAPICallCompleted(SteamAPICall_t hSteamAPICall, out bool pbFailed)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_IsAPICallCompleted(CSteamAPIContext.GetSteamUtils(), hSteamAPICall, out pbFailed);
		}

		// Token: 0x0600046F RID: 1135 RVA: 0x0000B96E File Offset: 0x00009B6E
		public static ESteamAPICallFailure GetAPICallFailureReason(SteamAPICall_t hSteamAPICall)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetAPICallFailureReason(CSteamAPIContext.GetSteamUtils(), hSteamAPICall);
		}

		// Token: 0x06000470 RID: 1136 RVA: 0x0000B980 File Offset: 0x00009B80
		public static bool GetAPICallResult(SteamAPICall_t hSteamAPICall, IntPtr pCallback, int cubCallback, int iCallbackExpected, out bool pbFailed)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetAPICallResult(CSteamAPIContext.GetSteamUtils(), hSteamAPICall, pCallback, cubCallback, iCallbackExpected, out pbFailed);
		}

		// Token: 0x06000471 RID: 1137 RVA: 0x0000B997 File Offset: 0x00009B97
		public static uint GetIPCCallCount()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetIPCCallCount(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000472 RID: 1138 RVA: 0x0000B9A8 File Offset: 0x00009BA8
		public static void SetWarningMessageHook(SteamAPIWarningMessageHook_t pFunction)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUtils_SetWarningMessageHook(CSteamAPIContext.GetSteamUtils(), pFunction);
		}

		// Token: 0x06000473 RID: 1139 RVA: 0x0000B9BA File Offset: 0x00009BBA
		public static bool IsOverlayEnabled()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_IsOverlayEnabled(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000474 RID: 1140 RVA: 0x0000B9CB File Offset: 0x00009BCB
		public static bool BOverlayNeedsPresent()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_BOverlayNeedsPresent(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000475 RID: 1141 RVA: 0x0000B9DC File Offset: 0x00009BDC
		public static SteamAPICall_t CheckFileSignature(string szFileName)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(szFileName))
			{
				result = (SteamAPICall_t)NativeMethods.ISteamUtils_CheckFileSignature(CSteamAPIContext.GetSteamUtils(), utf8StringHandle);
			}
			return result;
		}

		// Token: 0x06000476 RID: 1142 RVA: 0x0000BA24 File Offset: 0x00009C24
		public static bool ShowGamepadTextInput(EGamepadTextInputMode eInputMode, EGamepadTextInputLineMode eLineInputMode, string pchDescription, uint unCharMax, string pchExistingText)
		{
			InteropHelp.TestIfAvailableClient();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchDescription))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchExistingText))
				{
					result = NativeMethods.ISteamUtils_ShowGamepadTextInput(CSteamAPIContext.GetSteamUtils(), eInputMode, eLineInputMode, utf8StringHandle, unCharMax, utf8StringHandle2);
				}
			}
			return result;
		}

		// Token: 0x06000477 RID: 1143 RVA: 0x0000BA88 File Offset: 0x00009C88
		public static uint GetEnteredGamepadTextLength()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetEnteredGamepadTextLength(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000478 RID: 1144 RVA: 0x0000BA9C File Offset: 0x00009C9C
		public static bool GetEnteredGamepadTextInput(out string pchText, uint cchText)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal((int)cchText);
			bool flag = NativeMethods.ISteamUtils_GetEnteredGamepadTextInput(CSteamAPIContext.GetSteamUtils(), intPtr, cchText);
			pchText = (flag ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return flag;
		}

		// Token: 0x06000479 RID: 1145 RVA: 0x0000BAD7 File Offset: 0x00009CD7
		public static string GetSteamUILanguage()
		{
			InteropHelp.TestIfAvailableClient();
			return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamUtils_GetSteamUILanguage(CSteamAPIContext.GetSteamUtils()));
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x0000BAED File Offset: 0x00009CED
		public static bool IsSteamRunningInVR()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_IsSteamRunningInVR(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x0600047B RID: 1147 RVA: 0x0000BAFE File Offset: 0x00009CFE
		public static void SetOverlayNotificationInset(int nHorizontalInset, int nVerticalInset)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUtils_SetOverlayNotificationInset(CSteamAPIContext.GetSteamUtils(), nHorizontalInset, nVerticalInset);
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x0000BB11 File Offset: 0x00009D11
		public static bool IsSteamInBigPictureMode()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_IsSteamInBigPictureMode(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x0000BB22 File Offset: 0x00009D22
		public static void StartVRDashboard()
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUtils_StartVRDashboard(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x0000BB33 File Offset: 0x00009D33
		public static bool IsVRHeadsetStreamingEnabled()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_IsVRHeadsetStreamingEnabled(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x0600047F RID: 1151 RVA: 0x0000BB44 File Offset: 0x00009D44
		public static void SetVRHeadsetStreamingEnabled(bool bEnabled)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUtils_SetVRHeadsetStreamingEnabled(CSteamAPIContext.GetSteamUtils(), bEnabled);
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x0000BB56 File Offset: 0x00009D56
		public static bool IsSteamChinaLauncher()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_IsSteamChinaLauncher(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000481 RID: 1153 RVA: 0x0000BB67 File Offset: 0x00009D67
		public static bool InitFilterText(uint unFilterOptions = 0U)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_InitFilterText(CSteamAPIContext.GetSteamUtils(), unFilterOptions);
		}

		// Token: 0x06000482 RID: 1154 RVA: 0x0000BB7C File Offset: 0x00009D7C
		public static int FilterText(ETextFilteringContext eContext, CSteamID sourceSteamID, string pchInputMessage, out string pchOutFilteredText, uint nByteSizeOutFilteredText)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal((int)nByteSizeOutFilteredText);
			int result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchInputMessage))
			{
				int num = NativeMethods.ISteamUtils_FilterText(CSteamAPIContext.GetSteamUtils(), eContext, sourceSteamID, utf8StringHandle, intPtr, nByteSizeOutFilteredText);
				pchOutFilteredText = ((num != -1) ? InteropHelp.PtrToStringUTF8(intPtr) : null);
				Marshal.FreeHGlobal(intPtr);
				result = num;
			}
			return result;
		}

		// Token: 0x06000483 RID: 1155 RVA: 0x0000BBE4 File Offset: 0x00009DE4
		public static ESteamIPv6ConnectivityState GetIPv6ConnectivityState(ESteamIPv6ConnectivityProtocol eProtocol)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_GetIPv6ConnectivityState(CSteamAPIContext.GetSteamUtils(), eProtocol);
		}

		// Token: 0x06000484 RID: 1156 RVA: 0x0000BBF6 File Offset: 0x00009DF6
		public static bool IsSteamRunningOnSteamDeck()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_IsSteamRunningOnSteamDeck(CSteamAPIContext.GetSteamUtils());
		}

		// Token: 0x06000485 RID: 1157 RVA: 0x0000BC07 File Offset: 0x00009E07
		public static bool ShowFloatingGamepadTextInput(EFloatingGamepadTextInputMode eKeyboardMode, int nTextFieldXPosition, int nTextFieldYPosition, int nTextFieldWidth, int nTextFieldHeight)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_ShowFloatingGamepadTextInput(CSteamAPIContext.GetSteamUtils(), eKeyboardMode, nTextFieldXPosition, nTextFieldYPosition, nTextFieldWidth, nTextFieldHeight);
		}

		// Token: 0x06000486 RID: 1158 RVA: 0x0000BC1E File Offset: 0x00009E1E
		public static void SetGameLauncherMode(bool bLauncherMode)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUtils_SetGameLauncherMode(CSteamAPIContext.GetSteamUtils(), bLauncherMode);
		}

		// Token: 0x06000487 RID: 1159 RVA: 0x0000BC30 File Offset: 0x00009E30
		public static bool DismissFloatingGamepadTextInput()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamUtils_DismissFloatingGamepadTextInput(CSteamAPIContext.GetSteamUtils());
		}
	}
}
