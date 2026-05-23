using System;

namespace Steamworks
{
	// Token: 0x02000186 RID: 390
	[Serializable]
	public struct InputDigitalActionHandle_t : IEquatable<InputDigitalActionHandle_t>, IComparable<InputDigitalActionHandle_t>
	{
		// Token: 0x0600093E RID: 2366 RVA: 0x0000CEC1 File Offset: 0x0000B0C1
		public InputDigitalActionHandle_t(ulong value)
		{
			this.m_InputDigitalActionHandle = value;
		}

		// Token: 0x0600093F RID: 2367 RVA: 0x0000CECA File Offset: 0x0000B0CA
		public override string ToString()
		{
			return this.m_InputDigitalActionHandle.ToString();
		}

		// Token: 0x06000940 RID: 2368 RVA: 0x0000CED7 File Offset: 0x0000B0D7
		public override bool Equals(object other)
		{
			return other is InputDigitalActionHandle_t && this == (InputDigitalActionHandle_t)other;
		}

		// Token: 0x06000941 RID: 2369 RVA: 0x0000CEF4 File Offset: 0x0000B0F4
		public override int GetHashCode()
		{
			return this.m_InputDigitalActionHandle.GetHashCode();
		}

		// Token: 0x06000942 RID: 2370 RVA: 0x0000CF01 File Offset: 0x0000B101
		public static bool operator ==(InputDigitalActionHandle_t x, InputDigitalActionHandle_t y)
		{
			return x.m_InputDigitalActionHandle == y.m_InputDigitalActionHandle;
		}

		// Token: 0x06000943 RID: 2371 RVA: 0x0000CF11 File Offset: 0x0000B111
		public static bool operator !=(InputDigitalActionHandle_t x, InputDigitalActionHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000944 RID: 2372 RVA: 0x0000CF1D File Offset: 0x0000B11D
		public static explicit operator InputDigitalActionHandle_t(ulong value)
		{
			return new InputDigitalActionHandle_t(value);
		}

		// Token: 0x06000945 RID: 2373 RVA: 0x0000CF25 File Offset: 0x0000B125
		public static explicit operator ulong(InputDigitalActionHandle_t that)
		{
			return that.m_InputDigitalActionHandle;
		}

		// Token: 0x06000946 RID: 2374 RVA: 0x0000CF01 File Offset: 0x0000B101
		public bool Equals(InputDigitalActionHandle_t other)
		{
			return this.m_InputDigitalActionHandle == other.m_InputDigitalActionHandle;
		}

		// Token: 0x06000947 RID: 2375 RVA: 0x0000CF2D File Offset: 0x0000B12D
		public int CompareTo(InputDigitalActionHandle_t other)
		{
			return this.m_InputDigitalActionHandle.CompareTo(other.m_InputDigitalActionHandle);
		}

		// Token: 0x04000A19 RID: 2585
		public ulong m_InputDigitalActionHandle;
	}
}
