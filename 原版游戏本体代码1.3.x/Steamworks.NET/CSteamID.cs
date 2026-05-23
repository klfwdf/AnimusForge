using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200017C RID: 380
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct CSteamID : IEquatable<CSteamID>, IComparable<CSteamID>
	{
		// Token: 0x060008CA RID: 2250 RVA: 0x0000C6F1 File Offset: 0x0000A8F1
		public CSteamID(AccountID_t unAccountID, EUniverse eUniverse, EAccountType eAccountType)
		{
			this.m_SteamID = 0UL;
			this.Set(unAccountID, eUniverse, eAccountType);
		}

		// Token: 0x060008CB RID: 2251 RVA: 0x0000C704 File Offset: 0x0000A904
		public CSteamID(AccountID_t unAccountID, uint unAccountInstance, EUniverse eUniverse, EAccountType eAccountType)
		{
			this.m_SteamID = 0UL;
			this.InstancedSet(unAccountID, unAccountInstance, eUniverse, eAccountType);
		}

		// Token: 0x060008CC RID: 2252 RVA: 0x0000C719 File Offset: 0x0000A919
		public CSteamID(ulong ulSteamID)
		{
			this.m_SteamID = ulSteamID;
		}

		// Token: 0x060008CD RID: 2253 RVA: 0x0000C722 File Offset: 0x0000A922
		public void Set(AccountID_t unAccountID, EUniverse eUniverse, EAccountType eAccountType)
		{
			this.SetAccountID(unAccountID);
			this.SetEUniverse(eUniverse);
			this.SetEAccountType(eAccountType);
			if (eAccountType == EAccountType.k_EAccountTypeClan || eAccountType == EAccountType.k_EAccountTypeGameServer)
			{
				this.SetAccountInstance(0U);
				return;
			}
			this.SetAccountInstance(1U);
		}

		// Token: 0x060008CE RID: 2254 RVA: 0x0000C750 File Offset: 0x0000A950
		public void InstancedSet(AccountID_t unAccountID, uint unInstance, EUniverse eUniverse, EAccountType eAccountType)
		{
			this.SetAccountID(unAccountID);
			this.SetEUniverse(eUniverse);
			this.SetEAccountType(eAccountType);
			this.SetAccountInstance(unInstance);
		}

		// Token: 0x060008CF RID: 2255 RVA: 0x0000C76F File Offset: 0x0000A96F
		public void Clear()
		{
			this.m_SteamID = 0UL;
		}

		// Token: 0x060008D0 RID: 2256 RVA: 0x0000C779 File Offset: 0x0000A979
		public void CreateBlankAnonLogon(EUniverse eUniverse)
		{
			this.SetAccountID(new AccountID_t(0U));
			this.SetEUniverse(eUniverse);
			this.SetEAccountType(EAccountType.k_EAccountTypeAnonGameServer);
			this.SetAccountInstance(0U);
		}

		// Token: 0x060008D1 RID: 2257 RVA: 0x0000C79C File Offset: 0x0000A99C
		public void CreateBlankAnonUserLogon(EUniverse eUniverse)
		{
			this.SetAccountID(new AccountID_t(0U));
			this.SetEUniverse(eUniverse);
			this.SetEAccountType(EAccountType.k_EAccountTypeAnonUser);
			this.SetAccountInstance(0U);
		}

		// Token: 0x060008D2 RID: 2258 RVA: 0x0000C7C0 File Offset: 0x0000A9C0
		public bool BBlankAnonAccount()
		{
			return this.GetAccountID() == new AccountID_t(0U) && this.BAnonAccount() && this.GetUnAccountInstance() == 0U;
		}

		// Token: 0x060008D3 RID: 2259 RVA: 0x0000C7E8 File Offset: 0x0000A9E8
		public bool BGameServerAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeGameServer || this.GetEAccountType() == EAccountType.k_EAccountTypeAnonGameServer;
		}

		// Token: 0x060008D4 RID: 2260 RVA: 0x0000C7FE File Offset: 0x0000A9FE
		public bool BPersistentGameServerAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeGameServer;
		}

		// Token: 0x060008D5 RID: 2261 RVA: 0x0000C809 File Offset: 0x0000AA09
		public bool BAnonGameServerAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeAnonGameServer;
		}

		// Token: 0x060008D6 RID: 2262 RVA: 0x0000C814 File Offset: 0x0000AA14
		public bool BContentServerAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeContentServer;
		}

		// Token: 0x060008D7 RID: 2263 RVA: 0x0000C81F File Offset: 0x0000AA1F
		public bool BClanAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeClan;
		}

		// Token: 0x060008D8 RID: 2264 RVA: 0x0000C82A File Offset: 0x0000AA2A
		public bool BChatAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeChat;
		}

		// Token: 0x060008D9 RID: 2265 RVA: 0x0000C835 File Offset: 0x0000AA35
		public bool IsLobby()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeChat && (this.GetUnAccountInstance() & 262144U) > 0U;
		}

		// Token: 0x060008DA RID: 2266 RVA: 0x0000C851 File Offset: 0x0000AA51
		public bool BIndividualAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeIndividual || this.GetEAccountType() == EAccountType.k_EAccountTypeConsoleUser;
		}

		// Token: 0x060008DB RID: 2267 RVA: 0x0000C868 File Offset: 0x0000AA68
		public bool BAnonAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeAnonUser || this.GetEAccountType() == EAccountType.k_EAccountTypeAnonGameServer;
		}

		// Token: 0x060008DC RID: 2268 RVA: 0x0000C87F File Offset: 0x0000AA7F
		public bool BAnonUserAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeAnonUser;
		}

		// Token: 0x060008DD RID: 2269 RVA: 0x0000C88B File Offset: 0x0000AA8B
		public bool BConsoleUserAccount()
		{
			return this.GetEAccountType() == EAccountType.k_EAccountTypeConsoleUser;
		}

		// Token: 0x060008DE RID: 2270 RVA: 0x0000C897 File Offset: 0x0000AA97
		public void SetAccountID(AccountID_t other)
		{
			this.m_SteamID = (this.m_SteamID & 18446744069414584320UL) | ((ulong)(uint)other & (ulong)(-1));
		}

		// Token: 0x060008DF RID: 2271 RVA: 0x0000C8BA File Offset: 0x0000AABA
		public void SetAccountInstance(uint other)
		{
			this.m_SteamID = (this.m_SteamID & 18442240478377148415UL) | (((ulong)other & 1048575UL) << 32);
		}

		// Token: 0x060008E0 RID: 2272 RVA: 0x0000C8DF File Offset: 0x0000AADF
		public void SetEAccountType(EAccountType other)
		{
			this.m_SteamID = (this.m_SteamID & 18379190079298994175UL) | (ulong)((ulong)((long)other & 15L) << 52);
		}

		// Token: 0x060008E1 RID: 2273 RVA: 0x0000C901 File Offset: 0x0000AB01
		public void SetEUniverse(EUniverse other)
		{
			this.m_SteamID = (this.m_SteamID & 72057594037927935UL) | (ulong)((ulong)((long)other & 255L) << 56);
		}

		// Token: 0x060008E2 RID: 2274 RVA: 0x0000C926 File Offset: 0x0000AB26
		public AccountID_t GetAccountID()
		{
			return new AccountID_t((uint)(this.m_SteamID & (ulong)(-1)));
		}

		// Token: 0x060008E3 RID: 2275 RVA: 0x0000C937 File Offset: 0x0000AB37
		public uint GetUnAccountInstance()
		{
			return (uint)((this.m_SteamID >> 32) & 1048575UL);
		}

		// Token: 0x060008E4 RID: 2276 RVA: 0x0000C94A File Offset: 0x0000AB4A
		public EAccountType GetEAccountType()
		{
			return (EAccountType)((this.m_SteamID >> 52) & 15UL);
		}

		// Token: 0x060008E5 RID: 2277 RVA: 0x0000C95A File Offset: 0x0000AB5A
		public EUniverse GetEUniverse()
		{
			return (EUniverse)((this.m_SteamID >> 56) & 255UL);
		}

		// Token: 0x060008E6 RID: 2278 RVA: 0x0000C970 File Offset: 0x0000AB70
		public bool IsValid()
		{
			return this.GetEAccountType() > EAccountType.k_EAccountTypeInvalid && this.GetEAccountType() < EAccountType.k_EAccountTypeMax && this.GetEUniverse() > EUniverse.k_EUniverseInvalid && this.GetEUniverse() < EUniverse.k_EUniverseMax && (this.GetEAccountType() != EAccountType.k_EAccountTypeIndividual || (!(this.GetAccountID() == new AccountID_t(0U)) && this.GetUnAccountInstance() <= 1U)) && (this.GetEAccountType() != EAccountType.k_EAccountTypeClan || (!(this.GetAccountID() == new AccountID_t(0U)) && this.GetUnAccountInstance() == 0U)) && (this.GetEAccountType() != EAccountType.k_EAccountTypeGameServer || !(this.GetAccountID() == new AccountID_t(0U)));
		}

		// Token: 0x060008E7 RID: 2279 RVA: 0x0000CA12 File Offset: 0x0000AC12
		public override string ToString()
		{
			return this.m_SteamID.ToString();
		}

		// Token: 0x060008E8 RID: 2280 RVA: 0x0000CA1F File Offset: 0x0000AC1F
		public override bool Equals(object other)
		{
			return other is CSteamID && this == (CSteamID)other;
		}

		// Token: 0x060008E9 RID: 2281 RVA: 0x0000CA3C File Offset: 0x0000AC3C
		public override int GetHashCode()
		{
			return this.m_SteamID.GetHashCode();
		}

		// Token: 0x060008EA RID: 2282 RVA: 0x0000CA49 File Offset: 0x0000AC49
		public static bool operator ==(CSteamID x, CSteamID y)
		{
			return x.m_SteamID == y.m_SteamID;
		}

		// Token: 0x060008EB RID: 2283 RVA: 0x0000CA59 File Offset: 0x0000AC59
		public static bool operator !=(CSteamID x, CSteamID y)
		{
			return !(x == y);
		}

		// Token: 0x060008EC RID: 2284 RVA: 0x0000CA65 File Offset: 0x0000AC65
		public static explicit operator CSteamID(ulong value)
		{
			return new CSteamID(value);
		}

		// Token: 0x060008ED RID: 2285 RVA: 0x0000CA6D File Offset: 0x0000AC6D
		public static explicit operator ulong(CSteamID that)
		{
			return that.m_SteamID;
		}

		// Token: 0x060008EE RID: 2286 RVA: 0x0000CA49 File Offset: 0x0000AC49
		public bool Equals(CSteamID other)
		{
			return this.m_SteamID == other.m_SteamID;
		}

		// Token: 0x060008EF RID: 2287 RVA: 0x0000CA75 File Offset: 0x0000AC75
		public int CompareTo(CSteamID other)
		{
			return this.m_SteamID.CompareTo(other.m_SteamID);
		}

		// Token: 0x040009FB RID: 2555
		public static readonly CSteamID Nil = default(CSteamID);

		// Token: 0x040009FC RID: 2556
		public static readonly CSteamID OutofDateGS = new CSteamID(new AccountID_t(0U), 0U, EUniverse.k_EUniverseInvalid, EAccountType.k_EAccountTypeInvalid);

		// Token: 0x040009FD RID: 2557
		public static readonly CSteamID LanModeGS = new CSteamID(new AccountID_t(0U), 0U, EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeInvalid);

		// Token: 0x040009FE RID: 2558
		public static readonly CSteamID NotInitYetGS = new CSteamID(new AccountID_t(1U), 0U, EUniverse.k_EUniverseInvalid, EAccountType.k_EAccountTypeInvalid);

		// Token: 0x040009FF RID: 2559
		public static readonly CSteamID NonSteamGS = new CSteamID(new AccountID_t(2U), 0U, EUniverse.k_EUniverseInvalid, EAccountType.k_EAccountTypeInvalid);

		// Token: 0x04000A00 RID: 2560
		public ulong m_SteamID;
	}
}
