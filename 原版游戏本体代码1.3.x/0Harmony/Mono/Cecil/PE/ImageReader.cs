using System;
using System.IO;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

namespace Mono.Cecil.PE
{
	// Token: 0x020002C3 RID: 707
	internal sealed class ImageReader : BinaryStreamReader
	{
		// Token: 0x06001236 RID: 4662 RVA: 0x000382B0 File Offset: 0x000364B0
		public ImageReader(Disposable<Stream> stream, string file_name)
			: base(stream.value)
		{
			this.image = new Image();
			this.image.Stream = stream;
			this.image.FileName = file_name;
		}

		// Token: 0x06001237 RID: 4663 RVA: 0x000382E1 File Offset: 0x000364E1
		private void MoveTo(DataDirectory directory)
		{
			this.BaseStream.Position = (long)((ulong)this.image.ResolveVirtualAddress(directory.VirtualAddress));
		}

		// Token: 0x06001238 RID: 4664 RVA: 0x00038300 File Offset: 0x00036500
		private void ReadImage()
		{
			if (this.BaseStream.Length < 128L)
			{
				throw new BadImageFormatException();
			}
			if (this.ReadUInt16() != 23117)
			{
				throw new BadImageFormatException();
			}
			base.Advance(58);
			base.MoveTo(this.ReadUInt32());
			if (this.ReadUInt32() != 17744U)
			{
				throw new BadImageFormatException();
			}
			this.image.Architecture = this.ReadArchitecture();
			ushort sections = this.ReadUInt16();
			this.image.Timestamp = this.ReadUInt32();
			base.Advance(10);
			ushort characteristics = this.ReadUInt16();
			ushort subsystem;
			ushort dll_characteristics;
			this.ReadOptionalHeaders(out subsystem, out dll_characteristics);
			this.ReadSections(sections);
			this.ReadCLIHeader();
			this.ReadMetadata();
			this.ReadDebugHeader();
			this.image.Characteristics = (uint)characteristics;
			this.image.Kind = ImageReader.GetModuleKind(characteristics, subsystem);
			this.image.DllCharacteristics = (ModuleCharacteristics)dll_characteristics;
		}

		// Token: 0x06001239 RID: 4665 RVA: 0x000383E5 File Offset: 0x000365E5
		private TargetArchitecture ReadArchitecture()
		{
			return (TargetArchitecture)this.ReadUInt16();
		}

		// Token: 0x0600123A RID: 4666 RVA: 0x000383ED File Offset: 0x000365ED
		private static ModuleKind GetModuleKind(ushort characteristics, ushort subsystem)
		{
			if ((characteristics & 8192) != 0)
			{
				return ModuleKind.Dll;
			}
			if (subsystem == 2 || subsystem == 9)
			{
				return ModuleKind.Windows;
			}
			return ModuleKind.Console;
		}

		// Token: 0x0600123B RID: 4667 RVA: 0x00038408 File Offset: 0x00036608
		private void ReadOptionalHeaders(out ushort subsystem, out ushort dll_characteristics)
		{
			bool pe64 = this.ReadUInt16() == 523;
			this.image.LinkerVersion = this.ReadUInt16();
			base.Advance(44);
			this.image.SubSystemMajor = this.ReadUInt16();
			this.image.SubSystemMinor = this.ReadUInt16();
			base.Advance(16);
			subsystem = this.ReadUInt16();
			dll_characteristics = this.ReadUInt16();
			base.Advance(pe64 ? 56 : 40);
			this.image.Win32Resources = base.ReadDataDirectory();
			base.Advance(24);
			this.image.Debug = base.ReadDataDirectory();
			base.Advance(56);
			this.cli = base.ReadDataDirectory();
			if (this.cli.IsZero)
			{
				throw new BadImageFormatException();
			}
			base.Advance(8);
		}

