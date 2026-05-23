using System;
using System.IO;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

namespace Mono.Cecil.PE
{
	// Token: 0x020002C4 RID: 708
	internal sealed class ImageWriter : BinaryStreamWriter
	{
		// Token: 0x0600124C RID: 4684 RVA: 0x00039258 File Offset: 0x00037458
		private ImageWriter(ModuleDefinition module, string runtime_version, MetadataBuilder metadata, Disposable<Stream> stream, bool metadataOnly = false)
			: base(stream.value)
		{
			this.module = module;
			this.runtime_version = runtime_version;
			this.text_map = metadata.text_map;
			this.stream = stream;
			this.metadata = metadata;
			if (metadataOnly)
			{
				return;
			}
			this.pe64 = module.Architecture == TargetArchitecture.AMD64 || module.Architecture == TargetArchitecture.IA64 || module.Architecture == TargetArchitecture.ARM64;
			this.has_reloc = module.Architecture == TargetArchitecture.I386;
			this.GetDebugHeader();
			this.GetWin32Resources();
			this.BuildTextMap();
			this.sections = (this.has_reloc ? 2 : 1);
		}

		// Token: 0x0600124D RID: 4685 RVA: 0x00039308 File Offset: 0x00037508
		private void GetDebugHeader()
		{
			ISymbolWriter symbol_writer = this.metadata.symbol_writer;
			if (symbol_writer != null)
			{
				this.debug_header = symbol_writer.GetDebugHeader();
			}
			if (this.module.HasDebugHeader)
			{
				if (this.module.GetDebugHeader().GetDeterministicEntry() == null)
				{
					return;
				}
				this.debug_header = this.debug_header.AddDeterministicEntry();
			}
		}

		// Token: 0x0600124E RID: 4686 RVA: 0x00039364 File Offset: 0x00037564
		private void GetWin32Resources()
		{
			if (!this.module.HasImage)
			{
				return;
			}
			DataDirectory win32_resources_directory = this.module.Image.Win32Resources;
			uint size = win32_resources_directory.Size;
			if (size > 0U)
			{
				this.win32_resources = this.module.Image.GetReaderAt<uint, ByteBuffer>(win32_resources_directory.VirtualAddress, size, (uint s, BinaryStreamReader reader) => new ByteBuffer(reader.ReadBytes((int)s)));
			}
		}

		// Token: 0x0600124F RID: 4687 RVA: 0x000393D7 File Offset: 0x000375D7
		public static ImageWriter CreateWriter(ModuleDefinition module, MetadataBuilder metadata, Disposable<Stream> stream)
		{
			ImageWriter imageWriter = new ImageWriter(module, module.runtime_version, metadata, stream, false);
			imageWriter.BuildSections();
			return imageWriter;
		}

		// Token: 0x06001250 RID: 4688 RVA: 0x000393F0 File Offset: 0x000375F0
		public static ImageWriter CreateDebugWriter(ModuleDefinition module, MetadataBuilder metadata, Disposable<Stream> stream)
		{
			ImageWriter imageWriter = new ImageWriter(module, "PDB v1.0", metadata, stream, true);
			uint length = metadata.text_map.GetLength();
			imageWriter.text = new Section
			{
				SizeOfRawData = length,
				VirtualSize = length
			};
			return imageWriter;
		}

		// Token: 0x06001251 RID: 4689 RVA: 0x00039430 File Offset: 0x00037630
		private void BuildSections()
		{
			bool flag = this.win32_resources != null;
			if (flag)
			{
				this.sections += 1;
			}
			this.text = this.CreateSection(".text", this.text_map.GetLength(), null);
			Section previous = this.text;
			if (flag)
			{
				this.rsrc = this.CreateSection(".rsrc", (uint)this.win32_resources.length, previous);
				this.PatchWin32Resources(this.win32_resources);
				previous = this.rsrc;
			}
			if (this.has_reloc)
			{
				this.reloc = this.CreateSection(".reloc", 12U, previous);
			}
		}

		// Token: 0x06001252 RID: 4690 RVA: 0x000394CC File Offset: 0x000376CC
		private Section CreateSection(string name, uint size, Section previous)
		{
			return new Section
			{
				Name = name,
				VirtualAddress = ((previous != null) ? (previous.VirtualAddress + ImageWriter.Align(previous.VirtualSize, 8192U)) : 8192U),
				VirtualSize = size,
				PointerToRawData = ((previous != null) ? (previous.PointerToRawData + previous.SizeOfRawData) : ImageWriter.Align(this.GetHeaderSize(), 512U)),
				SizeOfRawData = ImageWriter.Align(size, 512U)
			};
		}

