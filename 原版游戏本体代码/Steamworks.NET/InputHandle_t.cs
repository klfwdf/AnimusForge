using System;

namespace Steamworks
{
	// Token: 0x02000187 RID: 391
	[Serializable]
	public struct InputHandle_t : IEquatable<InputHandle_t>, IComparable<InputHandle_t>
	{
		// Token: 0x06000948 RID: 2376 RVA: 0x0000CF40 File Offset: 0x0000B140
		public InputHandle_t(ulong value)
		{
			this.m_InputHandle = value;
		}

		// Token: 0x06000949 RID: 2377 RVA: 0x0000CF49 File Offset: 0x0000B149
		public override string ToString()
		{
			return this.m_InputHandle.ToString();
		}

		// Token: 0x0600094A RID: 2378 RVA: 0x0000CF56 File Offset: 0x0000B156
		public override bool Equals(object other)
		{
			return other is InputHandle_t && this == (InputHandle_t)other;
		}

		// Token: 0x0600094B RID: 2379 RVA: 0x0000CF73 File Offset: 0x0000B173
		public override int GetHashCode()
		{
			return this.m_InputHandle.GetHashCode();
		}

		// Token: 0x0600094C RID: 2380 RVA: 0x0000CF80 File Offset: 0x0000B180
		public static bool operator ==(InputHandle_t x, InputHandle_t y)
		{
			return x.m_InputHandle == y.m_InputHandle;
		}

		// Token: 0x0600094D RID: 2381 RVA: 0x0000CF90 File Offset: 0x0000B190
		public static bool operator !=(InputHandle_t x, InputHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x0600094E RID: 2382 RVA: 0x0000CF9C File Offset: 0x0000B19C
		public static explicit operator InputHandle_t(ulong value)
		{
			return new InputHandle_t(value);
		}

		// Token: 0x0600094F RID: 2383 RVA: 0x0000CFA4 File Offset: 0x0000B1A4
		public static explicit operator ulong(InputHandle_t that)
		{
			return that.m_InputHandle;
		}

		// Token: 0x06000950 RID: 2384 RVA: 0x0000CF80 File Offset: 0x0000B180
		public bool Equals(InputHandle_t other)
		{
			return this.m_InputHandle == other.m_InputHandle;
		}

		// Token: 0x06000951 RID: 2385 RVA: 0x0000CFAC File Offset: 0x0000B1AC
		public int CompareTo(InputHandle_t other)
		{
			return this.m_InputHandle.CompareTo(other.m_InputHandle);
		}

		// Token: 0x04000A1A RID: 2586
		public ulong m_InputHandle;
	}
}