		// Token: 0x0600123C RID: 4668 RVA: 0x000384E0 File Offset: 0x000366E0
		private string ReadAlignedString(int length)
		{
			int read = 0;
			char[] buffer = new char[length];
			while (read < length)
			{
				byte current = this.ReadByte();
				if (current == 0)
				{
					break;
				}
				buffer[read++] = (char)current;
			}
			base.Advance(-1 + ((read + 4) & -4) - read);
			return new string(buffer, 0, read);
		}

		// Token: 0x0600123D RID: 4669 RVA: 0x00038528 File Offset: 0x00036728
		private string ReadZeroTerminatedString(int length)
		{
			int read = 0;
			char[] buffer = new char[length];
			byte[] bytes = this.ReadBytes(length);
			while (read < length)
			{
				byte current = bytes[read];
				if (current == 0)
				{
					break;
				}
				buffer[read++] = (char)current;
			}
			return new string(buffer, 0, read);
		}

		// Token: 0x0600123E RID: 4670 RVA: 0x00038564 File Offset: 0x00036764
		private void ReadSections(ushort count)
		{
			Section[] sections = new Section[(int)count];
			for (int i = 0; i < (int)count; i++)
			{
				Section section = new Section();
				section.Name = this.ReadZeroTerminatedString(8);
				base.Advance(4);
				section.VirtualAddress = this.ReadUInt32();
				section.SizeOfRawData = this.ReadUInt32();
				section.PointerToRawData = this.ReadUInt32();
				base.Advance(16);
				sections[i] = section;
			}
			this.image.Sections = sections;
		}

		// Token: 0x0600123F RID: 4671 RVA: 0x000385DC File Offset: 0x000367DC
		private void ReadCLIHeader()
		{
			this.MoveTo(this.cli);
			base.Advance(8);
			this.metadata = base.ReadDataDirectory();
			this.image.Attributes = (ModuleAttributes)this.ReadUInt32();
			this.image.EntryPointToken = this.ReadUInt32();
			this.image.Resources = base.ReadDataDirectory();
			this.image.StrongName = base.ReadDataDirectory();
		}

		// Token: 0x06001240 RID: 4672 RVA: 0x0003864C File Offset: 0x0003684C
		private void ReadMetadata()
		{
			this.MoveTo(this.metadata);
			if (this.ReadUInt32() != 1112167234U)
			{
				throw new BadImageFormatException();
			}
			base.Advance(8);
			this.image.RuntimeVersion = this.ReadZeroTerminatedString(this.ReadInt32());
			base.Advance(2);
			ushort streams = this.ReadUInt16();
			Section section = this.image.GetSectionAtVirtualAddress(this.metadata.VirtualAddress);
			if (section == null)
			{
				throw new BadImageFormatException();
			}
			this.image.MetadataSection = section;
			for (int i = 0; i < (int)streams; i++)
			{
				this.ReadMetadataStream(section);
			}
			if (this.image.PdbHeap != null)
			{
				this.ReadPdbHeap();
			}
			if (this.image.TableHeap != null)
			{
				this.ReadTableHeap();
			}
		}

		// Token: 0x06001241 RID: 4673 RVA: 0x0003870C File Offset: 0x0003690C
		private void ReadDebugHeader()
		{
			if (this.image.Debug.IsZero)
			{
				this.image.DebugHeader = new ImageDebugHeader(Empty<ImageDebugHeaderEntry>.Array);
				return;
			}
			this.MoveTo(this.image.Debug);
			ImageDebugHeaderEntry[] entries = new ImageDebugHeaderEntry[this.image.Debug.Size / 28U];
			for (int i = 0; i < entries.Length; i++)
			{
				ImageDebugDirectory directory = new ImageDebugDirectory
				{
					Characteristics = this.ReadInt32(),
					TimeDateStamp = this.ReadInt32(),
					MajorVersion = this.ReadInt16(),
					MinorVersion = this.ReadInt16(),
					Type = (ImageDebugType)this.ReadInt32(),
					SizeOfData = this.ReadInt32(),
					AddressOfRawData = this.ReadInt32(),
					PointerToRawData = this.ReadInt32()
				};
				if (directory.PointerToRawData == 0 || directory.SizeOfData < 0)
				{
					entries[i] = new ImageDebugHeaderEntry(directory, Empty<byte>.Array);
				}
				else
				{
					int position = base.Position;
					try
					{
						base.MoveTo((uint)directory.PointerToRawData);
						byte[] data = this.ReadBytes(directory.SizeOfData);
						entries[i] = new ImageDebugHeaderEntry(directory, data);
					}
					finally
					{
						base.Position = position;
					}
				}
			}
			this.image.DebugHeader = new ImageDebugHeader(entries);
		}

