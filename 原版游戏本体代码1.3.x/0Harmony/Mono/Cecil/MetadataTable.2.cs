using System;

namespace Mono.Cecil
{
	// Token: 0x020001F7 RID: 503
	internal abstract class MetadataTable<TRow> : MetadataTable where TRow : struct
	{
		// Token: 0x17000250 RID: 592
		// (get) Token: 0x06000AAE RID: 2734 RVA: 0x00024A35 File Offset: 0x00022C35
		public sealed override int Length
		{
			get
			{
				return this.length;
			}
		}

		// Token: 0x06000AAF RID: 2735 RVA: 0x00024A40 File Offset: 0x00022C40
		public int AddRow(TRow row)
		{
			if (this.rows.Length == this.length)
			{
				this.Grow();
			}
			TRow[] array = this.rows;
			int num = this.length;
			this.length = num + 1;
			array[num] = row;
			return this.length;
		}

		// Token: 0x06000AB0 RID: 2736 RVA: 0x00024A88 File Offset: 0x00022C88
		private void Grow()
		{
			TRow[] rows = new TRow[this.rows.Length * 2];
			Array.Copy(this.rows, rows, this.rows.Length);
			this.rows = rows;
		}

		// Token: 0x06000AB1 RID: 2737 RVA: 0x0001B842 File Offset: 0x00019A42
		public override void Sort()
		{
		}

		// Token: 0x0400034C RID: 844
		internal TRow[] rows = new TRow[2];

		// Token: 0x0400034D RID: 845
		internal int length;
	}
}
