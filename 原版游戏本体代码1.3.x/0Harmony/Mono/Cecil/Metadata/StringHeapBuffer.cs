using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002CF RID: 719
	internal class StringHeapBuffer : HeapBuffer
	{
		// Token: 0x170004D5 RID: 1237
		// (get) Token: 0x060012B6 RID: 4790 RVA: 0x0003AFE5 File Offset: 0x000391E5
		public sealed override bool IsEmpty
		{
			get
			{
				return this.length <= 1;
			}
		}

		// Token: 0x060012B7 RID: 4791 RVA: 0x0003AFF3 File Offset: 0x000391F3
		public StringHeapBuffer()
			: base(1)
		{
			base.WriteByte(0);
		}

		// Token: 0x060012B8 RID: 4792 RVA: 0x0003B014 File Offset: 0x00039214
		public virtual uint GetStringIndex(string @string)
		{
			uint index;
			if (this.strings.TryGetValue(@string, out index))
			{
				return index;
			}
			index = (uint)(this.strings.Count + 1);
			this.strings.Add(@string, index);
			return index;
		}

		// Token: 0x060012B9 RID: 4793 RVA: 0x0003B050 File Offset: 0x00039250
		public uint[] WriteStrings()
		{
			List<KeyValuePair<string, uint>> list = StringHeapBuffer.SortStrings(this.strings);
			this.strings = null;
			uint[] string_offsets = new uint[list.Count + 1];
			string_offsets[0] = 0U;
			string previous = string.Empty;
			foreach (KeyValuePair<string, uint> entry in list)
			{
				string @string = entry.Key;
				uint index = entry.Value;
				int position = this.position;
				if (previous.EndsWith(@string, StringComparison.Ordinal) && !StringHeapBuffer.IsLowSurrogateChar((int)entry.Key[0]))
				{
					string_offsets[(int)index] = (uint)(position - (Encoding.UTF8.GetByteCount(entry.Key) + 1));
				}
				else
				{
					string_offsets[(int)index] = (uint)position;
					this.WriteString(@string);
				}
				previous = entry.Key;
			}
			return string_offsets;
		}

		// Token: 0x060012BA RID: 4794 RVA: 0x0003B12C File Offset: 0x0003932C
		private static List<KeyValuePair<string, uint>> SortStrings(Dictionary<string, uint> strings)
		{
			List<KeyValuePair<string, uint>> list = new List<KeyValuePair<string, uint>>(strings);
			list.Sort(new StringHeapBuffer.SuffixSort());
			return list;
		}

		// Token: 0x060012BB RID: 4795 RVA: 0x0003B13F File Offset: 0x0003933F
		private static bool IsLowSurrogateChar(int c)
		{
			return c - 56320 <= 1023;
		}

		// Token: 0x060012BC RID: 4796 RVA: 0x0003B152 File Offset: 0x00039352
		protected virtual void WriteString(string @string)
		{
			base.WriteBytes(Encoding.UTF8.GetBytes(@string));
			base.WriteByte(0);
		}

		// Token: 0x04000700 RID: 1792
		protected Dictionary<string, uint> strings = new Dictionary<string, uint>(StringComparer.Ordinal);

		// Token: 0x020002D0 RID: 720
		private class SuffixSort : IComparer<KeyValuePair<string, uint>>
		{
			// Token: 0x060012BD RID: 4797 RVA: 0x0003B16C File Offset: 0x0003936C
			public int Compare(KeyValuePair<string, uint> xPair, KeyValuePair<string, uint> yPair)
			{
				string x = xPair.Key;
				string y = yPair.Key;
				int i = x.Length - 1;
				int j = y.Length - 1;
				while ((i >= 0) & (j >= 0))
				{
					if (x[i] < y[j])
					{
						return -1;
					}
					if (x[i] > y[j])
					{
						return 1;
					}
					i--;
					j--;
				}
				return y.Length.CompareTo(x.Length);
			}
		}
	}
}