		// Token: 0x06001242 RID: 4674 RVA: 0x0003886C File Offset: 0x00036A6C
		private void ReadMetadataStream(Section section)
		{
			uint offset = this.metadata.VirtualAddress - section.VirtualAddress + this.ReadUInt32();
			uint size = this.ReadUInt32();
			byte[] data = this.ReadHeapData(offset, size);
			string name = this.ReadAlignedString(16);
			if (name != null)
			{
				switch (name.Length)
				{
				case 2:
				{
					char c = name[1];
					if (c != '-')
					{
						if (c != '~')
						{
							return;
						}
						if (!(name == "#~"))
						{
							return;
						}
					}
					else if (!(name == "#-"))
					{
						return;
					}
					this.image.TableHeap = new TableHeap(data);
					this.table_heap_offset = offset;
					return;
				}
				case 3:
					if (!(name == "#US"))
					{
						return;
					}
					this.image.UserStringHeap = new UserStringHeap(data);
					return;
				case 4:
					if (!(name == "#Pdb"))
					{
						return;
					}
					this.image.PdbHeap = new PdbHeap(data);
					this.pdb_heap_offset = offset;
					break;
				case 5:
				{
					char c = name[1];
					if (c != 'B')
					{
						if (c != 'G')
						{
							return;
						}
						if (!(name == "#GUID"))
						{
							return;
						}
						this.image.GuidHeap = new GuidHeap(data);
						return;
					}
					else
					{
						if (!(name == "#Blob"))
						{
							return;
						}
						this.image.BlobHeap = new BlobHeap(data);
						return;
					}
					break;
				}
				case 6:
				case 7:
					break;
				case 8:
					if (!(name == "#Strings"))
					{
						return;
					}
					this.image.StringHeap = new StringHeap(data);
					return;
				default:
					return;
				}
			}
		}

		// Token: 0x06001243 RID: 4675 RVA: 0x000389E8 File Offset: 0x00036BE8
		private byte[] ReadHeapData(uint offset, uint size)
		{
			long position = this.BaseStream.Position;
			base.MoveTo(offset + this.image.MetadataSection.PointerToRawData);
			byte[] result = this.ReadBytes((int)size);
			this.BaseStream.Position = position;
			return result;
		}

		// Token: 0x06001244 RID: 4676 RVA: 0x00038A2C File Offset: 0x00036C2C
		private void ReadTableHeap()
		{
			TableHeap heap = this.image.TableHeap;
			base.MoveTo(this.table_heap_offset + this.image.MetadataSection.PointerToRawData);
			base.Advance(6);
			byte sizes = this.ReadByte();
			base.Advance(1);
			heap.Valid = this.ReadInt64();
			heap.Sorted = this.ReadInt64();
			if (this.image.PdbHeap != null)
			{
				for (int i = 0; i < 58; i++)
				{
					if (this.image.PdbHeap.HasTable((Table)i))
					{
						heap.Tables[i].Length = this.image.PdbHeap.TypeSystemTableRows[i];
					}
				}
			}
			for (int j = 0; j < 58; j++)
			{
				if (heap.HasTable((Table)j))
				{
					heap.Tables[j].Length = this.ReadUInt32();
				}
			}
			ImageReader.SetIndexSize(this.image.StringHeap, (uint)sizes, 1);
			ImageReader.SetIndexSize(this.image.GuidHeap, (uint)sizes, 2);
			ImageReader.SetIndexSize(this.image.BlobHeap, (uint)sizes, 4);
			this.ComputeTableInformations();
		}

