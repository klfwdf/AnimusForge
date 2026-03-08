using System;

namespace Steamworks
{
	// Token: 0x0200017B RID: 379
	[Serializable]
	public struct CGameID : IEquatable<CGameID>, IComparable<CGameID>
	{
		// Token: 0x060008B1 RID: 2225 RVA: 0x0000C4DB File Offset: 0x0000A6DB
		public CGameID(ulong GameID)
		{
			this.m_GameID = GameID;
		}

		// Token: 0x060008B2 RID: 2226 RVA: 0x0000C4E4 File Offset: 0x0000A6E4
		public CGameID(AppId_t nAppID)
		{
			this.m_GameID = 0UL;
			this.SetAppID(nAppID);
		}

		// Token: 0x060008B3 RID: 2227 RVA: 0x0000C4F5 File Offset: 0x0000A6F5
		public CGameID(AppId_t nAppID, uint nModID)
		{
			this.m_GameID = 0UL;
			this.SetAppID(nAppID);
			this.SetType(CGameID.EGameIDType.k_EGameIDTypeGameMod);
			this.SetModID(nModID);
		}

		// Token: 0x060008B4 RID: 2228 RVA: 0x0000C514 File Offset: 0x0000A714
		public bool IsSteamApp()
		{
			return this.Type() == CGameID.EGameIDType.k_EGameIDTypeApp;
		}

		// Token: 0x060008B5 RID: 2229 RVA: 0x0000C51F File Offset: 0x0000A71F
		public bool IsMod()
		{
			return this.Type() == CGameID.EGameIDType.k_EGameIDTypeGameMod;
		}

		// Token: 0x060008B6 RID: 2230 RVA: 0x0000C52A File Offset: 0x0000A72A
		public bool IsShortcut()
		{
			return this.Type() == CGameID.EGameIDType.k_EGameIDTypeShortcut;
		}

		// Token: 0x060008B7 RID: 2231 RVA: 0x0000C535 File Offset: 0x0000A735
		public bool IsP2PFile()
		{
			return this.Type() == CGameID.EGameIDType.k_EGameIDTypeP2P;
		}

		// Token: 0x060008B8 RID: 2232 RVA: 0x0000C540 File Offset: 0x0000A740
		public AppId_t AppID()
		{
			return new AppId_t((uint)(this.m_GameID & 16777215UL));
		}

		// Token: 0x060008B9 RID: 2233 RVA: 0x0000C555 File Offset: 0x0000A755
		public CGameID.EGameIDType Type()
		{
			return (CGameID.EGameIDType)((this.m_GameID >> 24) & 255UL);
		}

		// Token: 0x060008BA RID: 2234 RVA: 0x0000C568 File Offset: 0x0000A768
		public uint ModID()
		{
			return (uint)((this.m_GameID >> 32) & (ulong)(-1));
		}

		// Token: 0x060008BB RID: 2235 RVA: 0x0000C578 File Offset: 0x0000A778
		public bool IsValid()
		{
			switch (this.Type())
			{
			case CGameID.EGameIDType.k_EGameIDTypeApp:
				return this.AppID() != AppId_t.Invalid;
			case CGameID.EGameIDType.k_EGameIDTypeGameMod:
				return this.AppID() != AppId_t.Invalid && (this.ModID() & 2147483648U) > 0U;
			case CGameID.EGameIDType.k_EGameIDTypeShortcut:
				return (this.ModID() & 2147483648U) > 0U;
			case CGameID.EGameIDType.k_EGameIDTypeP2P:
				return this.AppID() == AppId_t.Invalid && (this.ModID() & 2147483648U) > 0U;
			default:
				return false;
			}
		}

		// Token: 0x060008BC RID: 2236 RVA: 0x0000C60E File Offset: 0x0000A80E
		public void Reset()
		{
			this.m_GameID = 0UL;
		}

		// Token: 0x060008BD RID: 2237 RVA: 0x0000C4DB File Offset: 0x0000A6DB
		public void Set(ulong GameID)
		{
			this.m_GameID = GameID;
		}

		// Token: 0x060008BE RID: 2238 RVA: 0x0000C618 File Offset: 0x0000A818
		private void SetAppID(AppId_t other)
		{
			this.m_GameID = (this.m_GameID & 18446744073692774400UL) | ((ulong)(uint)other & 16777215UL);
		}

		// Token: 0x060008BF RID: 2239 RVA: 0x0000C63C File Offset: 0x0000A83C
		private void SetType(CGameID.EGameIDType other)
		{
			this.m_GameID = (this.m_GameID & 18446744069431361535UL) | (ulong)((ulong)((long)other & 255L) << 24);
		}

		// Token: 0x060008C0 RID: 2240 RVA: 0x0000C661 File Offset: 0x0000A861
		private void SetModID(uint other)
		{
			this.m_GameID = (this.m_GameID & (ulong)(-1)) | (((ulong)other & (ulong)(-1)) << 32);
		}

		// Token: 0x060008C1 RID: 2241 RVA: 0x0000C67B File Offset: 0x0000A87B
		public override string ToString()
		{
			return this.m_GameID.ToString();
		}

		// Token: 0x060008C2 RID: 2242 RVA: 0x0000C688 File Offset: 0x0000A888
		public override bool Equals(object other)
		{
			return other is CGameID && this == (CGameID)other;
		}

		// Token: 0x060008C3 RID: 2243 RVA: 0x0000C6A5 File Offset: 0x0000A8A5
		public override int GetHashCode()
		{
			return this.m_GameID.GetHashCode();
		}

		// Token: 0x060008C4 RID: 2244 RVA: 0x0000C6B2 File Offset: 0x0000A8B2
		public static bool operator ==(CGameID x, CGameID y)
		{
			return x.m_GameID == y.m_GameID;
		}

		// Token: 0x060008C5 RID: 2245 RVA: 0x0000C6C2 File Offset: 0x0000A8C2
		public static bool operator !=(CGameID x, CGameID y)
		{
			return !(x == y);
		}

		// Token: 0x060008C6 RID: 2246 RVA: 0x0000C6CE File Offset: 0x0000A8CE
		public static explicit operator CGameID(ulong value)
		{
			return new CGameID(value);
		}

		// Token: 0x060008C7 RID: 2247 RVA: 0x0000C6D6 File Offset: 0x0000A8D6
		public static explicit operator ulong(CGameID that)
		{
			return that.m_GameID;
		}

		// Token: 0x060008C8 RID: 2248 RVA: 0x0000C6B2 File Offset: 0x0000A8B2
		public bool Equals(CGameID other)
		{
			return this.m_GameID == other.m_GameID;
		}

		// Token: 0x060008C9 RID: 2249 RVA: 0x0000C6DE File Offset: 0x0000A8DE
		public int CompareTo(CGameID other)
		{
			return this.m_GameID.CompareTo(other.m_GameID);
		}

		// Token: 0x040009FA RID: 2554
		public ulong m_GameID;

		// Token: 0x020001C5 RID: 453
		public enum EGameIDType
		{
			// Token: 0x04000AC1 RID: 2753
			k_EGameIDTypeApp,
			// Token: 0x04000AC2 RID: 2754
			k_EGameIDTypeGameMod,
			// Token: 0x04000AC3 RID: 2755
			k_EGameIDTypeShortcut,
			// Token: 0x04000AC4 RID: 2756
			k_EGameIDTypeP2P
		}
	}
}
