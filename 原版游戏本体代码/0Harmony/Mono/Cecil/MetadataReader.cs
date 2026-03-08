using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020001F0 RID: 496
	internal sealed class MetadataReader : ByteBuffer
	{
		// Token: 0x060009C3 RID: 2499 RVA: 0x0001F9B8 File Offset: 0x0001DBB8
		public MetadataReader(ModuleDefinition module)
			: base(module.Image.TableHeap.data)
		{
			this.image = module.Image;
			this.module = module;
			this.metadata = module.MetadataSystem;
			this.code = new CodeReader(this);
		}

		// Token: 0x060009C4 RID: 2500 RVA: 0x0001FA06 File Offset: 0x0001DC06
		public MetadataReader(Image image, ModuleDefinition module, MetadataReader metadata_reader)
			: base(image.TableHeap.data)
		{
			this.image = image;
			this.module = module;
			this.metadata = module.MetadataSystem;
			this.metadata_reader = metadata_reader;
		}

		// Token: 0x060009C5 RID: 2501 RVA: 0x0001FA3A File Offset: 0x0001DC3A
		private int GetCodedIndexSize(CodedIndex index)
		{
			return this.image.GetCodedIndexSize(index);
		}

		// Token: 0x060009C6 RID: 2502 RVA: 0x0001FA48 File Offset: 0x0001DC48
		private uint ReadByIndexSize(int size)
		{
			if (size == 4)
			{
				return base.ReadUInt32();
			}
			return (uint)base.ReadUInt16();
		}

		// Token: 0x060009C7 RID: 2503 RVA: 0x0001FA5C File Offset: 0x0001DC5C
		private byte[] ReadBlob()
		{
			BlobHeap blob_heap = this.image.BlobHeap;
			if (blob_heap == null)
			{
				this.position += 2;
				return Empty<byte>.Array;
			}
			return blob_heap.Read(this.ReadBlobIndex());
		}

		// Token: 0x060009C8 RID: 2504 RVA: 0x0001FA98 File Offset: 0x0001DC98
		private byte[] ReadBlob(uint signature)
		{
			BlobHeap blob_heap = this.image.BlobHeap;
			if (blob_heap == null)
			{
				return Empty<byte>.Array;
			}
			return blob_heap.Read(signature);
		}

		// Token: 0x060009C9 RID: 2505 RVA: 0x0001FAC4 File Offset: 0x0001DCC4
		private uint ReadBlobIndex()
		{
			BlobHeap blob_heap = this.image.BlobHeap;
			return this.ReadByIndexSize((blob_heap != null) ? blob_heap.IndexSize : 2);
		}

		// Token: 0x060009CA RID: 2506 RVA: 0x0001FAF0 File Offset: 0x0001DCF0
		private void GetBlobView(uint signature, out byte[] blob, out int index, out int count)
		{
			BlobHeap blob_heap = this.image.BlobHeap;
			if (blob_heap == null)
			{
				blob = null;
				index = (count = 0);
				return;
			}
			blob_heap.GetView(signature, out blob, out index, out count);
		}

		// Token: 0x060009CB RID: 2507 RVA: 0x0001FB24 File Offset: 0x0001DD24
		private string ReadString()
		{
			return this.image.StringHeap.Read(this.ReadByIndexSize(this.image.StringHeap.IndexSize));
		}

		// Token: 0x060009CC RID: 2508 RVA: 0x0001FB4C File Offset: 0x0001DD4C
		private uint ReadStringIndex()
		{
			return this.ReadByIndexSize(this.image.StringHeap.IndexSize);
		}

		// Token: 0x060009CD RID: 2509 RVA: 0x0001FB64 File Offset: 0x0001DD64
		private Guid ReadGuid()
		{
			return this.image.GuidHeap.Read(this.ReadByIndexSize(this.image.GuidHeap.IndexSize));
		}

		// Token: 0x060009CE RID: 2510 RVA: 0x0001FB8C File Offset: 0x0001DD8C
		private uint ReadTableIndex(Table table)
		{
			return this.ReadByIndexSize(this.image.GetTableIndexSize(table));
		}

		// Token: 0x060009CF RID: 2511 RVA: 0x0001FBA0 File Offset: 0x0001DDA0
		private MetadataToken ReadMetadataToken(CodedIndex index)
		{
			return index.GetMetadataToken(this.ReadByIndexSize(this.GetCodedIndexSize(index)));
		}

		// Token: 0x060009D0 RID: 2512 RVA: 0x0001FBB8 File Offset: 0x0001DDB8
		private int MoveTo(Table table)
		{
			TableInformation info = this.image.TableHeap[table];
			if (info.Length != 0U)
			{
				this.position = (int)info.Offset;
			}
			return (int)info.Length;
		}

		// Token: 0x060009D1 RID: 2513 RVA: 0x0001FBF4 File Offset: 0x0001DDF4
		private bool MoveTo(Table table, uint row)
		{
			TableInformation info = this.image.TableHeap[table];
			uint length = info.Length;
			if (length == 0U || row > length)
			{
				return false;
			}
			this.position = (int)(info.Offset + info.RowSize * (row - 1U));
			return true;
		}

		// Token: 0x060009D2 RID: 2514 RVA: 0x0001FC3C File Offset: 0x0001DE3C
		public AssemblyNameDefinition ReadAssemblyNameDefinition()
		{
			if (this.MoveTo(Table.Assembly) == 0)
			{
				return null;
			}
			AssemblyNameDefinition name = new AssemblyNameDefinition();
			name.HashAlgorithm = (AssemblyHashAlgorithm)base.ReadUInt32();
			this.PopulateVersionAndFlags(name);
			name.PublicKey = this.ReadBlob();
			this.PopulateNameAndCulture(name);
			return name;
		}

		// Token: 0x060009D3 RID: 2515 RVA: 0x0001FC82 File Offset: 0x0001DE82
		public ModuleDefinition Populate(ModuleDefinition module)
		{
			if (this.MoveTo(Table.Module) == 0)
			{
				return module;
			}
			base.Advance(2);
			module.Name = this.ReadString();
			module.Mvid = this.ReadGuid();
			return module;
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x0001FCB0 File Offset: 0x0001DEB0
		private void InitializeAssemblyReferences()
		{
			if (this.metadata.AssemblyReferences != null)
			{
				return;
			}
			int length = this.MoveTo(Table.AssemblyRef);
			AssemblyNameReference[] references = (this.metadata.AssemblyReferences = new AssemblyNameReference[length]);
			uint i = 0U;
			while ((ulong)i < (ulong)((long)length))
			{
				AssemblyNameReference reference = new AssemblyNameReference();
				reference.token = new MetadataToken(TokenType.AssemblyRef, i + 1U);
				this.PopulateVersionAndFlags(reference);
				byte[] key_or_token = this.ReadBlob();
				if (reference.HasPublicKey)
				{
					reference.PublicKey = key_or_token;
				}
				else
				{
					reference.PublicKeyToken = key_or_token;
				}
				this.PopulateNameAndCulture(reference);
				reference.Hash = this.ReadBlob();
				references[(int)i] = reference;
				i += 1U;
			}
		}

		// Token: 0x060009D5 RID: 2517 RVA: 0x0001FD5C File Offset: 0x0001DF5C
		public Collection<AssemblyNameReference> ReadAssemblyReferences()
		{
			this.InitializeAssemblyReferences();
			Collection<AssemblyNameReference> references = new Collection<AssemblyNameReference>(this.metadata.AssemblyReferences);
			if (this.module.IsWindowsMetadata())
			{
				this.module.Projections.AddVirtualReferences(references);
			}
			return references;
		}

		// Token: 0x060009D6 RID: 2518 RVA: 0x0001FDA0 File Offset: 0x0001DFA0
		public MethodDefinition ReadEntryPoint()
		{
			if (this.module.Image.EntryPointToken == 0U)
			{
				return null;
			}
			MetadataToken token = new MetadataToken(this.module.Image.EntryPointToken);
			return this.GetMethodDefinition(token.RID);
		}

		// Token: 0x060009D7 RID: 2519 RVA: 0x0001FDE8 File Offset: 0x0001DFE8
		public Collection<ModuleDefinition> ReadModules()
		{
			Collection<ModuleDefinition> modules = new Collection<ModuleDefinition>(1);
			modules.Add(this.module);
			int length = this.MoveTo(Table.File);
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				bool flag = base.ReadUInt32() != 0U;
				string name = this.ReadString();
				this.ReadBlobIndex();
				if (!flag)
				{
					ReaderParameters parameters = new ReaderParameters
					{
						ReadingMode = this.module.ReadingMode,
						SymbolReaderProvider = this.module.SymbolReaderProvider,
						AssemblyResolver = this.module.AssemblyResolver
					};
					ModuleDefinition netmodule = ModuleDefinition.ReadModule(this.GetModuleFileName(name), parameters);
					netmodule.assembly = this.module.assembly;
					modules.Add(netmodule);
				}
				i += 1U;
			}
			return modules;
		}

		// Token: 0x060009D8 RID: 2520 RVA: 0x0001FE9D File Offset: 0x0001E09D
		private string GetModuleFileName(string name)
		{
			if (this.module.FileName == null)
			{
				throw new NotSupportedException();
			}
			return Path.Combine(Path.GetDirectoryName(this.module.FileName), name);
		}

		// Token: 0x060009D9 RID: 2521 RVA: 0x0001FEC8 File Offset: 0x0001E0C8
		private void InitializeModuleReferences()
		{
			if (this.metadata.ModuleReferences != null)
			{
				return;
			}
			int length = this.MoveTo(Table.ModuleRef);
			ModuleReference[] references = (this.metadata.ModuleReferences = new ModuleReference[length]);
			uint i = 0U;
			while ((ulong)i < (ulong)((long)length))
			{
				references[(int)i] = new ModuleReference(this.ReadString())
				{
					token = new MetadataToken(TokenType.ModuleRef, i + 1U)
				};
				i += 1U;
			}
		}

		// Token: 0x060009DA RID: 2522 RVA: 0x0001FF35 File Offset: 0x0001E135
		public Collection<ModuleReference> ReadModuleReferences()
		{
			this.InitializeModuleReferences();
			return new Collection<ModuleReference>(this.metadata.ModuleReferences);
		}

		// Token: 0x060009DB RID: 2523 RVA: 0x0001FF50 File Offset: 0x0001E150
		public bool HasFileResource()
		{
			int length = this.MoveTo(Table.File);
			if (length == 0)
			{
				return false;
			}
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				if (this.ReadFileRecord(i).Col1 == FileAttributes.ContainsNoMetaData)
				{
					return true;
				}
				i += 1U;
			}
			return false;
		}

		// Token: 0x060009DC RID: 2524 RVA: 0x0001FF8C File Offset: 0x0001E18C
		public Collection<Resource> ReadResources()
		{
			int length = this.MoveTo(Table.ManifestResource);
			Collection<Resource> resources = new Collection<Resource>(length);
			int i = 1;
			while (i <= length)
			{
				uint offset = base.ReadUInt32();
				ManifestResourceAttributes flags = (ManifestResourceAttributes)base.ReadUInt32();
				string name = this.ReadString();
				MetadataToken implementation = this.ReadMetadataToken(CodedIndex.Implementation);
				Resource resource;
				if (implementation.RID == 0U)
				{
					resource = new EmbeddedResource(name, flags, offset, this);
					goto IL_C6;
				}
				if (implementation.TokenType == TokenType.AssemblyRef)
				{
					resource = new AssemblyLinkedResource(name, flags)
					{
						Assembly = (AssemblyNameReference)this.GetTypeReferenceScope(implementation)
					};
					goto IL_C6;
				}
				if (implementation.TokenType == TokenType.File)
				{
					Row<FileAttributes, string, uint> file_record = this.ReadFileRecord(implementation.RID);
					resource = new LinkedResource(name, flags)
					{
						File = file_record.Col2,
						hash = this.ReadBlob(file_record.Col3)
					};
					goto IL_C6;
				}
				IL_CE:
				i++;
				continue;
				IL_C6:
				resources.Add(resource);
				goto IL_CE;
			}
			return resources;
		}

		// Token: 0x060009DD RID: 2525 RVA: 0x00020074 File Offset: 0x0001E274
		private Row<FileAttributes, string, uint> ReadFileRecord(uint rid)
		{
			int position = this.position;
			if (!this.MoveTo(Table.File, rid))
			{
				throw new ArgumentException();
			}
			Row<FileAttributes, string, uint> result = new Row<FileAttributes, string, uint>((FileAttributes)base.ReadUInt32(), this.ReadString(), this.ReadBlobIndex());
			this.position = position;
			return result;
		}

		// Token: 0x060009DE RID: 2526 RVA: 0x000200B8 File Offset: 0x0001E2B8
		public byte[] GetManagedResource(uint offset)
		{
			return this.image.GetReaderAt<uint, byte[]>(this.image.Resources.VirtualAddress, offset, delegate(uint o, BinaryStreamReader reader)
			{
				reader.Advance((int)o);
				return reader.ReadBytes(reader.ReadInt32());
			}) ?? Empty<byte>.Array;
		}

		// Token: 0x060009DF RID: 2527 RVA: 0x00020109 File Offset: 0x0001E309
		private void PopulateVersionAndFlags(AssemblyNameReference name)
		{
			name.Version = new Version((int)base.ReadUInt16(), (int)base.ReadUInt16(), (int)base.ReadUInt16(), (int)base.ReadUInt16());
			name.Attributes = (AssemblyAttributes)base.ReadUInt32();
		}

		// Token: 0x060009E0 RID: 2528 RVA: 0x0002013A File Offset: 0x0001E33A
		private void PopulateNameAndCulture(AssemblyNameReference name)
		{
			name.Name = this.ReadString();
			name.Culture = this.ReadString();
		}

		// Token: 0x060009E1 RID: 2529 RVA: 0x00020154 File Offset: 0x0001E354
		public TypeDefinitionCollection ReadTypes()
		{
			this.InitializeTypeDefinitions();
			TypeDefinition[] mtypes = this.metadata.Types;
			int type_count = mtypes.Length - this.metadata.NestedTypes.Count;
			TypeDefinitionCollection types = new TypeDefinitionCollection(this.module, type_count);
			foreach (TypeDefinition type in mtypes)
			{
				if (!MetadataReader.IsNested(type.Attributes))
				{
					types.Add(type);
				}
			}
			if (this.image.HasTable(Table.MethodPtr) || this.image.HasTable(Table.FieldPtr))
			{
				this.CompleteTypes();
			}
			return types;
		}

		// Token: 0x060009E2 RID: 2530 RVA: 0x000201E4 File Offset: 0x0001E3E4
		private void CompleteTypes()
		{
			foreach (TypeDefinition typeDefinition in this.metadata.Types)
			{
				Mixin.Read(typeDefinition.Fields);
				Mixin.Read(typeDefinition.Methods);
			}
		}

		// Token: 0x060009E3 RID: 2531 RVA: 0x00020224 File Offset: 0x0001E424
		private void InitializeTypeDefinitions()
		{
			if (this.metadata.Types != null)
			{
				return;
			}
			this.InitializeNestedTypes();
			this.InitializeFields();
			this.InitializeMethods();
			int length = this.MoveTo(Table.TypeDef);
			TypeDefinition[] types = (this.metadata.Types = new TypeDefinition[length]);
			uint i = 0U;
			while ((ulong)i < (ulong)((long)length))
			{
				if (types[(int)i] == null)
				{
					types[(int)i] = this.ReadType(i + 1U);
				}
				i += 1U;
			}
			if (this.module.IsWindowsMetadata())
			{
				uint j = 0U;
				while ((ulong)j < (ulong)((long)length))
				{
					WindowsRuntimeProjections.Project(types[(int)j]);
					j += 1U;
				}
			}
		}

		// Token: 0x060009E4 RID: 2532 RVA: 0x000202B8 File Offset: 0x0001E4B8
		private static bool IsNested(TypeAttributes attributes)
		{
			TypeAttributes typeAttributes = attributes & TypeAttributes.VisibilityMask;
			return typeAttributes - TypeAttributes.NestedPublic <= 5U;
		}

		// Token: 0x060009E5 RID: 2533 RVA: 0x000202D4 File Offset: 0x0001E4D4
		public bool HasNestedTypes(TypeDefinition type)
		{
			this.InitializeNestedTypes();
			Collection<uint> mapping;
			return this.metadata.TryGetNestedTypeMapping(type, out mapping) && mapping.Count > 0;
		}

		// Token: 0x060009E6 RID: 2534 RVA: 0x00020304 File Offset: 0x0001E504
		public Collection<TypeDefinition> ReadNestedTypes(TypeDefinition type)
		{
			this.InitializeNestedTypes();
			Collection<uint> mapping;
			if (!this.metadata.TryGetNestedTypeMapping(type, out mapping))
			{
				return new MemberDefinitionCollection<TypeDefinition>(type);
			}
			MemberDefinitionCollection<TypeDefinition> nested_types = new MemberDefinitionCollection<TypeDefinition>(type, mapping.Count);
			for (int i = 0; i < mapping.Count; i++)
			{
				TypeDefinition nested_type = this.GetTypeDefinition(mapping[i]);
				if (nested_type != null)
				{
					nested_types.Add(nested_type);
				}
			}
			return nested_types;
		}

		// Token: 0x060009E7 RID: 2535 RVA: 0x00020368 File Offset: 0x0001E568
		private void InitializeNestedTypes()
		{
			if (this.metadata.NestedTypes != null)
			{
				return;
			}
			int length = this.MoveTo(Table.NestedClass);
			this.metadata.NestedTypes = new Dictionary<uint, Collection<uint>>(length);
			this.metadata.ReverseNestedTypes = new Dictionary<uint, uint>(length);
			if (length == 0)
			{
				return;
			}
			for (int i = 1; i <= length; i++)
			{
				uint nested = this.ReadTableIndex(Table.TypeDef);
				uint declaring = this.ReadTableIndex(Table.TypeDef);
				this.AddNestedMapping(declaring, nested);
			}
		}

		// Token: 0x060009E8 RID: 2536 RVA: 0x000203D6 File Offset: 0x0001E5D6
		private void AddNestedMapping(uint declaring, uint nested)
		{
			this.metadata.SetNestedTypeMapping(declaring, MetadataReader.AddMapping<uint, uint>(this.metadata.NestedTypes, declaring, nested));
			this.metadata.SetReverseNestedTypeMapping(nested, declaring);
		}

		// Token: 0x060009E9 RID: 2537 RVA: 0x00020404 File Offset: 0x0001E604
		private static Collection<TValue> AddMapping<TKey, TValue>(Dictionary<TKey, Collection<TValue>> cache, TKey key, TValue value)
		{
			Collection<TValue> mapped;
			if (!cache.TryGetValue(key, out mapped))
			{
				mapped = new Collection<TValue>();
			}
			mapped.Add(value);
			return mapped;
		}

		// Token: 0x060009EA RID: 2538 RVA: 0x0002042C File Offset: 0x0001E62C
		private TypeDefinition ReadType(uint rid)
		{
			if (!this.MoveTo(Table.TypeDef, rid))
			{
				return null;
			}
			TypeAttributes attributes = (TypeAttributes)base.ReadUInt32();
			string name = this.ReadString();
			TypeDefinition type = new TypeDefinition(this.ReadString(), name, attributes);
			type.token = new MetadataToken(TokenType.TypeDef, rid);
			type.scope = this.module;
			type.module = this.module;
			this.metadata.AddTypeDefinition(type);
			this.context = type;
			type.BaseType = this.GetTypeDefOrRef(this.ReadMetadataToken(CodedIndex.TypeDefOrRef));
			type.fields_range = this.ReadListRange(rid, Table.TypeDef, Table.Field);
			type.methods_range = this.ReadListRange(rid, Table.TypeDef, Table.Method);
			if (MetadataReader.IsNested(attributes))
			{
				type.DeclaringType = this.GetNestedTypeDeclaringType(type);
			}
			return type;
		}

		// Token: 0x060009EB RID: 2539 RVA: 0x000204E4 File Offset: 0x0001E6E4
		private TypeDefinition GetNestedTypeDeclaringType(TypeDefinition type)
		{
			uint declaring_rid;
			if (!this.metadata.TryGetReverseNestedTypeMapping(type, out declaring_rid))
			{
				return null;
			}
			return this.GetTypeDefinition(declaring_rid);
		}

		// Token: 0x060009EC RID: 2540 RVA: 0x0002050C File Offset: 0x0001E70C
		private Range ReadListRange(uint current_index, Table current, Table target)
		{
			Range list = default(Range);
			uint start = this.ReadTableIndex(target);
			if (start == 0U)
			{
				return list;
			}
			TableInformation current_table = this.image.TableHeap[current];
			uint next_index;
			if (current_index == current_table.Length)
			{
				next_index = this.image.TableHeap[target].Length + 1U;
			}
			else
			{
				int position = this.position;
				this.position += (int)((ulong)current_table.RowSize - (ulong)((long)this.image.GetTableIndexSize(target)));
				next_index = this.ReadTableIndex(target);
				this.position = position;
			}
			list.Start = start;
			list.Length = next_index - start;
			return list;
		}

		// Token: 0x060009ED RID: 2541 RVA: 0x000205B4 File Offset: 0x0001E7B4
		public Row<short, int> ReadTypeLayout(TypeDefinition type)
		{
			this.InitializeTypeLayouts();
			uint rid = type.token.RID;
			Row<ushort, uint> class_layout;
			if (!this.metadata.ClassLayouts.TryGetValue(rid, out class_layout))
			{
				return new Row<short, int>(-1, -1);
			}
			type.PackingSize = (short)class_layout.Col1;
			type.ClassSize = (int)class_layout.Col2;
			this.metadata.ClassLayouts.Remove(rid);
			return new Row<short, int>((short)class_layout.Col1, (int)class_layout.Col2);
		}

		// Token: 0x060009EE RID: 2542 RVA: 0x00020630 File Offset: 0x0001E830
		private void InitializeTypeLayouts()
		{
			if (this.metadata.ClassLayouts != null)
			{
				return;
			}
			int length = this.MoveTo(Table.ClassLayout);
			Dictionary<uint, Row<ushort, uint>> class_layouts = (this.metadata.ClassLayouts = new Dictionary<uint, Row<ushort, uint>>(length));
			uint i = 0U;
			while ((ulong)i < (ulong)((long)length))
			{
				ushort packing_size = base.ReadUInt16();
				uint class_size = base.ReadUInt32();
				uint parent = this.ReadTableIndex(Table.TypeDef);
				class_layouts.Add(parent, new Row<ushort, uint>(packing_size, class_size));
				i += 1U;
			}
		}

		// Token: 0x060009EF RID: 2543 RVA: 0x000206A1 File Offset: 0x0001E8A1
		public TypeReference GetTypeDefOrRef(MetadataToken token)
		{
			return (TypeReference)this.LookupToken(token);
		}

		// Token: 0x060009F0 RID: 2544 RVA: 0x000206B0 File Offset: 0x0001E8B0
		public TypeDefinition GetTypeDefinition(uint rid)
		{
			this.InitializeTypeDefinitions();
			TypeDefinition type = this.metadata.GetTypeDefinition(rid);
			if (type != null)
			{
				return type;
			}
			type = this.ReadTypeDefinition(rid);
			if (this.module.IsWindowsMetadata())
			{
				WindowsRuntimeProjections.Project(type);
			}
			return type;
		}

		// Token: 0x060009F1 RID: 2545 RVA: 0x000206F1 File Offset: 0x0001E8F1
		private TypeDefinition ReadTypeDefinition(uint rid)
		{
			if (!this.MoveTo(Table.TypeDef, rid))
			{
				return null;
			}
			return this.ReadType(rid);
		}

		// Token: 0x060009F2 RID: 2546 RVA: 0x00020706 File Offset: 0x0001E906
		private void InitializeTypeReferences()
		{
			if (this.metadata.TypeReferences != null)
			{
				return;
			}
			this.metadata.TypeReferences = new TypeReference[this.image.GetTableLength(Table.TypeRef)];
		}

		// Token: 0x060009F3 RID: 2547 RVA: 0x00020734 File Offset: 0x0001E934
		public TypeReference GetTypeReference(string scope, string full_name)
		{
			this.InitializeTypeReferences();
			int length = this.metadata.TypeReferences.Length;
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				TypeReference type = this.GetTypeReference(i);
				if (!(type.FullName != full_name))
				{
					if (string.IsNullOrEmpty(scope))
					{
						return type;
					}
					if (type.Scope.Name == scope)
					{
						return type;
					}
				}
				i += 1U;
			}
			return null;
		}

		// Token: 0x060009F4 RID: 2548 RVA: 0x0002079C File Offset: 0x0001E99C
		private TypeReference GetTypeReference(uint rid)
		{
			this.InitializeTypeReferences();
			TypeReference type = this.metadata.GetTypeReference(rid);
			if (type != null)
			{
				return type;
			}
			return this.ReadTypeReference(rid);
		}

		// Token: 0x060009F5 RID: 2549 RVA: 0x000207C8 File Offset: 0x0001E9C8
		private TypeReference ReadTypeReference(uint rid)
		{
			if (!this.MoveTo(Table.TypeRef, rid))
			{
				return null;
			}
			TypeReference declaring_type = null;
			MetadataToken scope_token = this.ReadMetadataToken(CodedIndex.ResolutionScope);
			string name = this.ReadString();
			TypeReference type = new TypeReference(this.ReadString(), name, this.module, null);
			type.token = new MetadataToken(TokenType.TypeRef, rid);
			this.metadata.AddTypeReference(type);
			IMetadataScope scope;
			if (scope_token.TokenType == TokenType.TypeRef)
			{
				if (scope_token.RID != rid)
				{
					declaring_type = this.GetTypeDefOrRef(scope_token);
					IMetadataScope metadataScope2;
					if (declaring_type == null)
					{
						IMetadataScope metadataScope = this.module;
						metadataScope2 = metadataScope;
					}
					else
					{
						metadataScope2 = declaring_type.Scope;
					}
					scope = metadataScope2;
				}
				else
				{
					scope = this.module;
				}
			}
			else
			{
				scope = this.GetTypeReferenceScope(scope_token);
			}
			type.scope = scope;
			type.DeclaringType = declaring_type;
			MetadataSystem.TryProcessPrimitiveTypeReference(type);
			if (type.Module.IsWindowsMetadata())
			{
				WindowsRuntimeProjections.Project(type);
			}
			return type;
		}

		// Token: 0x060009F6 RID: 2550 RVA: 0x000208A0 File Offset: 0x0001EAA0
		private IMetadataScope GetTypeReferenceScope(MetadataToken scope)
		{
			if (scope.TokenType == TokenType.Module)
			{
				return this.module;
			}
			TokenType tokenType = scope.TokenType;
			IMetadataScope[] scopes;
			if (tokenType != TokenType.ModuleRef)
			{
				if (tokenType != TokenType.AssemblyRef)
				{
					throw new NotSupportedException();
				}
				this.InitializeAssemblyReferences();
				IMetadataScope[] array = this.metadata.AssemblyReferences;
				scopes = array;
			}
			else
			{
				this.InitializeModuleReferences();
				IMetadataScope[] array = this.metadata.ModuleReferences;
				scopes = array;
			}
			uint index = scope.RID - 1U;
			if (index < 0U || (ulong)index >= (ulong)((long)scopes.Length))
			{
				return null;
			}
			return scopes[(int)index];
		}

		// Token: 0x060009F7 RID: 2551 RVA: 0x00020924 File Offset: 0x0001EB24
		public IEnumerable<TypeReference> GetTypeReferences()
		{
			this.InitializeTypeReferences();
			int length = this.image.GetTableLength(Table.TypeRef);
			TypeReference[] type_references = new TypeReference[length];
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				type_references[(int)(i - 1U)] = this.GetTypeReference(i);
				i += 1U;
			}
			return type_references;
		}

		// Token: 0x060009F8 RID: 2552 RVA: 0x00020968 File Offset: 0x0001EB68
		private TypeReference GetTypeSpecification(uint rid)
		{
			if (!this.MoveTo(Table.TypeSpec, rid))
			{
				return null;
			}
			TypeReference type = this.ReadSignature(this.ReadBlobIndex()).ReadTypeSignature();
			if (type.token.RID == 0U)
			{
				type.token = new MetadataToken(TokenType.TypeSpec, rid);
			}
			return type;
		}

		// Token: 0x060009F9 RID: 2553 RVA: 0x000209B3 File Offset: 0x0001EBB3
		private SignatureReader ReadSignature(uint signature)
		{
			return new SignatureReader(signature, this);
		}

		// Token: 0x060009FA RID: 2554 RVA: 0x000209BC File Offset: 0x0001EBBC
		public bool HasInterfaces(TypeDefinition type)
		{
			this.InitializeInterfaces();
			Collection<Row<uint, MetadataToken>> mapping;
			return this.metadata.TryGetInterfaceMapping(type, out mapping);
		}

		// Token: 0x060009FB RID: 2555 RVA: 0x000209E0 File Offset: 0x0001EBE0
		public InterfaceImplementationCollection ReadInterfaces(TypeDefinition type)
		{
			this.InitializeInterfaces();
			Collection<Row<uint, MetadataToken>> mapping;
			if (!this.metadata.TryGetInterfaceMapping(type, out mapping))
			{
				return new InterfaceImplementationCollection(type);
			}
			InterfaceImplementationCollection interfaces = new InterfaceImplementationCollection(type, mapping.Count);
			this.context = type;
			for (int i = 0; i < mapping.Count; i++)
			{
				interfaces.Add(new InterfaceImplementation(this.GetTypeDefOrRef(mapping[i].Col2), new MetadataToken(TokenType.InterfaceImpl, mapping[i].Col1)));
			}
			return interfaces;
		}

		// Token: 0x060009FC RID: 2556 RVA: 0x00020A64 File Offset: 0x0001EC64
		private void InitializeInterfaces()
		{
			if (this.metadata.Interfaces != null)
			{
				return;
			}
			int length = this.MoveTo(Table.InterfaceImpl);
			this.metadata.Interfaces = new Dictionary<uint, Collection<Row<uint, MetadataToken>>>(length);
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				uint type = this.ReadTableIndex(Table.TypeDef);
				MetadataToken @interface = this.ReadMetadataToken(CodedIndex.TypeDefOrRef);
				this.AddInterfaceMapping(type, new Row<uint, MetadataToken>(i, @interface));
				i += 1U;
			}
		}

		// Token: 0x060009FD RID: 2557 RVA: 0x00020AC5 File Offset: 0x0001ECC5
		private void AddInterfaceMapping(uint type, Row<uint, MetadataToken> @interface)
		{
			this.metadata.SetInterfaceMapping(type, MetadataReader.AddMapping<uint, Row<uint, MetadataToken>>(this.metadata.Interfaces, type, @interface));
		}

		// Token: 0x060009FE RID: 2558 RVA: 0x00020AE8 File Offset: 0x0001ECE8
		public Collection<FieldDefinition> ReadFields(TypeDefinition type)
		{
			Range fields_range = type.fields_range;
			if (fields_range.Length == 0U)
			{
				return new MemberDefinitionCollection<FieldDefinition>(type);
			}
			MemberDefinitionCollection<FieldDefinition> fields = new MemberDefinitionCollection<FieldDefinition>(type, (int)fields_range.Length);
			this.context = type;
			if (!this.MoveTo(Table.FieldPtr, fields_range.Start))
			{
				if (!this.MoveTo(Table.Field, fields_range.Start))
				{
					return fields;
				}
				for (uint i = 0U; i < fields_range.Length; i += 1U)
				{
					this.ReadField(fields_range.Start + i, fields);
				}
			}
			else
			{
				this.ReadPointers<FieldDefinition>(Table.FieldPtr, Table.Field, fields_range, fields, new Action<uint, Collection<FieldDefinition>>(this.ReadField));
			}
			return fields;
		}

		// Token: 0x060009FF RID: 2559 RVA: 0x00020B78 File Offset: 0x0001ED78
		private void ReadField(uint field_rid, Collection<FieldDefinition> fields)
		{
			FieldAttributes attributes = (FieldAttributes)base.ReadUInt16();
			string name = this.ReadString();
			uint signature = this.ReadBlobIndex();
			FieldDefinition field = new FieldDefinition(name, attributes, this.ReadFieldType(signature));
			field.token = new MetadataToken(TokenType.Field, field_rid);
			this.metadata.AddFieldDefinition(field);
			if (MetadataReader.IsDeleted(field))
			{
				return;
			}
			fields.Add(field);
			if (this.module.IsWindowsMetadata())
			{
				WindowsRuntimeProjections.Project(field);
			}
		}

		// Token: 0x06000A00 RID: 2560 RVA: 0x00020BE7 File Offset: 0x0001EDE7
		private void InitializeFields()
		{
			if (this.metadata.Fields != null)
			{
				return;
			}
			this.metadata.Fields = new FieldDefinition[this.image.GetTableLength(Table.Field)];
		}

		// Token: 0x06000A01 RID: 2561 RVA: 0x00020C13 File Offset: 0x0001EE13
		private TypeReference ReadFieldType(uint signature)
		{
			SignatureReader signatureReader = this.ReadSignature(signature);
			if (signatureReader.ReadByte() != 6)
			{
				throw new NotSupportedException();
			}
			return signatureReader.ReadTypeSignature();
		}

		// Token: 0x06000A02 RID: 2562 RVA: 0x00020C30 File Offset: 0x0001EE30
		public int ReadFieldRVA(FieldDefinition field)
		{
			this.InitializeFieldRVAs();
			uint rid = field.token.RID;
			uint rva;
			if (!this.metadata.FieldRVAs.TryGetValue(rid, out rva))
			{
				return 0;
			}
			int size = MetadataReader.GetFieldTypeSize(field.FieldType);
			if (size == 0 || rva == 0U)
			{
				return 0;
			}
			this.metadata.FieldRVAs.Remove(rid);
			field.InitialValue = this.GetFieldInitializeValue(size, rva);
			return (int)rva;
		}

		// Token: 0x06000A03 RID: 2563 RVA: 0x00020C9B File Offset: 0x0001EE9B
		private byte[] GetFieldInitializeValue(int size, uint rva)
		{
			return this.image.GetReaderAt<int, byte[]>(rva, size, (int s, BinaryStreamReader reader) => reader.ReadBytes(s)) ?? Empty<byte>.Array;
		}

		// Token: 0x06000A04 RID: 2564 RVA: 0x00020CD4 File Offset: 0x0001EED4
		private static int GetFieldTypeSize(TypeReference type)
		{
			int size = 0;
			switch (type.etype)
			{
			case ElementType.Boolean:
			case ElementType.I1:
			case ElementType.U1:
				return 1;
			case ElementType.Char:
			case ElementType.I2:
			case ElementType.U2:
				return 2;
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.R4:
				return 4;
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R8:
				return 8;
			case ElementType.Ptr:
			case ElementType.FnPtr:
				return IntPtr.Size;
			case ElementType.CModReqD:
			case ElementType.CModOpt:
				return MetadataReader.GetFieldTypeSize(((IModifierType)type).ElementType);
			}
			TypeDefinition field_type = type.Resolve();
			if (field_type != null && field_type.HasLayoutInfo)
			{
				size = field_type.ClassSize;
			}
			return size;
		}

		// Token: 0x06000A05 RID: 2565 RVA: 0x00020DB4 File Offset: 0x0001EFB4
		private void InitializeFieldRVAs()
		{
			if (this.metadata.FieldRVAs != null)
			{
				return;
			}
			int length = this.MoveTo(Table.FieldRVA);
			Dictionary<uint, uint> field_rvas = (this.metadata.FieldRVAs = new Dictionary<uint, uint>(length));
			for (int i = 0; i < length; i++)
			{
				uint rva = base.ReadUInt32();
				uint field = this.ReadTableIndex(Table.Field);
				field_rvas.Add(field, rva);
			}
		}

		// Token: 0x06000A06 RID: 2566 RVA: 0x00020E14 File Offset: 0x0001F014
		public int ReadFieldLayout(FieldDefinition field)
		{
			this.InitializeFieldLayouts();
			uint rid = field.token.RID;
			uint offset;
			if (!this.metadata.FieldLayouts.TryGetValue(rid, out offset))
			{
				return -1;
			}
			this.metadata.FieldLayouts.Remove(rid);
			return (int)offset;
		}

		// Token: 0x06000A07 RID: 2567 RVA: 0x00020E60 File Offset: 0x0001F060
		private void InitializeFieldLayouts()
		{
			if (this.metadata.FieldLayouts != null)
			{
				return;
			}
			int length = this.MoveTo(Table.FieldLayout);
			Dictionary<uint, uint> field_layouts = (this.metadata.FieldLayouts = new Dictionary<uint, uint>(length));
			for (int i = 0; i < length; i++)
			{
				uint offset = base.ReadUInt32();
				uint field = this.ReadTableIndex(Table.Field);
				field_layouts.Add(field, offset);
			}
		}

		// Token: 0x06000A08 RID: 2568 RVA: 0x00020EC0 File Offset: 0x0001F0C0
		public bool HasEvents(TypeDefinition type)
		{
			this.InitializeEvents();
			Range range;
			return this.metadata.TryGetEventsRange(type, out range) && range.Length > 0U;
		}

		// Token: 0x06000A09 RID: 2569 RVA: 0x00020EF0 File Offset: 0x0001F0F0
		public Collection<EventDefinition> ReadEvents(TypeDefinition type)
		{
			this.InitializeEvents();
			Range range;
			if (!this.metadata.TryGetEventsRange(type, out range))
			{
				return new MemberDefinitionCollection<EventDefinition>(type);
			}
			MemberDefinitionCollection<EventDefinition> events = new MemberDefinitionCollection<EventDefinition>(type, (int)range.Length);
			if (range.Length == 0U)
			{
				return events;
			}
			this.context = type;
			if (!this.MoveTo(Table.EventPtr, range.Start))
			{
				if (!this.MoveTo(Table.Event, range.Start))
				{
					return events;
				}
				for (uint i = 0U; i < range.Length; i += 1U)
				{
					this.ReadEvent(range.Start + i, events);
				}
			}
			else
			{
				this.ReadPointers<EventDefinition>(Table.EventPtr, Table.Event, range, events, new Action<uint, Collection<EventDefinition>>(this.ReadEvent));
			}
			return events;
		}

		// Token: 0x06000A0A RID: 2570 RVA: 0x00020F98 File Offset: 0x0001F198
		private void ReadEvent(uint event_rid, Collection<EventDefinition> events)
		{
			EventAttributes attributes = (EventAttributes)base.ReadUInt16();
			string name = this.ReadString();
			TypeReference event_type = this.GetTypeDefOrRef(this.ReadMetadataToken(CodedIndex.TypeDefOrRef));
			EventDefinition @event = new EventDefinition(name, attributes, event_type);
			@event.token = new MetadataToken(TokenType.Event, event_rid);
			if (MetadataReader.IsDeleted(@event))
			{
				return;
			}
			events.Add(@event);
		}

		// Token: 0x06000A0B RID: 2571 RVA: 0x00020FEC File Offset: 0x0001F1EC
		private void InitializeEvents()
		{
			if (this.metadata.Events != null)
			{
				return;
			}
			int length = this.MoveTo(Table.EventMap);
			this.metadata.Events = new Dictionary<uint, Range>(length);
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				uint type_rid = this.ReadTableIndex(Table.TypeDef);
				Range events_range = this.ReadListRange(i, Table.EventMap, Table.Event);
				this.metadata.AddEventsRange(type_rid, events_range);
				i += 1U;
			}
		}

		// Token: 0x06000A0C RID: 2572 RVA: 0x00021050 File Offset: 0x0001F250
		public bool HasProperties(TypeDefinition type)
		{
			this.InitializeProperties();
			Range range;
			return this.metadata.TryGetPropertiesRange(type, out range) && range.Length > 0U;
		}

		// Token: 0x06000A0D RID: 2573 RVA: 0x00021080 File Offset: 0x0001F280
		public Collection<PropertyDefinition> ReadProperties(TypeDefinition type)
		{
			this.InitializeProperties();
			Range range;
			if (!this.metadata.TryGetPropertiesRange(type, out range))
			{
				return new MemberDefinitionCollection<PropertyDefinition>(type);
			}
			MemberDefinitionCollection<PropertyDefinition> properties = new MemberDefinitionCollection<PropertyDefinition>(type, (int)range.Length);
			if (range.Length == 0U)
			{
				return properties;
			}
			this.context = type;
			if (!this.MoveTo(Table.PropertyPtr, range.Start))
			{
				if (!this.MoveTo(Table.Property, range.Start))
				{
					return properties;
				}
				for (uint i = 0U; i < range.Length; i += 1U)
				{
					this.ReadProperty(range.Start + i, properties);
				}
			}
			else
			{
				this.ReadPointers<PropertyDefinition>(Table.PropertyPtr, Table.Property, range, properties, new Action<uint, Collection<PropertyDefinition>>(this.ReadProperty));
			}
			return properties;
		}

		// Token: 0x06000A0E RID: 2574 RVA: 0x00021128 File Offset: 0x0001F328
		private void ReadProperty(uint property_rid, Collection<PropertyDefinition> properties)
		{
			PropertyAttributes attributes = (PropertyAttributes)base.ReadUInt16();
			string name = this.ReadString();
			uint signature = this.ReadBlobIndex();
			SignatureReader reader = this.ReadSignature(signature);
			byte b = reader.ReadByte();
			if ((b & 8) == 0)
			{
				throw new NotSupportedException();
			}
			bool has_this = (b & 32) > 0;
			reader.ReadCompressedUInt32();
			PropertyDefinition property = new PropertyDefinition(name, attributes, reader.ReadTypeSignature());
			property.HasThis = has_this;
			property.token = new MetadataToken(TokenType.Property, property_rid);
			if (MetadataReader.IsDeleted(property))
			{
				return;
			}
			properties.Add(property);
		}

		// Token: 0x06000A0F RID: 2575 RVA: 0x000211AC File Offset: 0x0001F3AC
		private void InitializeProperties()
		{
			if (this.metadata.Properties != null)
			{
				return;
			}
			int length = this.MoveTo(Table.PropertyMap);
			this.metadata.Properties = new Dictionary<uint, Range>(length);
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				uint type_rid = this.ReadTableIndex(Table.TypeDef);
				Range properties_range = this.ReadListRange(i, Table.PropertyMap, Table.Property);
				this.metadata.AddPropertiesRange(type_rid, properties_range);
				i += 1U;
			}
		}

		// Token: 0x06000A10 RID: 2576 RVA: 0x00021210 File Offset: 0x0001F410
		private MethodSemanticsAttributes ReadMethodSemantics(MethodDefinition method)
		{
			this.InitializeMethodSemantics();
			Row<MethodSemanticsAttributes, MetadataToken> row;
			if (!this.metadata.Semantics.TryGetValue(method.token.RID, out row))
			{
				return MethodSemanticsAttributes.None;
			}
			TypeDefinition type = method.DeclaringType;
			MethodSemanticsAttributes col = row.Col1;
			if (col <= MethodSemanticsAttributes.AddOn)
			{
				switch (col)
				{
				case MethodSemanticsAttributes.Setter:
					MetadataReader.GetProperty(type, row.Col2).set_method = method;
					goto IL_16B;
				case MethodSemanticsAttributes.Getter:
					MetadataReader.GetProperty(type, row.Col2).get_method = method;
					goto IL_16B;
				case MethodSemanticsAttributes.Setter | MethodSemanticsAttributes.Getter:
					break;
				case MethodSemanticsAttributes.Other:
				{
					TokenType tokenType = row.Col2.TokenType;
					if (tokenType == TokenType.Event)
					{
						EventDefinition @event = MetadataReader.GetEvent(type, row.Col2);
						if (@event.other_methods == null)
						{
							@event.other_methods = new Collection<MethodDefinition>();
						}
						@event.other_methods.Add(method);
						goto IL_16B;
					}
					if (tokenType != TokenType.Property)
					{
						throw new NotSupportedException();
					}
					PropertyDefinition property = MetadataReader.GetProperty(type, row.Col2);
					if (property.other_methods == null)
					{
						property.other_methods = new Collection<MethodDefinition>();
					}
					property.other_methods.Add(method);
					goto IL_16B;
				}
				default:
					if (col == MethodSemanticsAttributes.AddOn)
					{
						MetadataReader.GetEvent(type, row.Col2).add_method = method;
						goto IL_16B;
					}
					break;
				}
			}
			else
			{
				if (col == MethodSemanticsAttributes.RemoveOn)
				{
					MetadataReader.GetEvent(type, row.Col2).remove_method = method;
					goto IL_16B;
				}
				if (col == MethodSemanticsAttributes.Fire)
				{
					MetadataReader.GetEvent(type, row.Col2).invoke_method = method;
					goto IL_16B;
				}
			}
			throw new NotSupportedException();
			IL_16B:
			this.metadata.Semantics.Remove(method.token.RID);
			return row.Col1;
		}

		// Token: 0x06000A11 RID: 2577 RVA: 0x000213AA File Offset: 0x0001F5AA
		private static EventDefinition GetEvent(TypeDefinition type, MetadataToken token)
		{
			if (token.TokenType != TokenType.Event)
			{
				throw new ArgumentException();
			}
			return MetadataReader.GetMember<EventDefinition>(type.Events, token);
		}

		// Token: 0x06000A12 RID: 2578 RVA: 0x000213CC File Offset: 0x0001F5CC
		private static PropertyDefinition GetProperty(TypeDefinition type, MetadataToken token)
		{
			if (token.TokenType != TokenType.Property)
			{
				throw new ArgumentException();
			}
			return MetadataReader.GetMember<PropertyDefinition>(type.Properties, token);
		}

		// Token: 0x06000A13 RID: 2579 RVA: 0x000213F0 File Offset: 0x0001F5F0
		private static TMember GetMember<TMember>(Collection<TMember> members, MetadataToken token) where TMember : IMemberDefinition
		{
			for (int i = 0; i < members.Count; i++)
			{
				TMember member = members[i];
				if (member.MetadataToken == token)
				{
					return member;
				}
			}
			throw new ArgumentException();
		}

		// Token: 0x06000A14 RID: 2580 RVA: 0x00021434 File Offset: 0x0001F634
		private void InitializeMethodSemantics()
		{
			if (this.metadata.Semantics != null)
			{
				return;
			}
			int length = this.MoveTo(Table.MethodSemantics);
			Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>> semantics = (this.metadata.Semantics = new Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>>(0));
			uint i = 0U;
			while ((ulong)i < (ulong)((long)length))
			{
				MethodSemanticsAttributes attributes = (MethodSemanticsAttributes)base.ReadUInt16();
				uint method_rid = this.ReadTableIndex(Table.Method);
				MetadataToken association = this.ReadMetadataToken(CodedIndex.HasSemantics);
				semantics[method_rid] = new Row<MethodSemanticsAttributes, MetadataToken>(attributes, association);
				i += 1U;
			}
		}

		// Token: 0x06000A15 RID: 2581 RVA: 0x000214A6 File Offset: 0x0001F6A6
		public void ReadMethods(PropertyDefinition property)
		{
			this.ReadAllSemantics(property.DeclaringType);
		}

		// Token: 0x06000A16 RID: 2582 RVA: 0x000214B4 File Offset: 0x0001F6B4
		public void ReadMethods(EventDefinition @event)
		{
			this.ReadAllSemantics(@event.DeclaringType);
		}

		// Token: 0x06000A17 RID: 2583 RVA: 0x000214C2 File Offset: 0x0001F6C2
		public void ReadAllSemantics(MethodDefinition method)
		{
			this.ReadAllSemantics(method.DeclaringType);
		}

		// Token: 0x06000A18 RID: 2584 RVA: 0x000214D0 File Offset: 0x0001F6D0
		private void ReadAllSemantics(TypeDefinition type)
		{
			Collection<MethodDefinition> methods = type.Methods;
			for (int i = 0; i < methods.Count; i++)
			{
				MethodDefinition method = methods[i];
				if (!method.sem_attrs_ready)
				{
					method.sem_attrs = this.ReadMethodSemantics(method);
					method.sem_attrs_ready = true;
				}
			}
		}

		// Token: 0x06000A19 RID: 2585 RVA: 0x00021520 File Offset: 0x0001F720
		public Collection<MethodDefinition> ReadMethods(TypeDefinition type)
		{
			Range methods_range = type.methods_range;
			if (methods_range.Length == 0U)
			{
				return new MemberDefinitionCollection<MethodDefinition>(type);
			}
			MemberDefinitionCollection<MethodDefinition> methods = new MemberDefinitionCollection<MethodDefinition>(type, (int)methods_range.Length);
			if (!this.MoveTo(Table.MethodPtr, methods_range.Start))
			{
				if (!this.MoveTo(Table.Method, methods_range.Start))
				{
					return methods;
				}
				for (uint i = 0U; i < methods_range.Length; i += 1U)
				{
					this.ReadMethod(methods_range.Start + i, methods);
				}
			}
			else
			{
				this.ReadPointers<MethodDefinition>(Table.MethodPtr, Table.Method, methods_range, methods, new Action<uint, Collection<MethodDefinition>>(this.ReadMethod));
			}
			return methods;
		}

		// Token: 0x06000A1A RID: 2586 RVA: 0x000215AC File Offset: 0x0001F7AC
		private void ReadPointers<TMember>(Table ptr, Table table, Range range, Collection<TMember> members, Action<uint, Collection<TMember>> reader) where TMember : IMemberDefinition
		{
			for (uint i = 0U; i < range.Length; i += 1U)
			{
				this.MoveTo(ptr, range.Start + i);
				uint rid = this.ReadTableIndex(table);
				this.MoveTo(table, rid);
				reader(rid, members);
			}
		}

		// Token: 0x06000A1B RID: 2587 RVA: 0x000215F5 File Offset: 0x0001F7F5
		private static bool IsDeleted(IMemberDefinition member)
		{
			return member.IsSpecialName && member.Name == "_Deleted";
		}

		// Token: 0x06000A1C RID: 2588 RVA: 0x00021611 File Offset: 0x0001F811
		private void InitializeMethods()
		{
			if (this.metadata.Methods != null)
			{
				return;
			}
			this.metadata.Methods = new MethodDefinition[this.image.GetTableLength(Table.Method)];
		}

		// Token: 0x06000A1D RID: 2589 RVA: 0x00021640 File Offset: 0x0001F840
		private void ReadMethod(uint method_rid, Collection<MethodDefinition> methods)
		{
			MethodDefinition method = new MethodDefinition();
			method.rva = base.ReadUInt32();
			method.ImplAttributes = (MethodImplAttributes)base.ReadUInt16();
			method.Attributes = (MethodAttributes)base.ReadUInt16();
			method.Name = this.ReadString();
			method.token = new MetadataToken(TokenType.Method, method_rid);
			if (MetadataReader.IsDeleted(method))
			{
				return;
			}
			methods.Add(method);
			uint signature = this.ReadBlobIndex();
			Range param_range = this.ReadListRange(method_rid, Table.Method, Table.Param);
			this.context = method;
			this.ReadMethodSignature(signature, method);
			this.metadata.AddMethodDefinition(method);
			if (param_range.Length != 0U)
			{
				int position = this.position;
				this.ReadParameters(method, param_range);
				this.position = position;
			}
			if (this.module.IsWindowsMetadata())
			{
				WindowsRuntimeProjections.Project(method);
			}
		}

		// Token: 0x06000A1E RID: 2590 RVA: 0x00021704 File Offset: 0x0001F904
		private void ReadParameters(MethodDefinition method, Range param_range)
		{
			if (this.MoveTo(Table.ParamPtr, param_range.Start))
			{
				this.ReadParameterPointers(method, param_range);
				return;
			}
			if (!this.MoveTo(Table.Param, param_range.Start))
			{
				return;
			}
			for (uint i = 0U; i < param_range.Length; i += 1U)
			{
				this.ReadParameter(param_range.Start + i, method);
			}
		}

		// Token: 0x06000A1F RID: 2591 RVA: 0x0002175C File Offset: 0x0001F95C
		private void ReadParameterPointers(MethodDefinition method, Range range)
		{
			for (uint i = 0U; i < range.Length; i += 1U)
			{
				this.MoveTo(Table.ParamPtr, range.Start + i);
				uint rid = this.ReadTableIndex(Table.Param);
				this.MoveTo(Table.Param, rid);
				this.ReadParameter(rid, method);
			}
		}

		// Token: 0x06000A20 RID: 2592 RVA: 0x000217A4 File Offset: 0x0001F9A4
		private void ReadParameter(uint param_rid, MethodDefinition method)
		{
			ParameterAttributes attributes = (ParameterAttributes)base.ReadUInt16();
			ushort sequence = base.ReadUInt16();
			string name = this.ReadString();
			ParameterDefinition parameterDefinition = ((sequence == 0) ? method.MethodReturnType.Parameter : method.Parameters[(int)(sequence - 1)]);
			parameterDefinition.token = new MetadataToken(TokenType.Param, param_rid);
			parameterDefinition.Name = name;
			parameterDefinition.Attributes = attributes;
		}

		// Token: 0x06000A21 RID: 2593 RVA: 0x00021802 File Offset: 0x0001FA02
		private void ReadMethodSignature(uint signature, IMethodSignature method)
		{
			this.ReadSignature(signature).ReadMethodSignature(method);
		}

		// Token: 0x06000A22 RID: 2594 RVA: 0x00021814 File Offset: 0x0001FA14
		public PInvokeInfo ReadPInvokeInfo(MethodDefinition method)
		{
			this.InitializePInvokes();
			uint rid = method.token.RID;
			Row<PInvokeAttributes, uint, uint> row;
			if (!this.metadata.PInvokes.TryGetValue(rid, out row))
			{
				return null;
			}
			this.metadata.PInvokes.Remove(rid);
			return new PInvokeInfo(row.Col1, this.image.StringHeap.Read(row.Col2), this.module.ModuleReferences[(int)(row.Col3 - 1U)]);
		}

		// Token: 0x06000A23 RID: 2595 RVA: 0x00021898 File Offset: 0x0001FA98
		private void InitializePInvokes()
		{
			if (this.metadata.PInvokes != null)
			{
				return;
			}
			int length = this.MoveTo(Table.ImplMap);
			Dictionary<uint, Row<PInvokeAttributes, uint, uint>> pinvokes = (this.metadata.PInvokes = new Dictionary<uint, Row<PInvokeAttributes, uint, uint>>(length));
			for (int i = 1; i <= length; i++)
			{
				PInvokeAttributes attributes = (PInvokeAttributes)base.ReadUInt16();
				MetadataToken method = this.ReadMetadataToken(CodedIndex.MemberForwarded);
				uint name = this.ReadStringIndex();
				uint scope = this.ReadTableIndex(Table.File);
				if (method.TokenType == TokenType.Method)
				{
					pinvokes.Add(method.RID, new Row<PInvokeAttributes, uint, uint>(attributes, name, scope));
				}
			}
		}

		// Token: 0x06000A24 RID: 2596 RVA: 0x00021928 File Offset: 0x0001FB28
		public bool HasGenericParameters(IGenericParameterProvider provider)
		{
			this.InitializeGenericParameters();
			Range[] ranges;
			return this.metadata.TryGetGenericParameterRanges(provider, out ranges) && MetadataReader.RangesSize(ranges) > 0;
		}

		// Token: 0x06000A25 RID: 2597 RVA: 0x00021958 File Offset: 0x0001FB58
		public Collection<GenericParameter> ReadGenericParameters(IGenericParameterProvider provider)
		{
			this.InitializeGenericParameters();
			Range[] ranges;
			if (!this.metadata.TryGetGenericParameterRanges(provider, out ranges))
			{
				return new GenericParameterCollection(provider);
			}
			GenericParameterCollection generic_parameters = new GenericParameterCollection(provider, MetadataReader.RangesSize(ranges));
			for (int i = 0; i < ranges.Length; i++)
			{
				this.ReadGenericParametersRange(ranges[i], provider, generic_parameters);
			}
			return generic_parameters;
		}

		// Token: 0x06000A26 RID: 2598 RVA: 0x000219B0 File Offset: 0x0001FBB0
		private void ReadGenericParametersRange(Range range, IGenericParameterProvider provider, GenericParameterCollection generic_parameters)
		{
			if (!this.MoveTo(Table.GenericParam, range.Start))
			{
				return;
			}
			for (uint i = 0U; i < range.Length; i += 1U)
			{
				base.ReadUInt16();
				GenericParameterAttributes flags = (GenericParameterAttributes)base.ReadUInt16();
				this.ReadMetadataToken(CodedIndex.TypeOrMethodDef);
				generic_parameters.Add(new GenericParameter(this.ReadString(), provider)
				{
					token = new MetadataToken(TokenType.GenericParam, range.Start + i),
					Attributes = flags
				});
			}
		}

		// Token: 0x06000A27 RID: 2599 RVA: 0x00021A29 File Offset: 0x0001FC29
		private void InitializeGenericParameters()
		{
			if (this.metadata.GenericParameters != null)
			{
				return;
			}
			this.metadata.GenericParameters = this.InitializeRanges(Table.GenericParam, delegate
			{
				base.Advance(4);
				MetadataToken result = this.ReadMetadataToken(CodedIndex.TypeOrMethodDef);
				this.ReadStringIndex();
				return result;
			});
		}

		// Token: 0x06000A28 RID: 2600 RVA: 0x00021A58 File Offset: 0x0001FC58
		private Dictionary<MetadataToken, Range[]> InitializeRanges(Table table, Func<MetadataToken> get_next)
		{
			int length = this.MoveTo(table);
			Dictionary<MetadataToken, Range[]> ranges = new Dictionary<MetadataToken, Range[]>(length);
			if (length == 0)
			{
				return ranges;
			}
			MetadataToken owner = MetadataToken.Zero;
			Range range = new Range(1U, 0U);
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				MetadataToken next = get_next();
				if (i == 1U)
				{
					owner = next;
					range.Length += 1U;
				}
				else if (next != owner)
				{
					MetadataReader.AddRange(ranges, owner, range);
					range = new Range(i, 1U);
					owner = next;
				}
				else
				{
					range.Length += 1U;
				}
				i += 1U;
			}
			MetadataReader.AddRange(ranges, owner, range);
			return ranges;
		}

		// Token: 0x06000A29 RID: 2601 RVA: 0x00021AF0 File Offset: 0x0001FCF0
		private static void AddRange(Dictionary<MetadataToken, Range[]> ranges, MetadataToken owner, Range range)
		{
			if (owner.RID == 0U)
			{
				return;
			}
			Range[] slots;
			if (!ranges.TryGetValue(owner, out slots))
			{
				ranges.Add(owner, new Range[] { range });
				return;
			}
			ranges[owner] = slots.Add(range);
		}

		// Token: 0x06000A2A RID: 2602 RVA: 0x00021B38 File Offset: 0x0001FD38
		public bool HasGenericConstraints(GenericParameter generic_parameter)
		{
			this.InitializeGenericConstraints();
			Collection<Row<uint, MetadataToken>> mapping;
			return this.metadata.TryGetGenericConstraintMapping(generic_parameter, out mapping) && mapping.Count > 0;
		}

		// Token: 0x06000A2B RID: 2603 RVA: 0x00021B68 File Offset: 0x0001FD68
		public GenericParameterConstraintCollection ReadGenericConstraints(GenericParameter generic_parameter)
		{
			this.InitializeGenericConstraints();
			Collection<Row<uint, MetadataToken>> mapping;
			if (!this.metadata.TryGetGenericConstraintMapping(generic_parameter, out mapping))
			{
				return new GenericParameterConstraintCollection(generic_parameter);
			}
			GenericParameterConstraintCollection constraints = new GenericParameterConstraintCollection(generic_parameter, mapping.Count);
			this.context = (IGenericContext)generic_parameter.Owner;
			for (int i = 0; i < mapping.Count; i++)
			{
				constraints.Add(new GenericParameterConstraint(this.GetTypeDefOrRef(mapping[i].Col2), new MetadataToken(TokenType.GenericParamConstraint, mapping[i].Col1)));
			}
			return constraints;
		}

		// Token: 0x06000A2C RID: 2604 RVA: 0x00021BF8 File Offset: 0x0001FDF8
		private void InitializeGenericConstraints()
		{
			if (this.metadata.GenericConstraints != null)
			{
				return;
			}
			int length = this.MoveTo(Table.GenericParamConstraint);
			this.metadata.GenericConstraints = new Dictionary<uint, Collection<Row<uint, MetadataToken>>>(length);
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				this.AddGenericConstraintMapping(this.ReadTableIndex(Table.GenericParam), new Row<uint, MetadataToken>(i, this.ReadMetadataToken(CodedIndex.TypeDefOrRef)));
				i += 1U;
			}
		}

		// Token: 0x06000A2D RID: 2605 RVA: 0x00021C56 File Offset: 0x0001FE56
		private void AddGenericConstraintMapping(uint generic_parameter, Row<uint, MetadataToken> constraint)
		{
			this.metadata.SetGenericConstraintMapping(generic_parameter, MetadataReader.AddMapping<uint, Row<uint, MetadataToken>>(this.metadata.GenericConstraints, generic_parameter, constraint));
		}

		// Token: 0x06000A2E RID: 2606 RVA: 0x00021C78 File Offset: 0x0001FE78
		public bool HasOverrides(MethodDefinition method)
		{
			this.InitializeOverrides();
			Collection<MetadataToken> mapping;
			return this.metadata.TryGetOverrideMapping(method, out mapping) && mapping.Count > 0;
		}

		// Token: 0x06000A2F RID: 2607 RVA: 0x00021CA8 File Offset: 0x0001FEA8
		public Collection<MethodReference> ReadOverrides(MethodDefinition method)
		{
			this.InitializeOverrides();
			Collection<MetadataToken> mapping;
			if (!this.metadata.TryGetOverrideMapping(method, out mapping))
			{
				return new Collection<MethodReference>();
			}
			Collection<MethodReference> overrides = new Collection<MethodReference>(mapping.Count);
			this.context = method;
			for (int i = 0; i < mapping.Count; i++)
			{
				overrides.Add((MethodReference)this.LookupToken(mapping[i]));
			}
			return overrides;
		}

		// Token: 0x06000A30 RID: 2608 RVA: 0x00021D10 File Offset: 0x0001FF10
		private void InitializeOverrides()
		{
			if (this.metadata.Overrides != null)
			{
				return;
			}
			int length = this.MoveTo(Table.MethodImpl);
			this.metadata.Overrides = new Dictionary<uint, Collection<MetadataToken>>(length);
			for (int i = 1; i <= length; i++)
			{
				this.ReadTableIndex(Table.TypeDef);
				MetadataToken method = this.ReadMetadataToken(CodedIndex.MethodDefOrRef);
				if (method.TokenType != TokenType.Method)
				{
					throw new NotSupportedException();
				}
				MetadataToken @override = this.ReadMetadataToken(CodedIndex.MethodDefOrRef);
				this.AddOverrideMapping(method.RID, @override);
			}
		}

		// Token: 0x06000A31 RID: 2609 RVA: 0x00021D8B File Offset: 0x0001FF8B
		private void AddOverrideMapping(uint method_rid, MetadataToken @override)
		{
			this.metadata.SetOverrideMapping(method_rid, MetadataReader.AddMapping<uint, MetadataToken>(this.metadata.Overrides, method_rid, @override));
		}

		// Token: 0x06000A32 RID: 2610 RVA: 0x00021DAB File Offset: 0x0001FFAB
		public MethodBody ReadMethodBody(MethodDefinition method)
		{
			return this.code.ReadMethodBody(method);
		}

		// Token: 0x06000A33 RID: 2611 RVA: 0x00021DB9 File Offset: 0x0001FFB9
		public int ReadCodeSize(MethodDefinition method)
		{
			return this.code.ReadCodeSize(method);
		}

		// Token: 0x06000A34 RID: 2612 RVA: 0x00021DC8 File Offset: 0x0001FFC8
		public CallSite ReadCallSite(MetadataToken token)
		{
			if (!this.MoveTo(Table.StandAloneSig, token.RID))
			{
				return null;
			}
			uint signature = this.ReadBlobIndex();
			CallSite call_site = new CallSite();
			this.ReadMethodSignature(signature, call_site);
			call_site.MetadataToken = token;
			return call_site;
		}

		// Token: 0x06000A35 RID: 2613 RVA: 0x00021E08 File Offset: 0x00020008
		public VariableDefinitionCollection ReadVariables(MetadataToken local_var_token, MethodDefinition method = null)
		{
			if (!this.MoveTo(Table.StandAloneSig, local_var_token.RID))
			{
				return null;
			}
			SignatureReader reader = this.ReadSignature(this.ReadBlobIndex());
			if (reader.ReadByte() != 7)
			{
				throw new NotSupportedException();
			}
			uint count = reader.ReadCompressedUInt32();
			if (count == 0U)
			{
				return null;
			}
			VariableDefinitionCollection variables = new VariableDefinitionCollection(method, (int)count);
			int i = 0;
			while ((long)i < (long)((ulong)count))
			{
				variables.Add(new VariableDefinition(reader.ReadTypeSignature()));
				i++;
			}
			return variables;
		}

		// Token: 0x06000A36 RID: 2614 RVA: 0x00021E78 File Offset: 0x00020078
		public IMetadataTokenProvider LookupToken(MetadataToken token)
		{
			uint rid = token.RID;
			if (rid == 0U)
			{
				return null;
			}
			if (this.metadata_reader != null)
			{
				return this.metadata_reader.LookupToken(token);
			}
			int position = this.position;
			IGenericContext context = this.context;
			TokenType tokenType = token.TokenType;
			IMetadataTokenProvider element;
			if (tokenType <= TokenType.Field)
			{
				if (tokenType == TokenType.TypeRef)
				{
					element = this.GetTypeReference(rid);
					goto IL_D8;
				}
				if (tokenType == TokenType.TypeDef)
				{
					element = this.GetTypeDefinition(rid);
					goto IL_D8;
				}
				if (tokenType == TokenType.Field)
				{
					element = this.GetFieldDefinition(rid);
					goto IL_D8;
				}
			}
			else if (tokenType <= TokenType.MemberRef)
			{
				if (tokenType == TokenType.Method)
				{
					element = this.GetMethodDefinition(rid);
					goto IL_D8;
				}
				if (tokenType == TokenType.MemberRef)
				{
					element = this.GetMemberReference(rid);
					goto IL_D8;
				}
			}
			else
			{
				if (tokenType == TokenType.TypeSpec)
				{
					element = this.GetTypeSpecification(rid);
					goto IL_D8;
				}
				if (tokenType == TokenType.MethodSpec)
				{
					element = this.GetMethodSpecification(rid);
					goto IL_D8;
				}
			}
			return null;
			IL_D8:
			this.position = position;
			this.context = context;
			return element;
		}

		// Token: 0x06000A37 RID: 2615 RVA: 0x00021F6C File Offset: 0x0002016C
		public FieldDefinition GetFieldDefinition(uint rid)
		{
			this.InitializeTypeDefinitions();
			FieldDefinition field = this.metadata.GetFieldDefinition(rid);
			if (field != null)
			{
				return field;
			}
			return this.LookupField(rid);
		}

		// Token: 0x06000A38 RID: 2616 RVA: 0x00021F98 File Offset: 0x00020198
		private FieldDefinition LookupField(uint rid)
		{
			TypeDefinition type = this.metadata.GetFieldDeclaringType(rid);
			if (type == null)
			{
				return null;
			}
			Mixin.Read(type.Fields);
			return this.metadata.GetFieldDefinition(rid);
		}

		// Token: 0x06000A39 RID: 2617 RVA: 0x00021FD0 File Offset: 0x000201D0
		public MethodDefinition GetMethodDefinition(uint rid)
		{
			this.InitializeTypeDefinitions();
			MethodDefinition method = this.metadata.GetMethodDefinition(rid);
			if (method != null)
			{
				return method;
			}
			return this.LookupMethod(rid);
		}

		// Token: 0x06000A3A RID: 2618 RVA: 0x00021FFC File Offset: 0x000201FC
		private MethodDefinition LookupMethod(uint rid)
		{
			TypeDefinition type = this.metadata.GetMethodDeclaringType(rid);
			if (type == null)
			{
				return null;
			}
			Mixin.Read(type.Methods);
			return this.metadata.GetMethodDefinition(rid);
		}

		// Token: 0x06000A3B RID: 2619 RVA: 0x00022034 File Offset: 0x00020234
		private MethodSpecification GetMethodSpecification(uint rid)
		{
			if (!this.MoveTo(Table.MethodSpec, rid))
			{
				return null;
			}
			MethodReference element_method = (MethodReference)this.LookupToken(this.ReadMetadataToken(CodedIndex.MethodDefOrRef));
			uint signature = this.ReadBlobIndex();
			MethodSpecification methodSpecification = this.ReadMethodSpecSignature(signature, element_method);
			methodSpecification.token = new MetadataToken(TokenType.MethodSpec, rid);
			return methodSpecification;
		}

		// Token: 0x06000A3C RID: 2620 RVA: 0x00022084 File Offset: 0x00020284
		private MethodSpecification ReadMethodSpecSignature(uint signature, MethodReference method)
		{
			SignatureReader signatureReader = this.ReadSignature(signature);
			if (signatureReader.ReadByte() != 10)
			{
				throw new NotSupportedException();
			}
			uint arity = signatureReader.ReadCompressedUInt32();
			GenericInstanceMethod instance = new GenericInstanceMethod(method, (int)arity);
			signatureReader.ReadGenericInstanceSignature(method, instance, arity);
			return instance;
		}

		// Token: 0x06000A3D RID: 2621 RVA: 0x000220C0 File Offset: 0x000202C0
		private MemberReference GetMemberReference(uint rid)
		{
			this.InitializeMemberReferences();
			MemberReference member = this.metadata.GetMemberReference(rid);
			if (member != null)
			{
				return member;
			}
			member = this.ReadMemberReference(rid);
			if (member != null && !member.ContainsGenericParameter)
			{
				this.metadata.AddMemberReference(member);
			}
			return member;
		}

		// Token: 0x06000A3E RID: 2622 RVA: 0x00022108 File Offset: 0x00020308
		private MemberReference ReadMemberReference(uint rid)
		{
			if (!this.MoveTo(Table.MemberRef, rid))
			{
				return null;
			}
			MetadataToken token = this.ReadMetadataToken(CodedIndex.MemberRefParent);
			string name = this.ReadString();
			uint signature = this.ReadBlobIndex();
			TokenType tokenType = token.TokenType;
			MemberReference member;
			if (tokenType <= TokenType.TypeDef)
			{
				if (tokenType != TokenType.TypeRef && tokenType != TokenType.TypeDef)
				{
					goto IL_73;
				}
			}
			else
			{
				if (tokenType == TokenType.Method)
				{
					member = this.ReadMethodMemberReference(token, name, signature);
					goto IL_79;
				}
				if (tokenType != TokenType.TypeSpec)
				{
					goto IL_73;
				}
			}
			member = this.ReadTypeMemberReference(token, name, signature);
			goto IL_79;
			IL_73:
			throw new NotSupportedException();
			IL_79:
			member.token = new MetadataToken(TokenType.MemberRef, rid);
			return member;
		}

		// Token: 0x06000A3F RID: 2623 RVA: 0x000221A0 File Offset: 0x000203A0
		private MemberReference ReadTypeMemberReference(MetadataToken type, string name, uint signature)
		{
			TypeReference declaring_type = this.GetTypeDefOrRef(type);
			if (!declaring_type.IsArray)
			{
				this.context = declaring_type;
			}
			MemberReference memberReference = this.ReadMemberReferenceSignature(signature, declaring_type);
			memberReference.Name = name;
			return memberReference;
		}

		// Token: 0x06000A40 RID: 2624 RVA: 0x000221D4 File Offset: 0x000203D4
		private MemberReference ReadMemberReferenceSignature(uint signature, TypeReference declaring_type)
		{
			SignatureReader reader = this.ReadSignature(signature);
			if (reader.buffer[reader.position] == 6)
			{
				reader.position++;
				return new FieldReference
				{
					DeclaringType = declaring_type,
					FieldType = reader.ReadTypeSignature()
				};
			}
			MethodReference method = new MethodReference();
			method.DeclaringType = declaring_type;
			reader.ReadMethodSignature(method);
			return method;
		}

		// Token: 0x06000A41 RID: 2625 RVA: 0x00022238 File Offset: 0x00020438
		private MemberReference ReadMethodMemberReference(MetadataToken token, string name, uint signature)
		{
			MethodDefinition method = this.GetMethodDefinition(token.RID);
			this.context = method;
			MemberReference memberReference = this.ReadMemberReferenceSignature(signature, method.DeclaringType);
			memberReference.Name = name;
			return memberReference;
		}

		// Token: 0x06000A42 RID: 2626 RVA: 0x0002226E File Offset: 0x0002046E
		private void InitializeMemberReferences()
		{
			if (this.metadata.MemberReferences != null)
			{
				return;
			}
			this.metadata.MemberReferences = new MemberReference[this.image.GetTableLength(Table.MemberRef)];
		}

		// Token: 0x06000A43 RID: 2627 RVA: 0x0002229C File Offset: 0x0002049C
		public IEnumerable<MemberReference> GetMemberReferences()
		{
			this.InitializeMemberReferences();
			int length = this.image.GetTableLength(Table.MemberRef);
			TypeSystem type_system = this.module.TypeSystem;
			MethodDefinition context = new MethodDefinition(string.Empty, MethodAttributes.Static, type_system.Void);
			context.DeclaringType = new TypeDefinition(string.Empty, string.Empty, TypeAttributes.Public);
			MemberReference[] member_references = new MemberReference[length];
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				this.context = context;
				member_references[(int)(i - 1U)] = this.GetMemberReference(i);
				i += 1U;
			}
			return member_references;
		}

		// Token: 0x06000A44 RID: 2628 RVA: 0x00022324 File Offset: 0x00020524
		private void InitializeConstants()
		{
			if (this.metadata.Constants != null)
			{
				return;
			}
			int length = this.MoveTo(Table.Constant);
			Dictionary<MetadataToken, Row<ElementType, uint>> constants = (this.metadata.Constants = new Dictionary<MetadataToken, Row<ElementType, uint>>(length));
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				ElementType type = (ElementType)base.ReadUInt16();
				MetadataToken owner = this.ReadMetadataToken(CodedIndex.HasConstant);
				uint signature = this.ReadBlobIndex();
				constants.Add(owner, new Row<ElementType, uint>(type, signature));
				i += 1U;
			}
		}

		// Token: 0x06000A45 RID: 2629 RVA: 0x00022396 File Offset: 0x00020596
		public TypeReference ReadConstantSignature(MetadataToken token)
		{
			if (token.TokenType != TokenType.Signature)
			{
				throw new NotSupportedException();
			}
			if (token.RID == 0U)
			{
				return null;
			}
			if (!this.MoveTo(Table.StandAloneSig, token.RID))
			{
				return null;
			}
			return this.ReadFieldType(this.ReadBlobIndex());
		}

		// Token: 0x06000A46 RID: 2630 RVA: 0x000223D8 File Offset: 0x000205D8
		public object ReadConstant(IConstantProvider owner)
		{
			this.InitializeConstants();
			Row<ElementType, uint> row;
			if (!this.metadata.Constants.TryGetValue(owner.MetadataToken, out row))
			{
				return Mixin.NoValue;
			}
			this.metadata.Constants.Remove(owner.MetadataToken);
			return this.ReadConstantValue(row.Col1, row.Col2);
		}

		// Token: 0x06000A47 RID: 2631 RVA: 0x00022434 File Offset: 0x00020634
		private object ReadConstantValue(ElementType etype, uint signature)
		{
			if (etype == ElementType.String)
			{
				return this.ReadConstantString(signature);
			}
			if (etype == ElementType.Class || etype == ElementType.Object)
			{
				return null;
			}
			return this.ReadConstantPrimitive(etype, signature);
		}

		// Token: 0x06000A48 RID: 2632 RVA: 0x00022458 File Offset: 0x00020658
		private string ReadConstantString(uint signature)
		{
			byte[] blob;
			int index;
			int count;
			this.GetBlobView(signature, out blob, out index, out count);
			if (count == 0)
			{
				return string.Empty;
			}
			if ((count & 1) == 1)
			{
				count--;
			}
			return Encoding.Unicode.GetString(blob, index, count);
		}

		// Token: 0x06000A49 RID: 2633 RVA: 0x00022492 File Offset: 0x00020692
		private object ReadConstantPrimitive(ElementType type, uint signature)
		{
			return this.ReadSignature(signature).ReadConstantSignature(type);
		}

		// Token: 0x06000A4A RID: 2634 RVA: 0x000224A1 File Offset: 0x000206A1
		internal void InitializeCustomAttributes()
		{
			if (this.metadata.CustomAttributes != null)
			{
				return;
			}
			this.metadata.CustomAttributes = this.InitializeRanges(Table.CustomAttribute, delegate
			{
				MetadataToken result = this.ReadMetadataToken(CodedIndex.HasCustomAttribute);
				this.ReadMetadataToken(CodedIndex.CustomAttributeType);
				this.ReadBlobIndex();
				return result;
			});
		}

		// Token: 0x06000A4B RID: 2635 RVA: 0x000224D0 File Offset: 0x000206D0
		public bool HasCustomAttributes(ICustomAttributeProvider owner)
		{
			this.InitializeCustomAttributes();
			Range[] ranges;
			return this.metadata.TryGetCustomAttributeRanges(owner, out ranges) && MetadataReader.RangesSize(ranges) > 0;
		}

		// Token: 0x06000A4C RID: 2636 RVA: 0x00022500 File Offset: 0x00020700
		public Collection<CustomAttribute> ReadCustomAttributes(ICustomAttributeProvider owner)
		{
			this.InitializeCustomAttributes();
			Range[] ranges;
			if (!this.metadata.TryGetCustomAttributeRanges(owner, out ranges))
			{
				return new Collection<CustomAttribute>();
			}
			Collection<CustomAttribute> custom_attributes = new Collection<CustomAttribute>(MetadataReader.RangesSize(ranges));
			for (int i = 0; i < ranges.Length; i++)
			{
				this.ReadCustomAttributeRange(ranges[i], custom_attributes);
			}
			if (this.module.IsWindowsMetadata())
			{
				foreach (CustomAttribute custom_attribute in custom_attributes)
				{
					WindowsRuntimeProjections.Project(owner, custom_attributes, custom_attribute);
				}
			}
			return custom_attributes;
		}

		// Token: 0x06000A4D RID: 2637 RVA: 0x000225A4 File Offset: 0x000207A4
		private void ReadCustomAttributeRange(Range range, Collection<CustomAttribute> custom_attributes)
		{
			if (!this.MoveTo(Table.CustomAttribute, range.Start))
			{
				return;
			}
			int i = 0;
			while ((long)i < (long)((ulong)range.Length))
			{
				this.ReadMetadataToken(CodedIndex.HasCustomAttribute);
				MethodReference constructor = (MethodReference)this.LookupToken(this.ReadMetadataToken(CodedIndex.CustomAttributeType));
				uint signature = this.ReadBlobIndex();
				custom_attributes.Add(new CustomAttribute(signature, constructor));
				i++;
			}
		}

		// Token: 0x06000A4E RID: 2638 RVA: 0x00022608 File Offset: 0x00020808
		private static int RangesSize(Range[] ranges)
		{
			uint size = 0U;
			for (int i = 0; i < ranges.Length; i++)
			{
				size += ranges[i].Length;
			}
			return (int)size;
		}

		// Token: 0x06000A4F RID: 2639 RVA: 0x00022638 File Offset: 0x00020838
		public IEnumerable<CustomAttribute> GetCustomAttributes()
		{
			this.InitializeTypeDefinitions();
			uint length = this.image.TableHeap[Table.CustomAttribute].Length;
			Collection<CustomAttribute> custom_attributes = new Collection<CustomAttribute>((int)length);
			this.ReadCustomAttributeRange(new Range(1U, length), custom_attributes);
			return custom_attributes;
		}

		// Token: 0x06000A50 RID: 2640 RVA: 0x00022679 File Offset: 0x00020879
		public byte[] ReadCustomAttributeBlob(uint signature)
		{
			return this.ReadBlob(signature);
		}

		// Token: 0x06000A51 RID: 2641 RVA: 0x00022684 File Offset: 0x00020884
		public void ReadCustomAttributeSignature(CustomAttribute attribute)
		{
			SignatureReader reader = this.ReadSignature(attribute.signature);
			if (!reader.CanReadMore())
			{
				return;
			}
			if (reader.ReadUInt16() != 1)
			{
				throw new InvalidOperationException();
			}
			MethodReference constructor = attribute.Constructor;
			if (constructor.HasParameters)
			{
				reader.ReadCustomAttributeConstructorArguments(attribute, constructor.Parameters);
			}
			if (!reader.CanReadMore())
			{
				return;
			}
			ushort named = reader.ReadUInt16();
			if (named == 0)
			{
				return;
			}
			reader.ReadCustomAttributeNamedArguments(named, ref attribute.fields, ref attribute.properties);
		}

		// Token: 0x06000A52 RID: 2642 RVA: 0x000226FC File Offset: 0x000208FC
		private void InitializeMarshalInfos()
		{
			if (this.metadata.FieldMarshals != null)
			{
				return;
			}
			int length = this.MoveTo(Table.FieldMarshal);
			Dictionary<MetadataToken, uint> marshals = (this.metadata.FieldMarshals = new Dictionary<MetadataToken, uint>(length));
			for (int i = 0; i < length; i++)
			{
				MetadataToken token = this.ReadMetadataToken(CodedIndex.HasFieldMarshal);
				uint signature = this.ReadBlobIndex();
				if (token.RID != 0U)
				{
					marshals.Add(token, signature);
				}
			}
		}

		// Token: 0x06000A53 RID: 2643 RVA: 0x00022765 File Offset: 0x00020965
		public bool HasMarshalInfo(IMarshalInfoProvider owner)
		{
			this.InitializeMarshalInfos();
			return this.metadata.FieldMarshals.ContainsKey(owner.MetadataToken);
		}

		// Token: 0x06000A54 RID: 2644 RVA: 0x00022784 File Offset: 0x00020984
		public MarshalInfo ReadMarshalInfo(IMarshalInfoProvider owner)
		{
			this.InitializeMarshalInfos();
			uint signature;
			if (!this.metadata.FieldMarshals.TryGetValue(owner.MetadataToken, out signature))
			{
				return null;
			}
			SignatureReader signatureReader = this.ReadSignature(signature);
			this.metadata.FieldMarshals.Remove(owner.MetadataToken);
			return signatureReader.ReadMarshalInfo();
		}

		// Token: 0x06000A55 RID: 2645 RVA: 0x000227D6 File Offset: 0x000209D6
		private void InitializeSecurityDeclarations()
		{
			if (this.metadata.SecurityDeclarations != null)
			{
				return;
			}
			this.metadata.SecurityDeclarations = this.InitializeRanges(Table.DeclSecurity, delegate
			{
				base.ReadUInt16();
				MetadataToken result = this.ReadMetadataToken(CodedIndex.HasDeclSecurity);
				this.ReadBlobIndex();
				return result;
			});
		}

		// Token: 0x06000A56 RID: 2646 RVA: 0x00022808 File Offset: 0x00020A08
		public bool HasSecurityDeclarations(ISecurityDeclarationProvider owner)
		{
			this.InitializeSecurityDeclarations();
			Range[] ranges;
			return this.metadata.TryGetSecurityDeclarationRanges(owner, out ranges) && MetadataReader.RangesSize(ranges) > 0;
		}

		// Token: 0x06000A57 RID: 2647 RVA: 0x00022838 File Offset: 0x00020A38
		public Collection<SecurityDeclaration> ReadSecurityDeclarations(ISecurityDeclarationProvider owner)
		{
			this.InitializeSecurityDeclarations();
			Range[] ranges;
			if (!this.metadata.TryGetSecurityDeclarationRanges(owner, out ranges))
			{
				return new Collection<SecurityDeclaration>();
			}
			Collection<SecurityDeclaration> security_declarations = new Collection<SecurityDeclaration>(MetadataReader.RangesSize(ranges));
			for (int i = 0; i < ranges.Length; i++)
			{
				this.ReadSecurityDeclarationRange(ranges[i], security_declarations);
			}
			return security_declarations;
		}

		// Token: 0x06000A58 RID: 2648 RVA: 0x0002288C File Offset: 0x00020A8C
		private void ReadSecurityDeclarationRange(Range range, Collection<SecurityDeclaration> security_declarations)
		{
			if (!this.MoveTo(Table.DeclSecurity, range.Start))
			{
				return;
			}
			int i = 0;
			while ((long)i < (long)((ulong)range.Length))
			{
				SecurityAction action = (SecurityAction)base.ReadUInt16();
				this.ReadMetadataToken(CodedIndex.HasDeclSecurity);
				uint signature = this.ReadBlobIndex();
				security_declarations.Add(new SecurityDeclaration(action, signature, this.module));
				i++;
			}
		}

		// Token: 0x06000A59 RID: 2649 RVA: 0x00022679 File Offset: 0x00020879
		public byte[] ReadSecurityDeclarationBlob(uint signature)
		{
			return this.ReadBlob(signature);
		}

		// Token: 0x06000A5A RID: 2650 RVA: 0x000228E8 File Offset: 0x00020AE8
		public void ReadSecurityDeclarationSignature(SecurityDeclaration declaration)
		{
			uint signature = declaration.signature;
			SignatureReader reader = this.ReadSignature(signature);
			if (reader.buffer[reader.position] != 46)
			{
				this.ReadXmlSecurityDeclaration(signature, declaration);
				return;
			}
			reader.position++;
			uint count = reader.ReadCompressedUInt32();
			Collection<SecurityAttribute> attributes = new Collection<SecurityAttribute>((int)count);
			int i = 0;
			while ((long)i < (long)((ulong)count))
			{
				attributes.Add(reader.ReadSecurityAttribute());
				i++;
			}
			declaration.security_attributes = attributes;
		}

		// Token: 0x06000A5B RID: 2651 RVA: 0x00022960 File Offset: 0x00020B60
		private void ReadXmlSecurityDeclaration(uint signature, SecurityDeclaration declaration)
		{
			declaration.security_attributes = new Collection<SecurityAttribute>(1)
			{
				new SecurityAttribute(this.module.TypeSystem.LookupType("System.Security.Permissions", "PermissionSetAttribute"))
				{
					properties = new Collection<CustomAttributeNamedArgument>(1),
					properties = 
					{
						new CustomAttributeNamedArgument("XML", new CustomAttributeArgument(this.module.TypeSystem.String, this.ReadUnicodeStringBlob(signature)))
					}
				}
			};
		}

		// Token: 0x06000A5C RID: 2652 RVA: 0x000229E0 File Offset: 0x00020BE0
		public Collection<ExportedType> ReadExportedTypes()
		{
			int length = this.MoveTo(Table.ExportedType);
			if (length == 0)
			{
				return new Collection<ExportedType>();
			}
			Collection<ExportedType> exported_types = new Collection<ExportedType>(length);
			for (int i = 1; i <= length; i++)
			{
				TypeAttributes attributes = (TypeAttributes)base.ReadUInt32();
				uint identifier = base.ReadUInt32();
				string name = this.ReadString();
				string @namespace = this.ReadString();
				MetadataToken implementation = this.ReadMetadataToken(CodedIndex.Implementation);
				ExportedType declaring_type = null;
				IMetadataScope scope = null;
				TokenType tokenType = implementation.TokenType;
				if (tokenType != TokenType.AssemblyRef && tokenType != TokenType.File)
				{
					if (tokenType == TokenType.ExportedType)
					{
						declaring_type = exported_types[(int)(implementation.RID - 1U)];
					}
				}
				else
				{
					scope = this.GetExportedTypeScope(implementation);
				}
				ExportedType exported_type = new ExportedType(@namespace, name, this.module, scope)
				{
					Attributes = attributes,
					Identifier = (int)identifier,
					DeclaringType = declaring_type
				};
				exported_type.token = new MetadataToken(TokenType.ExportedType, i);
				exported_types.Add(exported_type);
			}
			return exported_types;
		}

		// Token: 0x06000A5D RID: 2653 RVA: 0x00022AD0 File Offset: 0x00020CD0
		private IMetadataScope GetExportedTypeScope(MetadataToken token)
		{
			int position = this.position;
			TokenType tokenType = token.TokenType;
			IMetadataScope scope;
			if (tokenType != TokenType.AssemblyRef)
			{
				if (tokenType != TokenType.File)
				{
					throw new NotSupportedException();
				}
				this.InitializeModuleReferences();
				scope = this.GetModuleReferenceFromFile(token);
			}
			else
			{
				this.InitializeAssemblyReferences();
				scope = this.metadata.GetAssemblyNameReference(token.RID);
			}
			this.position = position;
			return scope;
		}

		// Token: 0x06000A5E RID: 2654 RVA: 0x00022B38 File Offset: 0x00020D38
		private ModuleReference GetModuleReferenceFromFile(MetadataToken token)
		{
			if (!this.MoveTo(Table.File, token.RID))
			{
				return null;
			}
			base.ReadUInt32();
			string file_name = this.ReadString();
			Collection<ModuleReference> modules = this.module.ModuleReferences;
			ModuleReference reference;
			for (int i = 0; i < modules.Count; i++)
			{
				reference = modules[i];
				if (reference.Name == file_name)
				{
					return reference;
				}
			}
			reference = new ModuleReference(file_name);
			modules.Add(reference);
			return reference;
		}

		// Token: 0x06000A5F RID: 2655 RVA: 0x00022BAC File Offset: 0x00020DAC
		private void InitializeDocuments()
		{
			if (this.metadata.Documents != null)
			{
				return;
			}
			int length = this.MoveTo(Table.Document);
			Document[] documents = (this.metadata.Documents = new Document[length]);
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				uint name_index = this.ReadBlobIndex();
				Guid hash_algorithm = this.ReadGuid();
				byte[] hash = this.ReadBlob();
				Guid language = this.ReadGuid();
				string name = this.ReadSignature(name_index).ReadDocumentName();
				documents[(int)(i - 1U)] = new Document(name)
				{
					HashAlgorithmGuid = hash_algorithm,
					Hash = hash,
					LanguageGuid = language,
					token = new MetadataToken(TokenType.Document, i)
				};
				i += 1U;
			}
		}

		// Token: 0x06000A60 RID: 2656 RVA: 0x00022C58 File Offset: 0x00020E58
		public Collection<SequencePoint> ReadSequencePoints(MethodDefinition method)
		{
			this.InitializeDocuments();
			if (!this.MoveTo(Table.MethodDebugInformation, method.MetadataToken.RID))
			{
				return new Collection<SequencePoint>(0);
			}
			uint document_index = this.ReadTableIndex(Table.Document);
			uint signature = this.ReadBlobIndex();
			if (signature == 0U)
			{
				return new Collection<SequencePoint>(0);
			}
			Document document = this.GetDocument(document_index);
			return this.ReadSignature(signature).ReadSequencePoints(document);
		}

		// Token: 0x06000A61 RID: 2657 RVA: 0x00022CBC File Offset: 0x00020EBC
		public Document GetDocument(uint rid)
		{
			Document document = this.metadata.GetDocument(rid);
			if (document == null)
			{
				return null;
			}
			document.custom_infos = this.GetCustomDebugInformation(document);
			return document;
		}

		// Token: 0x06000A62 RID: 2658 RVA: 0x00022CEC File Offset: 0x00020EEC
		private void InitializeLocalScopes()
		{
			if (this.metadata.LocalScopes != null)
			{
				return;
			}
			this.InitializeMethods();
			int length = this.MoveTo(Table.LocalScope);
			this.metadata.LocalScopes = new Dictionary<uint, Collection<Row<uint, Range, Range, uint, uint, uint>>>();
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				uint method = this.ReadTableIndex(Table.Method);
				uint import = this.ReadTableIndex(Table.ImportScope);
				Range variables = this.ReadListRange(i, Table.LocalScope, Table.LocalVariable);
				Range constants = this.ReadListRange(i, Table.LocalScope, Table.LocalConstant);
				uint scope_start = base.ReadUInt32();
				uint scope_length = base.ReadUInt32();
				this.metadata.SetLocalScopes(method, MetadataReader.AddMapping<uint, Row<uint, Range, Range, uint, uint, uint>>(this.metadata.LocalScopes, method, new Row<uint, Range, Range, uint, uint, uint>(import, variables, constants, scope_start, scope_length, i)));
				i += 1U;
			}
		}

		// Token: 0x06000A63 RID: 2659 RVA: 0x00022D9C File Offset: 0x00020F9C
		public ScopeDebugInformation ReadScope(MethodDefinition method)
		{
			this.InitializeLocalScopes();
			this.InitializeImportScopes();
			Collection<Row<uint, Range, Range, uint, uint, uint>> records;
			if (!this.metadata.TryGetLocalScopes(method, out records))
			{
				return null;
			}
			ScopeDebugInformation method_scope = null;
			for (int i = 0; i < records.Count; i++)
			{
				ScopeDebugInformation scope = this.ReadLocalScope(records[i]);
				if (i == 0)
				{
					method_scope = scope;
				}
				else if (!MetadataReader.AddScope(method_scope.scopes, scope))
				{
					method_scope.Scopes.Add(scope);
				}
			}
			return method_scope;
		}

		// Token: 0x06000A64 RID: 2660 RVA: 0x00022E0C File Offset: 0x0002100C
		private static bool AddScope(Collection<ScopeDebugInformation> scopes, ScopeDebugInformation scope)
		{
			if (scopes.IsNullOrEmpty<ScopeDebugInformation>())
			{
				return false;
			}
			foreach (ScopeDebugInformation sub_scope in scopes)
			{
				if (sub_scope.HasScopes && MetadataReader.AddScope(sub_scope.Scopes, scope))
				{
					return true;
				}
				if (scope.Start.Offset >= sub_scope.Start.Offset && scope.End.Offset <= sub_scope.End.Offset)
				{
					sub_scope.Scopes.Add(scope);
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000A65 RID: 2661 RVA: 0x00022EC8 File Offset: 0x000210C8
		private ScopeDebugInformation ReadLocalScope(Row<uint, Range, Range, uint, uint, uint> record)
		{
			ScopeDebugInformation scope = new ScopeDebugInformation
			{
				start = new InstructionOffset((int)record.Col4),
				end = new InstructionOffset((int)(record.Col4 + record.Col5)),
				token = new MetadataToken(TokenType.LocalScope, record.Col6)
			};
			if (record.Col1 > 0U)
			{
				scope.import = this.metadata.GetImportScope(record.Col1);
			}
			if (record.Col2.Length > 0U)
			{
				scope.variables = new Collection<VariableDebugInformation>((int)record.Col2.Length);
				for (uint i = 0U; i < record.Col2.Length; i += 1U)
				{
					VariableDebugInformation variable = this.ReadLocalVariable(record.Col2.Start + i);
					if (variable != null)
					{
						scope.variables.Add(variable);
					}
				}
			}
			if (record.Col3.Length > 0U)
			{
				scope.constants = new Collection<ConstantDebugInformation>((int)record.Col3.Length);
				for (uint j = 0U; j < record.Col3.Length; j += 1U)
				{
					ConstantDebugInformation constant = this.ReadLocalConstant(record.Col3.Start + j);
					if (constant != null)
					{
						scope.constants.Add(constant);
					}
				}
			}
			return scope;
		}

		// Token: 0x06000A66 RID: 2662 RVA: 0x00022FF8 File Offset: 0x000211F8
		private VariableDebugInformation ReadLocalVariable(uint rid)
		{
			if (!this.MoveTo(Table.LocalVariable, rid))
			{
				return null;
			}
			VariableAttributes attributes = (VariableAttributes)base.ReadUInt16();
			int index = (int)base.ReadUInt16();
			string name = this.ReadString();
			VariableDebugInformation variable = new VariableDebugInformation(index, name)
			{
				Attributes = attributes,
				token = new MetadataToken(TokenType.LocalVariable, rid)
			};
			variable.custom_infos = this.GetCustomDebugInformation(variable);
			return variable;
		}

		// Token: 0x06000A67 RID: 2663 RVA: 0x00023054 File Offset: 0x00021254
		private ConstantDebugInformation ReadLocalConstant(uint rid)
		{
			if (!this.MoveTo(Table.LocalConstant, rid))
			{
				return null;
			}
			string name = this.ReadString();
			SignatureReader signature = this.ReadSignature(this.ReadBlobIndex());
			TypeReference type = signature.ReadTypeSignature();
			object value;
			if (type.etype == ElementType.String)
			{
				if (!signature.CanReadMore())
				{
					value = "";
				}
				else if (signature.buffer[signature.position] != 255)
				{
					byte[] bytes = signature.ReadBytes((int)((ulong)signature.sig_length - (ulong)((long)signature.position - (long)((ulong)signature.start))));
					value = Encoding.Unicode.GetString(bytes, 0, bytes.Length);
				}
				else
				{
					value = null;
				}
			}
			else if (type.IsTypeOf("System", "Decimal"))
			{
				byte b = signature.ReadByte();
				value = new decimal(signature.ReadInt32(), signature.ReadInt32(), signature.ReadInt32(), (b & 128) > 0, b & 127);
			}
			else if (type.IsTypeOf("System", "DateTime"))
			{
				value = new DateTime(signature.ReadInt64());
			}
			else if (type.etype == ElementType.Object || type.etype == ElementType.None || type.etype == ElementType.Class || type.etype == ElementType.Array || type.etype == ElementType.GenericInst)
			{
				value = null;
			}
			else
			{
				value = signature.ReadConstantSignature(type.etype);
			}
			ConstantDebugInformation constant = new ConstantDebugInformation(name, type, value)
			{
				token = new MetadataToken(TokenType.LocalConstant, rid)
			};
			constant.custom_infos = this.GetCustomDebugInformation(constant);
			return constant;
		}

		// Token: 0x06000A68 RID: 2664 RVA: 0x000231D0 File Offset: 0x000213D0
		private void InitializeImportScopes()
		{
			if (this.metadata.ImportScopes != null)
			{
				return;
			}
			int length = this.MoveTo(Table.ImportScope);
			this.metadata.ImportScopes = new ImportDebugInformation[length];
			for (int i = 1; i <= length; i++)
			{
				this.ReadTableIndex(Table.ImportScope);
				ImportDebugInformation import = new ImportDebugInformation();
				import.token = new MetadataToken(TokenType.ImportScope, i);
				SignatureReader signature = this.ReadSignature(this.ReadBlobIndex());
				while (signature.CanReadMore())
				{
					import.Targets.Add(this.ReadImportTarget(signature));
				}
				this.metadata.ImportScopes[i - 1] = import;
			}
			this.MoveTo(Table.ImportScope);
			for (int j = 0; j < length; j++)
			{
				uint parent = this.ReadTableIndex(Table.ImportScope);
				this.ReadBlobIndex();
				if (parent != 0U)
				{
					this.metadata.ImportScopes[j].Parent = this.metadata.GetImportScope(parent);
				}
			}
		}

		// Token: 0x06000A69 RID: 2665 RVA: 0x000232B8 File Offset: 0x000214B8
		public string ReadUTF8StringBlob(uint signature)
		{
			return this.ReadStringBlob(signature, Encoding.UTF8);
		}

		// Token: 0x06000A6A RID: 2666 RVA: 0x000232C6 File Offset: 0x000214C6
		private string ReadUnicodeStringBlob(uint signature)
		{
			return this.ReadStringBlob(signature, Encoding.Unicode);
		}

		// Token: 0x06000A6B RID: 2667 RVA: 0x000232D4 File Offset: 0x000214D4
		private string ReadStringBlob(uint signature, Encoding encoding)
		{
			byte[] blob;
			int index;
			int count;
			this.GetBlobView(signature, out blob, out index, out count);
			if (count == 0)
			{
				return string.Empty;
			}
			return encoding.GetString(blob, index, count);
		}

		// Token: 0x06000A6C RID: 2668 RVA: 0x00023300 File Offset: 0x00021500
		private ImportTarget ReadImportTarget(SignatureReader signature)
		{
			AssemblyNameReference reference = null;
			string @namespace = null;
			string alias = null;
			TypeReference type = null;
			ImportTargetKind kind = (ImportTargetKind)signature.ReadCompressedUInt32();
			switch (kind)
			{
			case ImportTargetKind.ImportNamespace:
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.ImportNamespaceInAssembly:
				reference = this.metadata.GetAssemblyNameReference(signature.ReadCompressedUInt32());
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.ImportType:
				type = signature.ReadTypeToken();
				break;
			case ImportTargetKind.ImportXmlNamespaceWithAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.ImportAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.DefineAssemblyAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				reference = this.metadata.GetAssemblyNameReference(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.DefineNamespaceAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.DefineNamespaceInAssemblyAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				reference = this.metadata.GetAssemblyNameReference(signature.ReadCompressedUInt32());
				@namespace = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				break;
			case ImportTargetKind.DefineTypeAlias:
				alias = this.ReadUTF8StringBlob(signature.ReadCompressedUInt32());
				type = signature.ReadTypeToken();
				break;
			}
			return new ImportTarget(kind)
			{
				alias = alias,
				type = type,
				@namespace = @namespace,
				reference = reference
			};
		}

		// Token: 0x06000A6D RID: 2669 RVA: 0x00023464 File Offset: 0x00021664
		private void InitializeStateMachineMethods()
		{
			if (this.metadata.StateMachineMethods != null)
			{
				return;
			}
			int length = this.MoveTo(Table.StateMachineMethod);
			this.metadata.StateMachineMethods = new Dictionary<uint, uint>(length);
			for (int i = 0; i < length; i++)
			{
				this.metadata.StateMachineMethods.Add(this.ReadTableIndex(Table.Method), this.ReadTableIndex(Table.Method));
			}
		}

		// Token: 0x06000A6E RID: 2670 RVA: 0x000234C4 File Offset: 0x000216C4
		public MethodDefinition ReadStateMachineKickoffMethod(MethodDefinition method)
		{
			this.InitializeStateMachineMethods();
			uint rid;
			if (!this.metadata.TryGetStateMachineKickOffMethod(method, out rid))
			{
				return null;
			}
			return this.GetMethodDefinition(rid);
		}

		// Token: 0x06000A6F RID: 2671 RVA: 0x000234F0 File Offset: 0x000216F0
		private void InitializeCustomDebugInformations()
		{
			if (this.metadata.CustomDebugInformations != null)
			{
				return;
			}
			int length = this.MoveTo(Table.CustomDebugInformation);
			this.metadata.CustomDebugInformations = new Dictionary<MetadataToken, Row<Guid, uint, uint>[]>();
			uint i = 1U;
			while ((ulong)i <= (ulong)((long)length))
			{
				MetadataToken token = this.ReadMetadataToken(CodedIndex.HasCustomDebugInformation);
				Row<Guid, uint, uint> info = new Row<Guid, uint, uint>(this.ReadGuid(), this.ReadBlobIndex(), i);
				Row<Guid, uint, uint>[] infos;
				this.metadata.CustomDebugInformations.TryGetValue(token, out infos);
				this.metadata.CustomDebugInformations[token] = infos.Add(info);
				i += 1U;
			}
		}

		// Token: 0x06000A70 RID: 2672 RVA: 0x0002357C File Offset: 0x0002177C
		public bool HasCustomDebugInformation(ICustomDebugInformationProvider provider)
		{
			this.InitializeCustomDebugInformations();
			Row<Guid, uint, uint>[] rows;
			return this.metadata.CustomDebugInformations.TryGetValue(provider.MetadataToken, out rows) && rows.Length != 0;
		}

		// Token: 0x06000A71 RID: 2673 RVA: 0x000235B0 File Offset: 0x000217B0
		public Collection<CustomDebugInformation> GetCustomDebugInformation(ICustomDebugInformationProvider provider)
		{
			this.InitializeCustomDebugInformations();
			Row<Guid, uint, uint>[] rows;
			if (!this.metadata.CustomDebugInformations.TryGetValue(provider.MetadataToken, out rows))
			{
				return null;
			}
			Collection<CustomDebugInformation> infos = new Collection<CustomDebugInformation>(rows.Length);
			for (int i = 0; i < rows.Length; i++)
			{
				if (rows[i].Col1 == StateMachineScopeDebugInformation.KindIdentifier)
				{
					SignatureReader signature = this.ReadSignature(rows[i].Col2);
					Collection<StateMachineScope> scopes = new Collection<StateMachineScope>();
					while (signature.CanReadMore())
					{
						int start = signature.ReadInt32();
						int end = start + signature.ReadInt32();
						scopes.Add(new StateMachineScope(start, end));
					}
					infos.Add(new StateMachineScopeDebugInformation
					{
						scopes = scopes
					});
				}
				else if (rows[i].Col1 == AsyncMethodBodyDebugInformation.KindIdentifier)
				{
					SignatureReader signature2 = this.ReadSignature(rows[i].Col2);
					int catch_offset = signature2.ReadInt32() - 1;
					Collection<InstructionOffset> yields = new Collection<InstructionOffset>();
					Collection<InstructionOffset> resumes = new Collection<InstructionOffset>();
					Collection<MethodDefinition> resume_methods = new Collection<MethodDefinition>();
					while (signature2.CanReadMore())
					{
						yields.Add(new InstructionOffset(signature2.ReadInt32()));
						resumes.Add(new InstructionOffset(signature2.ReadInt32()));
						resume_methods.Add(this.GetMethodDefinition(signature2.ReadCompressedUInt32()));
					}
					infos.Add(new AsyncMethodBodyDebugInformation(catch_offset)
					{
						yields = yields,
						resumes = resumes,
						resume_methods = resume_methods
					});
				}
				else if (rows[i].Col1 == EmbeddedSourceDebugInformation.KindIdentifier)
				{
					infos.Add(new EmbeddedSourceDebugInformation(rows[i].Col2, this));
				}
				else if (rows[i].Col1 == SourceLinkDebugInformation.KindIdentifier)
				{
					infos.Add(new SourceLinkDebugInformation(Encoding.UTF8.GetString(this.ReadBlob(rows[i].Col2))));
				}
				else
				{
					infos.Add(new BinaryCustomDebugInformation(rows[i].Col1, this.ReadBlob(rows[i].Col2)));
				}
				infos[i].token = new MetadataToken(TokenType.CustomDebugInformation, rows[i].Col3);
			}
			return infos;
		}

		// Token: 0x06000A72 RID: 2674 RVA: 0x000237FC File Offset: 0x000219FC
		public byte[] ReadRawEmbeddedSourceDebugInformation(uint index)
		{
			SignatureReader signatureReader = this.ReadSignature(index);
			return signatureReader.ReadBytes((int)signatureReader.sig_length);
		}

		// Token: 0x06000A73 RID: 2675 RVA: 0x00023810 File Offset: 0x00021A10
		public Row<byte[], bool> ReadEmbeddedSourceDebugInformation(uint index)
		{
			SignatureReader signature = this.ReadSignature(index);
			int format = signature.ReadInt32();
			uint length = signature.sig_length - 4U;
			if (format == 0)
			{
				return new Row<byte[], bool>(signature.ReadBytes((int)length), false);
			}
			if (format > 0)
			{
				Stream stream = new MemoryStream(signature.ReadBytes((int)length));
				byte[] decompressed_document = new byte[format];
				MemoryStream decompressed_stream = new MemoryStream(decompressed_document);
				using (DeflateStream deflate_stream = new DeflateStream(stream, CompressionMode.Decompress, true))
				{
					deflate_stream.CopyTo(decompressed_stream);
				}
				return new Row<byte[], bool>(decompressed_document, true);
			}
			throw new NotSupportedException();
		}

		// Token: 0x0400033D RID: 829
		internal readonly Image image;

		// Token: 0x0400033E RID: 830
		internal readonly ModuleDefinition module;

		// Token: 0x0400033F RID: 831
		internal readonly MetadataSystem metadata;

		// Token: 0x04000340 RID: 832
		internal CodeReader code;

		// Token: 0x04000341 RID: 833
		internal IGenericContext context;

		// Token: 0x04000342 RID: 834
		private readonly MetadataReader metadata_reader;
	}
}