		// Token: 0x06001245 RID: 4677 RVA: 0x00038B4A File Offset: 0x00036D4A
		private static void SetIndexSize(Heap heap, uint sizes, byte flag)
		{
			if (heap == null)
			{
				return;
			}
			heap.IndexSize = (((sizes & (uint)flag) > 0U) ? 4 : 2);
		}

		// Token: 0x06001246 RID: 4678 RVA: 0x00038B60 File Offset: 0x00036D60
		private int GetTableIndexSize(Table table)
		{
			return this.image.GetTableIndexSize(table);
		}

		// Token: 0x06001247 RID: 4679 RVA: 0x00038B6E File Offset: 0x00036D6E
		private int GetCodedIndexSize(CodedIndex index)
		{
			return this.image.GetCodedIndexSize(index);
		}

		// Token: 0x06001248 RID: 4680 RVA: 0x00038B7C File Offset: 0x00036D7C
		private void ComputeTableInformations()
		{
			uint offset = (uint)this.BaseStream.Position - this.table_heap_offset - this.image.MetadataSection.PointerToRawData;
			int stridx_size = ((this.image.StringHeap != null) ? this.image.StringHeap.IndexSize : 2);
			int guididx_size = ((this.image.GuidHeap != null) ? this.image.GuidHeap.IndexSize : 2);
			int blobidx_size = ((this.image.BlobHeap != null) ? this.image.BlobHeap.IndexSize : 2);
			TableHeap heap = this.image.TableHeap;
			TableInformation[] tables = heap.Tables;
			for (int i = 0; i < 58; i++)
			{
				Table table = (Table)i;
				if (heap.HasTable(table))
				{
					int size;
					switch (table)
					{
					case Table.Module:
						size = 2 + stridx_size + guididx_size * 3;
						break;
					case Table.TypeRef:
						size = this.GetCodedIndexSize(CodedIndex.ResolutionScope) + stridx_size * 2;
						break;
					case Table.TypeDef:
						size = 4 + stridx_size * 2 + this.GetCodedIndexSize(CodedIndex.TypeDefOrRef) + this.GetTableIndexSize(Table.Field) + this.GetTableIndexSize(Table.Method);
						break;
					case Table.FieldPtr:
						size = this.GetTableIndexSize(Table.Field);
						break;
					case Table.Field:
						size = 2 + stridx_size + blobidx_size;
						break;
					case Table.MethodPtr:
						size = this.GetTableIndexSize(Table.Method);
						break;
					case Table.Method:
						size = 8 + stridx_size + blobidx_size + this.GetTableIndexSize(Table.Param);
						break;
					case Table.ParamPtr:
						size = this.GetTableIndexSize(Table.Param);
						break;
					case Table.Param:
						size = 4 + stridx_size;
						break;
					case Table.InterfaceImpl:
						size = this.GetTableIndexSize(Table.TypeDef) + this.GetCodedIndexSize(CodedIndex.TypeDefOrRef);
						break;
					case Table.MemberRef:
						size = this.GetCodedIndexSize(CodedIndex.MemberRefParent) + stridx_size + blobidx_size;
						break;
					case Table.Constant:
						size = 2 + this.GetCodedIndexSize(CodedIndex.HasConstant) + blobidx_size;
						break;
					case Table.CustomAttribute:
						size = this.GetCodedIndexSize(CodedIndex.HasCustomAttribute) + this.GetCodedIndexSize(CodedIndex.CustomAttributeType) + blobidx_size;
						break;
					case Table.FieldMarshal:
						size = this.GetCodedIndexSize(CodedIndex.HasFieldMarshal) + blobidx_size;
						break;
					case Table.DeclSecurity:
						size = 2 + this.GetCodedIndexSize(CodedIndex.HasDeclSecurity) + blobidx_size;
						break;
					case Table.ClassLayout:
						size = 6 + this.GetTableIndexSize(Table.TypeDef);
						break;
					case Table.FieldLayout:
						size = 4 + this.GetTableIndexSize(Table.Field);
						break;
					case Table.StandAloneSig:
						size = blobidx_size;
						break;
					case Table.EventMap:
						size = this.GetTableIndexSize(Table.TypeDef) + this.GetTableIndexSize(Table.Event);
						break;
					case Table.EventPtr:
						size = this.GetTableIndexSize(Table.Event);
						break;
					case Table.Event:
						size = 2 + stridx_size + this.GetCodedIndexSize(CodedIndex.TypeDefOrRef);
						break;
					case Table.PropertyMap:
						size = this.GetTableIndexSize(Table.TypeDef) + this.GetTableIndexSize(Table.Property);
						break;
					case Table.PropertyPtr:
						size = this.GetTableIndexSize(Table.Property);
						break;
					case Table.Property:
						size = 2 + stridx_size + blobidx_size;
						break;
					case Table.MethodSemantics:
						size = 2 + this.GetTableIndexSize(Table.Method) + this.GetCodedIndexSize(CodedIndex.HasSemantics);
						break;
					case Table.MethodImpl:
						size = this.GetTableIndexSize(Table.TypeDef) + this.GetCodedIndexSize(CodedIndex.MethodDefOrRef) + this.GetCodedIndexSize(CodedIndex.MethodDefOrRef);
						break;
					case Table.ModuleRef:
						size = stridx_size;
						break;
					case Table.TypeSpec:
						size = blobidx_size;
						break;
					case Table.ImplMap:
						size = 2 + this.GetCodedIndexSize(CodedIndex.MemberForwarded) + stridx_size + this.GetTableIndexSize(Table.ModuleRef);
						break;
					case Table.FieldRVA:
						size = 4 + this.GetTableIndexSize(Table.Field);
						break;
					case Table.EncLog:
						size = 8;
						break;
					case Table.EncMap:
						size = 4;
						break;
					case Table.Assembly:
						size = 16 + blobidx_size + stridx_size * 2;
						break;
					case Table.AssemblyProcessor:
						size = 4;
						break;
					case Table.AssemblyOS:
						size = 12;
						break;
					case Table.AssemblyRef:
						size = 12 + blobidx_size * 2 + stridx_size * 2;
						break;
					case Table.AssemblyRefProcessor:
						size = 4 + this.GetTableIndexSize(Table.AssemblyRef);
						break;
					case Table.AssemblyRefOS:
						size = 12 + this.GetTableIndexSize(Table.AssemblyRef);
						break;
					case Table.File:
						size = 4 + stridx_size + blobidx_size;
						break;
					case Table.ExportedType:
						size = 8 + stridx_size * 2 + this.GetCodedIndexSize(CodedIndex.Implementation);
						break;
					case Table.ManifestResource:
						size = 8 + stridx_size + this.GetCodedIndexSize(CodedIndex.Implementation);
						break;
					case Table.NestedClass:
						size = this.GetTableIndexSize(Table.TypeDef) + this.GetTableIndexSize(Table.TypeDef);
						break;
					case Table.GenericParam:
						size = 4 + this.GetCodedIndexSize(CodedIndex.TypeOrMethodDef) + stridx_size;
						break;
					case Table.MethodSpec:
						size = this.GetCodedIndexSize(CodedIndex.MethodDefOrRef) + blobidx_size;
						break;
					case Table.GenericParamConstraint:
						size = this.GetTableIndexSize(Table.GenericParam) + this.GetCodedIndexSize(CodedIndex.TypeDefOrRef);
						break;
					case (Table)45:
					case (Table)46:
					case (Table)47:
						goto IL_51E;
					case Table.Document:
						size = blobidx_size + guididx_size + blobidx_size + guididx_size;
						break;
					case Table.MethodDebugInformation:
						size = this.GetTableIndexSize(Table.Document) + blobidx_size;
						break;
					case Table.LocalScope:
						size = this.GetTableIndexSize(Table.Method) + this.GetTableIndexSize(Table.ImportScope) + this.GetTableIndexSize(Table.LocalVariable) + this.GetTableIndexSize(Table.LocalConstant) + 8;
						break;
					case Table.LocalVariable:
						size = 4 + stridx_size;
						break;
					case Table.LocalConstant:
						size = stridx_size + blobidx_size;
						break;
					case Table.ImportScope:
						size = this.GetTableIndexSize(Table.ImportScope) + blobidx_size;
						break;
					case Table.StateMachineMethod:
						size = this.GetTableIndexSize(Table.Method) + this.GetTableIndexSize(Table.Method);
						break;
					case Table.CustomDebugInformation:
						size = this.GetCodedIndexSize(CodedIndex.HasCustomDebugInformation) + guididx_size + blobidx_size;
						break;
					default:
						goto IL_51E;
					}
					tables[i].RowSize = (uint)size;
					tables[i].Offset = offset;
					offset += (uint)(size * (int)tables[i].Length);
					goto IL_557;
					IL_51E:
					throw new NotSupportedException();
				}
				IL_557:;
			}
		}

