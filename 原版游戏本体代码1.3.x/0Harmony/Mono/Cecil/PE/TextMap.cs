using System;

namespace Mono.Cecil.PE
{
	// Token: 0x020002C8 RID: 712
	internal sealed class TextMap
	{
		// Token: 0x06001284 RID: 4740 RVA: 0x0003A829 File Offset: 0x00038A29
		public void AddMap(TextSegment segment, int length)
		{
			this.map[(int)segment] = new Range(this.GetStart(segment), (uint)length);
		}

		// Token: 0x06001285 RID: 4741 RVA: 0x0003A844 File Offset: 0x00038A44
		private uint AlignUp(uint value, uint align)
		{
			align -= 1U;
			return (value + align) & ~align;
		}

		// Token: 0x06001286 RID: 4742 RVA: 0x0003A854 File Offset: 0x00038A54
		public void AddMap(TextSegment segment, int length, int align)
		{
			uint start;
			if (segment != TextSegment.ImportAddressTable)
			{
				int index = segment - TextSegment.CLIHeader;
				Range previous = this.map[index];
				start = this.AlignUp(previous.Start + previous.Length, (uint)align);
				this.map[index].Length = start - previous.Start;
			}
			else
			{
				start = 8192U;
			}
			this.map[(int)segment] = new Range(start, (uint)length);
		}

		// Token: 0x06001287 RID: 4743 RVA: 0x0003A8C0 File Offset: 0x00038AC0
		public void AddMap(TextSegment segment, Range range)
		{
			this.map[(int)segment] = range;
		}

		// Token: 0x06001288 RID: 4744 RVA: 0x0003A8CF File Offset: 0x00038ACF
		public Range GetRange(TextSegment segment)
		{
			return this.map[(int)segment];
		}

		// Token: 0x06001289 RID: 4745 RVA: 0x0003A8E0 File Offset: 0x00038AE0
		public DataDirectory GetDataDirectory(TextSegment segment)
		{
			Range range = this.map[(int)segment];
			return new DataDirectory((range.Length == 0U) ? 0U : range.Start, range.Length);
		}

		// Token: 0x0600128A RID: 4746 RVA: 0x0003A916 File Offset: 0x00038B16
		public uint GetRVA(TextSegment segment)
		{
			return this.map[(int)segment].Start;
		}

		// Token: 0x0600128B RID: 4747 RVA: 0x0003A92C File Offset: 0x00038B2C
		public uint GetNextRVA(TextSegment segment)
		{
			return this.map[(int)segment].Start + this.map[(int)segment].Length;
		}

		// Token: 0x0600128C RID: 4748 RVA: 0x0003A95E File Offset: 0x00038B5E
		public int GetLength(TextSegment segment)
		{
			return (int)this.map[(int)segment].Length;
		}

		// Token: 0x0600128D RID: 4749 RVA: 0x0003A974 File Offset: 0x00038B74
		private uint GetStart(TextSegment segment)
		{
			if (segment != TextSegment.ImportAddressTable)
			{
				return this.ComputeStart((int)segment);
			}
			return 8192U;
		}

		// Token: 0x0600128E RID: 4750 RVA: 0x0003A993 File Offset: 0x00038B93
		private uint ComputeStart(int index)
		{
			index--;
			return this.map[index].Start + this.map[index].Length;
		}

		// Token: 0x0600128F RID: 4751 RVA: 0x0003A9C0 File Offset: 0x00038BC0
		public uint GetLength()
		{
			Range range = this.map[16];
			return range.Start - 8192U + range.Length;
		}

		// Token: 0x040006F3 RID: 1779
		private readonly Range[] map = new Range[17];
	}
}
