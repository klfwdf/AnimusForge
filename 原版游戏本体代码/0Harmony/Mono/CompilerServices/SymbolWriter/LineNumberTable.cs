using System;
using System.Collections.Generic;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000344 RID: 836
	internal class LineNumberTable
	{
		// Token: 0x17000572 RID: 1394
		// (get) Token: 0x0600155C RID: 5468 RVA: 0x00043B82 File Offset: 0x00041D82
		public LineNumberEntry[] LineNumbers
		{
			get
			{
				return this._line_numbers;
			}
		}

		// Token: 0x0600155D RID: 5469 RVA: 0x00043B8C File Offset: 0x00041D8C
		protected LineNumberTable(MonoSymbolFile file)
		{
			this.LineBase = file.OffsetTable.LineNumberTable_LineBase;
			this.LineRange = file.OffsetTable.LineNumberTable_LineRange;
			this.OpcodeBase = (byte)file.OffsetTable.LineNumberTable_OpcodeBase;
			this.MaxAddressIncrement = (int)(byte.MaxValue - this.OpcodeBase) / this.LineRange;
		}

		// Token: 0x0600155E RID: 5470 RVA: 0x00043BEC File Offset: 0x00041DEC
		internal LineNumberTable(MonoSymbolFile file, LineNumberEntry[] lines)
			: this(file)
		{
			this._line_numbers = lines;
		}

		// Token: 0x0600155F RID: 5471 RVA: 0x00043BFC File Offset: 0x00041DFC
		internal void Write(MonoSymbolFile file, MyBinaryWriter bw, bool hasColumnsInfo, bool hasEndInfo)
		{
			int start = (int)bw.BaseStream.Position;
			bool last_is_hidden = false;
			int last_line = 1;
			int last_offset = 0;
			int last_file = 1;
			for (int i = 0; i < this.LineNumbers.Length; i++)
			{
				int line_inc = this.LineNumbers[i].Row - last_line;
				int offset_inc = this.LineNumbers[i].Offset - last_offset;
				if (this.LineNumbers[i].File != last_file)
				{
					bw.Write(4);
					bw.WriteLeb128(this.LineNumbers[i].File);
					last_file = this.LineNumbers[i].File;
				}
				if (this.LineNumbers[i].IsHidden != last_is_hidden)
				{
					bw.Write(0);
					bw.Write(1);
					bw.Write(64);
					last_is_hidden = this.LineNumbers[i].IsHidden;
				}
				if (offset_inc >= this.MaxAddressIncrement)
				{
					if (offset_inc < 2 * this.MaxAddressIncrement)
					{
						bw.Write(8);
						offset_inc -= this.MaxAddressIncrement;
					}
					else
					{
						bw.Write(2);
						bw.WriteLeb128(offset_inc);
						offset_inc = 0;
					}
				}
				if (line_inc < this.LineBase || line_inc >= this.LineBase + this.LineRange)
				{
					bw.Write(3);
					bw.WriteLeb128(line_inc);
					if (offset_inc != 0)
					{
						bw.Write(2);
						bw.WriteLeb128(offset_inc);
					}
					bw.Write(1);
				}
				else
				{
					byte opcode = (byte)(line_inc - this.LineBase + this.LineRange * offset_inc + (int)this.OpcodeBase);
					bw.Write(opcode);
				}
				last_line = this.LineNumbers[i].Row;
				last_offset = this.LineNumbers[i].Offset;
			}
			bw.Write(0);
			bw.Write(1);
			bw.Write(1);
			if (hasColumnsInfo)
			{
				for (int j = 0; j < this.LineNumbers.Length; j++)
				{
					LineNumberEntry ln = this.LineNumbers[j];
					if (ln.Row >= 0)
					{
						bw.WriteLeb128(ln.Column);
					}
				}
			}
			if (hasEndInfo)
			{
				for (int k = 0; k < this.LineNumbers.Length; k++)
				{
					LineNumberEntry ln2 = this.LineNumbers[k];
					if (ln2.EndRow == -1 || ln2.EndColumn == -1 || ln2.Row > ln2.EndRow)
					{
						bw.WriteLeb128(16777215);
					}
					else
					{
						bw.WriteLeb128(ln2.EndRow - ln2.Row);
						bw.WriteLeb128(ln2.EndColumn);
					}
				}
			}
			file.ExtendedLineNumberSize += (int)bw.BaseStream.Position - start;
		}

		// Token: 0x06001560 RID: 5472 RVA: 0x00043E7D File Offset: 0x0004207D
		internal static LineNumberTable Read(MonoSymbolFile file, MyBinaryReader br, bool readColumnsInfo, bool readEndInfo)
		{
			LineNumberTable lineNumberTable = new LineNumberTable(file);
			lineNumberTable.DoRead(file, br, readColumnsInfo, readEndInfo);
			return lineNumberTable;
		}

		// Token: 0x06001561 RID: 5473 RVA: 0x00043E90 File Offset: 0x00042090
		private void DoRead(MonoSymbolFile file, MyBinaryReader br, bool includesColumns, bool includesEnds)
		{
			List<LineNumberEntry> lines = new List<LineNumberEntry>();
			bool is_hidden = false;
			bool modified = false;
			int stm_line = 1;
			int stm_offset = 0;
			int stm_file = 1;
			byte opcode;
			for (;;)
			{
				opcode = br.ReadByte();
				if (opcode == 0)
				{
					byte size = br.ReadByte();
					long end_pos = br.BaseStream.Position + (long)((ulong)size);
					opcode = br.ReadByte();
					if (opcode == 1)
					{
						break;
					}
					if (opcode == 64)
					{
						is_hidden = !is_hidden;
						modified = true;
					}
					else if (opcode < 64 || opcode > 127)
					{
						goto IL_7F;
					}
					br.BaseStream.Position = end_pos;
				}
				else
				{
					if (opcode < this.OpcodeBase)
					{
						switch (opcode)
						{
						case 1:
							lines.Add(new LineNumberEntry(stm_file, stm_line, -1, stm_offset, is_hidden));
							modified = false;
							continue;
						case 2:
							stm_offset += br.ReadLeb128();
							modified = true;
							continue;
						case 3:
							stm_line += br.ReadLeb128();
							modified = true;
							continue;
						case 4:
							stm_file = br.ReadLeb128();
							modified = true;
							continue;
						case 8:
							stm_offset += this.MaxAddressIncrement;
							modified = true;
							continue;
						}
						goto Block_7;
					}
					opcode -= this.OpcodeBase;
					stm_offset += (int)opcode / this.LineRange;
					stm_line += this.LineBase + (int)opcode % this.LineRange;
					lines.Add(new LineNumberEntry(stm_file, stm_line, -1, stm_offset, is_hidden));
					modified = false;
				}
			}
			if (modified)
			{
				lines.Add(new LineNumberEntry(stm_file, stm_line, -1, stm_offset, is_hidden));
			}
			this._line_numbers = lines.ToArray();
			if (includesColumns)
			{
				for (int i = 0; i < this._line_numbers.Length; i++)
				{
					LineNumberEntry ln = this._line_numbers[i];
					if (ln.Row >= 0)
					{
						ln.Column = br.ReadLeb128();
					}
				}
			}
			if (includesEnds)
			{
				for (int j = 0; j < this._line_numbers.Length; j++)
				{
					LineNumberEntry ln2 = this._line_numbers[j];
					int row = br.ReadLeb128();
					if (row == 16777215)
					{
						ln2.EndRow = -1;
						ln2.EndColumn = -1;
					}
					else
					{
						ln2.EndRow = ln2.Row + row;
						ln2.EndColumn = br.ReadLeb128();
					}
				}
			}
			return;
			IL_7F:
			throw new MonoSymbolFileException("Unknown extended opcode {0:x}", new object[] { opcode });
			Block_7:
			throw new MonoSymbolFileException("Unknown standard opcode {0:x} in LNT", new object[] { opcode });
		}

		// Token: 0x06001562 RID: 5474 RVA: 0x000440ED File Offset: 0x000422ED
		public bool GetMethodBounds(out LineNumberEntry start, out LineNumberEntry end)
		{
			if (this._line_numbers.Length > 1)
			{
				start = this._line_numbers[0];
				end = this._line_numbers[this._line_numbers.Length - 1];
				return true;
			}
			start = LineNumberEntry.Null;
			end = LineNumberEntry.Null;
			return false;
		}

		// Token: 0x04000AD9 RID: 2777
		protected LineNumberEntry[] _line_numbers;

		// Token: 0x04000ADA RID: 2778
		public readonly int LineBase;

		// Token: 0x04000ADB RID: 2779
		public readonly int LineRange;

		// Token: 0x04000ADC RID: 2780
		public readonly byte OpcodeBase;

		// Token: 0x04000ADD RID: 2781
		public readonly int MaxAddressIncrement;

		// Token: 0x04000ADE RID: 2782
		public const int Default_LineBase = -1;

		// Token: 0x04000ADF RID: 2783
		public const int Default_LineRange = 8;

		// Token: 0x04000AE0 RID: 2784
		public const byte Default_OpcodeBase = 9;

		// Token: 0x04000AE1 RID: 2785
		public const byte DW_LNS_copy = 1;

		// Token: 0x04000AE2 RID: 2786
		public const byte DW_LNS_advance_pc = 2;

		// Token: 0x04000AE3 RID: 2787
		public const byte DW_LNS_advance_line = 3;

		// Token: 0x04000AE4 RID: 2788
		public const byte DW_LNS_set_file = 4;

		// Token: 0x04000AE5 RID: 2789
		public const byte DW_LNS_const_add_pc = 8;

		// Token: 0x04000AE6 RID: 2790
		public const byte DW_LNE_end_sequence = 1;

		// Token: 0x04000AE7 RID: 2791
		public const byte DW_LNE_MONO_negate_is_hidden = 64;

		// Token: 0x04000AE8 RID: 2792
		internal const byte DW_LNE_MONO__extensions_start = 64;

		// Token: 0x04000AE9 RID: 2793
		internal const byte DW_LNE_MONO__extensions_end = 127;
	}
}
