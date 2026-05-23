using System;

namespace Steamworks
{
	// Token: 0x020001A4 RID: 420
	[Serializable]
	public struct ScreenshotHandle : IEquatable<ScreenshotHandle>, IComparable<ScreenshotHandle>
	{
		// Token: 0x06000A42 RID: 2626 RVA: 0x0000DBBD File Offset: 0x0000BDBD
		public ScreenshotHandle(uint value)
		{
			this.m_ScreenshotHandle = value;
		}

		// Token: 0x06000A43 RID: 2627 RVA: 0x0000DBC6 File Offset: 0x0000BDC6
		public override string ToString()
		{
			return this.m_ScreenshotHandle.ToString();
		}

		// Token: 0x06000A44 RID: 2628 RVA: 0x0000DBD3 File Offset: 0x0000BDD3
		public override bool Equals(object other)
		{
			return other is ScreenshotHandle && this == (ScreenshotHandle)other;
		}

		// Token: 0x06000A45 RID: 2629 RVA: 0x0000DBF0 File Offset: 0x0000BDF0
		public override int GetHashCode()
		{
			return this.m_ScreenshotHandle.GetHashCode();
		}

		// Token: 0x06000A46 RID: 2630 RVA: 0x0000DBFD File Offset: 0x0000BDFD
		public static bool operator ==(ScreenshotHandle x, ScreenshotHandle y)
		{
			return x.m_ScreenshotHandle == y.m_ScreenshotHandle;
		}

		// Token: 0x06000A47 RID: 2631 RVA: 0x0000DC0D File Offset: 0x0000BE0D
		public static bool operator !=(ScreenshotHandle x, ScreenshotHandle y)
		{
			return !(x == y);
		}

		// Token: 0x06000A48 RID: 2632 RVA: 0x0000DC19 File Offset: 0x0000BE19
		public static explicit operator ScreenshotHandle(uint value)
		{
			return new ScreenshotHandle(value);
		}

		// Token: 0x06000A49 RID: 2633 RVA: 0x0000DC21 File Offset: 0x0000BE21
		public static explicit operator uint(ScreenshotHandle that)
		{
			return that.m_ScreenshotHandle;
		}

		// Token: 0x06000A4A RID: 2634 RVA: 0x0000DBFD File Offset: 0x0000BDFD
		public bool Equals(ScreenshotHandle other)
		{
			return this.m_ScreenshotHandle == other.m_ScreenshotHandle;
		}

		// Token: 0x06000A4B RID: 2635 RVA: 0x0000DC29 File Offset: 0x0000BE29
		public int CompareTo(ScreenshotHandle other)
		{
			return this.m_ScreenshotHandle.CompareTo(other.m_ScreenshotHandle);
		}

		// Token: 0x04000A57 RID: 2647
		public static readonly ScreenshotHandle Invalid = new ScreenshotHandle(0U);

		// Token: 0x04000A58 RID: 2648
		public uint m_ScreenshotHandle;
	}
}
