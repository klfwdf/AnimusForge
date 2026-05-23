using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000242 RID: 578
	[ComVisible(true)]
	public sealed class KeySizes
	{
		// Token: 0x170003E8 RID: 1000
		// (get) Token: 0x060020A7 RID: 8359 RVA: 0x000728A7 File Offset: 0x00070AA7
		public int MinSize
		{
			get
			{
				return this.m_minSize;
			}
		}

		// Token: 0x170003E9 RID: 1001
		// (get) Token: 0x060020A8 RID: 8360 RVA: 0x000728AF File Offset: 0x00070AAF
		public int MaxSize
		{
			get
			{
				return this.m_maxSize;
			}
		}

		// Token: 0x170003EA RID: 1002
		// (get) Token: 0x060020A9 RID: 8361 RVA: 0x000728B7 File Offset: 0x00070AB7
		public int SkipSize
		{
			get
			{
				return this.m_skipSize;
			}
		}

		// Token: 0x060020AA RID: 8362 RVA: 0x000728BF File Offset: 0x00070ABF
		public KeySizes(int minSize, int maxSize, int skipSize)
		{
			this.m_minSize = minSize;
			this.m_maxSize = maxSize;
			this.m_skipSize = skipSize;
		}

		// Token: 0x04000BDF RID: 3039
		private int m_minSize;

		// Token: 0x04000BE0 RID: 3040
		private int m_maxSize;

		// Token: 0x04000BE1 RID: 3041
		private int m_skipSize;
	}
}
