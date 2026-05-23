using System;

namespace Steamworks
{
	// Token: 0x02000185 RID: 389
	[Serializable]
	public struct InputAnalogActionHandle_t : IEquatable<InputAnalogActionHandle_t>, IComparable<InputAnalogActionHandle_t>
	{
		// Token: 0x06000934 RID: 2356 RVA: 0x0000CE42 File Offset: 0x0000B042
		public InputAnalogActionHandle_t(ulong value)
		{
			this.m_InputAnalogActionHandle = value;
		}

		// Token: 0x06000935 RID: 2357 RVA: 0x0000CE4B File Offset: 0x0000B04B
		public override string ToString()
		{
			return this.m_InputAnalogActionHandle.ToString();
		}

		// Token: 0x06000936 RID: 2358 RVA: 0x0000CE58 File Offset: 0x0000B058
		public override bool Equals(object other)
		{
			return other is InputAnalogActionHandle_t && this == (InputAnalogActionHandle_t)other;
		}

		// Token: 0x06000937 RID: 2359 RVA: 0x0000CE75 File Offset: 0x0000B075
		public override int GetHashCode()
		{
			return this.m_InputAnalogActionHandle.GetHashCode();
		}

		// Token: 0x06000938 RID: 2360 RVA: 0x0000CE82 File Offset: 0x0000B082
		public static bool operator ==(InputAnalogActionHandle_t x, InputAnalogActionHandle_t y)
		{
			return x.m_InputAnalogActionHandle == y.m_InputAnalogActionHandle;
		}

		// Token: 0x06000939 RID: 2361 RVA: 0x0000CE92 File Offset: 0x0000B092
		public static bool operator !=(InputAnalogActionHandle_t x, InputAnalogActionHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x0600093A RID: 2362 RVA: 0x0000CE9E File Offset: 0x0000B09E
		public static explicit operator InputAnalogActionHandle_t(ulong value)
		{
			return new InputAnalogActionHandle_t(value);
		}

		// Token: 0x0600093B RID: 2363 RVA: 0x0000CEA6 File Offset: 0x0000B0A6
		public static explicit operator ulong(InputAnalogActionHandle_t that)
		{
			return that.m_InputAnalogActionHandle;
		}

		// Token: 0x0600093C RID: 2364 RVA: 0x0000CE82 File Offset: 0x0000B082
		public bool Equals(InputAnalogActionHandle_t other)
		{
			return this.m_InputAnalogActionHandle == other.m_InputAnalogActionHandle;
		}

		// Token: 0x0600093D RID: 2365 RVA: 0x0000CEAE File Offset: 0x0000B0AE
		public int CompareTo(InputAnalogActionHandle_t other)
		{
			return this.m_InputAnalogActionHandle.CompareTo(other.m_InputAnalogActionHandle);
		}

		// Token: 0x04000A18 RID: 2584
		public ulong m_InputAnalogActionHandle;
	}
}
