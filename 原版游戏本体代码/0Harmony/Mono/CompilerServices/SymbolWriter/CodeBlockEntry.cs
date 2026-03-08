using System;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200033A RID: 826
	internal class CodeBlockEntry
	{
		// Token: 0x06001524 RID: 5412 RVA: 0x00042FF9 File Offset: 0x000411F9
		public CodeBlockEntry(int index, int parent, CodeBlockEntry.Type type, int start_offset)
		{
			this.Index = index;
			this.Parent = parent;
			this.BlockType = type;
			this.StartOffset = start_offset;
		}

		// Token: 0x06001525 RID: 5413 RVA: 0x00043020 File Offset: 0x00041220
		internal CodeBlockEntry(int index, MyBinaryReader reader)
		{
			this.Index = index;
			int type_flag = reader.ReadLeb128();
			this.BlockType = (CodeBlockEntry.Type)(type_flag & 63);
			this.Parent = reader.ReadLeb128();
			this.StartOffset = reader.ReadLeb128();
			this.EndOffset = reader.ReadLeb128();
			if ((type_flag & 64) != 0)
			{
				int data_size = (int)reader.ReadInt16();
				reader.BaseStream.Position += (long)data_size;
			}
		}

		// Token: 0x06001526 RID: 5414 RVA: 0x00043090 File Offset: 0x00041290
		public void Close(int end_offset)
		{
			this.EndOffset = end_offset;
		}

		// Token: 0x06001527 RID: 5415 RVA: 0x00043099 File Offset: 0x00041299
		internal void Write(MyBinaryWriter bw)
		{
			bw.WriteLeb128((int)this.BlockType);
			bw.WriteLeb128(this.Parent);
			bw.WriteLeb128(this.StartOffset);
			bw.WriteLeb128(this.EndOffset);
		}

		// Token: 0x06001528 RID: 5416 RVA: 0x000430CC File Offset: 0x000412CC
		public override string ToString()
		{
			return string.Format("[CodeBlock {0}:{1}:{2}:{3}:{4}]", new object[] { this.Index, this.Parent, this.BlockType, this.StartOffset, this.EndOffset });
		}

		// Token: 0x04000AAE RID: 2734
		public int Index;

		// Token: 0x04000AAF RID: 2735
		public int Parent;

		// Token: 0x04000AB0 RID: 2736
		public CodeBlockEntry.Type BlockType;

		// Token: 0x04000AB1 RID: 2737
		public int StartOffset;

		// Token: 0x04000AB2 RID: 2738
		public int EndOffset;

		// Token: 0x0200033B RID: 827
		public enum Type
		{
			// Token: 0x04000AB4 RID: 2740
			Lexical = 1,
			// Token: 0x04000AB5 RID: 2741
			CompilerGenerated,
			// Token: 0x04000AB6 RID: 2742
			IteratorBody,
			// Token: 0x04000AB7 RID: 2743
			IteratorDispatcher
		}
	}
}
