using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200019A RID: 410
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SteamNetworkingIdentity : IEquatable<SteamNetworkingIdentity>
	{
		// Token: 0x060009D3 RID: 2515 RVA: 0x0000D5BD File Offset: 0x0000B7BD
		public void Clear()
		{
			NativeMethods.SteamAPI_SteamNetworkingIdentity_Clear(ref this);
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x0000D5C5 File Offset: 0x0000B7C5
		public bool IsInvalid()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIdentity_IsInvalid(ref this);
		}

		// Token: 0x060009D5 RID: 2517 RVA: 0x0000D5CD File Offset: 0x0000B7CD
		public void SetSteamID(CSteamID steamID)
		{
			NativeMethods.SteamAPI_SteamNetworkingIdentity_SetSteamID(ref this, (ulong)steamID);
		}

		// Token: 0x060009D6 RID: 2518 RVA: 0x0000D5DB File Offset: 0x0000B7DB
		public CSteamID GetSteamID()
		{
			return (CSteamID)NativeMethods.SteamAPI_SteamNetworkingIdentity_GetSteamID(ref this);
		}

		// Token: 0x060009D7 RID: 2519 RVA: 0x0000D5E8 File Offset: 0x0000B7E8
		public void SetSteamID64(ulong steamID)
		{
			NativeMethods.SteamAPI_SteamNetworkingIdentity_SetSteamID64(ref this, steamID);
		}

		// Token: 0x060009D8 RID: 2520 RVA: 0x0000D5F1 File Offset: 0x0000B7F1
		public ulong GetSteamID64()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIdentity_GetSteamID64(ref this);
		}

		// Token: 0x060009D9 RID: 2521 RVA: 0x0000D5F9 File Offset: 0x0000B7F9
		public void SetIPAddr(SteamNetworkingIPAddr addr)
		{
			NativeMethods.SteamAPI_SteamNetworkingIdentity_SetIPAddr(ref this, ref addr);
		}

		// Token: 0x060009DA RID: 2522 RVA: 0x0000D604 File Offset: 0x0000B804
		public SteamNetworkingIPAddr GetIPAddr()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060009DB RID: 2523 RVA: 0x0000D60B File Offset: 0x0000B80B
		public void SetIPv4Addr(uint nIPv4, ushort nPort)
		{
			NativeMethods.SteamAPI_SteamNetworkingIdentity_SetIPv4Addr(ref this, nIPv4, nPort);
		}

		// Token: 0x060009DC RID: 2524 RVA: 0x0000D615 File Offset: 0x0000B815
		public uint GetIPv4()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIdentity_GetIPv4(ref this);
		}

		// Token: 0x060009DD RID: 2525 RVA: 0x0000D61D File Offset: 0x0000B81D
		public ESteamNetworkingFakeIPType GetFakeIPType()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIdentity_GetFakeIPType(ref this);
		}

		// Token: 0x060009DE RID: 2526 RVA: 0x0000D625 File Offset: 0x0000B825
		public bool IsFakeIP()
		{
			return this.GetFakeIPType() > ESteamNetworkingFakeIPType.k_ESteamNetworkingFakeIPType_NotFake;
		}

		// Token: 0x060009DF RID: 2527 RVA: 0x0000D630 File Offset: 0x0000B830
		public void SetLocalHost()
		{
			NativeMethods.SteamAPI_SteamNetworkingIdentity_SetLocalHost(ref this);
		}

		// Token: 0x060009E0 RID: 2528 RVA: 0x0000D638 File Offset: 0x0000B838
		public bool IsLocalHost()
		{
			return NativeMethods.SteamAPI_SteamNetworkingIdentity_IsLocalHost(ref this);
		}

		// Token: 0x060009E1 RID: 2529 RVA: 0x0000D640 File Offset: 0x0000B840
		public bool SetGenericString(string pszString)
		{
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszString))
			{
				result = NativeMethods.SteamAPI_SteamNetworkingIdentity_SetGenericString(ref this, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x060009E2 RID: 2530 RVA: 0x0000D67C File Offset: 0x0000B87C
		public string GetGenericString()
		{
			return InteropHelp.PtrToStringUTF8(NativeMethods.SteamAPI_SteamNetworkingIdentity_GetGenericString(ref this));
		}

		// Token: 0x060009E3 RID: 2531 RVA: 0x0000D689 File Offset: 0x0000B889
		public bool SetGenericBytes(byte[] data, uint cbLen)
		{
			return NativeMethods.SteamAPI_SteamNetworkingIdentity_SetGenericBytes(ref this, data, cbLen);
		}

		// Token: 0x060009E4 RID: 2532 RVA: 0x0000D604 File Offset: 0x0000B804
		public byte[] GetGenericBytes(out int cbLen)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060009E5 RID: 2533 RVA: 0x0000D693 File Offset: 0x0000B893
		public bool Equals(SteamNetworkingIdentity x)
		{
			return NativeMethods.SteamAPI_SteamNetworkingIdentity_IsEqualTo(ref this, ref x);
		}

		// Token: 0x060009E6 RID: 2534 RVA: 0x0000D6A0 File Offset: 0x0000B8A0
		public void ToString(out string buf)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(128);
			NativeMethods.SteamAPI_SteamNetworkingIdentity_ToString(ref this, intPtr, 128U);
			buf = InteropHelp.PtrToStringUTF8(intPtr);
			Marshal.FreeHGlobal(intPtr);
		}

		// Token: 0x060009E7 RID: 2535 RVA: 0x0000D6D4 File Offset: 0x0000B8D4
		public bool ParseString(string pszStr)
		{
			bool result;
			using (InteropHelp.UTF8StringHandle utf8StringHandle = new InteropHelp.UTF8StringHandle(pszStr))
			{
				result = NativeMethods.SteamAPI_SteamNetworkingIdentity_ParseString(ref this, utf8StringHandle);
			}
			return result;
		}

		// Token: 0x04000A35 RID: 2613
		public ESteamNetworkingIdentityType m_eType;

		// Token: 0x04000A36 RID: 2614
		private int m_cbSize;

		// Token: 0x04000A37 RID: 2615
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		private uint[] m_reserved;

		// Token: 0x04000A38 RID: 2616
		public const int k_cchMaxString = 128;

		// Token: 0x04000A39 RID: 2617
		public const int k_cchMaxGenericString = 32;

		// Token: 0x04000A3A RID: 2618
		public const int k_cbMaxGenericBytes = 32;
	}
}
