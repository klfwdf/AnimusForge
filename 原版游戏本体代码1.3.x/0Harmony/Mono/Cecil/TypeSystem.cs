using System;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002AE RID: 686
	internal abstract class TypeSystem
	{
		// Token: 0x0600117D RID: 4477 RVA: 0x00034BDE File Offset: 0x00032DDE
		private TypeSystem(ModuleDefinition module)
		{
			this.module = module;
		}

		// Token: 0x0600117E RID: 4478 RVA: 0x00034BED File Offset: 0x00032DED
		internal static TypeSystem CreateTypeSystem(ModuleDefinition module)
		{
			if (module.IsCoreLibrary())
			{
				return new TypeSystem.CoreTypeSystem(module);
			}
			return new TypeSystem.CommonTypeSystem(module);
		}

		// Token: 0x0600117F RID: 4479
		internal abstract TypeReference LookupType(string @namespace, string name);

		// Token: 0x06001180 RID: 4480 RVA: 0x00034C04 File Offset: 0x00032E04
		private TypeReference LookupSystemType(ref TypeReference reference, string name, ElementType element_type)
		{
			object syncRoot = this.module.SyncRoot;
			TypeReference result;
			lock (syncRoot)
			{
				if (reference != null)
				{
					result = reference;
				}
				else
				{
					TypeReference type = this.LookupType("System", name);
					type.etype = element_type;
					TypeReference typeReference;
					reference = (typeReference = type);
					result = typeReference;
				}
			}
			return result;
		}

		// Token: 0x06001181 RID: 4481 RVA: 0x00034C6C File Offset: 0x00032E6C
		private TypeReference LookupSystemValueType(ref TypeReference typeRef, string name, ElementType element_type)
		{
			object syncRoot = this.module.SyncRoot;
			TypeReference result;
			lock (syncRoot)
			{
				if (typeRef != null)
				{
					result = typeRef;
				}
				else
				{
					TypeReference type = this.LookupType("System", name);
					type.etype = element_type;
					type.KnownValueType();
					TypeReference typeReference;
					typeRef = (typeReference = type);
					result = typeReference;
				}
			}
			return result;
		}

		// Token: 0x170004B4 RID: 1204
		// (get) Token: 0x06001182 RID: 4482 RVA: 0x00034CD8 File Offset: 0x00032ED8
		[Obsolete("Use CoreLibrary")]
		public IMetadataScope Corlib
		{
			get
			{
				return this.CoreLibrary;
			}
		}

		// Token: 0x170004B5 RID: 1205
		// (get) Token: 0x06001183 RID: 4483 RVA: 0x00034CE0 File Offset: 0x00032EE0
		public IMetadataScope CoreLibrary
		{
			get
			{
				TypeSystem.CommonTypeSystem common = this as TypeSystem.CommonTypeSystem;
				if (common == null)
				{
					return this.module;
				}
				return common.GetCoreLibraryReference();
			}
		}

		// Token: 0x170004B6 RID: 1206
		// (get) Token: 0x06001184 RID: 4484 RVA: 0x00034D04 File Offset: 0x00032F04
		public TypeReference Object
		{
			get
			{
				return this.type_object ?? this.LookupSystemType(ref this.type_object, "Object", ElementType.Object);
			}
		}

		// Token: 0x170004B7 RID: 1207
		// (get) Token: 0x06001185 RID: 4485 RVA: 0x00034D23 File Offset: 0x00032F23
		public TypeReference Void
		{
			get
			{
				return this.type_void ?? this.LookupSystemType(ref this.type_void, "Void", ElementType.Void);
			}
		}

		// Token: 0x170004B8 RID: 1208
		// (get) Token: 0x06001186 RID: 4486 RVA: 0x00034D41 File Offset: 0x00032F41
		public TypeReference Boolean
		{
			get
			{
				return this.type_bool ?? this.LookupSystemValueType(ref this.type_bool, "Boolean", ElementType.Boolean);
			}
		}

		// Token: 0x170004B9 RID: 1209
		// (get) Token: 0x06001187 RID: 4487 RVA: 0x00034D5F File Offset: 0x00032F5F
		public TypeReference Char
		{
			get
			{
				return this.type_char ?? this.LookupSystemValueType(ref this.type_char, "Char", ElementType.Char);
			}
		}

		// Token: 0x170004BA RID: 1210
		// (get) Token: 0x06001188 RID: 4488 RVA: 0x00034D7D File Offset: 0x00032F7D
		public TypeReference SByte
		{
			get
			{
				return this.type_sbyte ?? this.LookupSystemValueType(ref this.type_sbyte, "SByte", ElementType.I1);
			}
		}

		// Token: 0x170004BB RID: 1211
		// (get) Token: 0x06001189 RID: 4489 RVA: 0x00034D9B File Offset: 0x00032F9B
		public TypeReference Byte
		{
			get
			{
				return this.type_byte ?? this.LookupSystemValueType(ref this.type_byte, "Byte", ElementType.U1);
			}
		}

		// Token: 0x170004BC RID: 1212
		// (get) Token: 0x0600118A RID: 4490 RVA: 0x00034DB9 File Offset: 0x00032FB9
		public TypeReference Int16
		{
			get
			{
				return this.type_int16 ?? this.LookupSystemValueType(ref this.type_int16, "Int16", ElementType.I2);
			}
		}

		// Token: 0x170004BD RID: 1213
		// (get) Token: 0x0600118B RID: 4491 RVA: 0x00034DD7 File Offset: 0x00032FD7
		public TypeReference UInt16
		{
			get
			{
				return this.type_uint16 ?? this.LookupSystemValueType(ref this.type_uint16, "UInt16", ElementType.U2);
			}
		}

		// Token: 0x170004BE RID: 1214
		// (get) Token: 0x0600118C RID: 4492 RVA: 0x00034DF5 File Offset: 0x00032FF5
		public TypeReference Int32
		{
			get
			{
				return this.type_int32 ?? this.LookupSystemValueType(ref this.type_int32, "Int32", ElementType.I4);
			}
		}

		// Token: 0x170004BF RID: 1215
		// (get) Token: 0x0600118D RID: 4493 RVA: 0x00034E13 File Offset: 0x00033013
		public TypeReference UInt32
		{
			get
			{
				return this.type_uint32 ?? this.LookupSystemValueType(ref this.type_uint32, "UInt32", ElementType.U4);
			}
		}

		// Token: 0x170004C0 RID: 1216
		// (get) Token: 0x0600118E RID: 4494 RVA: 0x00034E32 File Offset: 0x00033032
		public TypeReference Int64
		{
			get
			{
				return this.type_int64 ?? this.LookupSystemValueType(ref this.type_int64, "Int64", ElementType.I8);
			}
		}

		// Token: 0x170004C1 RID: 1217
		// (get) Token: 0x0600118F RID: 4495 RVA: 0x00034E51 File Offset: 0x00033051
		public TypeReference UInt64
		{
			get
			{
				return this.type_uint64 ?? this.LookupSystemValueType(ref this.type_uint64, "UInt64", ElementType.U8);
			}
		}

		// Token: 0x170004C2 RID: 1218
		// (get) Token: 0x06001190 RID: 4496 RVA: 0x00034E70 File Offset: 0x00033070
		public TypeReference Single
		{
			get
			{
				return this.type_single ?? this.LookupSystemValueType(ref this.type_single, "Single", ElementType.R4);
			}
		}

		// Token: 0x170004C3 RID: 1219
		// (get) Token: 0x06001191 RID: 4497 RVA: 0x00034E8F File Offset: 0x0003308F
		public TypeReference Double
		{
			get
			{
				return this.type_double ?? this.LookupSystemValueType(ref this.type_double, "Double", ElementType.R8);
			}
		}

		// Token: 0x170004C4 RID: 1220
		// (get) Token: 0x06001192 RID: 4498 RVA: 0x00034EAE File Offset: 0x000330AE
		public TypeReference IntPtr
		{
			get
			{
				return this.type_intptr ?? this.LookupSystemValueType(ref this.type_intptr, "IntPtr", ElementType.I);
			}
		}

		// Token: 0x170004C5 RID: 1221
		// (get) Token: 0x06001193 RID: 4499 RVA: 0x00034ECD File Offset: 0x000330CD
		public TypeReference UIntPtr
		{
			get
			{
				return this.type_uintptr ?? this.LookupSystemValueType(ref this.type_uintptr, "UIntPtr", ElementType.U);
			}
		}

		// Token: 0x170004C6 RID: 1222
		// (get) Token: 0x06001194 RID: 4500 RVA: 0x00034EEC File Offset: 0x000330EC
		public TypeReference String
		{
			get
			{
				return this.type_string ?? this.LookupSystemType(ref this.type_string, "String", ElementType.String);
			}
		}

		// Token: 0x170004C7 RID: 1223
		// (get) Token: 0x06001195 RID: 4501 RVA: 0x00034F0B File Offset: 0x0003310B
		public TypeReference TypedReference
		{
			get
			{
				return this.type_typedref ?? this.LookupSystemValueType(ref this.type_typedref, "TypedReference", ElementType.TypedByRef);
			}
		}

		// Token: 0x04000633 RID: 1587
		private readonly ModuleDefinition module;

		// Token: 0x04000634 RID: 1588
		private TypeReference type_object;

		// Token: 0x04000635 RID: 1589
		private TypeReference type_void;

		// Token: 0x04000636 RID: 1590
		private TypeReference type_bool;

		// Token: 0x04000637 RID: 1591
		private TypeReference type_char;

		// Token: 0x04000638 RID: 1592
		private TypeReference type_sbyte;

		// Token: 0x04000639 RID: 1593
		private TypeReference type_byte;

		// Token: 0x0400063A RID: 1594
		private TypeReference type_int16;

		// Token: 0x0400063B RID: 1595
		private TypeReference type_uint16;

		// Token: 0x0400063C RID: 1596
		private TypeReference type_int32;

		// Token: 0x0400063D RID: 1597
		private TypeReference type_uint32;

		// Token: 0x0400063E RID: 1598
		private TypeReference type_int64;

		// Token: 0x0400063F RID: 1599
		private TypeReference type_uint64;

		// Token: 0x04000640 RID: 1600
		private TypeReference type_single;

		// Token: 0x04000641 RID: 1601
		private TypeReference type_double;

		// Token: 0x04000642 RID: 1602
		private TypeReference type_intptr;

		// Token: 0x04000643 RID: 1603
		private TypeReference type_uintptr;

		// Token: 0x04000644 RID: 1604
		private TypeReference type_string;

		// Token: 0x04000645 RID: 1605
		private TypeReference type_typedref;

		// Token: 0x020002AF RID: 687
		private sealed class CoreTypeSystem : TypeSystem
		{
			// Token: 0x06001196 RID: 4502 RVA: 0x00034F2A File Offset: 0x0003312A
			public CoreTypeSystem(ModuleDefinition module)
				: base(module)
			{
			}

			// Token: 0x06001197 RID: 4503 RVA: 0x00034F34 File Offset: 0x00033134
			internal override TypeReference LookupType(string @namespace, string name)
			{
				TypeReference type = this.LookupTypeDefinition(@namespace, name) ?? this.LookupTypeForwarded(@namespace, name);
				if (type != null)
				{
					return type;
				}
				throw new NotSupportedException();
			}

			// Token: 0x06001198 RID: 4504 RVA: 0x00034F60 File Offset: 0x00033160
			private TypeReference LookupTypeDefinition(string @namespace, string name)
			{
				if (this.module.MetadataSystem.Types == null)
				{
					TypeSystem.CoreTypeSystem.Initialize(this.module.Types);
				}
				return this.module.Read<Row<string, string>, TypeDefinition>(new Row<string, string>(@namespace, name), delegate(Row<string, string> row, MetadataReader reader)
				{
					TypeDefinition[] types = reader.metadata.Types;
					for (int i = 0; i < types.Length; i++)
					{
						if (types[i] == null)
						{
							types[i] = reader.GetTypeDefinition((uint)(i + 1));
						}
						TypeDefinition type = types[i];
						if (type.Name == row.Col2 && type.Namespace == row.Col1)
						{
							return type;
						}
					}
					return null;
				});
			}

			// Token: 0x06001199 RID: 4505 RVA: 0x00034FC0 File Offset: 0x000331C0
			private TypeReference LookupTypeForwarded(string @namespace, string name)
			{
				if (!this.module.HasExportedTypes)
				{
					return null;
				}
				Collection<ExportedType> exported_types = this.module.ExportedTypes;
				for (int i = 0; i < exported_types.Count; i++)
				{
					ExportedType exported_type = exported_types[i];
					if (exported_type.Name == name && exported_type.Namespace == @namespace)
					{
						return exported_type.CreateReference();
					}
				}
				return null;
			}

			// Token: 0x0600119A RID: 4506 RVA: 0x0001B842 File Offset: 0x00019A42
			private static void Initialize(object obj)
			{
			}
		}

		// Token: 0x020002B1 RID: 689
		private sealed class CommonTypeSystem : TypeSystem
		{
			// Token: 0x0600119E RID: 4510 RVA: 0x00034F2A File Offset: 0x0003312A
			public CommonTypeSystem(ModuleDefinition module)
				: base(module)
			{
			}

			// Token: 0x0600119F RID: 4511 RVA: 0x00035099 File Offset: 0x00033299
			internal override TypeReference LookupType(string @namespace, string name)
			{
				return this.CreateTypeReference(@namespace, name);
			}

			// Token: 0x060011A0 RID: 4512 RVA: 0x000350A4 File Offset: 0x000332A4
			public AssemblyNameReference GetCoreLibraryReference()
			{
				if (this.core_library != null)
				{
					return this.core_library;
				}
				if (this.module.TryGetCoreLibraryReference(out this.core_library))
				{
					return this.core_library;
				}
				this.core_library = new AssemblyNameReference
				{
					Name = "mscorlib",
					Version = this.GetCorlibVersion(),
					PublicKeyToken = new byte[] { 183, 122, 92, 86, 25, 52, 224, 137 }
				};
				this.module.AssemblyReferences.Add(this.core_library);
				return this.core_library;
			}

			// Token: 0x060011A1 RID: 4513 RVA: 0x00035134 File Offset: 0x00033334
			private Version GetCorlibVersion()
			{
				switch (this.module.Runtime)
				{
				case TargetRuntime.Net_1_0:
				case TargetRuntime.Net_1_1:
					return new Version(1, 0, 0, 0);
				case TargetRuntime.Net_2_0:
					return new Version(2, 0, 0, 0);
				case TargetRuntime.Net_4_0:
					return new Version(4, 0, 0, 0);
				default:
					throw new NotSupportedException();
				}
			}

			// Token: 0x060011A2 RID: 4514 RVA: 0x00035188 File Offset: 0x00033388
			private TypeReference CreateTypeReference(string @namespace, string name)
			{
				return new TypeReference(@namespace, name, this.module, this.GetCoreLibraryReference());
			}

			// Token: 0x04000648 RID: 1608
			private AssemblyNameReference core_library;
		}
	}
}
