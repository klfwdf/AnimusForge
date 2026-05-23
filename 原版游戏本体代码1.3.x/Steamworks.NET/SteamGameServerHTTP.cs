using System;

namespace Steamworks
{
	// Token: 0x02000008 RID: 8
	public static class SteamGameServerHTTP
	{
		// Token: 0x060000E0 RID: 224 RVA: 0x000041C4 File Offset: 0x000023C4
		public static HTTPRequestHandle CreateHTTPRequest(EHTTPMethod eHTTPRequestMethod, string pchAbsoluteURL)
		{
			InteropHelp.TestIfAvailableGameServer();
			HTTPRequestHandle result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchAbsoluteURL))
			{
				result = (HTTPRequestHandle)NativeMethods.ISteamHTTP_CreateHTTPRequest(CSteamGameServerAPIContext.GetSteamHTTP(), eHTTPRequestMethod, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000420C File Offset: 0x0000240C
		public static bool SetHTTPRequestContextValue(HTTPRequestHandle hRequest, ulong ulContextValue)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_SetHTTPRequestContextValue(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, ulContextValue);
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000421F File Offset: 0x0000241F
		public static bool SetHTTPRequestNetworkActivityTimeout(HTTPRequestHandle hRequest, uint unTimeoutSeconds)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_SetHTTPRequestNetworkActivityTimeout(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, unTimeoutSeconds);
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00004234 File Offset: 0x00002434
		public static bool SetHTTPRequestHeaderValue(HTTPRequestHandle hRequest, string pchHeaderName, string pchHeaderValue)
		{
			InteropHelp.TestIfAvailableGameServer();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchHeaderName))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchHeaderValue))
				{
					result = NativeMethods.ISteamHTTP_SetHTTPRequestHeaderValue(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, utf8StringHandle, utf8StringHandle2);
				}
			}
			return result;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00004298 File Offset: 0x00002498
		public static bool SetHTTPRequestGetOrPostParameter(HTTPRequestHandle hRequest, string pchParamName, string pchParamValue)
		{
			InteropHelp.TestIfAvailableGameServer();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchParamName))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchParamValue))
				{
					result = NativeMethods.ISteamHTTP_SetHTTPRequestGetOrPostParameter(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, utf8StringHandle, utf8StringHandle2);
				}
			}
			return result;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x000042FC File Offset: 0x000024FC
		public static bool SendHTTPRequest(HTTPRequestHandle hRequest, out SteamAPICall_t pCallHandle)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_SendHTTPRequest(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, out pCallHandle);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000430F File Offset: 0x0000250F
		public static bool SendHTTPRequestAndStreamResponse(HTTPRequestHandle hRequest, out SteamAPICall_t pCallHandle)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_SendHTTPRequestAndStreamResponse(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, out pCallHandle);
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00004322 File Offset: 0x00002522
		public static bool DeferHTTPRequest(HTTPRequestHandle hRequest)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_DeferHTTPRequest(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest);
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00004334 File Offset: 0x00002534
		public static bool PrioritizeHTTPRequest(HTTPRequestHandle hRequest)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_PrioritizeHTTPRequest(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest);
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00004348 File Offset: 0x00002548
		public static bool GetHTTPResponseHeaderSize(HTTPRequestHandle hRequest, string pchHeaderName, out uint unResponseHeaderSize)
		{
			InteropHelp.TestIfAvailableGameServer();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchHeaderName))
			{
				result = NativeMethods.ISteamHTTP_GetHTTPResponseHeaderSize(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, utf8StringHandle, out unResponseHeaderSize);
			}
			return result;
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000438C File Offset: 0x0000258C
		public static bool GetHTTPResponseHeaderValue(HTTPRequestHandle hRequest, string pchHeaderName, byte[] pHeaderValueBuffer, uint unBufferSize)
		{
			InteropHelp.TestIfAvailableGameServer();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchHeaderName))
			{
				result = NativeMethods.ISteamHTTP_GetHTTPResponseHeaderValue(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, utf8StringHandle, pHeaderValueBuffer, unBufferSize);
			}
			return result;
		}

		// Token: 0x060000EB RID: 235 RVA: 0x000043D4 File Offset: 0x000025D4
		public static bool GetHTTPResponseBodySize(HTTPRequestHandle hRequest, out uint unBodySize)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_GetHTTPResponseBodySize(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, out unBodySize);
		}

		// Token: 0x060000EC RID: 236 RVA: 0x000043E7 File Offset: 0x000025E7
		public static bool GetHTTPResponseBodyData(HTTPRequestHandle hRequest, byte[] pBodyDataBuffer, uint unBufferSize)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_GetHTTPResponseBodyData(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, pBodyDataBuffer, unBufferSize);
		}

		// Token: 0x060000ED RID: 237 RVA: 0x000043FB File Offset: 0x000025FB
		public static bool GetHTTPStreamingResponseBodyData(HTTPRequestHandle hRequest, uint cOffset, byte[] pBodyDataBuffer, uint unBufferSize)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_GetHTTPStreamingResponseBodyData(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, cOffset, pBodyDataBuffer, unBufferSize);
		}

		// Token: 0x060000EE RID: 238 RVA: 0x00004410 File Offset: 0x00002610
		public static bool ReleaseHTTPRequest(HTTPRequestHandle hRequest)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_ReleaseHTTPRequest(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest);
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00004422 File Offset: 0x00002622
		public static bool GetHTTPDownloadProgressPct(HTTPRequestHandle hRequest, out float pflPercentOut)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_GetHTTPDownloadProgressPct(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, out pflPercentOut);
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00004438 File Offset: 0x00002638
		public static bool SetHTTPRequestRawPostBody(HTTPRequestHandle hRequest, string pchContentType, byte[] pubBody, uint unBodyLen)
		{
			InteropHelp.TestIfAvailableGameServer();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchContentType))
			{
				result = NativeMethods.ISteamHTTP_SetHTTPRequestRawPostBody(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, utf8StringHandle, pubBody, unBodyLen);
			}
			return result;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00004480 File Offset: 0x00002680
		public static HTTPCookieContainerHandle CreateCookieContainer(bool bAllowResponsesToModify)
		{
			InteropHelp.TestIfAvailableGameServer();
			return (HTTPCookieContainerHandle)NativeMethods.ISteamHTTP_CreateCookieContainer(CSteamGameServerAPIContext.GetSteamHTTP(), bAllowResponsesToModify);
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00004497 File Offset: 0x00002697
		public static bool ReleaseCookieContainer(HTTPCookieContainerHandle hCookieContainer)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_ReleaseCookieContainer(CSteamGameServerAPIContext.GetSteamHTTP(), hCookieContainer);
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x000044AC File Offset: 0x000026AC
		public static bool SetCookie(HTTPCookieContainerHandle hCookieContainer, string pchHost, string pchUrl, string pchCookie)
		{
			InteropHelp.TestIfAvailableGameServer();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchHost))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchUrl))
				{
					using (InteropHelp.UTF8StringHandle utf8StringHandle3 = new InteropHelp.UTF8StringHandle(pchCookie))
					{
						result = NativeMethods.ISteamHTTP_SetCookie(CSteamGameServerAPIContext.GetSteamHTTP(), hCookieContainer, utf8StringHandle, utf8StringHandle2, utf8StringHandle3);
					}
				}
			}
			return result;
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000452C File Offset: 0x0000272C
		public static bool SetHTTPRequestCookieContainer(HTTPRequestHandle hRequest, HTTPCookieContainerHandle hCookieContainer)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_SetHTTPRequestCookieContainer(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, hCookieContainer);
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00004540 File Offset: 0x00002740
		public static bool SetHTTPRequestUserAgentInfo(HTTPRequestHandle hRequest, string pchUserAgentInfo)
		{
			InteropHelp.TestIfAvailableGameServer();
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchUserAgentInfo))
			{
				result = NativeMethods.ISteamHTTP_SetHTTPRequestUserAgentInfo(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00004584 File Offset: 0x00002784
		public static bool SetHTTPRequestRequiresVerifiedCertificate(HTTPRequestHandle hRequest, bool bRequireVerifiedCertificate)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_SetHTTPRequestRequiresVerifiedCertificate(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, bRequireVerifiedCertificate);
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00004597 File Offset: 0x00002797
		public static bool SetHTTPRequestAbsoluteTimeoutMS(HTTPRequestHandle hRequest, uint unMilliseconds)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_SetHTTPRequestAbsoluteTimeoutMS(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, unMilliseconds);
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x000045AA File Offset: 0x000027AA
		public static bool GetHTTPRequestWasTimedOut(HTTPRequestHandle hRequest, out bool pbWasTimedOut)
		{
			InteropHelp.TestIfAvailableGameServer();
			return NativeMethods.ISteamHTTP_GetHTTPRequestWasTimedOut(CSteamGameServerAPIContext.GetSteamHTTP(), hRequest, out pbWasTimedOut);
		}
	}
}