		// Token: 0x06001249 RID: 4681 RVA: 0x000390F0 File Offset: 0x000372F0
		private void ReadPdbHeap()
		{
			PdbHeap heap = this.image.PdbHeap;
			ByteBuffer buffer = new ByteBuffer(heap.data);
			heap.Id = buffer.ReadBytes(20);
			heap.EntryPoint = buffer.ReadUInt32();
			heap.TypeSystemTables = buffer.ReadInt64();
			heap.TypeSystemTableRows = new uint[58];
			for (int i = 0; i < 58; i++)
			{
				Table table = (Table)i;
				if (heap.HasTable(table))
				{
					heap.TypeSystemTableRows[i] = buffer.ReadUInt32();
				}
			}
		}

		// Token: 0x0600124A RID: 4682 RVA: 0x00039170 File Offset: 0x00037370
		public static Image ReadImage(Disposable<Stream> stream, string file_name)
		{
			Image result;
			try
			{
				ImageReader imageReader = new ImageReader(stream, file_name);
				imageReader.ReadImage();
				result = imageReader.image;
			}
			catch (EndOfStreamException e)
			{
				throw new BadImageFormatException(stream.value.GetFileName(), e);
			}
			return result;
		}

		// Token: 0x0600124B RID: 4683 RVA: 0x000391B8 File Offset: 0x000373B8
		public static Image ReadPortablePdb(Disposable<Stream> stream, string file_name, out uint pdb_heap_offset)
		{
			Image result;
			try
			{
				ImageReader reader = new ImageReader(stream, file_name);
				uint length = (uint)stream.value.Length;
				reader.image.Sections = new Section[]
				{
					new Section
					{
						PointerToRawData = 0U,
						SizeOfRawData = length,
						VirtualAddress = 0U,
						VirtualSize = length
					}
				};
				reader.metadata = new DataDirectory(0U, length);
				reader.ReadMetadata();
				pdb_heap_offset = reader.pdb_heap_offset;
				result = reader.image;
			}
			catch (EndOfStreamException e)
			{
				throw new BadImageFormatException(stream.value.GetFileName(), e);
			}
			return result;
		}

		// Token: 0x040006C1 RID: 1729
		private readonly Image image;

		// Token: 0x040006C2 RID: 1730
		private DataDirectory cli;

		// Token: 0x040006C3 RID: 1731
		private DataDirectory metadata;

		// Token: 0x040006C4 RID: 1732
		private uint table_heap_offset;

		// Token: 0x040006C5 RID: 1733
		private uint pdb_heap_offset;
	}
}
