using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000017 RID: 23
	public static class SteamParties
	{
		// Token: 0x060002DB RID: 731 RVA: 0x000083D5 File Offset: 0x000065D5
		public static uint GetNumActiveBeacons()
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamParties_GetNumActiveBeacons(CSteamAPIContext.GetSteamParties());
		}

		// Token: 0x060002DC RID: 732 RVA: 0x000083E6 File Offset: 0x000065E6
		public static PartyBeaconID_t GetBeaconByIndex(uint unIndex)
		{
			InteropHelp.TestIfAvailableClient();
			return (PartyBeaconID_t)NativeMethods.ISteamParties_GetBeaconByIndex(CSteamAPIContext.GetSteamParties(), unIndex);
		}

		// Token: 0x060002DD RID: 733 RVA: 0x00008400 File Offset: 0x00006600
		public static bool GetBeaconDetails(PartyBeaconID_t ulBeaconID, out CSteamID pSteamIDBeaconOwner, out SteamPartyBeaconLocation_t pLocation, out string pchMetadata, int cchMetadata)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cchMetadata);
			bool flag = NativeMethods.ISteamParties_GetBeaconDetails(CSteamAPIContext.GetSteamParties(), ulBeaconID, out pSteamIDBeaconOwner, out pLocation, intPtr, cchMetadata);
			pchMetadata = (flag ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return flag;
		}

		// Token: 0x060002DE RID: 734 RVA: 0x00008440 File Offset: 0x00006640
		public static SteamAPICall_t JoinParty(PartyBeaconID_t ulBeaconID)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamParties_JoinParty(CSteamAPIContext.GetSteamParties(), ulBeaconID);
		}

		// Token: 0x060002DF RID: 735 RVA: 0x00008457 File Offset: 0x00006657
		public static bool GetNumAvailableBeaconLocations(out uint puNumLocations)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamParties_GetNumAvailableBeaconLocations(CSteamAPIContext.GetSteamParties(), out puNumLocations);
		}

		// Token: 0x060002E0 RID: 736 RVA: 0x00008469 File Offset: 0x00006669
		public static bool GetAvailableBeaconLocations(SteamPartyBeaconLocation_t[] pLocationList, uint uMaxNumLocations)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamParties_GetAvailableBeaconLocations(CSteamAPIContext.GetSteamParties(), pLocationList, uMaxNumLocations);
		}

		// Token: 0x060002E1 RID: 737 RVA: 0x0000847C File Offset: 0x0000667C
		public static SteamAPICall_t CreateBeacon(uint unOpenSlots, ref SteamPartyBeaconLocation_t pBeaconLocation, string pchConnectString, string pchMetadata)
		{
			InteropHelp.TestIfAvailableClient();
			SteamAPICall_t result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pchConnectString))
			{
				using (InteropHelp.UTF8StringHandle utf8StringHandle2 = new InteropHelp.UTF8StringHandle(pchMetadata))
				{
					result = (SteamAPICall_t)NativeMethods.ISteamParties_CreateBeacon(CSteamAPIContext.GetSteamParties(), unOpenSlots, ref pBeaconLocation, utf8StringHandle, utf8StringHandle2);
				}
			}
			return result;
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x000084E4 File Offset: 0x000066E4
		public static void OnReservationCompleted(PartyBeaconID_t ulBeacon, CSteamID steamIDUser)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamParties_OnReservationCompleted(CSteamAPIContext.GetSteamParties(), ulBeacon, steamIDUser);
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x000084F7 File Offset: 0x000066F7
		public static void CancelReservation(PartyBeaconID_t ulBeacon, CSteamID steamIDUser)
		{
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamParties_CancelReservation(CSteamAPIContext.GetSteamParties(), ulBeacon, steamIDUser);
		}

		// Token: 0x060002E4 RID: 740 RVA: 0x0000850A File Offset: 0x0000670A
		public static SteamAPICall_t ChangeNumOpenSlots(PartyBeaconID_t ulBeacon, uint unOpenSlots)
		{
			InteropHelp.TestIfAvailableClient();
			return (SteamAPICall_t)NativeMethods.ISteamParties_ChangeNumOpenSlots(CSteamAPIContext.GetSteamParties(), ulBeacon, unOpenSlots);
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x00008522 File Offset: 0x00006722
		public static bool DestroyBeacon(PartyBeaconID_t ulBeacon)
		{
			InteropHelp.TestIfAvailableClient();
			return NativeMethods.ISteamParties_DestroyBeacon(CSteamAPIContext.GetSteamParties(), ulBeacon);
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x00008534 File Offset: 0x00006734
		public static bool GetBeaconLocationData(SteamPartyBeaconLocation_t BeaconLocation, ESteamPartyBeaconLocationData eData, out string pchDataStringOut, int cchDataStringOut)
		{
			InteropHelp.TestIfAvailableClient();
			IntPtr intPtr = Marshal.AllocHGlobal(cchDataStringOut);
			bool flag = NativeMethods.ISteamParties_GetBeaconLocationData(CSteamAPIContext.GetSteamParties(), BeaconLocation, eData, intPtr, cchDataStringOut);
			pchDataStringOut = (flag ? InteropHelp.PtrToStringUTF8(intPtr) : null);
			Marshal.FreeHGlobal(intPtr);
			return flag;
		}
	}
}
