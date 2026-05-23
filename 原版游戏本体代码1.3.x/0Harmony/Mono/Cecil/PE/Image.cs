using System;
using System.IO;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

namespace Mono.Cecil.PE
{
	// Token: 0x020002C2 RID: 706
	internal sealed class Image : IDisposable
	{
		// Token: 0x06001229 RID: 4649 RVA: 0x00038071 File Offset: 0x00036271
		public Image()
		{
			this.counter = new Func<Table, int>(this.GetTableLength);
		}

		// Token: 0x0600122A RID: 4650 RVA: 0x00038098 File Offset: 0x00036298
		public bool HasTable(Table table)
		{
			return this.GetTableLength(table) > 0;
		}

		// Token: 0x0600122B RID: 4651 RVA: 0x000380A4 File Offset: 0x000362A4
		public int GetTableLength(Table table)
		{
			return (int)this.TableHeap[table].Length;
		}

		// Token: 0x0600122C RID: 4652 RVA: 0x000380B7 File Offset: 0x000362B7
		public int GetTableIndexSize(Table table)
		{
			if (this.GetTableLength(table) >= 65536)
			{
				return 4;
			}
			return 2;
		}

		// Token: 0x0600122D RID: 4653 RVA: 0x000380CC File Offset: 0x000362CC
		public int GetCodedIndexSize(CodedIndex coded_index)
		{
			int size = this.coded_index_sizes[(int)coded_index];
			if (size != 0)
			{
				return size;
			}
			return this.coded_index_sizes[(int)coded_index] = coded_index.GetSize(this.counter);
		}

		// Token: 0x0600122E RID: 4654 RVA: 0x00038100 File Offset: 0x00036300
		public uint ResolveVirtualAddress(uint rva)
		{
			Section section = this.GetSectionAtVirtualAddress(rva);
			if (section == null)
			{
				throw new ArgumentOutOfRangeException();
			}
			return this.ResolveVirtualAddressInSection(rva, section);
		}

		// Token: 0x0600122F RID: 4655 RVA: 0x00038126 File Offset: 0x00036326
		public uint ResolveVirtualAddressInSection(uint rva, Section section)
		{
			return rva + section.PointerToRawData - section.VirtualAddress;
		}

		// Token: 0x06001230 RID: 4656 RVA: 0x00038138 File Offset: 0x00036338
		public Section GetSection(string name)
		{
			foreach (Section section in this.Sections)
			{
				if (section.Name == name)
				{
					return section;
				}
			}
			return null;
		}

		// Token: 0x06001231 RID: 4657 RVA: 0x00038170 File Offset: 0x00036370
		public Section GetSectionAtVirtualAddress(uint rva)
		{
			foreach (Section section in this.Sections)
			{
				if (rva >= section.VirtualAddress && rva < section.VirtualAddress + section.SizeOfRawData)
				{
					return section;
				}
			}
			return null;
		}

		// Token: 0x06001232 RID: 4658 RVA: 0x000381B4 File Offset: 0x000363B4
		private BinaryStreamReader GetReaderAt(uint rva)
		{
			Section section = this.GetSectionAtVirtualAddress(rva);
			if (section == null)
			{
				return null;
			}
			BinaryStreamReader binaryStreamReader = new BinaryStreamReader(this.Stream.value);
			binaryStreamReader.MoveTo(this.ResolveVirtualAddressInSection(rva, section));
			return binaryStreamReader;
		}

		// Token: 0x06001233 RID: 4659 RVA: 0x000381EC File Offset: 0x000363EC
		public TRet GetReaderAt<TItem, TRet>(uint rva, TItem item, Func<TItem, BinaryStreamReader, TRet> read) where TRet : class
		{
			long position = this.Stream.value.Position;
			TRet tret;
			try
			{
				BinaryStreamReader reader = this.GetReaderAt(rva);
				if (reader == null)
				{
					tret = default(TRet);
					tret = tret;
				}
				else
				{
					tret = read(item, reader);
				}
			}
			finally
			{
				this.Stream.value.Position = position;
			}
			return tret;
		}

		// Token: 0x06001234 RID: 4660 RVA: 0x00038250 File Offset: 0x00036450
		public bool HasDebugTables()
		{
			return this.HasTable(Table.Document) || this.HasTable(Table.MethodDebugInformation) || this.HasTable(Table.LocalScope) || this.HasTable(Table.LocalVariable) || this.HasTable(Table.LocalConstant) || this.HasTable(Table.StateMachineMethod) || this.HasTable(Table.CustomDebugInformation);
		}

		// Token: 0x06001235 RID: 4661 RVA: 0x000382A3 File Offset: 0x000364A3
		public void Dispose()
		{
			this.Stream.Dispose();
		}

		// Token: 0x040006A5 RID: 1701
		public Disposable<Stream> Stream;

		// Token: 0x040006A6 RID: 1702
		public string FileName;

		// Token: 0x040006A7 RID: 1703
		public ModuleKind Kind;

		// Token: 0x040006A8 RID: 1704
		public uint Characteristics;

		// Token: 0x040006A9 RID: 1705
		public string RuntimeVersion;

		// Token: 0x040006AA RID: 1706
		public TargetArchitecture Architecture;

		// Token: 0x040006AB RID: 1707
		public ModuleCharacteristics DllCharacteristics;

		// Token: 0x040006AC RID: 1708
		public ushort LinkerVersion;

		// Token: 0x040006AD RID: 1709
		public ushort SubSystemMajor;

		// Token: 0x040006AE RID: 1710
		public ushort SubSystemMinor;

		// Token: 0x040006AF RID: 1711
		public ImageDebugHeader DebugHeader;

		// Token: 0x040006B0 RID: 1712
		public Section[] Sections;

		// Token: 0x040006B1 RID: 1713
		public Section MetadataSection;

		// Token: 0x040006B2 RID: 1714
		public uint EntryPointToken;

		// Token: 0x040006B3 RID: 1715
		public uint Timestamp;

		// Token: 0x040006B4 RID: 1716
		public ModuleAttributes Attributes;

		// Token: 0x040006B5 RID: 1717
		public DataDirectory Win32Resources;

		// Token: 0x040006B6 RID: 1718
		public DataDirectory Debug;

		// Token: 0x040006B7 RID: 1719
		public DataDirectory Resources;

		// Token: 0x040006B8 RID: 1720
		public DataDirectory StrongName;

		// Token: 0x040006B9 RID: 1721
		public StringHeap StringHeap;

		// Token: 0x040006BA RID: 1722
		public BlobHeap BlobHeap;

		// Token: 0x040006BB RID: 1723
		public UserStringHeap UserStringHeap;

		// Token: 0x040006BC RID: 1724
		public GuidHeap GuidHeap;

		// Token: 0x040006BD RID: 1725
		public TableHeap TableHeap;

		// Token: 0x040006BE RID: 1726
		public PdbHeap PdbHeap;

		// Token: 0x040006BF RID: 1727
		private readonly int[] coded_index_sizes = new int[14];

		// Token: 0x040006C0 RID: 1728
		private readonly Func<Table, int> counter;
	}
}
