using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200019B RID: 411
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SteamNetworkingIPAddr : IEquatable<SteamNetworkingIPAddr>
	{
		// Token: 0x060009E8 RID: 2536 RVA: 0x0000D710 File Offset: 0x0000B910
		public void Clear()
		{
			NativeMethods.SteamAPI_SteamNetworkingIPAddr_Clear(ref this);
		}

		// Token: 0x060009E9 RID: 2537 RVA: 0x0000D718 File Offset: 0x0000B918
		public bool IsIPv6AllZeros()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIPAddr_IsIPv6AllZeros(ref this);
		}

		// Token: 0x060009EA RID: 2538 RVA: 0x0000D720 File Offset: 0x0000B920
		public void SetIPv6(byte[] ipv6, ushort nPort)
		{
			NativeMethods.SteamAPI_SteamNetworkingIPAddr_SetIPv6(ref this, ipv6, nPort);
		}

		// Token: 0x060009EB RID: 2539 RVA: 0x0000D72A File Offset: 0x0000B92A
		public void SetIPv4(uint nIP, ushort nPort)
		{
			NativeMethods.SteamAPI_SteamNetworkingIPAddr_SetIPv4(ref this, nIP, nPort);
		}

		// Token: 0x060009EC RID: 2540 RVA: 0x0000D734 File Offset: 0x0000B934
		public bool IsIPv4()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIPAddr_IsIPv4(ref this);
		}

		// Token: 0x060009ED RID: 2541 RVA: 0x0000D73C File Offset: 0x0000B93C
		public uint GetIPv4()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIPAddr_GetIPv4(ref this);
		}

		// Token: 0x060009EE RID: 2542 RVA: 0x0000D744 File Offset: 0x0000B944
		public void SetIPv6LocalHost(ushort nPort = 0)
		{
			NativeMethods.SteamAPI_SteamNetworkingIPAddr_SetIPv6LocalHost(ref this, nPort);
		}

		// Token: 0x060009EF RID: 2543 RVA: 0x0000D74D File Offset: 0x0000B94D
		public bool IsLocalHost()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIPAddr_IsLocalHost(ref this);
		}

		// Token: 0x060009F0 RID: 2544 RVA: 0x0000D758 File Offset: 0x0000B958
		public void ToString(out string buf, bool bWithPort)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(48);
			NativeMethods.SteamAPI_SteamNetworkingIPAddr_ToString(ref this, intPtr, 48U, bWithPort);
			buf = InteropHelp.PtrToStringUTF8(intPtr);
			Marshal.FreeHGlobal(intPtr);
		}

		// Token: 0x060009F1 RID: 2545 RVA: 0x0000D788 File Offset: 0x0000B988
		public bool ParseString(string pszStr)
		{
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszStr))
			{
				result = NativeMethods.SteamAPI_SteamNetworkingIPAddr_ParseString(ref this, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x060009F2 RID: 2546 RVA: 0x0000D7C4 File Offset: 0x0000B9C4
		public bool Equals(SteamNetworkingIPAddr x)
		{
			return NativeMethods.SteamAPI_SteamNetworkingIPAddr_IsEqualTo(ref this, ref x);
		}

		// Token: 0x060009F3 RID: 2547 RVA: 0x0000D7CE File Offset: 0x0000B9CE
		public ESteamNetworkingFakeIPType GetFakeIPType()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIPAddr_GetFakeIPType(ref this);
		}

		// Token: 0x060009F4 RID: 2548 RVA: 0x0000D7D6 File Offset: 0x0000B9D6
		public bool IsFakeIP()
		{
			return this.GetFakeIPType() > ESteamNetworkingFakeIPType.k_ESteamNetworkingFakeIPType_NotFake;
		}

		// Token: 0x04000A3B RID: 2619
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public byte[] m_ipv6;

		// Token: 0x04000A3C RID: 2620
		public ushort m_port;

		// Token: 0x04000A3D RID: 2621
		public const int k_cchMaxString = 48;
	}
}