		// Token: 0x06001253 RID: 4691 RVA: 0x0003954C File Offset: 0x0003774C
		private static uint Align(uint value, uint align)
		{
			align -= 1U;
			return (value + align) & ~align;
		}

		// Token: 0x06001254 RID: 4692 RVA: 0x00039559 File Offset: 0x00037759
		private void WriteDOSHeader()
		{
			this.Write(new byte[]
			{
				77, 90, 144, 0, 3, 0, 0, 0, 4, 0,
				0, 0, byte.MaxValue, byte.MaxValue, 0, 0, 184, 0, 0, 0,
				0, 0, 0, 0, 64, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
				0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
				128, 0, 0, 0, 14, 31, 186, 14, 0, 180,
				9, 205, 33, 184, 1, 76, 205, 33, 84, 104,
				105, 115, 32, 112, 114, 111, 103, 114, 97, 109,
				32, 99, 97, 110, 110, 111, 116, 32, 98, 101,
				32, 114, 117, 110, 32, 105, 110, 32, 68, 79,
				83, 32, 109, 111, 100, 101, 46, 13, 13, 10,
				36, 0, 0, 0, 0, 0, 0, 0
			});
		}

		// Token: 0x06001255 RID: 4693 RVA: 0x00039576 File Offset: 0x00037776
		private ushort SizeOfOptionalHeader()
		{
			return (!this.pe64) ? 224 : 240;
		}

		// Token: 0x06001256 RID: 4694 RVA: 0x00039590 File Offset: 0x00037790
		private void WritePEFileHeader()
		{
			base.WriteUInt32(17744U);
			base.WriteUInt16((ushort)this.module.Architecture);
			base.WriteUInt16(this.sections);
			base.WriteUInt32(this.metadata.timestamp);
			base.WriteUInt32(0U);
			base.WriteUInt32(0U);
			base.WriteUInt16(this.SizeOfOptionalHeader());
			ushort characteristics = (ushort)(2 | ((!this.pe64) ? 256 : 32));
			if (this.module.Kind == ModuleKind.Dll || this.module.Kind == ModuleKind.NetModule)
			{
				characteristics |= 8192;
			}
			if (this.module.Image != null && (this.module.Image.Characteristics & 32U) != 0U)
			{
				characteristics |= 32;
			}
			base.WriteUInt16(characteristics);
		}

		// Token: 0x06001257 RID: 4695 RVA: 0x00039659 File Offset: 0x00037859
		private Section LastSection()
		{
			if (this.reloc != null)
			{
				return this.reloc;
			}
			if (this.rsrc != null)
			{
				return this.rsrc;
			}
			return this.text;
		}

