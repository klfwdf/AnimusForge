using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002CA RID: 714
	internal sealed class TableHeapBuffer : HeapBuffer
	{
		// Token: 0x170004D0 RID: 1232
		// (get) Token: 0x06001294 RID: 4756 RVA: 0x0001B69F File Offset: 0x0001989F
		public override bool IsEmpty
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06001295 RID: 4757 RVA: 0x0003AAB0 File Offset: 0x00038CB0
		public TableHeapBuffer(ModuleDefinition module, MetadataBuilder metadata)
			: base(24)
		{
			this.module = module;
			this.metadata = metadata;
			this.counter = new Func<Table, int>(this.GetTableLength);
		}

		// Token: 0x06001296 RID: 4758 RVA: 0x0003AB0C File Offset: 0x00038D0C
		private int GetTableLength(Table table)
		{
			return (int)this.table_infos[(int)table].Length;
		}

		// Token: 0x06001297 RID: 4759 RVA: 0x0003AB20 File Offset: 0x00038D20
		public TTable GetTable<TTable>(Table table) where TTable : MetadataTable, new()
		{
			TTable md_table = (TTable)((object)this.tables[(int)table]);
			if (md_table != null)
			{
				return md_table;
			}
			md_table = Activator.CreateInstance<TTable>();
			this.tables[(int)table] = md_table;
			return md_table;
		}

		// Token: 0x06001298 RID: 4760 RVA: 0x0003AB5A File Offset: 0x00038D5A
		public void WriteBySize(uint value, int size)
		{
			if (size == 4)
			{
				base.WriteUInt32(value);
				return;
			}
			base.WriteUInt16((ushort)value);
		}

		// Token: 0x06001299 RID: 4761 RVA: 0x0003AB70 File Offset: 0x00038D70
		public void WriteBySize(uint value, bool large)
		{
			if (large)
			{
				base.WriteUInt32(value);
				return;
			}
			base.WriteUInt16((ushort)value);
		}

		// Token: 0x0600129A RID: 4762 RVA: 0x0003AB85 File Offset: 0x00038D85
		public void WriteString(uint @string)
		{
			this.WriteBySize(this.string_offsets[(int)@string], this.large_string);
		}

		// Token: 0x0600129B RID: 4763 RVA: 0x0003AB9B File Offset: 0x00038D9B
		public void WriteBlob(uint blob)
		{
			this.WriteBySize(blob, this.large_blob);
		}

		// Token: 0x0600129C RID: 4764 RVA: 0x0003ABAA File Offset: 0x00038DAA
		public void WriteGuid(uint guid)
		{
			this.WriteBySize(guid, this.large_guid);
		}

		// Token: 0x0600129D RID: 4765 RVA: 0x0003ABB9 File Offset: 0x00038DB9
		public void WriteRID(uint rid, Table table)
		{
			this.WriteBySize(rid, this.table_infos[(int)table].IsLarge);
		}

		// Token: 0x0600129E RID: 4766 RVA: 0x0003ABD4 File Offset: 0x00038DD4
		private int GetCodedIndexSize(CodedIndex coded_index)
		{
			int size = this.coded_index_sizes[(int)coded_index];
			if (size != 0)
			{
				return size;
			}
			return this.coded_index_sizes[(int)coded_index] = coded_index.GetSize(this.counter);
		}

		// Token: 0x0600129F RID: 4767 RVA: 0x0003AC08 File Offset: 0x00038E08
		public void WriteCodedRID(uint rid, CodedIndex coded_index)
		{
			this.WriteBySize(rid, this.GetCodedIndexSize(coded_index));
		}

		// Token: 0x060012A0 RID: 4768 RVA: 0x0003AC18 File Offset: 0x00038E18
		public void WriteTableHeap()
		{
			base.WriteUInt32(0U);
			base.WriteByte(this.GetTableHeapVersion());
			base.WriteByte(0);
			base.WriteByte(this.GetHeapSizes());
			base.WriteByte(10);
			base.WriteUInt64(this.GetValid());
			base.WriteUInt64(55193285546867200UL);
			this.WriteRowCount();
			this.WriteTables();
		}

		// Token: 0x060012A1 RID: 4769 RVA: 0x0003AC7C File Offset: 0x00038E7C
		private void WriteRowCount()
		{
			for (int i = 0; i < this.tables.Length; i++)
			{
				MetadataTable table = this.tables[i];
				if (table != null && table.Length != 0)
				{
					base.WriteUInt32((uint)table.Length);
				}
			}
		}

		// Token: 0x060012A2 RID: 4770 RVA: 0x0003ACBC File Offset: 0x00038EBC
		private void WriteTables()
		{
			for (int i = 0; i < this.tables.Length; i++)
			{
				MetadataTable table = this.tables[i];
				if (table != null && table.Length != 0)
				{
					table.Write(this);
				}
			}
		}

		// Token: 0x060012A3 RID: 4771 RVA: 0x0003ACF8 File Offset: 0x00038EF8
		private ulong GetValid()
		{
			ulong valid = 0UL;
			for (int i = 0; i < this.tables.Length; i++)
			{
				MetadataTable table = this.tables[i];
				if (table != null && table.Length != 0)
				{
					table.Sort();
					valid |= 1UL << i;
				}
			}
			return valid;
		}

		// Token: 0x060012A4 RID: 4772 RVA: 0x0003AD40 File Offset: 0x00038F40
		public void ComputeTableInformations()
		{
			if (this.metadata.metadata_builder != null)
			{
				this.ComputeTableInformations(this.metadata.metadata_builder.table_heap);
			}
			this.ComputeTableInformations(this.metadata.table_heap);
		}

		// Token: 0x060012A5 RID: 4773 RVA: 0x0003AD78 File Offset: 0x00038F78
		private void ComputeTableInformations(TableHeapBuffer table_heap)
		{
			MetadataTable[] tables = table_heap.tables;
			for (int i = 0; i < tables.Length; i++)
			{
				MetadataTable table = tables[i];
				if (table != null && table.Length > 0)
				{
					this.table_infos[i].Length = (uint)table.Length;
				}
			}
		}

		// Token: 0x060012A6 RID: 4774 RVA: 0x0003ADC4 File Offset: 0x00038FC4
		private byte GetHeapSizes()
		{
			byte heap_sizes = 0;
			if (this.metadata.string_heap.IsLarge)
			{
				this.large_string = true;
				heap_sizes |= 1;
			}
			if (this.metadata.guid_heap.IsLarge)
			{
				this.large_guid = true;
				heap_sizes |= 2;
			}
			if (this.metadata.blob_heap.IsLarge)
			{
				this.large_blob = true;
				heap_sizes |= 4;
			}
			return heap_sizes;
		}

		// Token: 0x060012A7 RID: 4775 RVA: 0x0003AE30 File Offset: 0x00039030
		private byte GetTableHeapVersion()
		{
			TargetRuntime runtime = this.module.Runtime;
			if (runtime <= TargetRuntime.Net_1_1)
			{
				return 1;
			}
			return 2;
		}

		// Token: 0x060012A8 RID: 4776 RVA: 0x0003AE50 File Offset: 0x00039050
		public void FixupData(uint data_rva)
		{
			FieldRVATable table = this.GetTable<FieldRVATable>(Table.FieldRVA);
			if (table.length == 0)
			{
				return;
			}
			int field_idx_size = (this.GetTable<FieldTable>(Table.Field).IsLarge ? 4 : 2);
			int previous = this.position;
			this.position = table.position;
			for (int i = 0; i < table.length; i++)
			{
				uint rva = base.ReadUInt32();
				this.position -= 4;
				base.WriteUInt32(rva + data_rva);
				this.position += field_idx_size;
			}
			this.position = previous;
		}

		// Token: 0x040006F4 RID: 1780
		private readonly ModuleDefinition module;

		// Token: 0x040006F5 RID: 1781
		private readonly MetadataBuilder metadata;

		// Token: 0x040006F6 RID: 1782
		internal readonly TableInformation[] table_infos = new TableInformation[58];

		// Token: 0x040006F7 RID: 1783
		internal readonly MetadataTable[] tables = new MetadataTable[58];

		// Token: 0x040006F8 RID: 1784
		private bool large_string;

		// Token: 0x040006F9 RID: 1785
		private bool large_blob;

		// Token: 0x040006FA RID: 1786
		private bool large_guid;

		// Token: 0x040006FB RID: 1787
		private readonly int[] coded_index_sizes = new int[14];

		// Token: 0x040006FC RID: 1788
		private readonly Func<Table, int> counter;

		// Token: 0x040006FD RID: 1789
		internal uint[] string_offsets;
	}
}
