using System;
using System.Security;

namespace System.Reflection
{
	// Token: 0x020005FB RID: 1531
	[Serializable]
	internal struct ConstArray
	{
		// Token: 0x17000A9D RID: 2717
		// (get) Token: 0x0600467A RID: 18042 RVA: 0x00102A23 File Offset: 0x00100C23
		public IntPtr Signature
		{
			get
			{
				return this.m_constArray;
			}
		}

		// Token: 0x17000A9E RID: 2718
		// (get) Token: 0x0600467B RID: 18043 RVA: 0x00102A2B File Offset: 0x00100C2B
		public int Length
		{
			get
			{
				return this.m_length;
			}
		}

		// Token: 0x17000A9F RID: 2719
		public unsafe byte this[int index]
		{
			[SecuritySafeCritical]
			get
			{
				if (index < 0 || index >= this.m_length)
				{
					throw new IndexOutOfRangeException();
				}
				return ((byte*)this.m_constArray.ToPointer())[index];
			}
		}

		// Token: 0x04001D4E RID: 7502
		internal int m_length;

		// Token: 0x04001D4F RID: 7503
		internal IntPtr m_constArray;
	}
}
