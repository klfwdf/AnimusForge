using System;

namespace Steamworks
{
	// Token: 0x02000183 RID: 387
	[Serializable]
	public struct HTTPRequestHandle : IEquatable<HTTPRequestHandle>, IComparable<HTTPRequestHandle>
	{
		// Token: 0x0600091F RID: 2335 RVA: 0x0000CD37 File Offset: 0x0000AF37
		public HTTPRequestHandle(uint value)
		{
			this.m_HTTPRequestHandle = value;
		}

		// Token: 0x06000920 RID: 2336 RVA: 0x0000CD40 File Offset: 0x0000AF40
		public override string ToString()
		{
			return this.m_HTTPRequestHandle.ToString();
		}

		// Token: 0x06000921 RID: 2337 RVA: 0x0000CD4D File Offset: 0x0000AF4D
		public override bool Equals(object other)
		{
			return other is HTTPRequestHandle && this == (HTTPRequestHandle)other;
		}

		// Token: 0x06000922 RID: 2338 RVA: 0x0000CD6A File Offset: 0x0000AF6A
		public override int GetHashCode()
		{
			return this.m_HTTPRequestHandle.GetHashCode();
		}

		// Token: 0x06000923 RID: 2339 RVA: 0x0000CD77 File Offset: 0x0000AF77
		public static bool operator ==(HTTPRequestHandle x, HTTPRequestHandle y)
		{
			return x.m_HTTPRequestHandle == y.m_HTTPRequestHandle;
		}

		// Token: 0x06000924 RID: 2340 RVA: 0x0000CD87 File Offset: 0x0000AF87
		public static bool operator !=(HTTPRequestHandle x, HTTPRequestHandle y)
		{
			return !(x == y);
		}

		// Token: 0x06000925 RID: 2341 RVA: 0x0000CD93 File Offset: 0x0000AF93
		public static explicit operator HTTPRequestHandle(uint value)
		{
			return new HTTPRequestHandle(value);
		}

		// Token: 0x06000926 RID: 2342 RVA: 0x0000CD9B File Offset: 0x0000AF9B
		public static explicit operator uint(HTTPRequestHandle that)
		{
			return that.m_HTTPRequestHandle;
		}

		// Token: 0x06000927 RID: 2343 RVA: 0x0000CD77 File Offset: 0x0000AF77
		public bool Equals(HTTPRequestHandle other)
		{
			return this.m_HTTPRequestHandle == other.m_HTTPRequestHandle;
		}

		// Token: 0x06000928 RID: 2344 RVA: 0x0000CDA3 File Offset: 0x0000AFA3
		public int CompareTo(HTTPRequestHandle other)
		{
			return this.m_HTTPRequestHandle.CompareTo(other.m_HTTPRequestHandle);
		}

		// Token: 0x04000A15 RID: 2581
		public static readonly HTTPRequestHandle Invalid = new HTTPRequestHandle(0U);

		// Token: 0x04000A16 RID: 2582
		public uint m_HTTPRequestHandle;
	}
}
