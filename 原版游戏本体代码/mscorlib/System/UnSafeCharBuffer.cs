using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System
{
	// Token: 0x02000158 RID: 344
	internal struct UnSafeCharBuffer
	{
		// Token: 0x0600156D RID: 5485 RVA: 0x0003EC47 File Offset: 0x0003CE47
		[SecurityCritical]
		public unsafe UnSafeCharBuffer(char* buffer, int bufferSize)
		{
			this.m_buffer = buffer;
			this.m_totalSize = bufferSize;
			this.m_length = 0;
		}

		// Token: 0x0600156E RID: 5486 RVA: 0x0003EC60 File Offset: 0x0003CE60
		[SecuritySafeCritical]
		public unsafe void AppendString(string stringToAppend)
		{
			if (string.IsNullOrEmpty(stringToAppend))
			{
				return;
			}
			if (this.m_totalSize - this.m_length < stringToAppend.Length)
			{
				throw new IndexOutOfRangeException();
			}
			fixed (string text = stringToAppend)
			{
				char* ptr = text;
				if (ptr != null)
				{
					ptr += RuntimeHelpers.OffsetToStringData / 2;
				}
				Buffer.Memcpy((byte*)(this.m_buffer + this.m_length), (byte*)ptr, stringToAppend.Length * 2);
			}
			this.m_length += stringToAppend.Length;
		}

		// Token: 0x17000262 RID: 610
		// (get) Token: 0x0600156F RID: 5487 RVA: 0x0003ECD4 File Offset: 0x0003CED4
		public int Length
		{
			get
			{
				return this.m_length;
			}
		}

		// Token: 0x04000712 RID: 1810
		[SecurityCritical]
		private unsafe char* m_buffer;

		// Token: 0x04000713 RID: 1811
		private int m_totalSize;

		// Token: 0x04000714 RID: 1812
		private int m_length;
	}
}
