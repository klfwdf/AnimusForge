using System;

namespace Steamworks
{
	// Token: 0x02000181 RID: 385
	[Serializable]
	public struct HHTMLBrowser : IEquatable<HHTMLBrowser>, IComparable<HHTMLBrowser>
	{
		// Token: 0x06000909 RID: 2313 RVA: 0x0000CC1F File Offset: 0x0000AE1F
		public HHTMLBrowser(uint value)
		{
			this.m_HHTMLBrowser = value;
		}

		// Token: 0x0600090A RID: 2314 RVA: 0x0000CC28 File Offset: 0x0000AE28
		public override string ToString()
		{
			return this.m_HHTMLBrowser.ToString();
		}

		// Token: 0x0600090B RID: 2315 RVA: 0x0000CC35 File Offset: 0x0000AE35
		public override bool Equals(object other)
		{
			return other is HHTMLBrowser && this == (HHTMLBrowser)other;
		}

		// Token: 0x0600090C RID: 2316 RVA: 0x0000CC52 File Offset: 0x0000AE52
		public override int GetHashCode()
		{
			return this.m_HHTMLBrowser.GetHashCode();
		}

		// Token: 0x0600090D RID: 2317 RVA: 0x0000CC5F File Offset: 0x0000AE5F
		public static bool operator ==(HHTMLBrowser x, HHTMLBrowser y)
		{
			return x.m_HHTMLBrowser == y.m_HHTMLBrowser;
		}

		// Token: 0x0600090E RID: 2318 RVA: 0x0000CC6F File Offset: 0x0000AE6F
		public static bool operator !=(HHTMLBrowser x, HHTMLBrowser y)
		{
			return !(x == y);
		}

		// Token: 0x0600090F RID: 2319 RVA: 0x0000CC7B File Offset: 0x0000AE7B
		public static explicit operator HHTMLBrowser(uint value)
		{
			return new HHTMLBrowser(value);
		}

		// Token: 0x06000910 RID: 2320 RVA: 0x0000CC83 File Offset: 0x0000AE83
		public static explicit operator uint(HHTMLBrowser that)
		{
			return that.m_HHTMLBrowser;
		}

		// Token: 0x06000911 RID: 2321 RVA: 0x0000CC5F File Offset: 0x0000AE5F
		public bool Equals(HHTMLBrowser other)
		{
			return this.m_HHTMLBrowser == other.m_HHTMLBrowser;
		}

		// Token: 0x06000912 RID: 2322 RVA: 0x0000CC8B File Offset: 0x0000AE8B
		public int CompareTo(HHTMLBrowser other)
		{
			return this.m_HHTMLBrowser.CompareTo(other.m_HHTMLBrowser);
		}

		// Token: 0x04000A11 RID: 2577
		public static readonly HHTMLBrowser Invalid = new HHTMLBrowser(0U);

		// Token: 0x04000A12 RID: 2578
		public uint m_HHTMLBrowser;
	}
}
