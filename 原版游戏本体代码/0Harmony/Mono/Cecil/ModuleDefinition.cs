using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200027B RID: 635
	internal sealed class ModuleDefinition : ModuleReference, ICustomAttributeProvider, IMetadataTokenProvider, ICustomDebugInformationProvider, IDisposable
	{
		// Token: 0x170003D0 RID: 976
		// (get) Token: 0x06000F41 RID: 3905 RVA: 0x0002FFFE File Offset: 0x0002E1FE
		public bool IsMain
		{
			get
			{
				return this.kind != ModuleKind.NetModule;
			}
		}

		// Token: 0x170003D1 RID: 977
		// (get) Token: 0x06000F42 RID: 3906 RVA: 0x0003000C File Offset: 0x0002E20C
		// (set) Token: 0x06000F43 RID: 3907 RVA: 0x00030014 File Offset: 0x0002E214
		public ModuleKind Kind
		{
			get
			{
				return this.kind;
			}
			set
			{
				this.kind = value;
			}
		}

		// Token: 0x170003D2 RID: 978
		// (get) Token: 0x06000F44 RID: 3908 RVA: 0x0003001D File Offset: 0x0002E21D
		// (set) Token: 0x06000F45 RID: 3909 RVA: 0x00030025 File Offset: 0x0002E225
		public MetadataKind MetadataKind
		{
			get
			{
				return this.metadata_kind;
			}
			set
			{
				this.metadata_kind = value;
			}
		}

		// Token: 0x170003D3 RID: 979
		// (get) Token: 0x06000F46 RID: 3910 RVA: 0x0003002E File Offset: 0x0002E22E
		internal WindowsRuntimeProjections Projections
		{
			get
			{
				if (this.projections == null)
				{
					Interlocked.CompareExchange<WindowsRuntimeProjections>(ref this.projections, new WindowsRuntimeProjections(this), null);
				}
				return this.projections;
			}
		}

		// Token: 0x170003D4 RID: 980
		// (get) Token: 0x06000F47 RID: 3911 RVA: 0x00030051 File Offset: 0x0002E251
		// (set) Token: 0x06000F48 RID: 3912 RVA: 0x00030059 File Offset: 0x0002E259
		public TargetRuntime Runtime
		{
			get
			{
				return this.runtime;
			}
			set
			{
				this.runtime = value;
				this.runtime_version = this.runtime.RuntimeVersionString();
			}
		}

		// Token: 0x170003D5 RID: 981
		// (get) Token: 0x06000F49 RID: 3913 RVA: 0x00030073 File Offset: 0x0002E273
		// (set) Token: 0x06000F4A RID: 3914 RVA: 0x0003007B File Offset: 0x0002E27B
		public string RuntimeVersion
		{
			get
			{
				return this.runtime_version;
			}
			set
			{
				this.runtime_version = value;
				this.runtime = this.runtime_version.ParseRuntime();
			}
		}

		// Token: 0x170003D6 RID: 982
		// (get) Token: 0x06000F4B RID: 3915 RVA: 0x00030095 File Offset: 0x0002E295
		// (set) Token: 0x06000F4C RID: 3916 RVA: 0x0003009D File Offset: 0x0002E29D
		public TargetArchitecture Architecture
		{
			get
			{
				return this.architecture;
			}
			set
			{
				this.architecture = value;
			}
		}

		// Token: 0x170003D7 RID: 983
		// (get) Token: 0x06000F4D RID: 3917 RVA: 0x000300A6 File Offset: 0x0002E2A6
		// (set) Token: 0x06000F4E RID: 3918 RVA: 0x000300AE File Offset: 0x0002E2AE
		public ModuleAttributes Attributes
		{
			get
			{
				return this.attributes;
			}
			set
			{
				this.attributes = value;
			}
		}

		// Token: 0x170003D8 RID: 984
		// (get) Token: 0x06000F4F RID: 3919 RVA: 0x000300B7 File Offset: 0x0002E2B7
		// (set) Token: 0x06000F50 RID: 3920 RVA: 0x000300BF File Offset: 0x0002E2BF
		public ModuleCharacteristics Characteristics
		{
			get
			{
				return this.characteristics;
			}
			set
			{
				this.characteristics = value;
			}
		}

		// Token: 0x170003D9 RID: 985
		// (get) Token: 0x06000F51 RID: 3921 RVA: 0x000300C8 File Offset: 0x0002E2C8
		[Obsolete("Use FileName")]
		public string FullyQualifiedName
		{
			get
			{
				return this.file_name;
			}
		}

		// Token: 0x170003DA RID: 986
		// (get) Token: 0x06000F52 RID: 3922 RVA: 0x000300C8 File Offset: 0x0002E2C8
		public string FileName
		{
			get
			{
				return this.file_name;
			}
		}

		// Token: 0x170003DB RID: 987
		// (get) Token: 0x06000F53 RID: 3923 RVA: 0x000300D0 File Offset: 0x0002E2D0
		// (set) Token: 0x06000F54 RID: 3924 RVA: 0x000300D8 File Offset: 0x0002E2D8
		public Guid Mvid
		{
			get
			{
				return this.mvid;
			}
			set
			{
				this.mvid = value;
			}
		}

		// Token: 0x170003DC RID: 988
		// (get) Token: 0x06000F55 RID: 3925 RVA: 0x000300E1 File Offset: 0x0002E2E1
		internal bool HasImage
		{
			get
			{
				return this.Image != null;
			}
		}

		// Token: 0x170003DD RID: 989
		// (get) Token: 0x06000F56 RID: 3926 RVA: 0x000300EC File Offset: 0x0002E2EC
		public bool HasSymbols
		{
			get
			{
				return this.symbol_reader != null;
			}
		}

		// Token: 0x170003DE RID: 990
		// (get) Token: 0x06000F57 RID: 3927 RVA: 0x000300F7 File Offset: 0x0002E2F7
		public ISymbolReader SymbolReader
		{
			get
			{
				return this.symbol_reader;
			}
		}

		// Token: 0x170003DF RID: 991
		// (get) Token: 0x06000F58 RID: 3928 RVA: 0x0001EBDB File Offset: 0x0001CDDB
		public override MetadataScopeType MetadataScopeType
		{
			get
			{
				return MetadataScopeType.ModuleDefinition;
			}
		}

		// Token: 0x170003E0 RID: 992
		// (get) Token: 0x06000F59 RID: 3929 RVA: 0x000300FF File Offset: 0x0002E2FF
		public AssemblyDefinition Assembly
		{
			get
			{
				return this.assembly;
			}
		}

		// Token: 0x170003E1 RID: 993
		// (get) Token: 0x06000F5A RID: 3930 RVA: 0x00030107 File Offset: 0x0002E307
		internal IReflectionImporter ReflectionImporter
		{
			get
			{
				if (this.reflection_importer == null)
				{
					Interlocked.CompareExchange<IReflectionImporter>(ref this.reflection_importer, new DefaultReflectionImporter(this), null);
				}
				return this.reflection_importer;
			}
		}

		// Token: 0x170003E2 RID: 994
		// (get) Token: 0x06000F5B RID: 3931 RVA: 0x0003012A File Offset: 0x0002E32A
		internal IMetadataImporter MetadataImporter
		{
			get
			{
				if (this.metadata_importer == null)
				{
					Interlocked.CompareExchange<IMetadataImporter>(ref this.metadata_importer, new DefaultMetadataImporter(this), null);
				}
				return this.metadata_importer;
			}
		}

		// Token: 0x170003E3 RID: 995
		// (get) Token: 0x06000F5C RID: 3932 RVA: 0x00030150 File Offset: 0x0002E350
		public IAssemblyResolver AssemblyResolver
		{
			get
			{
				if (this.assembly_resolver.value == null)
				{
					object obj = this.module_lock;
					lock (obj)
					{
						this.assembly_resolver = Disposable.Owned<IAssemblyResolver>(new DefaultAssemblyResolver());
					}
				}
				return this.assembly_resolver.value;
			}
		}

		// Token: 0x170003E4 RID: 996
		// (get) Token: 0x06000F5D RID: 3933 RVA: 0x000301B4 File Offset: 0x0002E3B4
		public IMetadataResolver MetadataResolver
		{
			get
			{
				if (this.metadata_resolver == null)
				{
					Interlocked.CompareExchange<IMetadataResolver>(ref this.metadata_resolver, new MetadataResolver(this.AssemblyResolver), null);
				}
				return this.metadata_resolver;
			}
		}

		// Token: 0x170003E5 RID: 997
		// (get) Token: 0x06000F5E RID: 3934 RVA: 0x000301DC File Offset: 0x0002E3DC
		public TypeSystem TypeSystem
		{
			get
			{
				if (this.type_system == null)
				{
					Interlocked.CompareExchange<TypeSystem>(ref this.type_system, TypeSystem.CreateTypeSystem(this), null);
				}
				return this.type_system;
			}
		}

		// Token: 0x170003E6 RID: 998
		// (get) Token: 0x06000F5F RID: 3935 RVA: 0x000301FF File Offset: 0x0002E3FF
		public bool HasAssemblyReferences
		{
			get
			{
				if (this.references != null)
				{
					return this.references.Count > 0;
				}
				return this.HasImage && this.Image.HasTable(Table.AssemblyRef);
			}
		}

		// Token: 0x170003E7 RID: 999
		// (get) Token: 0x06000F60 RID: 3936 RVA: 0x00030230 File Offset: 0x0002E430
		public Collection<AssemblyNameReference> AssemblyReferences
		{
			get
			{
				if (this.references != null)
				{
					return this.references;
				}
				if (this.HasImage)
				{
					return this.Read<ModuleDefinition, Collection<AssemblyNameReference>>(ref this.references, this, (ModuleDefinition _, MetadataReader reader) => reader.ReadAssemblyReferences());
				}
				Interlocked.CompareExchange<Collection<AssemblyNameReference>>(ref this.references, new Collection<AssemblyNameReference>(), null);
				return this.references;
			}
		}

		// Token: 0x170003E8 RID: 1000
		// (get) Token: 0x06000F61 RID: 3937 RVA: 0x00030299 File Offset: 0x0002E499
		public bool HasModuleReferences
		{
			get
			{
				if (this.modules != null)
				{
					return this.modules.Count > 0;
				}
				return this.HasImage && this.Image.HasTable(Table.ModuleRef);
			}
		}

		// Token: 0x170003E9 RID: 1001
		// (get) Token: 0x06000F62 RID: 3938 RVA: 0x000302CC File Offset: 0x0002E4CC
		public Collection<ModuleReference> ModuleReferences
		{
			get
			{
				if (this.modules != null)
				{
					return this.modules;
				}
				if (this.HasImage)
				{
					return this.Read<ModuleDefinition, Collection<ModuleReference>>(ref this.modules, this, (ModuleDefinition _, MetadataReader reader) => reader.ReadModuleReferences());
				}
				Interlocked.CompareExchange<Collection<ModuleReference>>(ref this.modules, new Collection<ModuleReference>(), null);
				return this.modules;
			}
		}

		// Token: 0x170003EA RID: 1002
		// (get) Token: 0x06000F63 RID: 3939 RVA: 0x00030338 File Offset: 0x0002E538
		public bool HasResources
		{
			get
			{
				if (this.resources != null)
				{
					return this.resources.Count > 0;
				}
				if (!this.HasImage)
				{
					return false;
				}
				if (!this.Image.HasTable(Table.ManifestResource))
				{
					return this.Read<ModuleDefinition, bool>(this, (ModuleDefinition _, MetadataReader reader) => reader.HasFileResource());
				}
				return true;
			}
		}

		// Token: 0x170003EB RID: 1003
		// (get) Token: 0x06000F64 RID: 3940 RVA: 0x000303A0 File Offset: 0x0002E5A0
		public Collection<Resource> Resources
		{
			get
			{
				if (this.resources != null)
				{
					return this.resources;
				}
				if (this.HasImage)
				{
					return this.Read<ModuleDefinition, Collection<Resource>>(ref this.resources, this, (ModuleDefinition _, MetadataReader reader) => reader.ReadResources());
				}
				Interlocked.CompareExchange<Collection<Resource>>(ref this.resources, new Collection<Resource>(), null);
				return this.resources;
			}
		}

		// Token: 0x170003EC RID: 1004
		// (get) Token: 0x06000F65 RID: 3941 RVA: 0x00030409 File Offset: 0x0002E609
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.GetHasCustomAttributes(this);
			}
		}

		// Token: 0x170003ED RID: 1005
		// (get) Token: 0x06000F66 RID: 3942 RVA: 0x00030429 File Offset: 0x0002E629
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this);
			}
		}

		// Token: 0x170003EE RID: 1006
		// (get) Token: 0x06000F67 RID: 3943 RVA: 0x00030442 File Offset: 0x0002E642
		public bool HasTypes
		{
			get
			{
				if (this.types != null)
				{
					return this.types.Count > 0;
				}
				return this.HasImage && this.Image.HasTable(Table.TypeDef);
			}
		}

		// Token: 0x170003EF RID: 1007
		// (get) Token: 0x06000F68 RID: 3944 RVA: 0x00030474 File Offset: 0x0002E674
		public Collection<TypeDefinition> Types
		{
			get
			{
				if (this.types != null)
				{
					return this.types;
				}
				if (this.HasImage)
				{
					return this.Read<ModuleDefinition, TypeDefinitionCollection>(ref this.types, this, (ModuleDefinition _, MetadataReader reader) => reader.ReadTypes());
				}
				Interlocked.CompareExchange<TypeDefinitionCollection>(ref this.types, new TypeDefinitionCollection(this), null);
				return this.types;
			}
		}

		// Token: 0x170003F0 RID: 1008
		// (get) Token: 0x06000F69 RID: 3945 RVA: 0x000304DE File Offset: 0x0002E6DE
		public bool HasExportedTypes
		{
			get
			{
				if (this.exported_types != null)
				{
					return this.exported_types.Count > 0;
				}
				return this.HasImage && this.Image.HasTable(Table.ExportedType);
			}
		}

		// Token: 0x170003F1 RID: 1009
		// (get) Token: 0x06000F6A RID: 3946 RVA: 0x00030510 File Offset: 0x0002E710
		public Collection<ExportedType> ExportedTypes
		{
			get
			{
				if (this.exported_types != null)
				{
					return this.exported_types;
				}
				if (this.HasImage)
				{
					return this.Read<ModuleDefinition, Collection<ExportedType>>(ref this.exported_types, this, (ModuleDefinition _, MetadataReader reader) => reader.ReadExportedTypes());
				}
				Interlocked.CompareExchange<Collection<ExportedType>>(ref this.exported_types, new Collection<ExportedType>(), null);
				return this.exported_types;
			}
		}

		// Token: 0x170003F2 RID: 1010
		// (get) Token: 0x06000F6B RID: 3947 RVA: 0x0003057C File Offset: 0x0002E77C
		// (set) Token: 0x06000F6C RID: 3948 RVA: 0x000305E3 File Offset: 0x0002E7E3
		public MethodDefinition EntryPoint
		{
			get
			{
				if (this.entry_point_set)
				{
					return this.entry_point;
				}
				if (this.HasImage)
				{
					this.Read<ModuleDefinition, MethodDefinition>(ref this.entry_point, this, (ModuleDefinition _, MetadataReader reader) => reader.ReadEntryPoint());
				}
				else
				{
					this.entry_point = null;
				}
				this.entry_point_set = true;
				return this.entry_point;
			}
			set
			{
				this.entry_point = value;
				this.entry_point_set = true;
			}
		}

		// Token: 0x170003F3 RID: 1011
		// (get) Token: 0x06000F6D RID: 3949 RVA: 0x000305F3 File Offset: 0x0002E7F3
		public bool HasCustomDebugInformations
		{
			get
			{
				return this.custom_infos != null && this.custom_infos.Count > 0;
			}
		}

		// Token: 0x170003F4 RID: 1012
		// (get) Token: 0x06000F6E RID: 3950 RVA: 0x0003060D File Offset: 0x0002E80D
		public Collection<CustomDebugInformation> CustomDebugInformations
		{
			get
			{
				if (this.custom_infos == null)
				{
					Interlocked.CompareExchange<Collection<CustomDebugInformation>>(ref this.custom_infos, new Collection<CustomDebugInformation>(), null);
				}
				return this.custom_infos;
			}
		}

		// Token: 0x06000F6F RID: 3951 RVA: 0x0003062F File Offset: 0x0002E82F
		internal ModuleDefinition()
		{
			this.MetadataSystem = new MetadataSystem();
			this.token = new MetadataToken(TokenType.Module, 1);
		}

		// Token: 0x06000F70 RID: 3952 RVA: 0x00030668 File Offset: 0x0002E868
		internal ModuleDefinition(Image image)
			: this()
		{
			this.Image = image;
			this.kind = image.Kind;
			this.RuntimeVersion = image.RuntimeVersion;
			this.architecture = image.Architecture;
			this.attributes = image.Attributes;
			this.characteristics = image.DllCharacteristics;
			this.linker_version = image.LinkerVersion;
			this.subsystem_major = image.SubSystemMajor;
			this.subsystem_minor = image.SubSystemMinor;
			this.file_name = image.FileName;
			this.timestamp = image.Timestamp;
			this.reader = new MetadataReader(this);
		}

		// Token: 0x06000F71 RID: 3953 RVA: 0x00030706 File Offset: 0x0002E906
		public void Dispose()
		{
			if (this.Image != null)
			{
				this.Image.Dispose();
			}
			if (this.symbol_reader != null)
			{
				this.symbol_reader.Dispose();
			}
			if (this.assembly_resolver.value != null)
			{
				this.assembly_resolver.Dispose();
			}
		}

		// Token: 0x06000F72 RID: 3954 RVA: 0x00030746 File Offset: 0x0002E946
		public bool HasTypeReference(string fullName)
		{
			return this.HasTypeReference(string.Empty, fullName);
		}

		// Token: 0x06000F73 RID: 3955 RVA: 0x00030754 File Offset: 0x0002E954
		public bool HasTypeReference(string scope, string fullName)
		{
			Mixin.CheckFullName(fullName);
			return this.HasImage && this.GetTypeReference(scope, fullName) != null;
		}

		// Token: 0x06000F74 RID: 3956 RVA: 0x00030771 File Offset: 0x0002E971
		public bool TryGetTypeReference(string fullName, out TypeReference type)
		{
			return this.TryGetTypeReference(string.Empty, fullName, out type);
		}

		// Token: 0x06000F75 RID: 3957 RVA: 0x00030780 File Offset: 0x0002E980
		public bool TryGetTypeReference(string scope, string fullName, out TypeReference type)
		{
			Mixin.CheckFullName(fullName);
			if (!this.HasImage)
			{
				type = null;
				return false;
			}
			TypeReference typeReference;
			type = (typeReference = this.GetTypeReference(scope, fullName));
			return typeReference != null;
		}

		// Token: 0x06000F76 RID: 3958 RVA: 0x000307B0 File Offset: 0x0002E9B0
		private TypeReference GetTypeReference(string scope, string fullname)
		{
			return this.Read<Row<string, string>, TypeReference>(new Row<string, string>(scope, fullname), (Row<string, string> row, MetadataReader reader) => reader.GetTypeReference(row.Col1, row.Col2));
		}

		// Token: 0x06000F77 RID: 3959 RVA: 0x000307DE File Offset: 0x0002E9DE
		public IEnumerable<TypeReference> GetTypeReferences()
		{
			if (!this.HasImage)
			{
				return Empty<TypeReference>.Array;
			}
			return this.Read<ModuleDefinition, IEnumerable<TypeReference>>(this, (ModuleDefinition _, MetadataReader reader) => reader.GetTypeReferences());
		}

		// Token: 0x06000F78 RID: 3960 RVA: 0x00030814 File Offset: 0x0002EA14
		public IEnumerable<MemberReference> GetMemberReferences()
		{
			if (!this.HasImage)
			{
				return Empty<MemberReference>.Array;
			}
			return this.Read<ModuleDefinition, IEnumerable<MemberReference>>(this, (ModuleDefinition _, MetadataReader reader) => reader.GetMemberReferences());
		}

		// Token: 0x06000F79 RID: 3961 RVA: 0x0003084A File Offset: 0x0002EA4A
		public IEnumerable<CustomAttribute> GetCustomAttributes()
		{
			if (!this.HasImage)
			{
				return Empty<CustomAttribute>.Array;
			}
			return this.Read<ModuleDefinition, IEnumerable<CustomAttribute>>(this, (ModuleDefinition _, MetadataReader reader) => reader.GetCustomAttributes());
		}

		// Token: 0x06000F7A RID: 3962 RVA: 0x00030880 File Offset: 0x0002EA80
		public TypeReference GetType(string fullName, bool runtimeName)
		{
			if (!runtimeName)
			{
				return this.GetType(fullName);
			}
			return TypeParser.ParseType(this, fullName, true);
		}

		// Token: 0x06000F7B RID: 3963 RVA: 0x00030895 File Offset: 0x0002EA95
		public TypeDefinition GetType(string fullName)
		{
			Mixin.CheckFullName(fullName);
			if (fullName.IndexOf('/') > 0)
			{
				return this.GetNestedType(fullName);
			}
			return ((TypeDefinitionCollection)this.Types).GetType(fullName);
		}

		// Token: 0x06000F7C RID: 3964 RVA: 0x000308C1 File Offset: 0x0002EAC1
		public TypeDefinition GetType(string @namespace, string name)
		{
			Mixin.CheckName(name);
			return ((TypeDefinitionCollection)this.Types).GetType(@namespace ?? string.Empty, name);
		}

		// Token: 0x06000F7D RID: 3965 RVA: 0x000308E4 File Offset: 0x0002EAE4
		public IEnumerable<TypeDefinition> GetTypes()
		{
			return ModuleDefinition.GetTypes(this.Types);
		}

		// Token: 0x06000F7E RID: 3966 RVA: 0x000308F1 File Offset: 0x0002EAF1
		private static IEnumerable<TypeDefinition> GetTypes(Collection<TypeDefinition> types)
		{
			int num;
			for (int i = 0; i < types.Count; i = num + 1)
			{
				TypeDefinition type = types[i];
				yield return type;
				if (type.HasNestedTypes)
				{
					foreach (TypeDefinition nested in ModuleDefinition.GetTypes(type.NestedTypes))
					{
						yield return nested;
					}
					IEnumerator<TypeDefinition> enumerator = null;
					type = null;
				}
				num = i;
			}
			yield break;
			yield break;
		}

		// Token: 0x06000F7F RID: 3967 RVA: 0x00030904 File Offset: 0x0002EB04
		private TypeDefinition GetNestedType(string fullname)
		{
			string[] names = fullname.Split(new char[] { '/' });
			TypeDefinition type = this.GetType(names[0]);
			if (type == null)
			{
				return null;
			}
			for (int i = 1; i < names.Length; i++)
			{
				TypeDefinition nested_type = type.GetNestedType(names[i]);
				if (nested_type == null)
				{
					return null;
				}
				type = nested_type;
			}
			return type;
		}

		// Token: 0x06000F80 RID: 3968 RVA: 0x00030952 File Offset: 0x0002EB52
		internal FieldDefinition Resolve(FieldReference field)
		{
			return this.MetadataResolver.Resolve(field);
		}

		// Token: 0x06000F81 RID: 3969 RVA: 0x00030960 File Offset: 0x0002EB60
		internal MethodDefinition Resolve(MethodReference method)
		{
			return this.MetadataResolver.Resolve(method);
		}

		// Token: 0x06000F82 RID: 3970 RVA: 0x0003096E File Offset: 0x0002EB6E
		internal TypeDefinition Resolve(TypeReference type)
		{
			return this.MetadataResolver.Resolve(type);
		}

		// Token: 0x06000F83 RID: 3971 RVA: 0x0003097C File Offset: 0x0002EB7C
		private static void CheckContext(IGenericParameterProvider context, ModuleDefinition module)
		{
			if (context == null)
			{
				return;
			}
			if (context.Module != module)
			{
				throw new ArgumentException();
			}
		}

		// Token: 0x06000F84 RID: 3972 RVA: 0x00030991 File Offset: 0x0002EB91
		[Obsolete("Use ImportReference", false)]
		public TypeReference Import(Type type)
		{
			return this.ImportReference(type, null);
		}

		// Token: 0x06000F85 RID: 3973 RVA: 0x00030991 File Offset: 0x0002EB91
		public TypeReference ImportReference(Type type)
		{
			return this.ImportReference(type, null);
		}

		// Token: 0x06000F86 RID: 3974 RVA: 0x0003099B File Offset: 0x0002EB9B
		[Obsolete("Use ImportReference", false)]
		public TypeReference Import(Type type, IGenericParameterProvider context)
		{
			return this.ImportReference(type, context);
		}

		// Token: 0x06000F87 RID: 3975 RVA: 0x000309A5 File Offset: 0x0002EBA5
		public TypeReference ImportReference(Type type, IGenericParameterProvider context)
		{
			Mixin.CheckType(type);
			ModuleDefinition.CheckContext(context, this);
			return this.ReflectionImporter.ImportReference(type, context);
		}

		// Token: 0x06000F88 RID: 3976 RVA: 0x000309C1 File Offset: 0x0002EBC1
		[Obsolete("Use ImportReference", false)]
		public FieldReference Import(FieldInfo field)
		{
			return this.ImportReference(field, null);
		}

		// Token: 0x06000F89 RID: 3977 RVA: 0x000309CB File Offset: 0x0002EBCB
		[Obsolete("Use ImportReference", false)]
		public FieldReference Import(FieldInfo field, IGenericParameterProvider context)
		{
			return this.ImportReference(field, context);
		}

		// Token: 0x06000F8A RID: 3978 RVA: 0x000309C1 File Offset: 0x0002EBC1
		public FieldReference ImportReference(FieldInfo field)
		{
			return this.ImportReference(field, null);
		}

		// Token: 0x06000F8B RID: 3979 RVA: 0x000309D5 File Offset: 0x0002EBD5
		public FieldReference ImportReference(FieldInfo field, IGenericParameterProvider context)
		{
			Mixin.CheckField(field);
			ModuleDefinition.CheckContext(context, this);
			return this.ReflectionImporter.ImportReference(field, context);
		}

		// Token: 0x06000F8C RID: 3980 RVA: 0x000309F1 File Offset: 0x0002EBF1
		[Obsolete("Use ImportReference", false)]
		public MethodReference Import(MethodBase method)
		{
			return this.ImportReference(method, null);
		}

		// Token: 0x06000F8D RID: 3981 RVA: 0x000309FB File Offset: 0x0002EBFB
		[Obsolete("Use ImportReference", false)]
		public MethodReference Import(MethodBase method, IGenericParameterProvider context)
		{
			return this.ImportReference(method, context);
		}

		// Token: 0x06000F8E RID: 3982 RVA: 0x000309F1 File Offset: 0x0002EBF1
		public MethodReference ImportReference(MethodBase method)
		{
			return this.ImportReference(method, null);
		}

		// Token: 0x06000F8F RID: 3983 RVA: 0x00030A05 File Offset: 0x0002EC05
		public MethodReference ImportReference(MethodBase method, IGenericParameterProvider context)
		{
			Mixin.CheckMethod(method);
			ModuleDefinition.CheckContext(context, this);
			return this.ReflectionImporter.ImportReference(method, context);
		}

		// Token: 0x06000F90 RID: 3984 RVA: 0x00030A21 File Offset: 0x0002EC21
		[Obsolete("Use ImportReference", false)]
		public TypeReference Import(TypeReference type)
		{
			return this.ImportReference(type, null);
		}

		// Token: 0x06000F91 RID: 3985 RVA: 0x00030A2B File Offset: 0x0002EC2B
		[Obsolete("Use ImportReference", false)]
		public TypeReference Import(TypeReference type, IGenericParameterProvider context)
		{
			return this.ImportReference(type, context);
		}

		// Token: 0x06000F92 RID: 3986 RVA: 0x00030A21 File Offset: 0x0002EC21
		public TypeReference ImportReference(TypeReference type)
		{
			return this.ImportReference(type, null);
		}

		// Token: 0x06000F93 RID: 3987 RVA: 0x00030A35 File Offset: 0x0002EC35
		public TypeReference ImportReference(TypeReference type, IGenericParameterProvider context)
		{
			Mixin.CheckType(type);
			if (type.Module == this)
			{
				return type;
			}
			ModuleDefinition.CheckContext(context, this);
			return this.MetadataImporter.ImportReference(type, context);
		}

		// Token: 0x06000F94 RID: 3988 RVA: 0x00030A5C File Offset: 0x0002EC5C
		[Obsolete("Use ImportReference", false)]
		public FieldReference Import(FieldReference field)
		{
			return this.ImportReference(field, null);
		}

		// Token: 0x06000F95 RID: 3989 RVA: 0x00030A66 File Offset: 0x0002EC66
		[Obsolete("Use ImportReference", false)]
		public FieldReference Import(FieldReference field, IGenericParameterProvider context)
		{
			return this.ImportReference(field, context);
		}

		// Token: 0x06000F96 RID: 3990 RVA: 0x00030A5C File Offset: 0x0002EC5C
		public FieldReference ImportReference(FieldReference field)
		{
			return this.ImportReference(field, null);
		}

		// Token: 0x06000F97 RID: 3991 RVA: 0x00030A70 File Offset: 0x0002EC70
		public FieldReference ImportReference(FieldReference field, IGenericParameterProvider context)
		{
			Mixin.CheckField(field);
			if (field.Module == this)
			{
				return field;
			}
			ModuleDefinition.CheckContext(context, this);
			return this.MetadataImporter.ImportReference(field, context);
		}

		// Token: 0x06000F98 RID: 3992 RVA: 0x00030A97 File Offset: 0x0002EC97
		[Obsolete("Use ImportReference", false)]
		public MethodReference Import(MethodReference method)
		{
			return this.ImportReference(method, null);
		}

		// Token: 0x06000F99 RID: 3993 RVA: 0x00030AA1 File Offset: 0x0002ECA1
		[Obsolete("Use ImportReference", false)]
		public MethodReference Import(MethodReference method, IGenericParameterProvider context)
		{
			return this.ImportReference(method, context);
		}

		// Token: 0x06000F9A RID: 3994 RVA: 0x00030A97 File Offset: 0x0002EC97
		public MethodReference ImportReference(MethodReference method)
		{
			return this.ImportReference(method, null);
		}

		// Token: 0x06000F9B RID: 3995 RVA: 0x00030AAB File Offset: 0x0002ECAB
		public MethodReference ImportReference(MethodReference method, IGenericParameterProvider context)
		{
			Mixin.CheckMethod(method);
			if (method.Module == this)
			{
				return method;
			}
			ModuleDefinition.CheckContext(context, this);
			return this.MetadataImporter.ImportReference(method, context);
		}

		// Token: 0x06000F9C RID: 3996 RVA: 0x00030AD2 File Offset: 0x0002ECD2
		public IMetadataTokenProvider LookupToken(int token)
		{
			return this.LookupToken(new MetadataToken((uint)token));
		}

		// Token: 0x06000F9D RID: 3997 RVA: 0x00030AE0 File Offset: 0x0002ECE0
		public IMetadataTokenProvider LookupToken(MetadataToken token)
		{
			return this.Read<MetadataToken, IMetadataTokenProvider>(token, (MetadataToken t, MetadataReader reader) => reader.LookupToken(t));
		}

		// Token: 0x06000F9E RID: 3998 RVA: 0x00030B08 File Offset: 0x0002ED08
		public void ImmediateRead()
		{
			if (!this.HasImage)
			{
				return;
			}
			this.ReadingMode = ReadingMode.Immediate;
			new ImmediateModuleReader(this.Image).ReadModule(this, true);
		}

		// Token: 0x170003F5 RID: 1013
		// (get) Token: 0x06000F9F RID: 3999 RVA: 0x00030B2C File Offset: 0x0002ED2C
		internal object SyncRoot
		{
			get
			{
				return this.module_lock;
			}
		}

		// Token: 0x06000FA0 RID: 4000 RVA: 0x00030B34 File Offset: 0x0002ED34
		internal void Read<TItem>(TItem item, Action<TItem, MetadataReader> read)
		{
			object obj = this.module_lock;
			lock (obj)
			{
				int position = this.reader.position;
				IGenericContext context = this.reader.context;
				read(item, this.reader);
				this.reader.position = position;
				this.reader.context = context;
			}
		}

		// Token: 0x06000FA1 RID: 4001 RVA: 0x00030BAC File Offset: 0x0002EDAC
		internal TRet Read<TItem, TRet>(TItem item, Func<TItem, MetadataReader, TRet> read)
		{
			object obj = this.module_lock;
			TRet result;
			lock (obj)
			{
				int position = this.reader.position;
				IGenericContext context = this.reader.context;
				TRet tret = read(item, this.reader);
				this.reader.position = position;
				this.reader.context = context;
				result = tret;
			}
			return result;
		}

		// Token: 0x06000FA2 RID: 4002 RVA: 0x00030C28 File Offset: 0x0002EE28
		internal TRet Read<TItem, TRet>(ref TRet variable, TItem item, Func<TItem, MetadataReader, TRet> read) where TRet : class
		{
			object obj = this.module_lock;
			TRet result;
			lock (obj)
			{
				if (variable != null)
				{
					result = variable;
				}
				else
				{
					int position = this.reader.position;
					IGenericContext context = this.reader.context;
					TRet ret = read(item, this.reader);
					this.reader.position = position;
					this.reader.context = context;
					result = (variable = ret);
				}
			}
			return result;
		}

		// Token: 0x170003F6 RID: 1014
		// (get) Token: 0x06000FA3 RID: 4003 RVA: 0x00030CCC File Offset: 0x0002EECC
		public bool HasDebugHeader
		{
			get
			{
				return this.Image != null && this.Image.DebugHeader != null;
			}
		}

		// Token: 0x06000FA4 RID: 4004 RVA: 0x00030CE6 File Offset: 0x0002EEE6
		public ImageDebugHeader GetDebugHeader()
		{
			return this.Image.DebugHeader ?? new ImageDebugHeader();
		}

		// Token: 0x06000FA5 RID: 4005 RVA: 0x00030CFC File Offset: 0x0002EEFC
		public static ModuleDefinition CreateModule(string name, ModuleKind kind)
		{
			return ModuleDefinition.CreateModule(name, new ModuleParameters
			{
				Kind = kind
			});
		}

		// Token: 0x06000FA6 RID: 4006 RVA: 0x00030D10 File Offset: 0x0002EF10
		public static ModuleDefinition CreateModule(string name, ModuleParameters parameters)
		{
			Mixin.CheckName(name);
			Mixin.CheckParameters(parameters);
			ModuleDefinition module = new ModuleDefinition
			{
				Name = name,
				kind = parameters.Kind,
				timestamp = (parameters.Timestamp ?? Mixin.GetTimestamp()),
				Runtime = parameters.Runtime,
				architecture = parameters.Architecture,
				mvid = Guid.NewGuid(),
				Attributes = ModuleAttributes.ILOnly,
				Characteristics = (ModuleCharacteristics.DynamicBase | ModuleCharacteristics.NoSEH | ModuleCharacteristics.NXCompat | ModuleCharacteristics.TerminalServerAware)
			};
			if (parameters.AssemblyResolver != null)
			{
				module.assembly_resolver = Disposable.NotOwned<IAssemblyResolver>(parameters.AssemblyResolver);
			}
			if (parameters.MetadataResolver != null)
			{
				module.metadata_resolver = parameters.MetadataResolver;
			}
			if (parameters.MetadataImporterProvider != null)
			{
				module.metadata_importer = parameters.MetadataImporterProvider.GetMetadataImporter(module);
			}
			if (parameters.ReflectionImporterProvider != null)
			{
				module.reflection_importer = parameters.ReflectionImporterProvider.GetReflectionImporter(module);
			}
			if (parameters.Kind != ModuleKind.NetModule)
			{
				AssemblyDefinition assembly = new AssemblyDefinition();
				module.assembly = assembly;
				module.assembly.Name = ModuleDefinition.CreateAssemblyName(name);
				assembly.main_module = module;
			}
			module.Types.Add(new TypeDefinition(string.Empty, "<Module>", TypeAttributes.NotPublic));
			return module;
		}

		// Token: 0x06000FA7 RID: 4007 RVA: 0x00030E46 File Offset: 0x0002F046
		private static AssemblyNameDefinition CreateAssemblyName(string name)
		{
			if (name.EndsWith(".dll") || name.EndsWith(".exe"))
			{
				name = name.Substring(0, name.Length - 4);
			}
			return new AssemblyNameDefinition(name, Mixin.ZeroVersion);
		}

		// Token: 0x06000FA8 RID: 4008 RVA: 0x00030E80 File Offset: 0x0002F080
		public void ReadSymbols()
		{
			if (string.IsNullOrEmpty(this.file_name))
			{
				throw new InvalidOperationException();
			}
			DefaultSymbolReaderProvider provider = new DefaultSymbolReaderProvider(true);
			this.ReadSymbols(provider.GetSymbolReader(this, this.file_name), true);
		}

		// Token: 0x06000FA9 RID: 4009 RVA: 0x00030EBB File Offset: 0x0002F0BB
		public void ReadSymbols(ISymbolReader reader)
		{
			this.ReadSymbols(reader, true);
		}

		// Token: 0x06000FAA RID: 4010 RVA: 0x00030EC8 File Offset: 0x0002F0C8
		public void ReadSymbols(ISymbolReader reader, bool throwIfSymbolsAreNotMaching)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			this.symbol_reader = reader;
			if (this.symbol_reader.ProcessDebugHeader(this.GetDebugHeader()))
			{
				if (this.HasImage && this.ReadingMode == ReadingMode.Immediate)
				{
					new ImmediateModuleReader(this.Image).ReadSymbols(this);
				}
				return;
			}
			this.symbol_reader = null;
			if (throwIfSymbolsAreNotMaching)
			{
				throw new SymbolsNotMatchingException("Symbols were found but are not matching the assembly");
			}
		}

		// Token: 0x06000FAB RID: 4011 RVA: 0x00030F35 File Offset: 0x0002F135
		public static ModuleDefinition ReadModule(string fileName)
		{
			return ModuleDefinition.ReadModule(fileName, new ReaderParameters(ReadingMode.Deferred));
		}

		// Token: 0x06000FAC RID: 4012 RVA: 0x00030F44 File Offset: 0x0002F144
		public static ModuleDefinition ReadModule(string fileName, ReaderParameters parameters)
		{
			Stream stream = ModuleDefinition.GetFileStream(fileName, FileMode.Open, parameters.ReadWrite ? FileAccess.ReadWrite : FileAccess.Read, FileShare.Read);
			if (parameters.InMemory)
			{
				MemoryStream memory = new MemoryStream(stream.CanSeek ? ((int)stream.Length) : 0);
				using (stream)
				{
					stream.CopyTo(memory);
				}
				memory.Position = 0L;
				stream = memory;
			}
			ModuleDefinition result;
			try
			{
				result = ModuleDefinition.ReadModule(Disposable.Owned<Stream>(stream), fileName, parameters);
			}
			catch (Exception)
			{
				stream.Dispose();
				throw;
			}
			return result;
		}

		// Token: 0x06000FAD RID: 4013 RVA: 0x00030FDC File Offset: 0x0002F1DC
		private static Stream GetFileStream(string fileName, FileMode mode, FileAccess access, FileShare share)
		{
			Mixin.CheckFileName(fileName);
			return new FileStream(fileName, mode, access, share);
		}

		// Token: 0x06000FAE RID: 4014 RVA: 0x00030FED File Offset: 0x0002F1ED
		public static ModuleDefinition ReadModule(Stream stream)
		{
			return ModuleDefinition.ReadModule(stream, new ReaderParameters(ReadingMode.Deferred));
		}

		// Token: 0x06000FAF RID: 4015 RVA: 0x00030FFB File Offset: 0x0002F1FB
		public static ModuleDefinition ReadModule(Stream stream, ReaderParameters parameters)
		{
			Mixin.CheckStream(stream);
			Mixin.CheckReadSeek(stream);
			return ModuleDefinition.ReadModule(Disposable.NotOwned<Stream>(stream), stream.GetFileName(), parameters);
		}

		// Token: 0x06000FB0 RID: 4016 RVA: 0x0003101B File Offset: 0x0002F21B
		private static ModuleDefinition ReadModule(Disposable<Stream> stream, string fileName, ReaderParameters parameters)
		{
			Mixin.CheckParameters(parameters);
			return ModuleReader.CreateModule(ImageReader.ReadImage(stream, fileName), parameters);
		}

		// Token: 0x06000FB1 RID: 4017 RVA: 0x00031030 File Offset: 0x0002F230
		public void Write(string fileName)
		{
			this.Write(fileName, new WriterParameters());
		}

		// Token: 0x06000FB2 RID: 4018 RVA: 0x00031040 File Offset: 0x0002F240
		public void Write(string fileName, WriterParameters parameters)
		{
			Mixin.CheckParameters(parameters);
			Stream file = ModuleDefinition.GetFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
			ModuleWriter.WriteModule(this, Disposable.Owned<Stream>(file), parameters);
		}

		// Token: 0x06000FB3 RID: 4019 RVA: 0x0003106A File Offset: 0x0002F26A
		public void Write()
		{
			this.Write(new WriterParameters());
		}

		// Token: 0x06000FB4 RID: 4020 RVA: 0x00031077 File Offset: 0x0002F277
		public void Write(WriterParameters parameters)
		{
			if (!this.HasImage)
			{
				throw new InvalidOperationException();
			}
			this.Write(this.Image.Stream.value, parameters);
		}

		// Token: 0x06000FB5 RID: 4021 RVA: 0x0003109E File Offset: 0x0002F29E
		public void Write(Stream stream)
		{
			this.Write(stream, new WriterParameters());
		}

		// Token: 0x06000FB6 RID: 4022 RVA: 0x000310AC File Offset: 0x0002F2AC
		public void Write(Stream stream, WriterParameters parameters)
		{
			Mixin.CheckStream(stream);
			Mixin.CheckWriteSeek(stream);
			Mixin.CheckParameters(parameters);
			ModuleWriter.WriteModule(this, Disposable.NotOwned<Stream>(stream), parameters);
		}

		// Token: 0x040004B6 RID: 1206
		internal Image Image;

		// Token: 0x040004B7 RID: 1207
		internal MetadataSystem MetadataSystem;

		// Token: 0x040004B8 RID: 1208
		internal ReadingMode ReadingMode;

		// Token: 0x040004B9 RID: 1209
		internal ISymbolReaderProvider SymbolReaderProvider;

		// Token: 0x040004BA RID: 1210
		internal ISymbolReader symbol_reader;

		// Token: 0x040004BB RID: 1211
		internal Disposable<IAssemblyResolver> assembly_resolver;

		// Token: 0x040004BC RID: 1212
		internal IMetadataResolver metadata_resolver;

		// Token: 0x040004BD RID: 1213
		internal TypeSystem type_system;

		// Token: 0x040004BE RID: 1214
		internal readonly MetadataReader reader;

		// Token: 0x040004BF RID: 1215
		private readonly string file_name;

		// Token: 0x040004C0 RID: 1216
		internal string runtime_version;

		// Token: 0x040004C1 RID: 1217
		internal ModuleKind kind;

		// Token: 0x040004C2 RID: 1218
		private WindowsRuntimeProjections projections;

		// Token: 0x040004C3 RID: 1219
		private MetadataKind metadata_kind;

		// Token: 0x040004C4 RID: 1220
		private TargetRuntime runtime;

		// Token: 0x040004C5 RID: 1221
		private TargetArchitecture architecture;

		// Token: 0x040004C6 RID: 1222
		private ModuleAttributes attributes;

		// Token: 0x040004C7 RID: 1223
		private ModuleCharacteristics characteristics;

		// Token: 0x040004C8 RID: 1224
		private Guid mvid;

		// Token: 0x040004C9 RID: 1225
		internal ushort linker_version = 8;

		// Token: 0x040004CA RID: 1226
		internal ushort subsystem_major = 4;

		// Token: 0x040004CB RID: 1227
		internal ushort subsystem_minor;

		// Token: 0x040004CC RID: 1228
		internal uint timestamp;

		// Token: 0x040004CD RID: 1229
		internal AssemblyDefinition assembly;

		// Token: 0x040004CE RID: 1230
		private MethodDefinition entry_point;

		// Token: 0x040004CF RID: 1231
		private bool entry_point_set;

		// Token: 0x040004D0 RID: 1232
		internal IReflectionImporter reflection_importer;

		// Token: 0x040004D1 RID: 1233
		internal IMetadataImporter metadata_importer;

		// Token: 0x040004D2 RID: 1234
		private Collection<CustomAttribute> custom_attributes;

		// Token: 0x040004D3 RID: 1235
		private Collection<AssemblyNameReference> references;

		// Token: 0x040004D4 RID: 1236
		private Collection<ModuleReference> modules;

		// Token: 0x040004D5 RID: 1237
		private Collection<Resource> resources;

		// Token: 0x040004D6 RID: 1238
		private Collection<ExportedType> exported_types;

		// Token: 0x040004D7 RID: 1239
		private TypeDefinitionCollection types;

		// Token: 0x040004D8 RID: 1240
		internal Collection<CustomDebugInformation> custom_infos;

		// Token: 0x040004D9 RID: 1241
		internal MetadataBuilder metadata_builder;

		// Token: 0x040004DA RID: 1242
		private readonly object module_lock = new object();
	}
}