		// Token: 0x06001258 RID: 4696 RVA: 0x00039680 File Offset: 0x00037880
		private void WriteOptionalHeaders()
		{
			base.WriteUInt16((!this.pe64) ? 267 : 523);
			base.WriteUInt16(this.module.linker_version);
			base.WriteUInt32(this.text.SizeOfRawData);
			base.WriteUInt32(((this.reloc != null) ? this.reloc.SizeOfRawData : 0U) + ((this.rsrc != null) ? this.rsrc.SizeOfRawData : 0U));
			base.WriteUInt32(0U);
			Range startub_stub = this.text_map.GetRange(TextSegment.StartupStub);
			base.WriteUInt32((startub_stub.Length > 0U) ? startub_stub.Start : 0U);
			base.WriteUInt32(8192U);
			if (!this.pe64)
			{
				base.WriteUInt32(0U);
				base.WriteUInt32(4194304U);
			}
			else
			{
				base.WriteUInt64(4194304UL);
			}
			base.WriteUInt32(8192U);
			base.WriteUInt32(512U);
			base.WriteUInt16(4);
			base.WriteUInt16(0);
			base.WriteUInt16(0);
			base.WriteUInt16(0);
			base.WriteUInt16(this.module.subsystem_major);
			base.WriteUInt16(this.module.subsystem_minor);
			base.WriteUInt32(0U);
			Section last_section = this.LastSection();
			base.WriteUInt32(last_section.VirtualAddress + ImageWriter.Align(last_section.VirtualSize, 8192U));
			base.WriteUInt32(this.text.PointerToRawData);
			base.WriteUInt32(0U);
			base.WriteUInt16(this.GetSubSystem());
			base.WriteUInt16((ushort)this.module.Characteristics);
			if (!this.pe64)
			{
				base.WriteUInt32(1048576U);
				base.WriteUInt32(4096U);
				base.WriteUInt32(1048576U);
				base.WriteUInt32(4096U);
			}
			else
			{
				base.WriteUInt64(4194304UL);
				base.WriteUInt64(16384UL);
				base.WriteUInt64(1048576UL);
				base.WriteUInt64(8192UL);
			}
			base.WriteUInt32(0U);
			base.WriteUInt32(16U);
			this.WriteZeroDataDirectory();
			base.WriteDataDirectory(this.text_map.GetDataDirectory(TextSegment.ImportDirectory));
			if (this.rsrc != null)
			{
				base.WriteUInt32(this.rsrc.VirtualAddress);
				base.WriteUInt32(this.rsrc.VirtualSize);
			}
			else
			{
				this.WriteZeroDataDirectory();
			}
			this.WriteZeroDataDirectory();
			this.WriteZeroDataDirectory();
			base.WriteUInt32((this.reloc != null) ? this.reloc.VirtualAddress : 0U);
			base.WriteUInt32((this.reloc != null) ? this.reloc.VirtualSize : 0U);
			if (this.text_map.GetLength(TextSegment.DebugDirectory) > 0)
			{
				base.WriteUInt32(this.text_map.GetRVA(TextSegment.DebugDirectory));
				base.WriteUInt32((uint)(this.debug_header.Entries.Length * 28));
			}
			else
			{
				this.WriteZeroDataDirectory();
			}
			this.WriteZeroDataDirectory();
			this.WriteZeroDataDirectory();
			this.WriteZeroDataDirectory();
			this.WriteZeroDataDirectory();
			this.WriteZeroDataDirectory();
			base.WriteDataDirectory(this.text_map.GetDataDirectory(TextSegment.ImportAddressTable));
			this.WriteZeroDataDirectory();
			base.WriteDataDirectory(this.text_map.GetDataDirectory(TextSegment.CLIHeader));
			this.WriteZeroDataDirectory();
		}

		// Token: 0x06001259 RID: 4697 RVA: 0x000399A5 File Offset: 0x00037BA5
		private void WriteZeroDataDirectory()
		{
			base.WriteUInt32(0U);
			base.WriteUInt32(0U);
		}

