using System;

namespace Steamworks
{
	// Token: 0x02000182 RID: 386
	[Serializable]
	public struct HTTPCookieContainerHandle : IEquatable<HTTPCookieContainerHandle>, IComparable<HTTPCookieContainerHandle>
	{
		// Token: 0x06000914 RID: 2324 RVA: 0x0000CCAB File Offset: 0x0000AEAB
		public HTTPCookieContainerHandle(uint value)
		{
			this.m_HTTPCookieContainerHandle = value;
		}

		// Token: 0x06000915 RID: 2325 RVA: 0x0000CCB4 File Offset: 0x0000AEB4
		public override string ToString()
		{
			return this.m_HTTPCookieContainerHandle.ToString();
		}

		// Token: 0x06000916 RID: 2326 RVA: 0x0000CCC1 File Offset: 0x0000AEC1
		public override bool Equals(object other)
		{
			return other is HTTPCookieContainerHandle && this == (HTTPCookieContainerHandle)other;
		}

		// Token: 0x06000917 RID: 2327 RVA: 0x0000CCDE File Offset: 0x0000AEDE
		public override int GetHashCode()
		{
			return this.m_HTTPCookieContainerHandle.GetHashCode();
		}

		// Token: 0x06000918 RID: 2328 RVA: 0x0000CCEB File Offset: 0x0000AEEB
		public static bool operator ==(HTTPCookieContainerHandle x, HTTPCookieContainerHandle y)
		{
			return x.m_HTTPCookieContainerHandle == y.m_HTTPCookieContainerHandle;
		}

		// Token: 0x06000919 RID: 2329 RVA: 0x0000CCFB File Offset: 0x0000AEFB
		public static bool operator !=(HTTPCookieContainerHandle x, HTTPCookieContainerHandle y)
		{
			return !(x == y);
		}

		// Token: 0x0600091A RID: 2330 RVA: 0x0000CD07 File Offset: 0x0000AF07
		public static explicit operator HTTPCookieContainerHandle(uint value)
		{
			return new HTTPCookieContainerHandle(value);
		}

		// Token: 0x0600091B RID: 2331 RVA: 0x0000CD0F File Offset: 0x0000AF0F
		public static explicit operator uint(HTTPCookieContainerHandle that)
		{
			return that.m_HTTPCookieContainerHandle;
		}

		// Token: 0x0600091C RID: 2332 RVA: 0x0000CCEB File Offset: 0x0000AEEB
		public bool Equals(HTTPCookieContainerHandle other)
		{
			return this.m_HTTPCookieContainerHandle == other.m_HTTPCookieContainerHandle;
		}

		// Token: 0x0600091D RID: 2333 RVA: 0x0000CD17 File Offset: 0x0000AF17
		public int CompareTo(HTTPCookieContainerHandle other)
		{
			return this.m_HTTPCookieContainerHandle.CompareTo(other.m_HTTPCookieContainerHandle);
		}

		// Token: 0x04000A13 RID: 2579
		public static readonly HTTPCookieContainerHandle Invalid = new HTTPCookieContainerHandle(0U);

		// Token: 0x04000A14 RID: 2580
		public uint m_HTTPCookieContainerHandle;
	}
}
