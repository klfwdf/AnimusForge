using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200017F RID: 383
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamDatagramRelayAuthTicket
	{
		// Token: 0x060008FD RID: 2301 RVA: 0x0000CB91 File Offset: 0x0000AD91
		public void Clear()
		{
		}

		// Token: 0x04000A05 RID: 2565
		private SteamNetworkingIdentity m_identityGameserver;

		// Token: 0x04000A06 RID: 2566
		private SteamNetworkingIdentity m_identityAuthorizedClient;

		// Token: 0x04000A07 RID: 2567
		private uint m_unPublicIP;

		// Token: 0x04000A08 RID: 2568
		private RTime32 m_rtimeTicketExpiry;

		// Token: 0x04000A09 RID: 2569
		private SteamDatagramHostedAddress m_routing;

		// Token: 0x04000A0A RID: 2570
		private uint m_nAppID;

		// Token: 0x04000A0B RID: 2571
		private int m_nRestrictToVirtualPort;

		// Token: 0x04000A0C RID: 2572
		private const int k_nMaxExtraFields = 16;

		// Token: 0x04000A0D RID: 2573
		private int m_nExtraFields;

		// Token: 0x04000A0E RID: 2574
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		private SteamDatagramRelayAuthTicket.ExtraField[] m_vecExtraFields;

		// Token: 0x020001C6 RID: 454
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct ExtraField
		{
			// Token: 0x04000AC5 RID: 2757
			private SteamDatagramRelayAuthTicket.ExtraField.EType m_eType;

			// Token: 0x04000AC6 RID: 2758
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
			private byte[] m_szName;

			// Token: 0x04000AC7 RID: 2759
			private SteamDatagramRelayAuthTicket.ExtraField.OptionValue m_val;

			// Token: 0x020001EA RID: 490
			private enum EType
			{
				// Token: 0x04000AE6 RID: 2790
				k_EType_String,
				// Token: 0x04000AE7 RID: 2791
				k_EType_Int,
				// Token: 0x04000AE8 RID: 2792
				k_EType_Fixed64
			}

			// Token: 0x020001EB RID: 491
			[StructLayout(LayoutKind.Explicit)]
			private struct OptionValue
			{
				// Token: 0x04000AE9 RID: 2793
				[FieldOffset(0)]
				private long m_nIntValue;

				// Token: 0x04000AEA RID: 2794
				[FieldOffset(0)]
				private ulong m_nFixed64Value;
			}
		}
	}
}
