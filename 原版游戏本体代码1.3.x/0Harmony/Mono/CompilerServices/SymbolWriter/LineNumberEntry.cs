using System;
using System.Collections.Generic;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000338 RID: 824
	internal class LineNumberEntry
	{
		// Token: 0x0600151B RID: 5403 RVA: 0x00042EBF File Offset: 0x000410BF
		public LineNumberEntry(int file, int row, int column, int offset)
			: this(file, row, column, offset, false)
		{
		}

		// Token: 0x0600151C RID: 5404 RVA: 0x00042ECD File Offset: 0x000410CD
		public LineNumberEntry(int file, int row, int offset)
			: this(file, row, -1, offset, false)
		{
		}

		// Token: 0x0600151D RID: 5405 RVA: 0x00042EDA File Offset: 0x000410DA
		public LineNumberEntry(int file, int row, int column, int offset, bool is_hidden)
			: this(file, row, column, -1, -1, offset, is_hidden)
		{
		}

		// Token: 0x0600151E RID: 5406 RVA: 0x00042EEB File Offset: 0x000410EB
		public LineNumberEntry(int file, int row, int column, int end_row, int end_column, int offset, bool is_hidden)
		{
			this.File = file;
			this.Row = row;
			this.Column = column;
			this.EndRow = end_row;
			this.EndColumn = end_column;
			this.Offset = offset;
			this.IsHidden = is_hidden;
		}

		// Token: 0x0600151F RID: 5407 RVA: 0x00042F28 File Offset: 0x00041128
		public override string ToString()
		{
			return string.Format("[Line {0}:{1},{2}-{3},{4}:{5}]", new object[] { this.File, this.Row, this.Column, this.EndRow, this.EndColumn, this.Offset });
		}

		// Token: 0x04000AA5 RID: 2725
		public readonly int Row;

		// Token: 0x04000AA6 RID: 2726
		public int Column;

		// Token: 0x04000AA7 RID: 2727
		public int EndRow;

		// Token: 0x04000AA8 RID: 2728
		public int EndColumn;

		// Token: 0x04000AA9 RID: 2729
		public readonly int File;

		// Token: 0x04000AAA RID: 2730
		public readonly int Offset;

		// Token: 0x04000AAB RID: 2731
		public readonly bool IsHidden;

		// Token: 0x04000AAC RID: 2732
		public static readonly LineNumberEntry Null = new LineNumberEntry(0, 0, 0, 0);

		// Token: 0x02000339 RID: 825
		public sealed class LocationComparer : IComparer<LineNumberEntry>
		{
			// Token: 0x06001521 RID: 5409 RVA: 0x00042FAC File Offset: 0x000411AC
			public int Compare(LineNumberEntry l1, LineNumberEntry l2)
			{
				if (l1.Row != l2.Row)
				{
					return l1.Row.CompareTo(l2.Row);
				}
				return l1.Column.CompareTo(l2.Column);
			}

			// Token: 0x04000AAD RID: 2733
			public static readonly LineNumberEntry.LocationComparer Default = new LineNumberEntry.LocationComparer();
		}
	}
}
