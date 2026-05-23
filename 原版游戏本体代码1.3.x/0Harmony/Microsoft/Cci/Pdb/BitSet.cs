using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200036C RID: 876
	internal struct BitSet
	{
		// Token: 0x06001764 RID: 5988 RVA: 0x0004826B File Offset: 0x0004646B
		internal BitSet(BitAccess bits)
		{
			bits.ReadInt32(out this.size);
			this.words = new uint[this.size];
			bits.ReadUInt32(this.words);
		}

		// Token: 0x06001765 RID: 5989 RVA: 0x00048298 File Offset: 0x00046498
		internal bool IsSet(int index)
		{
			int word = index / 32;
			return word < this.size && (this.words[word] & BitSet.GetBit(index)) > 0U;
		}

		// Token: 0x06001766 RID: 5990 RVA: 0x000482C7 File Offset: 0x000464C7
		private static uint GetBit(int index)
		{
			return 1U << index % 32;
		}

		// Token: 0x17000587 RID: 1415
		// (get) Token: 0x06001767 RID: 5991 RVA: 0x000482D2 File Offset: 0x000464D2
		internal bool IsEmpty
		{
			get
			{
				return this.size == 0;
			}
		}

		// Token: 0x04000B5D RID: 2909
		private int size;

		// Token: 0x04000B5E RID: 2910
		private uint[] words;
	}
}
