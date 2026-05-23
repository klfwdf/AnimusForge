using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200042E RID: 1070
	internal struct SessionMask
	{
		// Token: 0x06003560 RID: 13664 RVA: 0x000CF10B File Offset: 0x000CD30B
		public SessionMask(SessionMask m)
		{
			this.m_mask = m.m_mask;
		}

		// Token: 0x06003561 RID: 13665 RVA: 0x000CF119 File Offset: 0x000CD319
		public SessionMask(uint mask = 0U)
		{
			this.m_mask = mask & 15U;
		}

		// Token: 0x06003562 RID: 13666 RVA: 0x000CF125 File Offset: 0x000CD325
		public bool IsEqualOrSupersetOf(SessionMask m)
		{
			return (this.m_mask | m.m_mask) == this.m_mask;
		}

		// Token: 0x170007F2 RID: 2034
		// (get) Token: 0x06003563 RID: 13667 RVA: 0x000CF13C File Offset: 0x000CD33C
		public static SessionMask All
		{
			get
			{
				return new SessionMask(15U);
			}
		}

		// Token: 0x06003564 RID: 13668 RVA: 0x000CF145 File Offset: 0x000CD345
		public static SessionMask FromId(int perEventSourceSessionId)
		{
			return new SessionMask(1U << perEventSourceSessionId);
		}

		// Token: 0x06003565 RID: 13669 RVA: 0x000CF152 File Offset: 0x000CD352
		public ulong ToEventKeywords()
		{
			return (ulong)this.m_mask << 44;
		}

		// Token: 0x06003566 RID: 13670 RVA: 0x000CF15E File Offset: 0x000CD35E
		public static SessionMask FromEventKeywords(ulong m)
		{
			return new SessionMask((uint)(m >> 44));
		}

		// Token: 0x170007F3 RID: 2035
		public bool this[int perEventSourceSessionId]
		{
			get
			{
				return ((ulong)this.m_mask & (ulong)(1L << (perEventSourceSessionId & 31))) > 0UL;
			}
			set
			{
				if (value)
				{
					this.m_mask |= 1U << perEventSourceSessionId;
					return;
				}
				this.m_mask &= ~(1U << perEventSourceSessionId);
			}
		}

		// Token: 0x06003569 RID: 13673 RVA: 0x000CF1AC File Offset: 0x000CD3AC
		public static SessionMask operator |(SessionMask m1, SessionMask m2)
		{
			return new SessionMask(m1.m_mask | m2.m_mask);
		}

		// Token: 0x0600356A RID: 13674 RVA: 0x000CF1C0 File Offset: 0x000CD3C0
		public static SessionMask operator &(SessionMask m1, SessionMask m2)
		{
			return new SessionMask(m1.m_mask & m2.m_mask);
		}

		// Token: 0x0600356B RID: 13675 RVA: 0x000CF1D4 File Offset: 0x000CD3D4
		public static SessionMask operator ^(SessionMask m1, SessionMask m2)
		{
			return new SessionMask(m1.m_mask ^ m2.m_mask);
		}

		// Token: 0x0600356C RID: 13676 RVA: 0x000CF1E8 File Offset: 0x000CD3E8
		public static SessionMask operator ~(SessionMask m)
		{
			return new SessionMask(15U & ~m.m_mask);
		}

		// Token: 0x0600356D RID: 13677 RVA: 0x000CF1F9 File Offset: 0x000CD3F9
		public static explicit operator ulong(SessionMask m)
		{
			return (ulong)m.m_mask;
		}

		// Token: 0x0600356E RID: 13678 RVA: 0x000CF202 File Offset: 0x000CD402
		public static explicit operator uint(SessionMask m)
		{
			return m.m_mask;
		}

		// Token: 0x040017BF RID: 6079
		private uint m_mask;

		// Token: 0x040017C0 RID: 6080
		internal const int SHIFT_SESSION_TO_KEYWORD = 44;

		// Token: 0x040017C1 RID: 6081
		internal const uint MASK = 15U;

		// Token: 0x040017C2 RID: 6082
		internal const uint MAX = 4U;
	}
}
