using System;

namespace Iced.Intel
{
	// Token: 0x0200065D RID: 1629
	internal readonly struct MemorySizeInfo
	{
		// Token: 0x170007F4 RID: 2036
		// (get) Token: 0x06002374 RID: 9076 RVA: 0x00072AA7 File Offset: 0x00070CA7
		public MemorySize MemorySize
		{
			get
			{
				return (MemorySize)this.memorySize;
			}
		}

		// Token: 0x170007F5 RID: 2037
		// (get) Token: 0x06002375 RID: 9077 RVA: 0x00072AAF File Offset: 0x00070CAF
		public int Size
		{
			get
			{
				return (int)this.size;
			}
		}

		// Token: 0x170007F6 RID: 2038
		// (get) Token: 0x06002376 RID: 9078 RVA: 0x00072AB7 File Offset: 0x00070CB7
		public int ElementSize
		{
			get
			{
				return (int)this.elementSize;
			}
		}

		// Token: 0x170007F7 RID: 2039
		// (get) Token: 0x06002377 RID: 9079 RVA: 0x00072ABF File Offset: 0x00070CBF
		public MemorySize ElementType
		{
			get
			{
				return (MemorySize)this.elementType;
			}
		}

		// Token: 0x170007F8 RID: 2040
		// (get) Token: 0x06002378 RID: 9080 RVA: 0x00072AC7 File Offset: 0x00070CC7
		public bool IsSigned
		{
			get
			{
				return this.isSigned;
			}
		}

		// Token: 0x170007F9 RID: 2041
		// (get) Token: 0x06002379 RID: 9081 RVA: 0x00072ACF File Offset: 0x00070CCF
		public bool IsBroadcast
		{
			get
			{
				return this.isBroadcast;
			}
		}

		// Token: 0x170007FA RID: 2042
		// (get) Token: 0x0600237A RID: 9082 RVA: 0x00072AD7 File Offset: 0x00070CD7
		public bool IsPacked
		{
			get
			{
				return this.elementSize < this.size;
			}
		}

		// Token: 0x170007FB RID: 2043
		// (get) Token: 0x0600237B RID: 9083 RVA: 0x00072AE7 File Offset: 0x00070CE7
		public int ElementCount
		{
			get
			{
				if (this.elementSize != this.size)
				{
					return (int)(this.size / this.elementSize);
				}
				return 1;
			}
		}

		// Token: 0x0600237C RID: 9084 RVA: 0x00072B08 File Offset: 0x00070D08
		public MemorySizeInfo(MemorySize memorySize, int size, int elementSize, MemorySize elementType, bool isSigned, bool isBroadcast)
		{
			if (size < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_size();
			}
			if (elementSize < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_elementSize();
			}
			if (elementSize > size)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_elementSize();
			}
			this.memorySize = (byte)memorySize;
			this.size = (ushort)size;
			this.elementSize = (ushort)elementSize;
			this.elementType = (byte)elementType;
			this.isSigned = isSigned;
			this.isBroadcast = isBroadcast;
		}

		// Token: 0x04002BA5 RID: 11173
		private readonly ushort size;

		// Token: 0x04002BA6 RID: 11174
		private readonly ushort elementSize;

		// Token: 0x04002BA7 RID: 11175
		private readonly byte memorySize;

		// Token: 0x04002BA8 RID: 11176
		private readonly byte elementType;

		// Token: 0x04002BA9 RID: 11177
		private readonly bool isSigned;

		// Token: 0x04002BAA RID: 11178
		private readonly bool isBroadcast;
	}
}
