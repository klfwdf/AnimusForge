using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000061 RID: 97
	internal sealed class AlphanumComparatorFast : IComparer<string>, IComparer
	{
		// Token: 0x06000397 RID: 919 RVA: 0x0000D21F File Offset: 0x0000B41F
		[NullableContext(2)]
		public int Compare(object x, object y)
		{
			return this.Compare(x as string, y as string);
		}

		// Token: 0x06000398 RID: 920 RVA: 0x0000D234 File Offset: 0x0000B434
		[NullableContext(2)]
		public int Compare(string s1, string s2)
		{
			if (s1 == null && s2 == null)
			{
				return 0;
			}
			if (s1 == null)
			{
				return -1;
			}
			if (s2 == null)
			{
				return 1;
			}
			int len = s1.Length;
			int len2 = s2.Length;
			if (len == 0 && len2 == 0)
			{
				return 0;
			}
			if (len == 0)
			{
				return -1;
			}
			if (len2 == 0)
			{
				return 1;
			}
			int marker = 0;
			int marker2 = 0;
			while (marker < len || marker2 < len2)
			{
				if (marker >= len)
				{
					return -1;
				}
				if (marker2 >= len2)
				{
					return 1;
				}
				char ch = s1[marker];
				char ch2 = s2[marker2];
				StringBuilder chunk = new StringBuilder();
				StringBuilder chunk2 = new StringBuilder();
				while (marker < len)
				{
					if (chunk.Length != 0 && !AlphanumComparatorFast.InChunk(ch, chunk[0]))
					{
						break;
					}
					chunk.Append(ch);
					marker++;
					if (marker < len)
					{
						ch = s1[marker];
					}
				}
				while (marker2 < len2 && (chunk2.Length == 0 || AlphanumComparatorFast.InChunk(ch2, chunk2[0])))
				{
					chunk2.Append(ch2);
					marker2++;
					if (marker2 < len2)
					{
						ch2 = s2[marker2];
					}
				}
				if (char.IsDigit(chunk[0]) && char.IsDigit(chunk2[0]))
				{
					int numericChunk = Convert.ToInt32(chunk.ToString());
					int numericChunk2 = Convert.ToInt32(chunk2.ToString());
					if (numericChunk < numericChunk2)
					{
						return -1;
					}
					if (numericChunk > numericChunk2)
					{
						return 1;
					}
				}
				else
				{
					int result = string.CompareOrdinal(chunk.ToString(), chunk2.ToString());
					if (result >= 1)
					{
						return 1;
					}
					if (result <= -1)
					{
						return -1;
					}
				}
			}
			return 0;
		}

		// Token: 0x06000399 RID: 921 RVA: 0x0000D39C File Offset: 0x0000B59C
		private static bool InChunk(char ch, char otherCh)
		{
			AlphanumComparatorFast.ChunkType type = AlphanumComparatorFast.ChunkType.Alphanumeric;
			if (char.IsDigit(otherCh))
			{
				type = AlphanumComparatorFast.ChunkType.Numeric;
			}
			return (type != AlphanumComparatorFast.ChunkType.Alphanumeric || !char.IsDigit(ch)) && (type != AlphanumComparatorFast.ChunkType.Numeric || char.IsDigit(ch));
		}

		// Token: 0x020000C3 RID: 195
		private enum ChunkType
		{
			// Token: 0x040001FE RID: 510
			Alphanumeric,
			// Token: 0x040001FF RID: 511
			Numeric
		}
	}
}