		// Token: 0x0600125A RID: 4698 RVA: 0x000399B8 File Offset: 0x00037BB8
		private ushort GetSubSystem()
		{
			switch (this.module.Kind)
			{
			case ModuleKind.Dll:
			case ModuleKind.Console:
			case ModuleKind.NetModule:
				return 3;
			case ModuleKind.Windows:
				return 2;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		// Token: 0x0600125B RID: 4699 RVA: 0x000399F4 File Offset: 0x00037BF4
		private void WriteSectionHeaders()
		{
			this.WriteSection(this.text, 1610612768U);
			if (this.rsrc != null)
			{
				this.WriteSection(this.rsrc, 1073741888U);
			}
			if (this.reloc != null)
			{
				this.WriteSection(this.reloc, 1107296320U);
			}
		}

		// Token: 0x0600125C RID: 4700 RVA: 0x00039A44 File Offset: 0x00037C44
		private void WriteSection(Section section, uint characteristics)
		{
			byte[] name = new byte[8];
			string sect_name = section.Name;
			for (int i = 0; i < sect_name.Length; i++)
			{
				name[i] = (byte)sect_name[i];
			}
			base.WriteBytes(name);
			base.WriteUInt32(section.VirtualSize);
			base.WriteUInt32(section.VirtualAddress);
			base.WriteUInt32(section.SizeOfRawData);
			base.WriteUInt32(section.PointerToRawData);
			base.WriteUInt32(0U);
			base.WriteUInt32(0U);
			base.WriteUInt16(0);
			base.WriteUInt16(0);
			base.WriteUInt32(characteristics);
		}

		// Token: 0x0600125D RID: 4701 RVA: 0x00039AD5 File Offset: 0x00037CD5
		private uint GetRVAFileOffset(Section section, uint rva)
		{
			return section.PointerToRawData + rva - section.VirtualAddress;
		}

		// Token: 0x0600125E RID: 4702 RVA: 0x00039AE6 File Offset: 0x00037CE6
		private void MoveTo(uint pointer)
		{
			this.BaseStream.Seek((long)((ulong)pointer), SeekOrigin.Begin);
		}

		// Token: 0x0600125F RID: 4703 RVA: 0x00039AF7 File Offset: 0x00037CF7
		private void MoveToRVA(Section section, uint rva)
		{
			this.BaseStream.Seek((long)((ulong)this.GetRVAFileOffset(section, rva)), SeekOrigin.Begin);
		}

		// Token: 0x06001260 RID: 4704 RVA: 0x00039B0F File Offset: 0x00037D0F
		internal void MoveToRVA(TextSegment segment)
		{
			this.MoveToRVA(this.text, this.text_map.GetRVA(segment));
		}

		// Token: 0x06001261 RID: 4705 RVA: 0x00039B29 File Offset: 0x00037D29
		private void WriteRVA(uint rva)
		{
			if (!this.pe64)
			{
				base.WriteUInt32(rva);
				return;
			}
			base.WriteUInt64((ulong)rva);
		}

		// Token: 0x06001262 RID: 4706 RVA: 0x00039B44 File Offset: 0x00037D44
		private void PrepareSection(Section section)
		{
			this.MoveTo(section.PointerToRawData);
			if (section.SizeOfRawData <= 4096U)
			{
				this.Write(new byte[section.SizeOfRawData]);
				this.MoveTo(section.PointerToRawData);
				return;
			}
			int written = 0;
			byte[] buffer = new byte[4096];
			while ((long)written != (long)((ulong)section.SizeOfRawData))
			{
				int write_size = Math.Min((int)(section.SizeOfRawData - (uint)written), 4096);
				this.Write(buffer, 0, write_size);
				written += write_size;
			}
			this.MoveTo(section.PointerToRawData);
		}

		// Token: 0x06001263 RID: 4707 RVA: 0x00039BD0 File Offset: 0x00037DD0
		private void WriteText()
		{
			this.PrepareSection(this.text);
			if (this.has_reloc)
			{
				this.WriteRVA(this.text_map.GetRVA(TextSegment.ImportHintNameTable));
				this.WriteRVA(0U);
			}
			base.WriteUInt32(72U);
			base.WriteUInt16(2);
			base.WriteUInt16((this.module.Runtime <= TargetRuntime.Net_1_1) ? 0 : 5);
			base.WriteUInt32(this.text_map.GetRVA(TextSegment.MetadataHeader));
			base.WriteUInt32(this.GetMetadataLength());
			base.WriteUInt32((uint)this.module.Attributes);
			base.WriteUInt32(this.metadata.entry_point.ToUInt32());
			base.WriteDataDirectory(this.text_map.GetDataDirectory(TextSegment.Resources));
			base.WriteDataDirectory(this.text_map.GetDataDirectory(TextSegment.StrongNameSignature));
			this.WriteZeroDataDirectory();
			this.WriteZeroDataDirectory();
			this.WriteZeroDataDirectory();
			this.WriteZeroDataDirectory();
			this.MoveToRVA(TextSegment.Code);
			base.WriteBuffer(this.metadata.code);
			this.MoveToRVA(TextSegment.Resources);
			base.WriteBuffer(this.metadata.resources);
			if (this.metadata.data.length > 0)
			{
				this.MoveToRVA(TextSegment.Data);
				base.WriteBuffer(this.metadata.data);
			}
			this.MoveToRVA(TextSegment.MetadataHeader);
			this.WriteMetadataHeader();
			this.WriteMetadata();
			if (this.text_map.GetLength(TextSegment.DebugDirectory) > 0)
			{
				this.MoveToRVA(TextSegment.DebugDirectory);
				this.WriteDebugDirectory();
			}
			if (!this.has_reloc)
			{
				return;
			}
			this.MoveToRVA(TextSegment.ImportDirectory);
			this.WriteImportDirectory();
			this.MoveToRVA(TextSegment.StartupStub);
			this.WriteStartupStub();
		}

		// Token: 0x06001264 RID: 4708 RVA: 0x00039D65 File Offset: 0x00037F65
		private uint GetMetadataLength()
		{
			return this.text_map.GetRVA(TextSegment.DebugDirectory) - this.text_map.GetRVA(TextSegment.MetadataHeader);
		}

		// Token: 0x06001265 RID: 4709 RVA: 0x00039D84 File Offset: 0x00037F84
		public void WriteMetadataHeader()
		{
			base.WriteUInt32(1112167234U);
			base.WriteUInt16(1);
			base.WriteUInt16(1);
			base.WriteUInt32(0U);
			byte[] version = ImageWriter.GetZeroTerminatedString(this.runtime_version);
			base.WriteUInt32((uint)version.Length);
			base.WriteBytes(version);
			base.WriteUInt16(0);
			base.WriteUInt16(this.GetStreamCount());
			uint offset = this.text_map.GetRVA(TextSegment.TableHeap) - this.text_map.GetRVA(TextSegment.MetadataHeader);
			this.WriteStreamHeader(ref offset, TextSegment.TableHeap, "#~");
			this.WriteStreamHeader(ref offset, TextSegment.StringHeap, "#Strings");
			this.WriteStreamHeader(ref offset, TextSegment.UserStringHeap, "#US");
			this.WriteStreamHeader(ref offset, TextSegment.GuidHeap, "#GUID");
			this.WriteStreamHeader(ref offset, TextSegment.BlobHeap, "#Blob");
			this.WriteStreamHeader(ref offset, TextSegment.PdbHeap, "#Pdb");
		}

		// Token: 0x06001266 RID: 4710 RVA: 0x00039E54 File Offset: 0x00038054
		private ushort GetStreamCount()
		{
			return (ushort)(2 + ((!this.metadata.user_string_heap.IsEmpty) ? 1 : 0) + ((!this.metadata.guid_heap.IsEmpty) ? 1 : 0) + ((!this.metadata.blob_heap.IsEmpty) ? 1 : 0) + ((this.metadata.pdb_heap != null) ? 1 : 0));
		}

		// Token: 0x06001267 RID: 4711 RVA: 0x00039EB0 File Offset: 0x000380B0
		private void WriteStreamHeader(ref uint offset, TextSegment heap, string name)
		{
			uint length = (uint)this.text_map.GetLength(heap);
			if (length == 0U)
			{
				return;
			}
			base.WriteUInt32(offset);
			base.WriteUInt32(length);
			base.WriteBytes(ImageWriter.GetZeroTerminatedString(name));
			offset += length;
		}

		// Token: 0x06001268 RID: 4712 RVA: 0x00039EEF File Offset: 0x000380EF
		private static int GetZeroTerminatedStringLength(string @string)
		{
			return (@string.Length + 1 + 3) & -4;
		}

		// Token: 0x06001269 RID: 4713 RVA: 0x00039EFE File Offset: 0x000380FE
		private static byte[] GetZeroTerminatedString(string @string)
		{
			return ImageWriter.GetString(@string, ImageWriter.GetZeroTerminatedStringLength(@string));
		}

		// Token: 0x0600126A RID: 4714 RVA: 0x00039F0C File Offset: 0x0003810C
		private static byte[] GetSimpleString(string @string)
		{
			return ImageWriter.GetString(@string, @string.Length);
		}

		// Token: 0x0600126B RID: 4715 RVA: 0x00039F1C File Offset: 0x0003811C
		private static byte[] GetString(string @string, int length)
		{
			byte[] bytes = new byte[length];
			for (int i = 0; i < @string.Length; i++)
			{
				bytes[i] = (byte)@string[i];
			}
			return bytes;
		}

		// Token: 0x0600126C RID: 4716 RVA: 0x00039F50 File Offset: 0x00038150
		public void WriteMetadata()
		{
			this.WriteHeap(TextSegment.TableHeap, this.metadata.table_heap);
			this.WriteHeap(TextSegment.StringHeap, this.metadata.string_heap);
			this.WriteHeap(TextSegment.UserStringHeap, this.metadata.user_string_heap);
			this.WriteHeap(TextSegment.GuidHeap, this.metadata.guid_heap);
			this.WriteHeap(TextSegment.BlobHeap, this.metadata.blob_heap);
			this.WriteHeap(TextSegment.PdbHeap, this.metadata.pdb_heap);
		}

		// Token: 0x0600126D RID: 4717 RVA: 0x00039FCD File Offset: 0x000381CD
		private void WriteHeap(TextSegment heap, HeapBuffer buffer)
		{
			if (buffer == null || buffer.IsEmpty)
			{
				return;
			}
			this.MoveToRVA(heap);
			base.WriteBuffer(buffer);
		}

		// Token: 0x0600126E RID: 4718 RVA: 0x00039FEC File Offset: 0x000381EC
		private void WriteDebugDirectory()
		{
			int data_start = (int)this.BaseStream.Position + this.debug_header.Entries.Length * 28;
			for (int i = 0; i < this.debug_header.Entries.Length; i++)
			{
				ImageDebugHeaderEntry entry = this.debug_header.Entries[i];
				ImageDebugDirectory directory = entry.Directory;
				base.WriteInt32(directory.Characteristics);
				base.WriteInt32(directory.TimeDateStamp);
				base.WriteInt16(directory.MajorVersion);
				base.WriteInt16(directory.MinorVersion);
				base.WriteInt32((int)directory.Type);
				base.WriteInt32(directory.SizeOfData);
				base.WriteInt32(directory.AddressOfRawData);
				base.WriteInt32(data_start);
				data_start += entry.Data.Length;
			}
			this.debug_header_entries_position = this.BaseStream.Position;
			for (int j = 0; j < this.debug_header.Entries.Length; j++)
			{
				ImageDebugHeaderEntry entry2 = this.debug_header.Entries[j];
				base.WriteBytes(entry2.Data);
			}
		}

		// Token: 0x0600126F RID: 4719 RVA: 0x0003A0F8 File Offset: 0x000382F8
		private void WriteImportDirectory()
		{
			base.WriteUInt32(this.text_map.GetRVA(TextSegment.ImportDirectory) + 40U);
			base.WriteUInt32(0U);
			base.WriteUInt32(0U);
			base.WriteUInt32(this.text_map.GetRVA(TextSegment.ImportHintNameTable) + 14U);
			base.WriteUInt32(this.text_map.GetRVA(TextSegment.ImportAddressTable));
			base.Advance(20);
			base.WriteUInt32(this.text_map.GetRVA(TextSegment.ImportHintNameTable));
			this.MoveToRVA(TextSegment.ImportHintNameTable);
			base.WriteUInt16(0);
			base.WriteBytes(this.GetRuntimeMain());
			base.WriteByte(0);
			base.WriteBytes(ImageWriter.GetSimpleString("mscoree.dll"));
			base.WriteUInt16(0);
		}

		// Token: 0x06001270 RID: 4720 RVA: 0x0003A1A5 File Offset: 0x000383A5
		private byte[] GetRuntimeMain()
		{
			if (this.module.Kind != ModuleKind.Dll && this.module.Kind != ModuleKind.NetModule)
			{
				return ImageWriter.GetSimpleString("_CorExeMain");
			}
			return ImageWriter.GetSimpleString("_CorDllMain");
		}

		// Token: 0x06001271 RID: 4721 RVA: 0x0003A1D7 File Offset: 0x000383D7
		private void WriteStartupStub()
		{
			if (this.module.Architecture == TargetArchitecture.I386)
			{
				base.WriteUInt16(9727);
				base.WriteUInt32(4194304U + this.text_map.GetRVA(TextSegment.ImportAddressTable));
				return;
			}
			throw new NotSupportedException();
		}

		// Token: 0x06001272 RID: 4722 RVA: 0x0003A214 File Offset: 0x00038414
		private void WriteRsrc()
		{
			this.PrepareSection(this.rsrc);
			base.WriteBuffer(this.win32_resources);
		}

		// Token: 0x06001273 RID: 4723 RVA: 0x0003A230 File Offset: 0x00038430
		private void WriteReloc()
		{
			this.PrepareSection(this.reloc);
			uint reloc_rva = this.text_map.GetRVA(TextSegment.StartupStub);
			reloc_rva += ((this.module.Architecture == TargetArchitecture.IA64) ? 32U : 2U);
			uint page_rva = reloc_rva & 4294963200U;
			base.WriteUInt32(page_rva);
			base.WriteUInt32(12U);
			if (this.module.Architecture == TargetArchitecture.I386)
			{
				base.WriteUInt32(12288U + reloc_rva - page_rva);
				return;
			}
			throw new NotSupportedException();
		}

		// Token: 0x06001274 RID: 4724 RVA: 0x0003A2B0 File Offset: 0x000384B0
		public void WriteImage()
		{
			this.WriteDOSHeader();
			this.WritePEFileHeader();
			this.WriteOptionalHeaders();
			this.WriteSectionHeaders();
			this.WriteText();
			if (this.rsrc != null)
			{
				this.WriteRsrc();
			}
			if (this.reloc != null)
			{
				this.WriteReloc();
			}
			this.Flush();
		}

		// Token: 0x06001275 RID: 4725 RVA: 0x0003A300 File Offset: 0x00038500
		private void BuildTextMap()
		{
			TextMap map = this.text_map;
			map.AddMap(TextSegment.Code, this.metadata.code.length, (!this.pe64) ? 4 : 16);
			map.AddMap(TextSegment.Resources, this.metadata.resources.length, 8);
			map.AddMap(TextSegment.Data, this.metadata.data.length, this.metadata.data.BufferAlign);
			if (this.metadata.data.length > 0)
			{
				this.metadata.table_heap.FixupData(map.GetRVA(TextSegment.Data));
			}
			map.AddMap(TextSegment.StrongNameSignature, this.GetStrongNameLength(), 4);
			this.BuildMetadataTextMap();
			int debug_dir_len = 0;
			if (this.debug_header != null && this.debug_header.HasEntries)
			{
				int directories_len = this.debug_header.Entries.Length * 28;
				int data_address = (int)(map.GetNextRVA(TextSegment.BlobHeap) + (uint)directories_len);
				int data_len = 0;
				for (int i = 0; i < this.debug_header.Entries.Length; i++)
				{
					ImageDebugHeaderEntry entry = this.debug_header.Entries[i];
					ImageDebugDirectory directory = entry.Directory;
					directory.AddressOfRawData = ((entry.Data.Length == 0) ? 0 : data_address);
					entry.Directory = directory;
					data_len += entry.Data.Length;
					data_address += entry.Data.Length;
				}
				debug_dir_len = directories_len + data_len;
			}
			map.AddMap(TextSegment.DebugDirectory, debug_dir_len, 4);
			if (!this.has_reloc)
			{
				uint start = map.GetNextRVA(TextSegment.DebugDirectory);
				map.AddMap(TextSegment.ImportDirectory, new Range(start, 0U));
				map.AddMap(TextSegment.ImportHintNameTable, new Range(start, 0U));
				map.AddMap(TextSegment.StartupStub, new Range(start, 0U));
				return;
			}
			uint import_dir_rva = map.GetNextRVA(TextSegment.DebugDirectory);
			uint import_hnt_rva = import_dir_rva + 48U;
			import_hnt_rva = (import_hnt_rva + 15U) & 4294967280U;
			uint import_dir_len = import_hnt_rva - import_dir_rva + 27U;
			uint startup_stub_rva = import_dir_rva + import_dir_len;
			startup_stub_rva = ((this.module.Architecture == TargetArchitecture.IA64) ? ((startup_stub_rva + 15U) & 4294967280U) : (2U + ((startup_stub_rva + 3U) & 4294967292U)));
			map.AddMap(TextSegment.ImportDirectory, new Range(import_dir_rva, import_dir_len));
			map.AddMap(TextSegment.ImportHintNameTable, new Range(import_hnt_rva, 0U));
			map.AddMap(TextSegment.StartupStub, new Range(startup_stub_rva, this.GetStartupStubLength()));
		}

		// Token: 0x06001276 RID: 4726 RVA: 0x0003A538 File Offset: 0x00038738
		public void BuildMetadataTextMap()
		{
			TextMap textMap = this.text_map;
			textMap.AddMap(TextSegment.MetadataHeader, this.GetMetadataHeaderLength(this.module.RuntimeVersion));
			textMap.AddMap(TextSegment.TableHeap, this.metadata.table_heap.length, 4);
			textMap.AddMap(TextSegment.StringHeap, this.metadata.string_heap.length, 4);
			textMap.AddMap(TextSegment.UserStringHeap, this.metadata.user_string_heap.IsEmpty ? 0 : this.metadata.user_string_heap.length, 4);
			textMap.AddMap(TextSegment.GuidHeap, this.metadata.guid_heap.length, 4);
			textMap.AddMap(TextSegment.BlobHeap, this.metadata.blob_heap.IsEmpty ? 0 : this.metadata.blob_heap.length, 4);
			textMap.AddMap(TextSegment.PdbHeap, (this.metadata.pdb_heap == null) ? 0 : this.metadata.pdb_heap.length, 4);
		}

		// Token: 0x06001277 RID: 4727 RVA: 0x0003A630 File Offset: 0x00038830
		private uint GetStartupStubLength()
		{
			if (this.module.Architecture == TargetArchitecture.I386)
			{
				return 6U;
			}
			throw new NotSupportedException();
		}

		// Token: 0x06001278 RID: 4728 RVA: 0x0003A64C File Offset: 0x0003884C
		private int GetMetadataHeaderLength(string runtimeVersion)
		{
			return 20 + ImageWriter.GetZeroTerminatedStringLength(runtimeVersion) + 12 + 20 + (this.metadata.user_string_heap.IsEmpty ? 0 : 12) + 16 + (this.metadata.blob_heap.IsEmpty ? 0 : 16) + ((this.metadata.pdb_heap == null) ? 0 : 16);
		}

		// Token: 0x06001279 RID: 4729 RVA: 0x0003A6B0 File Offset: 0x000388B0
		private int GetStrongNameLength()
		{
			if (this.module.kind == ModuleKind.NetModule || this.module.Assembly == null)
			{
				return 0;
			}
			byte[] public_key = this.module.Assembly.Name.PublicKey;
			if (public_key.IsNullOrEmpty<byte>())
			{
				return 0;
			}
			int size = public_key.Length;
			if (size > 32)
			{
				return size - 32;
			}
			return 128;
		}

		// Token: 0x0600127A RID: 4730 RVA: 0x0003A70D File Offset: 0x0003890D
		public DataDirectory GetStrongNameSignatureDirectory()
		{
			return this.text_map.GetDataDirectory(TextSegment.StrongNameSignature);
		}

		// Token: 0x0600127B RID: 4731 RVA: 0x0003A71B File Offset: 0x0003891B
		public uint GetHeaderSize()
		{
			return (uint)(152 + this.SizeOfOptionalHeader() + this.sections * 40);
		}

		// Token: 0x0600127C RID: 4732 RVA: 0x0003A733 File Offset: 0x00038933
		private void PatchWin32Resources(ByteBuffer resources)
		{
			this.PatchResourceDirectoryTable(resources);
		}

		// Token: 0x0600127D RID: 4733 RVA: 0x0003A73C File Offset: 0x0003893C
		private void PatchResourceDirectoryTable(ByteBuffer resources)
		{
			resources.Advance(12);
			int entries = (int)(resources.ReadUInt16() + resources.ReadUInt16());
			for (int i = 0; i < entries; i++)
			{
				this.PatchResourceDirectoryEntry(resources);
			}
		}

		// Token: 0x0600127E RID: 4734 RVA: 0x0003A774 File Offset: 0x00038974
		private void PatchResourceDirectoryEntry(ByteBuffer resources)
		{
			resources.Advance(4);
			uint child = resources.ReadUInt32();
			int position = resources.position;
			resources.position = (int)(child & 2147483647U);
			if ((child & 2147483648U) != 0U)
			{
				this.PatchResourceDirectoryTable(resources);
			}
			else
			{
				this.PatchResourceDataEntry(resources);
			}
			resources.position = position;
		}

		// Token: 0x0600127F RID: 4735 RVA: 0x0003A7C4 File Offset: 0x000389C4
		private void PatchResourceDataEntry(ByteBuffer resources)
		{
			uint rva = resources.ReadUInt32();
			resources.position -= 4;
			resources.WriteUInt32(rva - this.module.Image.Win32Resources.VirtualAddress + this.rsrc.VirtualAddress);
		}

		// Token: 0x040006C6 RID: 1734
		private readonly ModuleDefinition module;

		// Token: 0x040006C7 RID: 1735
		private readonly MetadataBuilder metadata;

		// Token: 0x040006C8 RID: 1736
		private readonly TextMap text_map;

		// Token: 0x040006C9 RID: 1737
		internal readonly Disposable<Stream> stream;

		// Token: 0x040006CA RID: 1738
		private readonly string runtime_version;

		// Token: 0x040006CB RID: 1739
		private ImageDebugHeader debug_header;

		// Token: 0x040006CC RID: 1740
		private ByteBuffer win32_resources;

		// Token: 0x040006CD RID: 1741
		private const uint pe_header_size = 152U;

		// Token: 0x040006CE RID: 1742
		private const uint section_header_size = 40U;

		// Token: 0x040006CF RID: 1743
		private const uint file_alignment = 512U;

		// Token: 0x040006D0 RID: 1744
		private const uint section_alignment = 8192U;

		// Token: 0x040006D1 RID: 1745
		private const ulong image_base = 4194304UL;

		// Token: 0x040006D2 RID: 1746
		internal const uint text_rva = 8192U;

		// Token: 0x040006D3 RID: 1747
		private readonly bool pe64;

		// Token: 0x040006D4 RID: 1748
		private readonly bool has_reloc;

		// Token: 0x040006D5 RID: 1749
		internal Section text;

		// Token: 0x040006D6 RID: 1750
		internal Section rsrc;

		// Token: 0x040006D7 RID: 1751
		internal Section reloc;

		// Token: 0x040006D8 RID: 1752
		private ushort sections;

		// Token: 0x040006D9 RID: 1753
		internal long debug_header_entries_position;
	}
}
