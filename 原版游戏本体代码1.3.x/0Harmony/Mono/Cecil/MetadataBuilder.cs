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
	// Token: 0x02000223 RID: 547
	internal sealed class MetadataBuilder
	{
		// Token: 0x06000B16 RID: 2838 RVA: 0x00025D6C File Offset: 0x00023F6C
		public MetadataBuilder(ModuleDefinition module, string fq_name, uint timestamp, ISymbolWriterProvider symbol_writer_provider)
		{
			this.module = module;
			this.text_map = this.CreateTextMap();
			this.fq_name = fq_name;
			this.timestamp = timestamp;
			this.symbol_writer_provider = symbol_writer_provider;
			this.code = new CodeWriter(this);
			this.data = new DataBuffer();
			this.resources = new ResourceBuffer();
			this.string_heap = new StringHeapBuffer();
			this.guid_heap = new GuidHeapBuffer();
			this.user_string_heap = new UserStringHeapBuffer();
			this.blob_heap = new BlobHeapBuffer();
			this.table_heap = new TableHeapBuffer(module, this);
			this.type_ref_table = this.GetTable<TypeRefTable>(Table.TypeRef);
			this.type_def_table = this.GetTable<TypeDefTable>(Table.TypeDef);
			this.field_table = this.GetTable<FieldTable>(Table.Field);
			this.method_table = this.GetTable<MethodTable>(Table.Method);
			this.param_table = this.GetTable<ParamTable>(Table.Param);
			this.iface_impl_table = this.GetTable<InterfaceImplTable>(Table.InterfaceImpl);
			this.member_ref_table = this.GetTable<MemberRefTable>(Table.MemberRef);
			this.constant_table = this.GetTable<ConstantTable>(Table.Constant);
			this.custom_attribute_table = this.GetTable<CustomAttributeTable>(Table.CustomAttribute);
			this.declsec_table = this.GetTable<DeclSecurityTable>(Table.DeclSecurity);
			this.standalone_sig_table = this.GetTable<StandAloneSigTable>(Table.StandAloneSig);
			this.event_map_table = this.GetTable<EventMapTable>(Table.EventMap);
			this.event_table = this.GetTable<EventTable>(Table.Event);
			this.property_map_table = this.GetTable<PropertyMapTable>(Table.PropertyMap);
			this.property_table = this.GetTable<PropertyTable>(Table.Property);
			this.typespec_table = this.GetTable<TypeSpecTable>(Table.TypeSpec);
			this.method_spec_table = this.GetTable<MethodSpecTable>(Table.MethodSpec);
			RowEqualityComparer row_equality_comparer = new RowEqualityComparer();
			this.type_ref_map = new Dictionary<Row<uint, uint, uint>, MetadataToken>(row_equality_comparer);
			this.type_spec_map = new Dictionary<uint, MetadataToken>();
			this.member_ref_map = new Dictionary<Row<uint, uint, uint>, MetadataToken>(row_equality_comparer);
			this.method_spec_map = new Dictionary<Row<uint, uint>, MetadataToken>(row_equality_comparer);
			this.generic_parameters = new Collection<GenericParameter>();
			this.document_table = this.GetTable<DocumentTable>(Table.Document);
			this.method_debug_information_table = this.GetTable<MethodDebugInformationTable>(Table.MethodDebugInformation);
			this.local_scope_table = this.GetTable<LocalScopeTable>(Table.LocalScope);
			this.local_variable_table = this.GetTable<LocalVariableTable>(Table.LocalVariable);
			this.local_constant_table = this.GetTable<LocalConstantTable>(Table.LocalConstant);
			this.import_scope_table = this.GetTable<ImportScopeTable>(Table.ImportScope);
			this.state_machine_method_table = this.GetTable<StateMachineMethodTable>(Table.StateMachineMethod);
			this.custom_debug_information_table = this.GetTable<CustomDebugInformationTable>(Table.CustomDebugInformation);
			this.document_map = new Dictionary<string, MetadataToken>(StringComparer.Ordinal);
			this.import_scope_map = new Dictionary<Row<uint, uint>, MetadataToken>(row_equality_comparer);
		}

		// Token: 0x06000B17 RID: 2839 RVA: 0x00025FF0 File Offset: 0x000241F0
		public MetadataBuilder(ModuleDefinition module, PortablePdbWriterProvider writer_provider)
		{
			this.module = module;
			this.text_map = new TextMap();
			this.symbol_writer_provider = writer_provider;
			this.string_heap = new StringHeapBuffer();
			this.guid_heap = new GuidHeapBuffer();
			this.user_string_heap = new UserStringHeapBuffer();
			this.blob_heap = new BlobHeapBuffer();
			this.table_heap = new TableHeapBuffer(module, this);
			this.pdb_heap = new PdbHeapBuffer();
			this.document_table = this.GetTable<DocumentTable>(Table.Document);
			this.method_debug_information_table = this.GetTable<MethodDebugInformationTable>(Table.MethodDebugInformation);
			this.local_scope_table = this.GetTable<LocalScopeTable>(Table.LocalScope);
			this.local_variable_table = this.GetTable<LocalVariableTable>(Table.LocalVariable);
			this.local_constant_table = this.GetTable<LocalConstantTable>(Table.LocalConstant);
			this.import_scope_table = this.GetTable<ImportScopeTable>(Table.ImportScope);
			this.state_machine_method_table = this.GetTable<StateMachineMethodTable>(Table.StateMachineMethod);
			this.custom_debug_information_table = this.GetTable<CustomDebugInformationTable>(Table.CustomDebugInformation);
			RowEqualityComparer row_equality_comparer = new RowEqualityComparer();
			this.document_map = new Dictionary<string, MetadataToken>();
			this.import_scope_map = new Dictionary<Row<uint, uint>, MetadataToken>(row_equality_comparer);
		}

		// Token: 0x06000B18 RID: 2840 RVA: 0x00026128 File Offset: 0x00024328
		public void SetSymbolWriter(ISymbolWriter writer)
		{
			this.symbol_writer = writer;
			if (this.symbol_writer == null && this.module.HasImage && this.module.Image.HasDebugTables())
			{
				this.symbol_writer = new PortablePdbWriter(this, this.module);
			}
		}

		// Token: 0x06000B19 RID: 2841 RVA: 0x00026178 File Offset: 0x00024378
		private TextMap CreateTextMap()
		{
			TextMap textMap = new TextMap();
			textMap.AddMap(TextSegment.ImportAddressTable, (this.module.Architecture == TargetArchitecture.I386) ? 8 : 0);
			textMap.AddMap(TextSegment.CLIHeader, 72, 8);
			bool pe64 = this.module.Architecture == TargetArchitecture.AMD64 || this.module.Architecture == TargetArchitecture.IA64 || this.module.Architecture == TargetArchitecture.ARM64;
			textMap.AddMap(TextSegment.Code, 0, (!pe64) ? 4 : 16);
			return textMap;
		}

		// Token: 0x06000B1A RID: 2842 RVA: 0x000261FB File Offset: 0x000243FB
		private TTable GetTable<TTable>(Table table) where TTable : MetadataTable, new()
		{
			return this.table_heap.GetTable<TTable>(table);
		}

		// Token: 0x06000B1B RID: 2843 RVA: 0x00026209 File Offset: 0x00024409
		private uint GetStringIndex(string @string)
		{
			if (string.IsNullOrEmpty(@string))
			{
				return 0U;
			}
			return this.string_heap.GetStringIndex(@string);
		}

		// Token: 0x06000B1C RID: 2844 RVA: 0x00026221 File Offset: 0x00024421
		private uint GetGuidIndex(Guid guid)
		{
			return this.guid_heap.GetGuidIndex(guid);
		}

		// Token: 0x06000B1D RID: 2845 RVA: 0x0002622F File Offset: 0x0002442F
		private uint GetBlobIndex(ByteBuffer blob)
		{
			if (blob.length == 0)
			{
				return 0U;
			}
			return this.blob_heap.GetBlobIndex(blob);
		}

		// Token: 0x06000B1E RID: 2846 RVA: 0x00026247 File Offset: 0x00024447
		private uint GetBlobIndex(byte[] blob)
		{
			if (blob.IsNullOrEmpty<byte>())
			{
				return 0U;
			}
			return this.GetBlobIndex(new ByteBuffer(blob));
		}

		// Token: 0x06000B1F RID: 2847 RVA: 0x0002625F File Offset: 0x0002445F
		public void BuildMetadata()
		{
			this.BuildModule();
			this.table_heap.string_offsets = this.string_heap.WriteStrings();
			this.table_heap.ComputeTableInformations();
			this.table_heap.WriteTableHeap();
		}

		// Token: 0x06000B20 RID: 2848 RVA: 0x00026294 File Offset: 0x00024494
		private void BuildModule()
		{
			ModuleTable table = this.GetTable<ModuleTable>(Table.Module);
			table.row.Col1 = this.GetStringIndex(this.module.Name);
			table.row.Col2 = this.GetGuidIndex(this.module.Mvid);
			AssemblyDefinition assembly = this.module.Assembly;
			if (this.module.kind != ModuleKind.NetModule && assembly != null)
			{
				this.BuildAssembly();
			}
			if (this.module.HasAssemblyReferences)
			{
				this.AddAssemblyReferences();
			}
			if (this.module.HasModuleReferences)
			{
				this.AddModuleReferences();
			}
			if (this.module.HasResources)
			{
				this.AddResources();
			}
			if (this.module.HasExportedTypes)
			{
				this.AddExportedTypes();
			}
			this.BuildTypes();
			if (this.module.kind != ModuleKind.NetModule && assembly != null)
			{
				if (assembly.HasCustomAttributes)
				{
					this.AddCustomAttributes(assembly);
				}
				if (assembly.HasSecurityDeclarations)
				{
					this.AddSecurityDeclarations(assembly);
				}
			}
			if (this.module.HasCustomAttributes)
			{
				this.AddCustomAttributes(this.module);
			}
			if (this.module.EntryPoint != null)
			{
				this.entry_point = this.LookupToken(this.module.EntryPoint);
			}
		}

		// Token: 0x06000B21 RID: 2849 RVA: 0x000263C0 File Offset: 0x000245C0
		private void BuildAssembly()
		{
			AssemblyDefinition assembly = this.module.Assembly;
			AssemblyNameDefinition name = assembly.Name;
			this.GetTable<AssemblyTable>(Table.Assembly).row = new Row<AssemblyHashAlgorithm, ushort, ushort, ushort, ushort, AssemblyAttributes, uint, uint, uint>(name.HashAlgorithm, (ushort)name.Version.Major, (ushort)name.Version.Minor, (ushort)name.Version.Build, (ushort)name.Version.Revision, name.Attributes, this.GetBlobIndex(name.PublicKey), this.GetStringIndex(name.Name), this.GetStringIndex(name.Culture));
			if (assembly.Modules.Count > 1)
			{
				this.BuildModules();
			}
		}

		// Token: 0x06000B22 RID: 2850 RVA: 0x00026464 File Offset: 0x00024664
		private void BuildModules()
		{
			Collection<ModuleDefinition> modules = this.module.Assembly.Modules;
			FileTable table = this.GetTable<FileTable>(Table.File);
			for (int i = 0; i < modules.Count; i++)
			{
				ModuleDefinition module = modules[i];
				if (!module.IsMain)
				{
					WriterParameters parameters = new WriterParameters
					{
						SymbolWriterProvider = this.symbol_writer_provider
					};
					string file_name = this.GetModuleFileName(module.Name);
					module.Write(file_name, parameters);
					byte[] hash = CryptoService.ComputeHash(file_name);
					table.AddRow(new Row<FileAttributes, uint, uint>(FileAttributes.ContainsMetaData, this.GetStringIndex(module.Name), this.GetBlobIndex(hash)));
				}
			}
		}

		// Token: 0x06000B23 RID: 2851 RVA: 0x00026501 File Offset: 0x00024701
		private string GetModuleFileName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new NotSupportedException();
			}
			return Path.Combine(Path.GetDirectoryName(this.fq_name), name);
		}

		// Token: 0x06000B24 RID: 2852 RVA: 0x00026524 File Offset: 0x00024724
		private void AddAssemblyReferences()
		{
			Collection<AssemblyNameReference> references = this.module.AssemblyReferences;
			AssemblyRefTable table = this.GetTable<AssemblyRefTable>(Table.AssemblyRef);
			if (this.module.IsWindowsMetadata())
			{
				this.module.Projections.RemoveVirtualReferences(references);
			}
			for (int i = 0; i < references.Count; i++)
			{
				AssemblyNameReference reference = references[i];
				byte[] key_or_token = (reference.PublicKey.IsNullOrEmpty<byte>() ? reference.PublicKeyToken : reference.PublicKey);
				Version version = reference.Version;
				int rid = table.AddRow(new Row<ushort, ushort, ushort, ushort, AssemblyAttributes, uint, uint, uint, uint>((ushort)version.Major, (ushort)version.Minor, (ushort)version.Build, (ushort)version.Revision, reference.Attributes, this.GetBlobIndex(key_or_token), this.GetStringIndex(reference.Name), this.GetStringIndex(reference.Culture), this.GetBlobIndex(reference.Hash)));
				reference.token = new MetadataToken(TokenType.AssemblyRef, rid);
			}
			if (this.module.IsWindowsMetadata())
			{
				this.module.Projections.AddVirtualReferences(references);
			}
		}

		// Token: 0x06000B25 RID: 2853 RVA: 0x00026638 File Offset: 0x00024838
		private void AddModuleReferences()
		{
			Collection<ModuleReference> references = this.module.ModuleReferences;
			ModuleRefTable table = this.GetTable<ModuleRefTable>(Table.ModuleRef);
			for (int i = 0; i < references.Count; i++)
			{
				ModuleReference reference = references[i];
				reference.token = new MetadataToken(TokenType.ModuleRef, table.AddRow(this.GetStringIndex(reference.Name)));
			}
		}

		// Token: 0x06000B26 RID: 2854 RVA: 0x00026698 File Offset: 0x00024898
		private void AddResources()
		{
			Collection<Resource> resources = this.module.Resources;
			ManifestResourceTable table = this.GetTable<ManifestResourceTable>(Table.ManifestResource);
			for (int i = 0; i < resources.Count; i++)
			{
				Resource resource = resources[i];
				Row<uint, ManifestResourceAttributes, uint, uint> row = new Row<uint, ManifestResourceAttributes, uint, uint>(0U, resource.Attributes, this.GetStringIndex(resource.Name), 0U);
				switch (resource.ResourceType)
				{
				case ResourceType.Linked:
					row.Col4 = CodedIndex.Implementation.CompressMetadataToken(new MetadataToken(TokenType.File, this.AddLinkedResource((LinkedResource)resource)));
					break;
				case ResourceType.Embedded:
					row.Col1 = this.AddEmbeddedResource((EmbeddedResource)resource);
					break;
				case ResourceType.AssemblyLinked:
					row.Col4 = CodedIndex.Implementation.CompressMetadataToken(((AssemblyLinkedResource)resource).Assembly.MetadataToken);
					break;
				default:
					throw new NotSupportedException();
				}
				table.AddRow(row);
			}
		}

		// Token: 0x06000B27 RID: 2855 RVA: 0x0002677C File Offset: 0x0002497C
		private uint AddLinkedResource(LinkedResource resource)
		{
			MetadataTable<Row<FileAttributes, uint, uint>> table = this.GetTable<FileTable>(Table.File);
			byte[] hash = resource.Hash;
			if (hash.IsNullOrEmpty<byte>())
			{
				hash = CryptoService.ComputeHash(resource.File);
			}
			return (uint)table.AddRow(new Row<FileAttributes, uint, uint>(FileAttributes.ContainsNoMetaData, this.GetStringIndex(resource.File), this.GetBlobIndex(hash)));
		}

		// Token: 0x06000B28 RID: 2856 RVA: 0x000267CA File Offset: 0x000249CA
		private uint AddEmbeddedResource(EmbeddedResource resource)
		{
			return this.resources.AddResource(resource.GetResourceData());
		}

		// Token: 0x06000B29 RID: 2857 RVA: 0x000267E0 File Offset: 0x000249E0
		private void AddExportedTypes()
		{
			Collection<ExportedType> exported_types = this.module.ExportedTypes;
			ExportedTypeTable table = this.GetTable<ExportedTypeTable>(Table.ExportedType);
			for (int i = 0; i < exported_types.Count; i++)
			{
				ExportedType exported_type = exported_types[i];
				int rid = table.AddRow(new Row<TypeAttributes, uint, uint, uint, uint>(exported_type.Attributes, (uint)exported_type.Identifier, this.GetStringIndex(exported_type.Name), this.GetStringIndex(exported_type.Namespace), MetadataBuilder.MakeCodedRID(this.GetExportedTypeScope(exported_type), CodedIndex.Implementation)));
				exported_type.token = new MetadataToken(TokenType.ExportedType, rid);
			}
		}

		// Token: 0x06000B2A RID: 2858 RVA: 0x0002686C File Offset: 0x00024A6C
		private MetadataToken GetExportedTypeScope(ExportedType exported_type)
		{
			if (exported_type.DeclaringType != null)
			{
				return exported_type.DeclaringType.MetadataToken;
			}
			IMetadataScope scope = exported_type.Scope;
			TokenType tokenType = scope.MetadataToken.TokenType;
			if (tokenType != TokenType.ModuleRef)
			{
				if (tokenType == TokenType.AssemblyRef)
				{
					return scope.MetadataToken;
				}
			}
			else
			{
				FileTable file_table = this.GetTable<FileTable>(Table.File);
				for (int i = 0; i < file_table.length; i++)
				{
					if (file_table.rows[i].Col2 == this.GetStringIndex(scope.Name))
					{
						return new MetadataToken(TokenType.File, i + 1);
					}
				}
			}
			throw new NotSupportedException();
		}

		// Token: 0x06000B2B RID: 2859 RVA: 0x0002690C File Offset: 0x00024B0C
		private void BuildTypes()
		{
			if (!this.module.HasTypes)
			{
				return;
			}
			this.AttachTokens();
			this.AddTypes();
			this.AddGenericParameters();
		}

		// Token: 0x06000B2C RID: 2860 RVA: 0x00026930 File Offset: 0x00024B30
		private void AttachTokens()
		{
			Collection<TypeDefinition> types = this.module.Types;
			for (int i = 0; i < types.Count; i++)
			{
				this.AttachTypeToken(types[i]);
			}
		}

		// Token: 0x06000B2D RID: 2861 RVA: 0x00026968 File Offset: 0x00024B68
		private void AttachTypeToken(TypeDefinition type)
		{
			TypeDefinitionProjection treatment = WindowsRuntimeProjections.RemoveProjection(type);
			TokenType type2 = TokenType.TypeDef;
			uint num = this.type_rid;
			this.type_rid = num + 1U;
			type.token = new MetadataToken(type2, num);
			type.fields_range.Start = this.field_rid;
			type.methods_range.Start = this.method_rid;
			if (type.HasFields)
			{
				this.AttachFieldsToken(type);
			}
			if (type.HasMethods)
			{
				this.AttachMethodsToken(type);
			}
			if (type.HasNestedTypes)
			{
				this.AttachNestedTypesToken(type);
			}
			WindowsRuntimeProjections.ApplyProjection(type, treatment);
		}

		// Token: 0x06000B2E RID: 2862 RVA: 0x000269F4 File Offset: 0x00024BF4
		private void AttachNestedTypesToken(TypeDefinition type)
		{
			Collection<TypeDefinition> nested_types = type.NestedTypes;
			for (int i = 0; i < nested_types.Count; i++)
			{
				this.AttachTypeToken(nested_types[i]);
			}
		}

		// Token: 0x06000B2F RID: 2863 RVA: 0x00026A28 File Offset: 0x00024C28
		private void AttachFieldsToken(TypeDefinition type)
		{
			Collection<FieldDefinition> fields = type.Fields;
			type.fields_range.Length = (uint)fields.Count;
			for (int i = 0; i < fields.Count; i++)
			{
				MemberReference memberReference = fields[i];
				TokenType type2 = TokenType.Field;
				uint num = this.field_rid;
				this.field_rid = num + 1U;
				memberReference.token = new MetadataToken(type2, num);
			}
		}

		// Token: 0x06000B30 RID: 2864 RVA: 0x00026A88 File Offset: 0x00024C88
		private void AttachMethodsToken(TypeDefinition type)
		{
			Collection<MethodDefinition> methods = type.Methods;
			type.methods_range.Length = (uint)methods.Count;
			for (int i = 0; i < methods.Count; i++)
			{
				MemberReference memberReference = methods[i];
				TokenType type2 = TokenType.Method;
				uint num = this.method_rid;
				this.method_rid = num + 1U;
				memberReference.token = new MetadataToken(type2, num);
			}
		}

		// Token: 0x06000B31 RID: 2865 RVA: 0x00026AE5 File Offset: 0x00024CE5
		private MetadataToken GetTypeToken(TypeReference type)
		{
			if (type == null)
			{
				return MetadataToken.Zero;
			}
			if (type.IsDefinition)
			{
				return type.token;
			}
			if (type.IsTypeSpecification())
			{
				return this.GetTypeSpecToken(type);
			}
			return this.GetTypeRefToken(type);
		}

		// Token: 0x06000B32 RID: 2866 RVA: 0x00026B18 File Offset: 0x00024D18
		private MetadataToken GetTypeSpecToken(TypeReference type)
		{
			uint row = this.GetBlobIndex(this.GetTypeSpecSignature(type));
			MetadataToken token;
			if (this.type_spec_map.TryGetValue(row, out token))
			{
				return token;
			}
			return this.AddTypeSpecification(type, row);
		}

		// Token: 0x06000B33 RID: 2867 RVA: 0x00026B50 File Offset: 0x00024D50
		private MetadataToken AddTypeSpecification(TypeReference type, uint row)
		{
			type.token = new MetadataToken(TokenType.TypeSpec, this.typespec_table.AddRow(row));
			MetadataToken token = type.token;
			this.type_spec_map.Add(row, token);
			return token;
		}

		// Token: 0x06000B34 RID: 2868 RVA: 0x00026B90 File Offset: 0x00024D90
		private MetadataToken GetTypeRefToken(TypeReference type)
		{
			TypeReferenceProjection projection = WindowsRuntimeProjections.RemoveProjection(type);
			Row<uint, uint, uint> row = this.CreateTypeRefRow(type);
			MetadataToken token;
			if (!this.type_ref_map.TryGetValue(row, out token))
			{
				token = this.AddTypeReference(type, row);
			}
			WindowsRuntimeProjections.ApplyProjection(type, projection);
			return token;
		}

		// Token: 0x06000B35 RID: 2869 RVA: 0x00026BCD File Offset: 0x00024DCD
		private Row<uint, uint, uint> CreateTypeRefRow(TypeReference type)
		{
			return new Row<uint, uint, uint>(MetadataBuilder.MakeCodedRID(this.GetScopeToken(type), CodedIndex.ResolutionScope), this.GetStringIndex(type.Name), this.GetStringIndex(type.Namespace));
		}

		// Token: 0x06000B36 RID: 2870 RVA: 0x00026BFC File Offset: 0x00024DFC
		private MetadataToken GetScopeToken(TypeReference type)
		{
			if (type.IsNested)
			{
				return this.GetTypeRefToken(type.DeclaringType);
			}
			IMetadataScope scope = type.Scope;
			if (scope == null)
			{
				return MetadataToken.Zero;
			}
			return scope.MetadataToken;
		}

		// Token: 0x06000B37 RID: 2871 RVA: 0x00026C34 File Offset: 0x00024E34
		private static uint MakeCodedRID(IMetadataTokenProvider provider, CodedIndex index)
		{
			return MetadataBuilder.MakeCodedRID(provider.MetadataToken, index);
		}

		// Token: 0x06000B38 RID: 2872 RVA: 0x00026C42 File Offset: 0x00024E42
		private static uint MakeCodedRID(MetadataToken token, CodedIndex index)
		{
			return index.CompressMetadataToken(token);
		}

		// Token: 0x06000B39 RID: 2873 RVA: 0x00026C4C File Offset: 0x00024E4C
		private MetadataToken AddTypeReference(TypeReference type, Row<uint, uint, uint> row)
		{
			type.token = new MetadataToken(TokenType.TypeRef, this.type_ref_table.AddRow(row));
			MetadataToken token = type.token;
			this.type_ref_map.Add(row, token);
			return token;
		}

		// Token: 0x06000B3A RID: 2874 RVA: 0x00026C8C File Offset: 0x00024E8C
		private void AddTypes()
		{
			Collection<TypeDefinition> types = this.module.Types;
			for (int i = 0; i < types.Count; i++)
			{
				this.AddType(types[i]);
			}
		}

		// Token: 0x06000B3B RID: 2875 RVA: 0x00026CC4 File Offset: 0x00024EC4
		private void AddType(TypeDefinition type)
		{
			TypeDefinitionProjection treatment = WindowsRuntimeProjections.RemoveProjection(type);
			this.type_def_table.AddRow(new Row<TypeAttributes, uint, uint, uint, uint, uint>(type.Attributes, this.GetStringIndex(type.Name), this.GetStringIndex(type.Namespace), MetadataBuilder.MakeCodedRID(this.GetTypeToken(type.BaseType), CodedIndex.TypeDefOrRef), type.fields_range.Start, type.methods_range.Start));
			if (type.HasGenericParameters)
			{
				this.AddGenericParameters(type);
			}
			if (type.HasInterfaces)
			{
				this.AddInterfaces(type);
			}
			if (type.HasLayoutInfo)
			{
				this.AddLayoutInfo(type);
			}
			if (type.HasFields)
			{
				this.AddFields(type);
			}
			if (type.HasMethods)
			{
				this.AddMethods(type);
			}
			if (type.HasProperties)
			{
				this.AddProperties(type);
			}
			if (type.HasEvents)
			{
				this.AddEvents(type);
			}
			if (type.HasCustomAttributes)
			{
				this.AddCustomAttributes(type);
			}
			if (type.HasSecurityDeclarations)
			{
				this.AddSecurityDeclarations(type);
			}
			if (type.HasNestedTypes)
			{
				this.AddNestedTypes(type);
			}
			if (this.symbol_writer != null && type.HasCustomDebugInformations)
			{
				this.symbol_writer.Write(type);
			}
			WindowsRuntimeProjections.ApplyProjection(type, treatment);
		}

		// Token: 0x06000B3C RID: 2876 RVA: 0x00026DE8 File Offset: 0x00024FE8
		private void AddGenericParameters(IGenericParameterProvider owner)
		{
			Collection<GenericParameter> parameters = owner.GenericParameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				this.generic_parameters.Add(parameters[i]);
			}
		}

		// Token: 0x06000B3D RID: 2877 RVA: 0x00026E20 File Offset: 0x00025020
		private void AddGenericParameters()
		{
			GenericParameter[] items = this.generic_parameters.items;
			int size = this.generic_parameters.size;
			Array.Sort<GenericParameter>(items, 0, size, new MetadataBuilder.GenericParameterComparer());
			GenericParamTable generic_param_table = this.GetTable<GenericParamTable>(Table.GenericParam);
			GenericParamConstraintTable generic_param_constraint_table = this.GetTable<GenericParamConstraintTable>(Table.GenericParamConstraint);
			for (int i = 0; i < size; i++)
			{
				GenericParameter generic_parameter = items[i];
				int rid = generic_param_table.AddRow(new Row<ushort, GenericParameterAttributes, uint, uint>((ushort)generic_parameter.Position, generic_parameter.Attributes, MetadataBuilder.MakeCodedRID(generic_parameter.Owner, CodedIndex.TypeOrMethodDef), this.GetStringIndex(generic_parameter.Name)));
				generic_parameter.token = new MetadataToken(TokenType.GenericParam, rid);
				if (generic_parameter.HasConstraints)
				{
					this.AddConstraints(generic_parameter, generic_param_constraint_table);
				}
				if (generic_parameter.HasCustomAttributes)
				{
					this.AddCustomAttributes(generic_parameter);
				}
			}
		}

		// Token: 0x06000B3E RID: 2878 RVA: 0x00026EE8 File Offset: 0x000250E8
		private void AddConstraints(GenericParameter generic_parameter, GenericParamConstraintTable table)
		{
			Collection<GenericParameterConstraint> constraints = generic_parameter.Constraints;
			uint gp_rid = generic_parameter.token.RID;
			for (int i = 0; i < constraints.Count; i++)
			{
				GenericParameterConstraint constraint = constraints[i];
				int rid = table.AddRow(new Row<uint, uint>(gp_rid, MetadataBuilder.MakeCodedRID(this.GetTypeToken(constraint.ConstraintType), CodedIndex.TypeDefOrRef)));
				constraint.token = new MetadataToken(TokenType.GenericParamConstraint, rid);
				if (constraint.HasCustomAttributes)
				{
					this.AddCustomAttributes(constraint);
				}
			}
		}

		// Token: 0x06000B3F RID: 2879 RVA: 0x00026F64 File Offset: 0x00025164
		private void AddInterfaces(TypeDefinition type)
		{
			Collection<InterfaceImplementation> interfaces = type.Interfaces;
			uint type_rid = type.token.RID;
			for (int i = 0; i < interfaces.Count; i++)
			{
				InterfaceImplementation iface_impl = interfaces[i];
				int rid = this.iface_impl_table.AddRow(new Row<uint, uint>(type_rid, MetadataBuilder.MakeCodedRID(this.GetTypeToken(iface_impl.InterfaceType), CodedIndex.TypeDefOrRef)));
				iface_impl.token = new MetadataToken(TokenType.InterfaceImpl, rid);
				if (iface_impl.HasCustomAttributes)
				{
					this.AddCustomAttributes(iface_impl);
				}
			}
		}

		// Token: 0x06000B40 RID: 2880 RVA: 0x00026FE3 File Offset: 0x000251E3
		private void AddLayoutInfo(TypeDefinition type)
		{
			this.GetTable<ClassLayoutTable>(Table.ClassLayout).AddRow(new Row<ushort, uint, uint>((ushort)type.PackingSize, (uint)type.ClassSize, type.token.RID));
		}

		// Token: 0x06000B41 RID: 2881 RVA: 0x00027010 File Offset: 0x00025210
		private void AddNestedTypes(TypeDefinition type)
		{
			Collection<TypeDefinition> nested_types = type.NestedTypes;
			NestedClassTable nested_table = this.GetTable<NestedClassTable>(Table.NestedClass);
			for (int i = 0; i < nested_types.Count; i++)
			{
				TypeDefinition nested = nested_types[i];
				this.AddType(nested);
				nested_table.AddRow(new Row<uint, uint>(nested.token.RID, type.token.RID));
			}
		}

		// Token: 0x06000B42 RID: 2882 RVA: 0x00027070 File Offset: 0x00025270
		private void AddFields(TypeDefinition type)
		{
			Collection<FieldDefinition> fields = type.Fields;
			for (int i = 0; i < fields.Count; i++)
			{
				this.AddField(fields[i]);
			}
		}

		// Token: 0x06000B43 RID: 2883 RVA: 0x000270A4 File Offset: 0x000252A4
		private void AddField(FieldDefinition field)
		{
			FieldDefinitionProjection projection = WindowsRuntimeProjections.RemoveProjection(field);
			this.field_table.AddRow(new Row<FieldAttributes, uint, uint>(field.Attributes, this.GetStringIndex(field.Name), this.GetBlobIndex(this.GetFieldSignature(field))));
			if (!field.InitialValue.IsNullOrEmpty<byte>())
			{
				this.AddFieldRVA(field);
			}
			if (field.HasLayoutInfo)
			{
				this.AddFieldLayout(field);
			}
			if (field.HasCustomAttributes)
			{
				this.AddCustomAttributes(field);
			}
			if (field.HasConstant)
			{
				this.AddConstant(field, field.FieldType);
			}
			if (field.HasMarshalInfo)
			{
				this.AddMarshalInfo(field);
			}
			WindowsRuntimeProjections.ApplyProjection(field, projection);
		}

		// Token: 0x06000B44 RID: 2884 RVA: 0x00027148 File Offset: 0x00025348
		private void AddFieldRVA(FieldDefinition field)
		{
			MetadataTable<Row<uint, uint>> table = this.GetTable<FieldRVATable>(Table.FieldRVA);
			int align = 1;
			if (field.FieldType.IsDefinition && !field.FieldType.IsGenericInstance)
			{
				TypeDefinition type = field.FieldType.Resolve();
				if (type.Module == this.module && type.PackingSize > 1)
				{
					align = (int)type.PackingSize;
				}
			}
			table.AddRow(new Row<uint, uint>(this.data.AddData(field.InitialValue, align), field.token.RID));
		}

		// Token: 0x06000B45 RID: 2885 RVA: 0x000271CB File Offset: 0x000253CB
		private void AddFieldLayout(FieldDefinition field)
		{
			this.GetTable<FieldLayoutTable>(Table.FieldLayout).AddRow(new Row<uint, uint>((uint)field.Offset, field.token.RID));
		}

		// Token: 0x06000B46 RID: 2886 RVA: 0x000271F4 File Offset: 0x000253F4
		private void AddMethods(TypeDefinition type)
		{
			Collection<MethodDefinition> methods = type.Methods;
			for (int i = 0; i < methods.Count; i++)
			{
				this.AddMethod(methods[i]);
			}
		}

		// Token: 0x06000B47 RID: 2887 RVA: 0x00027228 File Offset: 0x00025428
		private void AddMethod(MethodDefinition method)
		{
			MethodDefinitionProjection projection = WindowsRuntimeProjections.RemoveProjection(method);
			this.method_table.AddRow(new Row<uint, MethodImplAttributes, MethodAttributes, uint, uint, uint>(method.HasBody ? this.code.WriteMethodBody(method) : 0U, method.ImplAttributes, method.Attributes, this.GetStringIndex(method.Name), this.GetBlobIndex(this.GetMethodSignature(method)), this.param_rid));
			this.AddParameters(method);
			if (method.HasGenericParameters)
			{
				this.AddGenericParameters(method);
			}
			if (method.IsPInvokeImpl)
			{
				this.AddPInvokeInfo(method);
			}
			if (method.HasCustomAttributes)
			{
				this.AddCustomAttributes(method);
			}
			if (method.HasSecurityDeclarations)
			{
				this.AddSecurityDeclarations(method);
			}
			if (method.HasOverrides)
			{
				this.AddOverrides(method);
			}
			WindowsRuntimeProjections.ApplyProjection(method, projection);
		}

		// Token: 0x06000B48 RID: 2888 RVA: 0x000272E8 File Offset: 0x000254E8
		private void AddParameters(MethodDefinition method)
		{
			ParameterDefinition return_parameter = method.MethodReturnType.parameter;
			if (return_parameter != null && MetadataBuilder.RequiresParameterRow(return_parameter))
			{
				this.AddParameter(0, return_parameter, this.param_table);
			}
			if (!method.HasParameters)
			{
				return;
			}
			Collection<ParameterDefinition> parameters = method.Parameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				ParameterDefinition parameter = parameters[i];
				if (MetadataBuilder.RequiresParameterRow(parameter))
				{
					this.AddParameter((ushort)(i + 1), parameter, this.param_table);
				}
			}
		}

		// Token: 0x06000B49 RID: 2889 RVA: 0x0002735C File Offset: 0x0002555C
		private void AddPInvokeInfo(MethodDefinition method)
		{
			PInvokeInfo pinvoke = method.PInvokeInfo;
			if (pinvoke == null)
			{
				return;
			}
			this.GetTable<ImplMapTable>(Table.ImplMap).AddRow(new Row<PInvokeAttributes, uint, uint, uint>(pinvoke.Attributes, MetadataBuilder.MakeCodedRID(method, CodedIndex.MemberForwarded), this.GetStringIndex(pinvoke.EntryPoint), pinvoke.Module.MetadataToken.RID));
		}

		// Token: 0x06000B4A RID: 2890 RVA: 0x000273B4 File Offset: 0x000255B4
		private void AddOverrides(MethodDefinition method)
		{
			Collection<MethodReference> overrides = method.Overrides;
			MethodImplTable table = this.GetTable<MethodImplTable>(Table.MethodImpl);
			for (int i = 0; i < overrides.Count; i++)
			{
				table.AddRow(new Row<uint, uint, uint>(method.DeclaringType.token.RID, MetadataBuilder.MakeCodedRID(method, CodedIndex.MethodDefOrRef), MetadataBuilder.MakeCodedRID(this.LookupToken(overrides[i]), CodedIndex.MethodDefOrRef)));
			}
		}

		// Token: 0x06000B4B RID: 2891 RVA: 0x00027418 File Offset: 0x00025618
		private static bool RequiresParameterRow(ParameterDefinition parameter)
		{
			return !string.IsNullOrEmpty(parameter.Name) || parameter.Attributes != ParameterAttributes.None || parameter.HasMarshalInfo || parameter.HasConstant || parameter.HasCustomAttributes;
		}

		// Token: 0x06000B4C RID: 2892 RVA: 0x00027448 File Offset: 0x00025648
		private void AddParameter(ushort sequence, ParameterDefinition parameter, ParamTable table)
		{
			table.AddRow(new Row<ParameterAttributes, ushort, uint>(parameter.Attributes, sequence, this.GetStringIndex(parameter.Name)));
			TokenType type = TokenType.Param;
			uint num = this.param_rid;
			this.param_rid = num + 1U;
			parameter.token = new MetadataToken(type, num);
			if (parameter.HasCustomAttributes)
			{
				this.AddCustomAttributes(parameter);
			}
			if (parameter.HasConstant)
			{
				this.AddConstant(parameter, parameter.ParameterType);
			}
			if (parameter.HasMarshalInfo)
			{
				this.AddMarshalInfo(parameter);
			}
		}

		// Token: 0x06000B4D RID: 2893 RVA: 0x000274C8 File Offset: 0x000256C8
		private void AddMarshalInfo(IMarshalInfoProvider owner)
		{
			this.GetTable<FieldMarshalTable>(Table.FieldMarshal).AddRow(new Row<uint, uint>(MetadataBuilder.MakeCodedRID(owner, CodedIndex.HasFieldMarshal), this.GetBlobIndex(this.GetMarshalInfoSignature(owner))));
		}

		// Token: 0x06000B4E RID: 2894 RVA: 0x000274F4 File Offset: 0x000256F4
		private void AddProperties(TypeDefinition type)
		{
			Collection<PropertyDefinition> properties = type.Properties;
			this.property_map_table.AddRow(new Row<uint, uint>(type.token.RID, this.property_rid));
			for (int i = 0; i < properties.Count; i++)
			{
				this.AddProperty(properties[i]);
			}
		}

		// Token: 0x06000B4F RID: 2895 RVA: 0x00027548 File Offset: 0x00025748
		private void AddProperty(PropertyDefinition property)
		{
			this.property_table.AddRow(new Row<PropertyAttributes, uint, uint>(property.Attributes, this.GetStringIndex(property.Name), this.GetBlobIndex(this.GetPropertySignature(property))));
			TokenType type = TokenType.Property;
			uint num = this.property_rid;
			this.property_rid = num + 1U;
			property.token = new MetadataToken(type, num);
			MethodDefinition method = property.GetMethod;
			if (method != null)
			{
				this.AddSemantic(MethodSemanticsAttributes.Getter, property, method);
			}
			method = property.SetMethod;
			if (method != null)
			{
				this.AddSemantic(MethodSemanticsAttributes.Setter, property, method);
			}
			if (property.HasOtherMethods)
			{
				this.AddOtherSemantic(property, property.OtherMethods);
			}
			if (property.HasCustomAttributes)
			{
				this.AddCustomAttributes(property);
			}
			if (property.HasConstant)
			{
				this.AddConstant(property, property.PropertyType);
			}
		}

		// Token: 0x06000B50 RID: 2896 RVA: 0x00027608 File Offset: 0x00025808
		private void AddOtherSemantic(IMetadataTokenProvider owner, Collection<MethodDefinition> others)
		{
			for (int i = 0; i < others.Count; i++)
			{
				this.AddSemantic(MethodSemanticsAttributes.Other, owner, others[i]);
			}
		}

		// Token: 0x06000B51 RID: 2897 RVA: 0x00027638 File Offset: 0x00025838
		private void AddEvents(TypeDefinition type)
		{
			Collection<EventDefinition> events = type.Events;
			this.event_map_table.AddRow(new Row<uint, uint>(type.token.RID, this.event_rid));
			for (int i = 0; i < events.Count; i++)
			{
				this.AddEvent(events[i]);
			}
		}

		// Token: 0x06000B52 RID: 2898 RVA: 0x0002768C File Offset: 0x0002588C
		private void AddEvent(EventDefinition @event)
		{
			this.event_table.AddRow(new Row<EventAttributes, uint, uint>(@event.Attributes, this.GetStringIndex(@event.Name), MetadataBuilder.MakeCodedRID(this.GetTypeToken(@event.EventType), CodedIndex.TypeDefOrRef)));
			TokenType type = TokenType.Event;
			uint num = this.event_rid;
			this.event_rid = num + 1U;
			@event.token = new MetadataToken(type, num);
			MethodDefinition method = @event.AddMethod;
			if (method != null)
			{
				this.AddSemantic(MethodSemanticsAttributes.AddOn, @event, method);
			}
			method = @event.InvokeMethod;
			if (method != null)
			{
				this.AddSemantic(MethodSemanticsAttributes.Fire, @event, method);
			}
			method = @event.RemoveMethod;
			if (method != null)
			{
				this.AddSemantic(MethodSemanticsAttributes.RemoveOn, @event, method);
			}
			if (@event.HasOtherMethods)
			{
				this.AddOtherSemantic(@event, @event.OtherMethods);
			}
			if (@event.HasCustomAttributes)
			{
				this.AddCustomAttributes(@event);
			}
		}

		// Token: 0x06000B53 RID: 2899 RVA: 0x0002774E File Offset: 0x0002594E
		private void AddSemantic(MethodSemanticsAttributes semantics, IMetadataTokenProvider provider, MethodDefinition method)
		{
			method.SemanticsAttributes = semantics;
			this.GetTable<MethodSemanticsTable>(Table.MethodSemantics).AddRow(new Row<MethodSemanticsAttributes, uint, uint>(semantics, method.token.RID, MetadataBuilder.MakeCodedRID(provider, CodedIndex.HasSemantics)));
		}

		// Token: 0x06000B54 RID: 2900 RVA: 0x00027780 File Offset: 0x00025980
		private void AddConstant(IConstantProvider owner, TypeReference type)
		{
			object constant = owner.Constant;
			ElementType etype = MetadataBuilder.GetConstantType(type, constant);
			this.constant_table.AddRow(new Row<ElementType, uint, uint>(etype, MetadataBuilder.MakeCodedRID(owner.MetadataToken, CodedIndex.HasConstant), this.GetBlobIndex(this.GetConstantSignature(etype, constant))));
		}

		// Token: 0x06000B55 RID: 2901 RVA: 0x000277C8 File Offset: 0x000259C8
		private static ElementType GetConstantType(TypeReference constant_type, object constant)
		{
			if (constant == null)
			{
				return ElementType.Class;
			}
			ElementType etype = constant_type.etype;
			switch (etype)
			{
			case ElementType.None:
			{
				TypeDefinition type = constant_type.CheckedResolve();
				if (type.IsEnum)
				{
					return MetadataBuilder.GetConstantType(type.GetEnumUnderlyingType(), constant);
				}
				return ElementType.Class;
			}
			case ElementType.Void:
			case ElementType.Ptr:
			case ElementType.ValueType:
			case ElementType.Class:
			case ElementType.TypedByRef:
			case (ElementType)23:
			case (ElementType)26:
			case ElementType.FnPtr:
				return etype;
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.I:
			case ElementType.U:
				return MetadataBuilder.GetConstantType(constant.GetType());
			case ElementType.String:
				return ElementType.String;
			case ElementType.ByRef:
			case ElementType.CModReqD:
			case ElementType.CModOpt:
				break;
			case ElementType.Var:
			case ElementType.Array:
			case ElementType.SzArray:
			case ElementType.MVar:
				return ElementType.Class;
			case ElementType.GenericInst:
			{
				GenericInstanceType generic_instance = (GenericInstanceType)constant_type;
				if (generic_instance.ElementType.IsTypeOf("System", "Nullable`1"))
				{
					return MetadataBuilder.GetConstantType(generic_instance.GenericArguments[0], constant);
				}
				return MetadataBuilder.GetConstantType(((TypeSpecification)constant_type).ElementType, constant);
			}
			case ElementType.Object:
				return MetadataBuilder.GetConstantType(constant.GetType());
			default:
				if (etype != ElementType.Sentinel)
				{
					return etype;
				}
				break;
			}
			return MetadataBuilder.GetConstantType(((TypeSpecification)constant_type).ElementType, constant);
		}

		// Token: 0x06000B56 RID: 2902 RVA: 0x0002790C File Offset: 0x00025B0C
		private static ElementType GetConstantType(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.Boolean:
				return ElementType.Boolean;
			case TypeCode.Char:
				return ElementType.Char;
			case TypeCode.SByte:
				return ElementType.I1;
			case TypeCode.Byte:
				return ElementType.U1;
			case TypeCode.Int16:
				return ElementType.I2;
			case TypeCode.UInt16:
				return ElementType.U2;
			case TypeCode.Int32:
				return ElementType.I4;
			case TypeCode.UInt32:
				return ElementType.U4;
			case TypeCode.Int64:
				return ElementType.I8;
			case TypeCode.UInt64:
				return ElementType.U8;
			case TypeCode.Single:
				return ElementType.R4;
			case TypeCode.Double:
				return ElementType.R8;
			case TypeCode.String:
				return ElementType.String;
			}
			throw new NotSupportedException(type.FullName);
		}

		// Token: 0x06000B57 RID: 2903 RVA: 0x00027998 File Offset: 0x00025B98
		private void AddCustomAttributes(ICustomAttributeProvider owner)
		{
			Collection<CustomAttribute> custom_attributes = owner.CustomAttributes;
			for (int i = 0; i < custom_attributes.Count; i++)
			{
				CustomAttribute attribute = custom_attributes[i];
				CustomAttributeValueProjection projection = WindowsRuntimeProjections.RemoveProjection(attribute);
				this.custom_attribute_table.AddRow(new Row<uint, uint, uint>(MetadataBuilder.MakeCodedRID(owner, CodedIndex.HasCustomAttribute), MetadataBuilder.MakeCodedRID(this.LookupToken(attribute.Constructor), CodedIndex.CustomAttributeType), this.GetBlobIndex(this.GetCustomAttributeSignature(attribute))));
				WindowsRuntimeProjections.ApplyProjection(attribute, projection);
			}
		}

		// Token: 0x06000B58 RID: 2904 RVA: 0x00027A0C File Offset: 0x00025C0C
		private void AddSecurityDeclarations(ISecurityDeclarationProvider owner)
		{
			Collection<SecurityDeclaration> declarations = owner.SecurityDeclarations;
			for (int i = 0; i < declarations.Count; i++)
			{
				SecurityDeclaration declaration = declarations[i];
				this.declsec_table.AddRow(new Row<SecurityAction, uint, uint>(declaration.Action, MetadataBuilder.MakeCodedRID(owner, CodedIndex.HasDeclSecurity), this.GetBlobIndex(this.GetSecurityDeclarationSignature(declaration))));
			}
		}

		// Token: 0x06000B59 RID: 2905 RVA: 0x00027A64 File Offset: 0x00025C64
		private MetadataToken GetMemberRefToken(MemberReference member)
		{
			Row<uint, uint, uint> row = this.CreateMemberRefRow(member);
			MetadataToken token;
			if (!this.member_ref_map.TryGetValue(row, out token))
			{
				token = this.AddMemberReference(member, row);
			}
			return token;
		}

		// Token: 0x06000B5A RID: 2906 RVA: 0x00027A93 File Offset: 0x00025C93
		private Row<uint, uint, uint> CreateMemberRefRow(MemberReference member)
		{
			return new Row<uint, uint, uint>(MetadataBuilder.MakeCodedRID(this.GetTypeToken(member.DeclaringType), CodedIndex.MemberRefParent), this.GetStringIndex(member.Name), this.GetBlobIndex(this.GetMemberRefSignature(member)));
		}

		// Token: 0x06000B5B RID: 2907 RVA: 0x00027AC8 File Offset: 0x00025CC8
		private MetadataToken AddMemberReference(MemberReference member, Row<uint, uint, uint> row)
		{
			member.token = new MetadataToken(TokenType.MemberRef, this.member_ref_table.AddRow(row));
			MetadataToken token = member.token;
			this.member_ref_map.Add(row, token);
			return token;
		}

		// Token: 0x06000B5C RID: 2908 RVA: 0x00027B08 File Offset: 0x00025D08
		private MetadataToken GetMethodSpecToken(MethodSpecification method_spec)
		{
			Row<uint, uint> row = this.CreateMethodSpecRow(method_spec);
			MetadataToken token;
			if (this.method_spec_map.TryGetValue(row, out token))
			{
				return token;
			}
			this.AddMethodSpecification(method_spec, row);
			return method_spec.token;
		}

		// Token: 0x06000B5D RID: 2909 RVA: 0x00027B3D File Offset: 0x00025D3D
		private void AddMethodSpecification(MethodSpecification method_spec, Row<uint, uint> row)
		{
			method_spec.token = new MetadataToken(TokenType.MethodSpec, this.method_spec_table.AddRow(row));
			this.method_spec_map.Add(row, method_spec.token);
		}

		// Token: 0x06000B5E RID: 2910 RVA: 0x00027B6D File Offset: 0x00025D6D
		private Row<uint, uint> CreateMethodSpecRow(MethodSpecification method_spec)
		{
			return new Row<uint, uint>(MetadataBuilder.MakeCodedRID(this.LookupToken(method_spec.ElementMethod), CodedIndex.MethodDefOrRef), this.GetBlobIndex(this.GetMethodSpecSignature(method_spec)));
		}

		// Token: 0x06000B5F RID: 2911 RVA: 0x00027B93 File Offset: 0x00025D93
		private SignatureWriter CreateSignatureWriter()
		{
			return new SignatureWriter(this);
		}

		// Token: 0x06000B60 RID: 2912 RVA: 0x00027B9C File Offset: 0x00025D9C
		private SignatureWriter GetMethodSpecSignature(MethodSpecification method_spec)
		{
			if (!method_spec.IsGenericInstance)
			{
				throw new NotSupportedException();
			}
			GenericInstanceMethod generic_instance = (GenericInstanceMethod)method_spec;
			SignatureWriter signatureWriter = this.CreateSignatureWriter();
			signatureWriter.WriteByte(10);
			signatureWriter.WriteGenericInstanceSignature(generic_instance);
			return signatureWriter;
		}

		// Token: 0x06000B61 RID: 2913 RVA: 0x00027BD3 File Offset: 0x00025DD3
		public uint AddStandAloneSignature(uint signature)
		{
			return (uint)this.standalone_sig_table.AddRow(signature);
		}

		// Token: 0x06000B62 RID: 2914 RVA: 0x00027BE1 File Offset: 0x00025DE1
		public uint GetLocalVariableBlobIndex(Collection<VariableDefinition> variables)
		{
			return this.GetBlobIndex(this.GetVariablesSignature(variables));
		}

		// Token: 0x06000B63 RID: 2915 RVA: 0x00027BF0 File Offset: 0x00025DF0
		public uint GetCallSiteBlobIndex(CallSite call_site)
		{
			return this.GetBlobIndex(this.GetMethodSignature(call_site));
		}

		// Token: 0x06000B64 RID: 2916 RVA: 0x00027BFF File Offset: 0x00025DFF
		public uint GetConstantTypeBlobIndex(TypeReference constant_type)
		{
			return this.GetBlobIndex(this.GetConstantTypeSignature(constant_type));
		}

		// Token: 0x06000B65 RID: 2917 RVA: 0x00027C10 File Offset: 0x00025E10
		private SignatureWriter GetVariablesSignature(Collection<VariableDefinition> variables)
		{
			SignatureWriter signature = this.CreateSignatureWriter();
			signature.WriteByte(7);
			signature.WriteCompressedUInt32((uint)variables.Count);
			for (int i = 0; i < variables.Count; i++)
			{
				signature.WriteTypeSignature(variables[i].VariableType);
			}
			return signature;
		}

		// Token: 0x06000B66 RID: 2918 RVA: 0x00027C5B File Offset: 0x00025E5B
		private SignatureWriter GetConstantTypeSignature(TypeReference constant_type)
		{
			SignatureWriter signatureWriter = this.CreateSignatureWriter();
			signatureWriter.WriteByte(6);
			signatureWriter.WriteTypeSignature(constant_type);
			return signatureWriter;
		}

		// Token: 0x06000B67 RID: 2919 RVA: 0x00027C71 File Offset: 0x00025E71
		private SignatureWriter GetFieldSignature(FieldReference field)
		{
			SignatureWriter signatureWriter = this.CreateSignatureWriter();
			signatureWriter.WriteByte(6);
			signatureWriter.WriteTypeSignature(field.FieldType);
			return signatureWriter;
		}

		// Token: 0x06000B68 RID: 2920 RVA: 0x00027C8C File Offset: 0x00025E8C
		private SignatureWriter GetMethodSignature(IMethodSignature method)
		{
			SignatureWriter signatureWriter = this.CreateSignatureWriter();
			signatureWriter.WriteMethodSignature(method);
			return signatureWriter;
		}

		// Token: 0x06000B69 RID: 2921 RVA: 0x00027C9C File Offset: 0x00025E9C
		private SignatureWriter GetMemberRefSignature(MemberReference member)
		{
			FieldReference field = member as FieldReference;
			if (field != null)
			{
				return this.GetFieldSignature(field);
			}
			MethodReference method = member as MethodReference;
			if (method != null)
			{
				return this.GetMethodSignature(method);
			}
			throw new NotSupportedException();
		}

		// Token: 0x06000B6A RID: 2922 RVA: 0x00027CD4 File Offset: 0x00025ED4
		private SignatureWriter GetPropertySignature(PropertyDefinition property)
		{
			SignatureWriter signature = this.CreateSignatureWriter();
			byte calling_convention = 8;
			if (property.HasThis)
			{
				calling_convention |= 32;
			}
			uint param_count = 0U;
			Collection<ParameterDefinition> parameters = null;
			if (property.HasParameters)
			{
				parameters = property.Parameters;
				param_count = (uint)parameters.Count;
			}
			signature.WriteByte(calling_convention);
			signature.WriteCompressedUInt32(param_count);
			signature.WriteTypeSignature(property.PropertyType);
			if (param_count == 0U)
			{
				return signature;
			}
			int i = 0;
			while ((long)i < (long)((ulong)param_count))
			{
				signature.WriteTypeSignature(parameters[i].ParameterType);
				i++;
			}
			return signature;
		}

		// Token: 0x06000B6B RID: 2923 RVA: 0x00027D57 File Offset: 0x00025F57
		private SignatureWriter GetTypeSpecSignature(TypeReference type)
		{
			SignatureWriter signatureWriter = this.CreateSignatureWriter();
			signatureWriter.WriteTypeSignature(type);
			return signatureWriter;
		}

		// Token: 0x06000B6C RID: 2924 RVA: 0x00027D68 File Offset: 0x00025F68
		private SignatureWriter GetConstantSignature(ElementType type, object value)
		{
			SignatureWriter signature = this.CreateSignatureWriter();
			if (type <= ElementType.String)
			{
				if (type != ElementType.None)
				{
					if (type != ElementType.String)
					{
						goto IL_3B;
					}
					signature.WriteConstantString((string)value);
					return signature;
				}
			}
			else if (type - ElementType.Class > 3 && type - ElementType.Object > 2)
			{
				goto IL_3B;
			}
			signature.WriteInt32(0);
			return signature;
			IL_3B:
			signature.WriteConstantPrimitive(value);
			return signature;
		}

		// Token: 0x06000B6D RID: 2925 RVA: 0x00027DB8 File Offset: 0x00025FB8
		private SignatureWriter GetCustomAttributeSignature(CustomAttribute attribute)
		{
			SignatureWriter signature = this.CreateSignatureWriter();
			if (!attribute.resolved)
			{
				signature.WriteBytes(attribute.GetBlob());
				return signature;
			}
			signature.WriteUInt16(1);
			signature.WriteCustomAttributeConstructorArguments(attribute);
			signature.WriteCustomAttributeNamedArguments(attribute);
			return signature;
		}

		// Token: 0x06000B6E RID: 2926 RVA: 0x00027DF8 File Offset: 0x00025FF8
		private SignatureWriter GetSecurityDeclarationSignature(SecurityDeclaration declaration)
		{
			SignatureWriter signature = this.CreateSignatureWriter();
			if (!declaration.resolved)
			{
				signature.WriteBytes(declaration.GetBlob());
			}
			else if (this.module.Runtime < TargetRuntime.Net_2_0)
			{
				signature.WriteXmlSecurityDeclaration(declaration);
			}
			else
			{
				signature.WriteSecurityDeclaration(declaration);
			}
			return signature;
		}

		// Token: 0x06000B6F RID: 2927 RVA: 0x00027E41 File Offset: 0x00026041
		private SignatureWriter GetMarshalInfoSignature(IMarshalInfoProvider owner)
		{
			SignatureWriter signatureWriter = this.CreateSignatureWriter();
			signatureWriter.WriteMarshalInfo(owner.MarshalInfo);
			return signatureWriter;
		}

		// Token: 0x06000B70 RID: 2928 RVA: 0x00027E55 File Offset: 0x00026055
		private static Exception CreateForeignMemberException(MemberReference member)
		{
			return new ArgumentException(string.Format("Member '{0}' is declared in another module and needs to be imported", member));
		}

		// Token: 0x06000B71 RID: 2929 RVA: 0x00027E68 File Offset: 0x00026068
		public MetadataToken LookupToken(IMetadataTokenProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException();
			}
			if (this.metadata_builder != null)
			{
				return this.metadata_builder.LookupToken(provider);
			}
			MemberReference member = provider as MemberReference;
			if (member == null || member.Module != this.module)
			{
				throw MetadataBuilder.CreateForeignMemberException(member);
			}
			MetadataToken token = provider.MetadataToken;
			TokenType tokenType = token.TokenType;
			if (tokenType <= TokenType.MemberRef)
			{
				if (tokenType <= TokenType.TypeDef)
				{
					if (tokenType == TokenType.TypeRef)
					{
						goto IL_BE;
					}
					if (tokenType != TokenType.TypeDef)
					{
						goto IL_E0;
					}
				}
				else if (tokenType != TokenType.Field && tokenType != TokenType.Method)
				{
					if (tokenType != TokenType.MemberRef)
					{
						goto IL_E0;
					}
					return this.GetMemberRefToken(member);
				}
			}
			else if (tokenType <= TokenType.Property)
			{
				if (tokenType != TokenType.Event && tokenType != TokenType.Property)
				{
					goto IL_E0;
				}
			}
			else
			{
				if (tokenType == TokenType.TypeSpec || tokenType == TokenType.GenericParam)
				{
					goto IL_BE;
				}
				if (tokenType != TokenType.MethodSpec)
				{
					goto IL_E0;
				}
				return this.GetMethodSpecToken((MethodSpecification)provider);
			}
			return token;
			IL_BE:
			return this.GetTypeToken((TypeReference)provider);
			IL_E0:
			throw new NotSupportedException();
		}

		// Token: 0x06000B72 RID: 2930 RVA: 0x00027F5C File Offset: 0x0002615C
		public void AddMethodDebugInformation(MethodDebugInformation method_info)
		{
			if (method_info.HasSequencePoints)
			{
				this.AddSequencePoints(method_info);
			}
			if (method_info.Scope != null)
			{
				this.AddLocalScope(method_info, method_info.Scope);
			}
			if (method_info.StateMachineKickOffMethod != null)
			{
				this.AddStateMachineMethod(method_info);
			}
			this.AddCustomDebugInformations(method_info.Method);
		}

		// Token: 0x06000B73 RID: 2931 RVA: 0x00027FA8 File Offset: 0x000261A8
		private void AddStateMachineMethod(MethodDebugInformation method_info)
		{
			this.state_machine_method_table.AddRow(new Row<uint, uint>(method_info.Method.MetadataToken.RID, method_info.StateMachineKickOffMethod.MetadataToken.RID));
		}

		// Token: 0x06000B74 RID: 2932 RVA: 0x00027FEC File Offset: 0x000261EC
		private void AddLocalScope(MethodDebugInformation method_info, ScopeDebugInformation scope)
		{
			int rid = this.local_scope_table.AddRow(new Row<uint, uint, uint, uint, uint, uint>(method_info.Method.MetadataToken.RID, (scope.import != null) ? this.AddImportScope(scope.import) : 0U, this.local_variable_rid, this.local_constant_rid, (uint)scope.Start.Offset, (uint)((scope.End.IsEndOfMethod ? method_info.code_size : scope.End.Offset) - scope.Start.Offset)));
			scope.token = new MetadataToken(TokenType.LocalScope, rid);
			this.AddCustomDebugInformations(scope);
			if (scope.HasVariables)
			{
				this.AddLocalVariables(scope);
			}
			if (scope.HasConstants)
			{
				this.AddLocalConstants(scope);
			}
			for (int i = 0; i < scope.Scopes.Count; i++)
			{
				this.AddLocalScope(method_info, scope.Scopes[i]);
			}
		}

		// Token: 0x06000B75 RID: 2933 RVA: 0x000280E4 File Offset: 0x000262E4
		private void AddLocalVariables(ScopeDebugInformation scope)
		{
			for (int i = 0; i < scope.Variables.Count; i++)
			{
				VariableDebugInformation variable = scope.Variables[i];
				this.local_variable_table.AddRow(new Row<VariableAttributes, ushort, uint>(variable.Attributes, (ushort)variable.Index, this.GetStringIndex(variable.Name)));
				variable.token = new MetadataToken(TokenType.LocalVariable, this.local_variable_rid);
				this.local_variable_rid += 1U;
				this.AddCustomDebugInformations(variable);
			}
		}

		// Token: 0x06000B76 RID: 2934 RVA: 0x0002816C File Offset: 0x0002636C
		private void AddLocalConstants(ScopeDebugInformation scope)
		{
			for (int i = 0; i < scope.Constants.Count; i++)
			{
				ConstantDebugInformation constant = scope.Constants[i];
				this.local_constant_table.AddRow(new Row<uint, uint>(this.GetStringIndex(constant.Name), this.GetBlobIndex(this.GetConstantSignature(constant))));
				constant.token = new MetadataToken(TokenType.LocalConstant, this.local_constant_rid);
				this.local_constant_rid += 1U;
			}
		}

		// Token: 0x06000B77 RID: 2935 RVA: 0x000281EC File Offset: 0x000263EC
		private SignatureWriter GetConstantSignature(ConstantDebugInformation constant)
		{
			TypeReference type = constant.ConstantType;
			SignatureWriter signature = this.CreateSignatureWriter();
			signature.WriteTypeSignature(type);
			if (type.IsTypeOf("System", "Decimal"))
			{
				int[] bits = decimal.GetBits((decimal)constant.Value);
				uint low = (uint)bits[0];
				uint mid = (uint)bits[1];
				uint high = (uint)bits[2];
				byte scale = (byte)(bits[3] >> 16);
				bool negative = ((long)bits[3] & (long)((ulong)int.MinValue)) != 0L;
				signature.WriteByte(scale | (negative ? 128 : 0));
				signature.WriteUInt32(low);
				signature.WriteUInt32(mid);
				signature.WriteUInt32(high);
				return signature;
			}
			if (type.IsTypeOf("System", "DateTime"))
			{
				signature.WriteInt64(((DateTime)constant.Value).Ticks);
				return signature;
			}
			signature.WriteBytes(this.GetConstantSignature(type.etype, constant.Value));
			return signature;
		}

		// Token: 0x06000B78 RID: 2936 RVA: 0x000282CC File Offset: 0x000264CC
		public void AddCustomDebugInformations(ICustomDebugInformationProvider provider)
		{
			if (!provider.HasCustomDebugInformations)
			{
				return;
			}
			Collection<CustomDebugInformation> custom_infos = provider.CustomDebugInformations;
			int i = 0;
			while (i < custom_infos.Count)
			{
				CustomDebugInformation custom_info = custom_infos[i];
				switch (custom_info.Kind)
				{
				case CustomDebugInformationKind.Binary:
				{
					BinaryCustomDebugInformation binary_info = (BinaryCustomDebugInformation)custom_info;
					this.AddCustomDebugInformation(provider, binary_info, this.GetBlobIndex(binary_info.Data));
					break;
				}
				case CustomDebugInformationKind.StateMachineScope:
					this.AddStateMachineScopeDebugInformation(provider, (StateMachineScopeDebugInformation)custom_info);
					break;
				case CustomDebugInformationKind.DynamicVariable:
				case CustomDebugInformationKind.DefaultNamespace:
					goto IL_A5;
				case CustomDebugInformationKind.AsyncMethodBody:
					this.AddAsyncMethodBodyDebugInformation(provider, (AsyncMethodBodyDebugInformation)custom_info);
					break;
				case CustomDebugInformationKind.EmbeddedSource:
					this.AddEmbeddedSourceDebugInformation(provider, (EmbeddedSourceDebugInformation)custom_info);
					break;
				case CustomDebugInformationKind.SourceLink:
					this.AddSourceLinkDebugInformation(provider, (SourceLinkDebugInformation)custom_info);
					break;
				default:
					goto IL_A5;
				}
				i++;
				continue;
				IL_A5:
				throw new NotImplementedException();
			}
		}

		// Token: 0x06000B79 RID: 2937 RVA: 0x00028394 File Offset: 0x00026594
		private void AddStateMachineScopeDebugInformation(ICustomDebugInformationProvider provider, StateMachineScopeDebugInformation state_machine_scope)
		{
			MethodDebugInformation method_info = ((MethodDefinition)provider).DebugInformation;
			SignatureWriter signature = this.CreateSignatureWriter();
			Collection<StateMachineScope> scopes = state_machine_scope.Scopes;
			for (int i = 0; i < scopes.Count; i++)
			{
				StateMachineScope scope = scopes[i];
				signature.WriteUInt32((uint)scope.Start.Offset);
				int end_offset = (scope.End.IsEndOfMethod ? method_info.code_size : scope.End.Offset);
				signature.WriteUInt32((uint)(end_offset - scope.Start.Offset));
			}
			this.AddCustomDebugInformation(provider, state_machine_scope, signature);
		}

		// Token: 0x06000B7A RID: 2938 RVA: 0x0002843C File Offset: 0x0002663C
		private void AddAsyncMethodBodyDebugInformation(ICustomDebugInformationProvider provider, AsyncMethodBodyDebugInformation async_method)
		{
			SignatureWriter signature = this.CreateSignatureWriter();
			signature.WriteUInt32((uint)(async_method.catch_handler.Offset + 1));
			if (!async_method.yields.IsNullOrEmpty<InstructionOffset>())
			{
				for (int i = 0; i < async_method.yields.Count; i++)
				{
					signature.WriteUInt32((uint)async_method.yields[i].Offset);
					signature.WriteUInt32((uint)async_method.resumes[i].Offset);
					signature.WriteCompressedUInt32(async_method.resume_methods[i].MetadataToken.RID);
				}
			}
			this.AddCustomDebugInformation(provider, async_method, signature);
		}

		// Token: 0x06000B7B RID: 2939 RVA: 0x000284E4 File Offset: 0x000266E4
		private void AddEmbeddedSourceDebugInformation(ICustomDebugInformationProvider provider, EmbeddedSourceDebugInformation embedded_source)
		{
			SignatureWriter signature = this.CreateSignatureWriter();
			if (!embedded_source.resolved)
			{
				signature.WriteBytes(embedded_source.ReadRawEmbeddedSourceDebugInformation());
				this.AddCustomDebugInformation(provider, embedded_source, signature);
				return;
			}
			byte[] content = embedded_source.content ?? Empty<byte>.Array;
			if (embedded_source.compress)
			{
				signature.WriteInt32(content.Length);
				MemoryStream decompressed_stream = new MemoryStream(content);
				MemoryStream content_stream = new MemoryStream();
				using (DeflateStream compress_stream = new DeflateStream(content_stream, CompressionMode.Compress, true))
				{
					decompressed_stream.CopyTo(compress_stream);
				}
				signature.WriteBytes(content_stream.ToArray());
			}
			else
			{
				signature.WriteInt32(0);
				signature.WriteBytes(content);
			}
			this.AddCustomDebugInformation(provider, embedded_source, signature);
		}

		// Token: 0x06000B7C RID: 2940 RVA: 0x0002859C File Offset: 0x0002679C
		private void AddSourceLinkDebugInformation(ICustomDebugInformationProvider provider, SourceLinkDebugInformation source_link)
		{
			SignatureWriter signature = this.CreateSignatureWriter();
			signature.WriteBytes(Encoding.UTF8.GetBytes(source_link.content));
			this.AddCustomDebugInformation(provider, source_link, signature);
		}

		// Token: 0x06000B7D RID: 2941 RVA: 0x000285CF File Offset: 0x000267CF
		private void AddCustomDebugInformation(ICustomDebugInformationProvider provider, CustomDebugInformation custom_info, SignatureWriter signature)
		{
			this.AddCustomDebugInformation(provider, custom_info, this.GetBlobIndex(signature));
		}

		// Token: 0x06000B7E RID: 2942 RVA: 0x000285E0 File Offset: 0x000267E0
		private void AddCustomDebugInformation(ICustomDebugInformationProvider provider, CustomDebugInformation custom_info, uint blob_index)
		{
			int rid = this.custom_debug_information_table.AddRow(new Row<uint, uint, uint>(MetadataBuilder.MakeCodedRID(provider.MetadataToken, CodedIndex.HasCustomDebugInformation), this.GetGuidIndex(custom_info.Identifier), blob_index));
			custom_info.token = new MetadataToken(TokenType.CustomDebugInformation, rid);
		}

		// Token: 0x06000B7F RID: 2943 RVA: 0x0002862C File Offset: 0x0002682C
		private uint AddImportScope(ImportDebugInformation import)
		{
			uint parent = 0U;
			if (import.Parent != null)
			{
				parent = this.AddImportScope(import.Parent);
			}
			uint targets_index = 0U;
			if (import.HasTargets)
			{
				SignatureWriter signature = this.CreateSignatureWriter();
				for (int i = 0; i < import.Targets.Count; i++)
				{
					this.AddImportTarget(import.Targets[i], signature);
				}
				targets_index = this.GetBlobIndex(signature);
			}
			Row<uint, uint> row = new Row<uint, uint>(parent, targets_index);
			MetadataToken import_token;
			if (this.import_scope_map.TryGetValue(row, out import_token))
			{
				return import_token.RID;
			}
			import_token = new MetadataToken(TokenType.ImportScope, this.import_scope_table.AddRow(row));
			this.import_scope_map.Add(row, import_token);
			return import_token.RID;
		}

		// Token: 0x06000B80 RID: 2944 RVA: 0x000286E8 File Offset: 0x000268E8
		private void AddImportTarget(ImportTarget target, SignatureWriter signature)
		{
			signature.WriteCompressedUInt32((uint)target.kind);
			switch (target.kind)
			{
			case ImportTargetKind.ImportNamespace:
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.@namespace));
				return;
			case ImportTargetKind.ImportNamespaceInAssembly:
				signature.WriteCompressedUInt32(target.reference.MetadataToken.RID);
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.@namespace));
				return;
			case ImportTargetKind.ImportType:
				signature.WriteTypeToken(target.type);
				return;
			case ImportTargetKind.ImportXmlNamespaceWithAlias:
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.alias));
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.@namespace));
				return;
			case ImportTargetKind.ImportAlias:
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.alias));
				return;
			case ImportTargetKind.DefineAssemblyAlias:
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.alias));
				signature.WriteCompressedUInt32(target.reference.MetadataToken.RID);
				return;
			case ImportTargetKind.DefineNamespaceAlias:
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.alias));
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.@namespace));
				return;
			case ImportTargetKind.DefineNamespaceInAssemblyAlias:
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.alias));
				signature.WriteCompressedUInt32(target.reference.MetadataToken.RID);
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.@namespace));
				return;
			case ImportTargetKind.DefineTypeAlias:
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(target.alias));
				signature.WriteTypeToken(target.type);
				return;
			default:
				return;
			}
		}

		// Token: 0x06000B81 RID: 2945 RVA: 0x00028866 File Offset: 0x00026A66
		private uint GetUTF8StringBlobIndex(string s)
		{
			return this.GetBlobIndex(Encoding.UTF8.GetBytes(s));
		}

		// Token: 0x06000B82 RID: 2946 RVA: 0x0002887C File Offset: 0x00026A7C
		public MetadataToken GetDocumentToken(Document document)
		{
			MetadataToken token;
			if (this.document_map.TryGetValue(document.Url, out token))
			{
				return token;
			}
			token = new MetadataToken(TokenType.Document, this.document_table.AddRow(new Row<uint, uint, uint, uint>(this.GetBlobIndex(this.GetDocumentNameSignature(document)), this.GetGuidIndex(document.HashAlgorithm.ToGuid()), this.GetBlobIndex(document.Hash), this.GetGuidIndex(document.Language.ToGuid()))));
			document.token = token;
			this.AddCustomDebugInformations(document);
			this.document_map.Add(document.Url, token);
			return token;
		}

		// Token: 0x06000B83 RID: 2947 RVA: 0x00028918 File Offset: 0x00026B18
		private SignatureWriter GetDocumentNameSignature(Document document)
		{
			string name = document.Url;
			SignatureWriter signature = this.CreateSignatureWriter();
			char separator;
			if (!MetadataBuilder.TryGetDocumentNameSeparator(name, out separator))
			{
				signature.WriteByte(0);
				signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(name));
				return signature;
			}
			signature.WriteByte((byte)separator);
			string[] parts = name.Split(new char[] { separator });
			for (int i = 0; i < parts.Length; i++)
			{
				if (parts[i] == string.Empty)
				{
					signature.WriteCompressedUInt32(0U);
				}
				else
				{
					signature.WriteCompressedUInt32(this.GetUTF8StringBlobIndex(parts[i]));
				}
			}
			return signature;
		}

		// Token: 0x06000B84 RID: 2948 RVA: 0x000289A8 File Offset: 0x00026BA8
		private static bool TryGetDocumentNameSeparator(string path, out char separator)
		{
			separator = '\0';
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}
			int unix_count = 0;
			int win_count = 0;
			for (int i = 0; i < path.Length; i++)
			{
				if (path[i] == '/')
				{
					unix_count++;
				}
				else if (path[i] == '\\')
				{
					win_count++;
				}
			}
			if (unix_count == 0 && win_count == 0)
			{
				return false;
			}
			if (unix_count >= win_count)
			{
				separator = '/';
				return true;
			}
			separator = '\\';
			return true;
		}

		// Token: 0x06000B85 RID: 2949 RVA: 0x00028A10 File Offset: 0x00026C10
		private void AddSequencePoints(MethodDebugInformation info)
		{
			uint rid = info.Method.MetadataToken.RID;
			Document document;
			if (info.TryGetUniqueDocument(out document))
			{
				this.method_debug_information_table.rows[(int)(rid - 1U)].Col1 = this.GetDocumentToken(document).RID;
			}
			SignatureWriter signature = this.CreateSignatureWriter();
			signature.WriteSequencePoints(info);
			this.method_debug_information_table.rows[(int)(rid - 1U)].Col2 = this.GetBlobIndex(signature);
		}

		// Token: 0x0400034F RID: 847
		internal readonly ModuleDefinition module;

		// Token: 0x04000350 RID: 848
		internal readonly ISymbolWriterProvider symbol_writer_provider;

		// Token: 0x04000351 RID: 849
		internal ISymbolWriter symbol_writer;

		// Token: 0x04000352 RID: 850
		internal readonly TextMap text_map;

		// Token: 0x04000353 RID: 851
		internal readonly string fq_name;

		// Token: 0x04000354 RID: 852
		internal readonly uint timestamp;

		// Token: 0x04000355 RID: 853
		private readonly Dictionary<Row<uint, uint, uint>, MetadataToken> type_ref_map;

		// Token: 0x04000356 RID: 854
		private readonly Dictionary<uint, MetadataToken> type_spec_map;

		// Token: 0x04000357 RID: 855
		private readonly Dictionary<Row<uint, uint, uint>, MetadataToken> member_ref_map;

		// Token: 0x04000358 RID: 856
		private readonly Dictionary<Row<uint, uint>, MetadataToken> method_spec_map;

		// Token: 0x04000359 RID: 857
		private readonly Collection<GenericParameter> generic_parameters;

		// Token: 0x0400035A RID: 858
		internal readonly CodeWriter code;

		// Token: 0x0400035B RID: 859
		internal readonly DataBuffer data;

		// Token: 0x0400035C RID: 860
		internal readonly ResourceBuffer resources;

		// Token: 0x0400035D RID: 861
		internal readonly StringHeapBuffer string_heap;

		// Token: 0x0400035E RID: 862
		internal readonly GuidHeapBuffer guid_heap;

		// Token: 0x0400035F RID: 863
		internal readonly UserStringHeapBuffer user_string_heap;

		// Token: 0x04000360 RID: 864
		internal readonly BlobHeapBuffer blob_heap;

		// Token: 0x04000361 RID: 865
		internal readonly TableHeapBuffer table_heap;

		// Token: 0x04000362 RID: 866
		internal readonly PdbHeapBuffer pdb_heap;

		// Token: 0x04000363 RID: 867
		internal MetadataToken entry_point;

		// Token: 0x04000364 RID: 868
		internal uint type_rid = 1U;

		// Token: 0x04000365 RID: 869
		internal uint field_rid = 1U;

		// Token: 0x04000366 RID: 870
		internal uint method_rid = 1U;

		// Token: 0x04000367 RID: 871
		internal uint param_rid = 1U;

		// Token: 0x04000368 RID: 872
		internal uint property_rid = 1U;

		// Token: 0x04000369 RID: 873
		internal uint event_rid = 1U;

		// Token: 0x0400036A RID: 874
		internal uint local_variable_rid = 1U;

		// Token: 0x0400036B RID: 875
		internal uint local_constant_rid = 1U;

		// Token: 0x0400036C RID: 876
		private readonly TypeRefTable type_ref_table;

		// Token: 0x0400036D RID: 877
		private readonly TypeDefTable type_def_table;

		// Token: 0x0400036E RID: 878
		private readonly FieldTable field_table;

		// Token: 0x0400036F RID: 879
		private readonly MethodTable method_table;

		// Token: 0x04000370 RID: 880
		private readonly ParamTable param_table;

		// Token: 0x04000371 RID: 881
		private readonly InterfaceImplTable iface_impl_table;

		// Token: 0x04000372 RID: 882
		private readonly MemberRefTable member_ref_table;

		// Token: 0x04000373 RID: 883
		private readonly ConstantTable constant_table;

		// Token: 0x04000374 RID: 884
		private readonly CustomAttributeTable custom_attribute_table;

		// Token: 0x04000375 RID: 885
		private readonly DeclSecurityTable declsec_table;

		// Token: 0x04000376 RID: 886
		private readonly StandAloneSigTable standalone_sig_table;

		// Token: 0x04000377 RID: 887
		private readonly EventMapTable event_map_table;

		// Token: 0x04000378 RID: 888
		private readonly EventTable event_table;

		// Token: 0x04000379 RID: 889
		private readonly PropertyMapTable property_map_table;

		// Token: 0x0400037A RID: 890
		private readonly PropertyTable property_table;

		// Token: 0x0400037B RID: 891
		private readonly TypeSpecTable typespec_table;

		// Token: 0x0400037C RID: 892
		private readonly MethodSpecTable method_spec_table;

		// Token: 0x0400037D RID: 893
		internal MetadataBuilder metadata_builder;

		// Token: 0x0400037E RID: 894
		private readonly DocumentTable document_table;

		// Token: 0x0400037F RID: 895
		private readonly MethodDebugInformationTable method_debug_information_table;

		// Token: 0x04000380 RID: 896
		private readonly LocalScopeTable local_scope_table;

		// Token: 0x04000381 RID: 897
		private readonly LocalVariableTable local_variable_table;

		// Token: 0x04000382 RID: 898
		private readonly LocalConstantTable local_constant_table;

		// Token: 0x04000383 RID: 899
		private readonly ImportScopeTable import_scope_table;

		// Token: 0x04000384 RID: 900
		private readonly StateMachineMethodTable state_machine_method_table;

		// Token: 0x04000385 RID: 901
		private readonly CustomDebugInformationTable custom_debug_information_table;

		// Token: 0x04000386 RID: 902
		private readonly Dictionary<Row<uint, uint>, MetadataToken> import_scope_map;

		// Token: 0x04000387 RID: 903
		private readonly Dictionary<string, MetadataToken> document_map;

		// Token: 0x02000224 RID: 548
		private sealed class GenericParameterComparer : IComparer<GenericParameter>
		{
			// Token: 0x06000B86 RID: 2950 RVA: 0x00028A90 File Offset: 0x00026C90
			public int Compare(GenericParameter a, GenericParameter b)
			{
				uint a_owner = MetadataBuilder.MakeCodedRID(a.Owner, CodedIndex.TypeOrMethodDef);
				uint b_owner = MetadataBuilder.MakeCodedRID(b.Owner, CodedIndex.TypeOrMethodDef);
				if (a_owner == b_owner)
				{
					int a_pos = a.Position;
					int b_pos = b.Position;
					if (a_pos == b_pos)
					{
						return 0;
					}
					if (a_pos <= b_pos)
					{
						return -1;
					}
					return 1;
				}
				else
				{
					if (a_owner <= b_owner)
					{
						return -1;
					}
					return 1;
				}
			}
		}
	}
}
