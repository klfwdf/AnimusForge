using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000305 RID: 773
	internal sealed class SequencePoint
	{
		// Token: 0x1700050C RID: 1292
		// (get) Token: 0x06001426 RID: 5158 RVA: 0x00040AA4 File Offset: 0x0003ECA4
		public int Offset
		{
			get
			{
				return this.offset.Offset;
			}
		}

		// Token: 0x1700050D RID: 1293
		// (get) Token: 0x06001427 RID: 5159 RVA: 0x00040AB1 File Offset: 0x0003ECB1
		// (set) Token: 0x06001428 RID: 5160 RVA: 0x00040AB9 File Offset: 0x0003ECB9
		public int StartLine
		{
			get
			{
				return this.start_line;
			}
			set
			{
				this.start_line = value;
			}
		}

		// Token: 0x1700050E RID: 1294
		// (get) Token: 0x06001429 RID: 5161 RVA: 0x00040AC2 File Offset: 0x0003ECC2
		// (set) Token: 0x0600142A RID: 5162 RVA: 0x00040ACA File Offset: 0x0003ECCA
		public int StartColumn
		{
			get
			{
				return this.start_column;
			}
			set
			{
				this.start_column = value;
			}
		}

		// Token: 0x1700050F RID: 1295
		// (get) Token: 0x0600142B RID: 5163 RVA: 0x00040AD3 File Offset: 0x0003ECD3
		// (set) Token: 0x0600142C RID: 5164 RVA: 0x00040ADB File Offset: 0x0003ECDB
		public int EndLine
		{
			get
			{
				return this.end_line;
			}
			set
			{
				this.end_line = value;
			}
		}

		// Token: 0x17000510 RID: 1296
		// (get) Token: 0x0600142D RID: 5165 RVA: 0x00040AE4 File Offset: 0x0003ECE4
		// (set) Token: 0x0600142E RID: 5166 RVA: 0x00040AEC File Offset: 0x0003ECEC
		public int EndColumn
		{
			get
			{
				return this.end_column;
			}
			set
			{
				this.end_column = value;
			}
		}

		// Token: 0x17000511 RID: 1297
		// (get) Token: 0x0600142F RID: 5167 RVA: 0x00040AF5 File Offset: 0x0003ECF5
		public bool IsHidden
		{
			get
			{
				return this.start_line == 16707566 && this.start_line == this.end_line;
			}
		}

		// Token: 0x17000512 RID: 1298
		// (get) Token: 0x06001430 RID: 5168 RVA: 0x00040B14 File Offset: 0x0003ED14
		// (set) Token: 0x06001431 RID: 5169 RVA: 0x00040B1C File Offset: 0x0003ED1C
		public Document Document
		{
			get
			{
				return this.document;
			}
			set
			{
				this.document = value;
			}
		}

		// Token: 0x06001432 RID: 5170 RVA: 0x00040B25 File Offset: 0x0003ED25
		internal SequencePoint(int offset, Document document)
		{
			if (document == null)
			{
				throw new ArgumentNullException("document");
			}
			this.offset = new InstructionOffset(offset);
			this.document = document;
		}

		// Token: 0x06001433 RID: 5171 RVA: 0x00040B4E File Offset: 0x0003ED4E
		public SequencePoint(Instruction instruction, Document document)
		{
			if (document == null)
			{
				throw new ArgumentNullException("document");
			}
			this.offset = new InstructionOffset(instruction);
			this.document = document;
		}

		// Token: 0x04000A01 RID: 2561
		internal InstructionOffset offset;

		// Token: 0x04000A02 RID: 2562
		private Document document;

		// Token: 0x04000A03 RID: 2563
		private int start_line;

		// Token: 0x04000A04 RID: 2564
		private int start_column;

		// Token: 0x04000A05 RID: 2565
		private int end_line;

		// Token: 0x04000A06 RID: 2566
		private int end_column;
	}
}
