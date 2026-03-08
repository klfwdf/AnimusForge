using System;
using System.Threading;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002A2 RID: 674
	internal sealed class TypeDefinition : TypeReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider, ISecurityDeclarationProvider, ICustomDebugInformationProvider
	{
		// Token: 0x17000454 RID: 1108
		// (get) Token: 0x0600108A RID: 4234 RVA: 0x0003224E File Offset: 0x0003044E
		// (set) Token: 0x0600108B RID: 4235 RVA: 0x00032256 File Offset: 0x00030456
		public TypeAttributes Attributes
		{
			get
			{
				return (TypeAttributes)this.attributes;
			}
			set
			{
				if (base.IsWindowsRuntimeProjection && (uint)((ushort)value) != this.attributes)
				{
					throw new InvalidOperationException();
				}
				this.attributes = (uint)value;
			}
		}

		// Token: 0x17000455 RID: 1109
		// (get) Token: 0x0600108C RID: 4236 RVA: 0x00032277 File Offset: 0x00030477
		// (set) Token: 0x0600108D RID: 4237 RVA: 0x0003227F File Offset: 0x0003047F
		public TypeReference BaseType
		{
			get
			{
				return this.base_type;
			}
			set
			{
				this.base_type = value;
			}
		}

		// Token: 0x17000456 RID: 1110
		// (get) Token: 0x0600108E RID: 4238 RVA: 0x00032288 File Offset: 0x00030488
		// (set) Token: 0x0600108F RID: 4239 RVA: 0x00032290 File Offset: 0x00030490
		public override string Name
		{
			get
			{
				return base.Name;
			}
			set
			{
				if (base.IsWindowsRuntimeProjection && value != base.Name)
				{
					throw new InvalidOperationException();
				}
				base.Name = value;
			}
		}

		// Token: 0x06001090 RID: 4240 RVA: 0x000322B8 File Offset: 0x000304B8
		private void ResolveLayout()
		{
			if (!base.HasImage)
			{
				this.packing_size = -1;
				this.class_size = -1;
				return;
			}
			object syncRoot = this.Module.SyncRoot;
			lock (syncRoot)
			{
				if (this.packing_size == -2 && this.class_size == -2)
				{
					Row<short, int> row = this.Module.Read<TypeDefinition, Row<short, int>>(this, (TypeDefinition type, MetadataReader reader) => reader.ReadTypeLayout(type));
					this.packing_size = row.Col1;
					this.class_size = row.Col2;
				}
			}
		}

		// Token: 0x17000457 RID: 1111
		// (get) Token: 0x06001091 RID: 4241 RVA: 0x00032368 File Offset: 0x00030568
		public bool HasLayoutInfo
		{
			get
			{
				if (this.packing_size >= 0 || this.class_size >= 0)
				{
					return true;
				}
				this.ResolveLayout();
				return this.packing_size >= 0 || this.class_size >= 0;
			}
		}

		// Token: 0x17000458 RID: 1112
		// (get) Token: 0x06001092 RID: 4242 RVA: 0x0003239B File Offset: 0x0003059B
		// (set) Token: 0x06001093 RID: 4243 RVA: 0x000323C4 File Offset: 0x000305C4
		public short PackingSize
		{
			get
			{
				if (this.packing_size >= 0)
				{
					return this.packing_size;
				}
				this.ResolveLayout();
				if (this.packing_size < 0)
				{
					return -1;
				}
				return this.packing_size;
			}
			set
			{
				this.packing_size = value;
			}
		}

		// Token: 0x17000459 RID: 1113
		// (get) Token: 0x06001094 RID: 4244 RVA: 0x000323CD File Offset: 0x000305CD
		// (set) Token: 0x06001095 RID: 4245 RVA: 0x000323F6 File Offset: 0x000305F6
		public int ClassSize
		{
			get
			{
				if (this.class_size >= 0)
				{
					return this.class_size;
				}
				this.ResolveLayout();
				if (this.class_size < 0)
				{
					return -1;
				}
				return this.class_size;
			}
			set
			{
				this.class_size = value;
			}
		}

		// Token: 0x1700045A RID: 1114
		// (get) Token: 0x06001096 RID: 4246 RVA: 0x00032400 File Offset: 0x00030600
		public bool HasInterfaces
		{
			get
			{
				if (this.interfaces != null)
				{
					return this.interfaces.Count > 0;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, bool>(this, (TypeDefinition type, MetadataReader reader) => reader.HasInterfaces(type));
				}
				return false;
			}
		}

		// Token: 0x1700045B RID: 1115
		// (get) Token: 0x06001097 RID: 4247 RVA: 0x0003245C File Offset: 0x0003065C
		public Collection<InterfaceImplementation> Interfaces
		{
			get
			{
				if (this.interfaces != null)
				{
					return this.interfaces;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, InterfaceImplementationCollection>(ref this.interfaces, this, (TypeDefinition type, MetadataReader reader) => reader.ReadInterfaces(type));
				}
				Interlocked.CompareExchange<InterfaceImplementationCollection>(ref this.interfaces, new InterfaceImplementationCollection(this), null);
				return this.interfaces;
			}
		}

		// Token: 0x1700045C RID: 1116
		// (get) Token: 0x06001098 RID: 4248 RVA: 0x000324CC File Offset: 0x000306CC
		public bool HasNestedTypes
		{
			get
			{
				if (this.nested_types != null)
				{
					return this.nested_types.Count > 0;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, bool>(this, (TypeDefinition type, MetadataReader reader) => reader.HasNestedTypes(type));
				}
				return false;
			}
		}

		// Token: 0x1700045D RID: 1117
		// (get) Token: 0x06001099 RID: 4249 RVA: 0x00032528 File Offset: 0x00030728
		public Collection<TypeDefinition> NestedTypes
		{
			get
			{
				if (this.nested_types != null)
				{
					return this.nested_types;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, Collection<TypeDefinition>>(ref this.nested_types, this, (TypeDefinition type, MetadataReader reader) => reader.ReadNestedTypes(type));
				}
				Interlocked.CompareExchange<Collection<TypeDefinition>>(ref this.nested_types, new MemberDefinitionCollection<TypeDefinition>(this), null);
				return this.nested_types;
			}
		}

		// Token: 0x1700045E RID: 1118
		// (get) Token: 0x0600109A RID: 4250 RVA: 0x00032597 File Offset: 0x00030797
		public bool HasMethods
		{
			get
			{
				if (this.methods != null)
				{
					return this.methods.Count > 0;
				}
				return base.HasImage && this.methods_range.Length > 0U;
			}
		}

		// Token: 0x1700045F RID: 1119
		// (get) Token: 0x0600109B RID: 4251 RVA: 0x000325C8 File Offset: 0x000307C8
		public Collection<MethodDefinition> Methods
		{
			get
			{
				if (this.methods != null)
				{
					return this.methods;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, Collection<MethodDefinition>>(ref this.methods, this, (TypeDefinition type, MetadataReader reader) => reader.ReadMethods(type));
				}
				Interlocked.CompareExchange<Collection<MethodDefinition>>(ref this.methods, new MemberDefinitionCollection<MethodDefinition>(this), null);
				return this.methods;
			}
		}

		// Token: 0x17000460 RID: 1120
		// (get) Token: 0x0600109C RID: 4252 RVA: 0x00032637 File Offset: 0x00030837
		public bool HasFields
		{
			get
			{
				if (this.fields != null)
				{
					return this.fields.Count > 0;
				}
				return base.HasImage && this.fields_range.Length > 0U;
			}
		}

		// Token: 0x17000461 RID: 1121
		// (get) Token: 0x0600109D RID: 4253 RVA: 0x00032668 File Offset: 0x00030868
		public Collection<FieldDefinition> Fields
		{
			get
			{
				if (this.fields != null)
				{
					return this.fields;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, Collection<FieldDefinition>>(ref this.fields, this, (TypeDefinition type, MetadataReader reader) => reader.ReadFields(type));
				}
				Interlocked.CompareExchange<Collection<FieldDefinition>>(ref this.fields, new MemberDefinitionCollection<FieldDefinition>(this), null);
				return this.fields;
			}
		}

		// Token: 0x17000462 RID: 1122
		// (get) Token: 0x0600109E RID: 4254 RVA: 0x000326D8 File Offset: 0x000308D8
		public bool HasEvents
		{
			get
			{
				if (this.events != null)
				{
					return this.events.Count > 0;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, bool>(this, (TypeDefinition type, MetadataReader reader) => reader.HasEvents(type));
				}
				return false;
			}
		}

		// Token: 0x17000463 RID: 1123
		// (get) Token: 0x0600109F RID: 4255 RVA: 0x00032734 File Offset: 0x00030934
		public Collection<EventDefinition> Events
		{
			get
			{
				if (this.events != null)
				{
					return this.events;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, Collection<EventDefinition>>(ref this.events, this, (TypeDefinition type, MetadataReader reader) => reader.ReadEvents(type));
				}
				Interlocked.CompareExchange<Collection<EventDefinition>>(ref this.events, new MemberDefinitionCollection<EventDefinition>(this), null);
				return this.events;
			}
		}

		// Token: 0x17000464 RID: 1124
		// (get) Token: 0x060010A0 RID: 4256 RVA: 0x000327A4 File Offset: 0x000309A4
		public bool HasProperties
		{
			get
			{
				if (this.properties != null)
				{
					return this.properties.Count > 0;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, bool>(this, (TypeDefinition type, MetadataReader reader) => reader.HasProperties(type));
				}
				return false;
			}
		}

		// Token: 0x17000465 RID: 1125
		// (get) Token: 0x060010A1 RID: 4257 RVA: 0x00032800 File Offset: 0x00030A00
		public Collection<PropertyDefinition> Properties
		{
			get
			{
				if (this.properties != null)
				{
					return this.properties;
				}
				if (base.HasImage)
				{
					return this.Module.Read<TypeDefinition, Collection<PropertyDefinition>>(ref this.properties, this, (TypeDefinition type, MetadataReader reader) => reader.ReadProperties(type));
				}
				Interlocked.CompareExchange<Collection<PropertyDefinition>>(ref this.properties, new MemberDefinitionCollection<PropertyDefinition>(this), null);
				return this.properties;
			}
		}

		// Token: 0x17000466 RID: 1126
		// (get) Token: 0x060010A2 RID: 4258 RVA: 0x0003286F File Offset: 0x00030A6F
		public bool HasSecurityDeclarations
		{
			get
			{
				if (this.security_declarations != null)
				{
					return this.security_declarations.Count > 0;
				}
				return this.GetHasSecurityDeclarations(this.Module);
			}
		}

		// Token: 0x17000467 RID: 1127
		// (get) Token: 0x060010A3 RID: 4259 RVA: 0x00032894 File Offset: 0x00030A94
		public Collection<SecurityDeclaration> SecurityDeclarations
		{
			get
			{
				return this.security_declarations ?? this.GetSecurityDeclarations(ref this.security_declarations, this.Module);
			}
		}

		// Token: 0x17000468 RID: 1128
		// (get) Token: 0x060010A4 RID: 4260 RVA: 0x000328B2 File Offset: 0x00030AB2
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.GetHasCustomAttributes(this.Module);
			}
		}

		// Token: 0x17000469 RID: 1129
		// (get) Token: 0x060010A5 RID: 4261 RVA: 0x000328D7 File Offset: 0x00030AD7
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.Module);
			}
		}

		// Token: 0x1700046A RID: 1130
		// (get) Token: 0x060010A6 RID: 4262 RVA: 0x000328F5 File Offset: 0x00030AF5
		public override bool HasGenericParameters
		{
			get
			{
				if (this.generic_parameters != null)
				{
					return this.generic_parameters.Count > 0;
				}
				return this.GetHasGenericParameters(this.Module);
			}
		}

		// Token: 0x1700046B RID: 1131
		// (get) Token: 0x060010A7 RID: 4263 RVA: 0x0003291A File Offset: 0x00030B1A
		public override Collection<GenericParameter> GenericParameters
		{
			get
			{
				return this.generic_parameters ?? this.GetGenericParameters(ref this.generic_parameters, this.Module);
			}
		}

		// Token: 0x1700046C RID: 1132
		// (get) Token: 0x060010A8 RID: 4264 RVA: 0x00032938 File Offset: 0x00030B38
		public bool HasCustomDebugInformations
		{
			get
			{
				if (this.custom_infos != null)
				{
					return this.custom_infos.Count > 0;
				}
				return this.GetHasCustomDebugInformations(ref this.custom_infos, this.Module);
			}
		}

		// Token: 0x1700046D RID: 1133
		// (get) Token: 0x060010A9 RID: 4265 RVA: 0x00032963 File Offset: 0x00030B63
		public Collection<CustomDebugInformation> CustomDebugInformations
		{
			get
			{
				return this.custom_infos ?? this.GetCustomDebugInformations(ref this.custom_infos, this.module);
			}
		}

		// Token: 0x1700046E RID: 1134
		// (get) Token: 0x060010AA RID: 4266 RVA: 0x00032981 File Offset: 0x00030B81
		// (set) Token: 0x060010AB RID: 4267 RVA: 0x00032990 File Offset: 0x00030B90
		public bool IsNotPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 0U, value);
			}
		}

		// Token: 0x1700046F RID: 1135
		// (get) Token: 0x060010AC RID: 4268 RVA: 0x000329A6 File Offset: 0x00030BA6
		// (set) Token: 0x060010AD RID: 4269 RVA: 0x000329B5 File Offset: 0x00030BB5
		public bool IsPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 1U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 1U, value);
			}
		}

		// Token: 0x17000470 RID: 1136
		// (get) Token: 0x060010AE RID: 4270 RVA: 0x000329CB File Offset: 0x00030BCB
		// (set) Token: 0x060010AF RID: 4271 RVA: 0x000329DA File Offset: 0x00030BDA
		public bool IsNestedPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 2U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 2U, value);
			}
		}

		// Token: 0x17000471 RID: 1137
		// (get) Token: 0x060010B0 RID: 4272 RVA: 0x000329F0 File Offset: 0x00030BF0
		// (set) Token: 0x060010B1 RID: 4273 RVA: 0x000329FF File Offset: 0x00030BFF
		public bool IsNestedPrivate
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 3U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 3U, value);
			}
		}

		// Token: 0x17000472 RID: 1138
		// (get) Token: 0x060010B2 RID: 4274 RVA: 0x00032A15 File Offset: 0x00030C15
		// (set) Token: 0x060010B3 RID: 4275 RVA: 0x00032A24 File Offset: 0x00030C24
		public bool IsNestedFamily
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 4U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 4U, value);
			}
		}

		// Token: 0x17000473 RID: 1139
		// (get) Token: 0x060010B4 RID: 4276 RVA: 0x00032A3A File Offset: 0x00030C3A
		// (set) Token: 0x060010B5 RID: 4277 RVA: 0x00032A49 File Offset: 0x00030C49
		public bool IsNestedAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 5U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 5U, value);
			}
		}

		// Token: 0x17000474 RID: 1140
		// (get) Token: 0x060010B6 RID: 4278 RVA: 0x00032A5F File Offset: 0x00030C5F
		// (set) Token: 0x060010B7 RID: 4279 RVA: 0x00032A6E File Offset: 0x00030C6E
		public bool IsNestedFamilyAndAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 6U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 6U, value);
			}
		}

		// Token: 0x17000475 RID: 1141
		// (get) Token: 0x060010B8 RID: 4280 RVA: 0x00032A84 File Offset: 0x00030C84
		// (set) Token: 0x060010B9 RID: 4281 RVA: 0x00032A93 File Offset: 0x00030C93
		public bool IsNestedFamilyOrAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 7U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 7U, value);
			}
		}

		// Token: 0x17000476 RID: 1142
		// (get) Token: 0x060010BA RID: 4282 RVA: 0x00032AA9 File Offset: 0x00030CA9
		// (set) Token: 0x060010BB RID: 4283 RVA: 0x00032AB9 File Offset: 0x00030CB9
		public bool IsAutoLayout
		{
			get
			{
				return this.attributes.GetMaskedAttributes(24U, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(24U, 0U, value);
			}
		}

		// Token: 0x17000477 RID: 1143
		// (get) Token: 0x060010BC RID: 4284 RVA: 0x00032AD0 File Offset: 0x00030CD0
		// (set) Token: 0x060010BD RID: 4285 RVA: 0x00032AE0 File Offset: 0x00030CE0
		public bool IsSequentialLayout
		{
			get
			{
				return this.attributes.GetMaskedAttributes(24U, 8U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(24U, 8U, value);
			}
		}

		// Token: 0x17000478 RID: 1144
		// (get) Token: 0x060010BE RID: 4286 RVA: 0x00032AF7 File Offset: 0x00030CF7
		// (set) Token: 0x060010BF RID: 4287 RVA: 0x00032B08 File Offset: 0x00030D08
		public bool IsExplicitLayout
		{
			get
			{
				return this.attributes.GetMaskedAttributes(24U, 16U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(24U, 16U, value);
			}
		}

		// Token: 0x17000479 RID: 1145
		// (get) Token: 0x060010C0 RID: 4288 RVA: 0x00032B20 File Offset: 0x00030D20
		// (set) Token: 0x060010C1 RID: 4289 RVA: 0x00032B30 File Offset: 0x00030D30
		public bool IsClass
		{
			get
			{
				return this.attributes.GetMaskedAttributes(32U, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(32U, 0U, value);
			}
		}

		// Token: 0x1700047A RID: 1146
		// (get) Token: 0x060010C2 RID: 4290 RVA: 0x00032B47 File Offset: 0x00030D47
		// (set) Token: 0x060010C3 RID: 4291 RVA: 0x00032B58 File Offset: 0x00030D58
		public bool IsInterface
		{
			get
			{
				return this.attributes.GetMaskedAttributes(32U, 32U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(32U, 32U, value);
			}
		}

		// Token: 0x1700047B RID: 1147
		// (get) Token: 0x060010C4 RID: 4292 RVA: 0x00032B70 File Offset: 0x00030D70
		// (set) Token: 0x060010C5 RID: 4293 RVA: 0x00032B82 File Offset: 0x00030D82
		public bool IsAbstract
		{
			get
			{
				return this.attributes.GetAttributes(128U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(128U, value);
			}
		}

		// Token: 0x1700047C RID: 1148
		// (get) Token: 0x060010C6 RID: 4294 RVA: 0x00032B9B File Offset: 0x00030D9B
		// (set) Token: 0x060010C7 RID: 4295 RVA: 0x00032BAD File Offset: 0x00030DAD
		public bool IsSealed
		{
			get
			{
				return this.attributes.GetAttributes(256U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(256U, value);
			}
		}

		// Token: 0x1700047D RID: 1149
		// (get) Token: 0x060010C8 RID: 4296 RVA: 0x00032BC6 File Offset: 0x00030DC6
		// (set) Token: 0x060010C9 RID: 4297 RVA: 0x00032BD8 File Offset: 0x00030DD8
		public bool IsSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(1024U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1024U, value);
			}
		}

		// Token: 0x1700047E RID: 1150
		// (get) Token: 0x060010CA RID: 4298 RVA: 0x00032BF1 File Offset: 0x00030DF1
		// (set) Token: 0x060010CB RID: 4299 RVA: 0x00032C03 File Offset: 0x00030E03
		public bool IsImport
		{
			get
			{
				return this.attributes.GetAttributes(4096U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(4096U, value);
			}
		}

		// Token: 0x1700047F RID: 1151
		// (get) Token: 0x060010CC RID: 4300 RVA: 0x00032C1C File Offset: 0x00030E1C
		// (set) Token: 0x060010CD RID: 4301 RVA: 0x00032C2E File Offset: 0x00030E2E
		public bool IsSerializable
		{
			get
			{
				return this.attributes.GetAttributes(8192U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(8192U, value);
			}
		}

		// Token: 0x17000480 RID: 1152
		// (get) Token: 0x060010CE RID: 4302 RVA: 0x00032C47 File Offset: 0x00030E47
		// (set) Token: 0x060010CF RID: 4303 RVA: 0x00032C59 File Offset: 0x00030E59
		public bool IsWindowsRuntime
		{
			get
			{
				return this.attributes.GetAttributes(16384U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(16384U, value);
			}
		}

		// Token: 0x17000481 RID: 1153
		// (get) Token: 0x060010D0 RID: 4304 RVA: 0x00032C72 File Offset: 0x00030E72
		// (set) Token: 0x060010D1 RID: 4305 RVA: 0x00032C85 File Offset: 0x00030E85
		public bool IsAnsiClass
		{
			get
			{
				return this.attributes.GetMaskedAttributes(196608U, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(196608U, 0U, value);
			}
		}

		// Token: 0x17000482 RID: 1154
		// (get) Token: 0x060010D2 RID: 4306 RVA: 0x00032C9F File Offset: 0x00030E9F
		// (set) Token: 0x060010D3 RID: 4307 RVA: 0x00032CB6 File Offset: 0x00030EB6
		public bool IsUnicodeClass
		{
			get
			{
				return this.attributes.GetMaskedAttributes(196608U, 65536U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(196608U, 65536U, value);
			}
		}

		// Token: 0x17000483 RID: 1155
		// (get) Token: 0x060010D4 RID: 4308 RVA: 0x00032CD4 File Offset: 0x00030ED4
		// (set) Token: 0x060010D5 RID: 4309 RVA: 0x00032CEB File Offset: 0x00030EEB
		public bool IsAutoClass
		{
			get
			{
				return this.attributes.GetMaskedAttributes(196608U, 131072U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(196608U, 131072U, value);
			}
		}

		// Token: 0x17000484 RID: 1156
		// (get) Token: 0x060010D6 RID: 4310 RVA: 0x00032D09 File Offset: 0x00030F09
		// (set) Token: 0x060010D7 RID: 4311 RVA: 0x00032D1B File Offset: 0x00030F1B
		public bool IsBeforeFieldInit
		{
			get
			{
				return this.attributes.GetAttributes(1048576U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1048576U, value);
			}
		}

		// Token: 0x17000485 RID: 1157
		// (get) Token: 0x060010D8 RID: 4312 RVA: 0x00032D34 File Offset: 0x00030F34
		// (set) Token: 0x060010D9 RID: 4313 RVA: 0x00032D46 File Offset: 0x00030F46
		public bool IsRuntimeSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(2048U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(2048U, value);
			}
		}

		// Token: 0x17000486 RID: 1158
		// (get) Token: 0x060010DA RID: 4314 RVA: 0x00032D5F File Offset: 0x00030F5F
		// (set) Token: 0x060010DB RID: 4315 RVA: 0x00032D71 File Offset: 0x00030F71
		public bool HasSecurity
		{
			get
			{
				return this.attributes.GetAttributes(262144U);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(262144U, value);
			}
		}

		// Token: 0x17000487 RID: 1159
		// (get) Token: 0x060010DC RID: 4316 RVA: 0x00032D8A File Offset: 0x00030F8A
		public bool IsEnum
		{
			get
			{
				return this.base_type != null && this.base_type.IsTypeOf("System", "Enum");
			}
		}

		// Token: 0x17000488 RID: 1160
		// (get) Token: 0x060010DD RID: 4317 RVA: 0x00032DAC File Offset: 0x00030FAC
		// (set) Token: 0x060010DE RID: 4318 RVA: 0x00003BBE File Offset: 0x00001DBE
		public override bool IsValueType
		{
			get
			{
				return this.base_type != null && (this.base_type.IsTypeOf("System", "Enum") || (this.base_type.IsTypeOf("System", "ValueType") && !this.IsTypeOf("System", "Enum")));
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000489 RID: 1161
		// (get) Token: 0x060010DF RID: 4319 RVA: 0x00032E08 File Offset: 0x00031008
		public override bool IsPrimitive
		{
			get
			{
				ElementType primitive_etype;
				return MetadataSystem.TryGetPrimitiveElementType(this, out primitive_etype) && primitive_etype.IsPrimitive();
			}
		}

		// Token: 0x1700048A RID: 1162
		// (get) Token: 0x060010E0 RID: 4320 RVA: 0x00032E28 File Offset: 0x00031028
		public override MetadataType MetadataType
		{
			get
			{
				ElementType primitive_etype;
				if (MetadataSystem.TryGetPrimitiveElementType(this, out primitive_etype))
				{
					return (MetadataType)primitive_etype;
				}
				return base.MetadataType;
			}
		}

		// Token: 0x1700048B RID: 1163
		// (get) Token: 0x060010E1 RID: 4321 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsDefinition
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700048C RID: 1164
		// (get) Token: 0x060010E2 RID: 4322 RVA: 0x00032E47 File Offset: 0x00031047
		// (set) Token: 0x060010E3 RID: 4323 RVA: 0x00032E54 File Offset: 0x00031054
		public new TypeDefinition DeclaringType
		{
			get
			{
				return (TypeDefinition)base.DeclaringType;
			}
			set
			{
				base.DeclaringType = value;
			}
		}

		// Token: 0x1700048D RID: 1165
		// (get) Token: 0x060010E4 RID: 4324 RVA: 0x00032E5D File Offset: 0x0003105D
		// (set) Token: 0x060010E5 RID: 4325 RVA: 0x0002B182 File Offset: 0x00029382
		internal new TypeDefinitionProjection WindowsRuntimeProjection
		{
			get
			{
				return (TypeDefinitionProjection)this.projection;
			}
			set
			{
				this.projection = value;
			}
		}

		// Token: 0x060010E6 RID: 4326 RVA: 0x00032E6A File Offset: 0x0003106A
		public TypeDefinition(string @namespace, string name, TypeAttributes attributes)
			: base(@namespace, name)
		{
			this.attributes = (uint)attributes;
			this.token = new MetadataToken(TokenType.TypeDef);
		}

		// Token: 0x060010E7 RID: 4327 RVA: 0x00032E9B File Offset: 0x0003109B
		public TypeDefinition(string @namespace, string name, TypeAttributes attributes, TypeReference baseType)
			: this(@namespace, name, attributes)
		{
			this.BaseType = baseType;
		}

		// Token: 0x060010E8 RID: 4328 RVA: 0x00032EB0 File Offset: 0x000310B0
		protected override void ClearFullName()
		{
			base.ClearFullName();
			if (!this.HasNestedTypes)
			{
				return;
			}
			Collection<TypeDefinition> nested_types = this.NestedTypes;
			for (int i = 0; i < nested_types.Count; i++)
			{
				nested_types[i].ClearFullName();
			}
		}

		// Token: 0x060010E9 RID: 4329 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public override TypeDefinition Resolve()
		{
			return this;
		}

		// Token: 0x040005DB RID: 1499
		private uint attributes;

		// Token: 0x040005DC RID: 1500
		private TypeReference base_type;

		// Token: 0x040005DD RID: 1501
		internal Range fields_range;

		// Token: 0x040005DE RID: 1502
		internal Range methods_range;

		// Token: 0x040005DF RID: 1503
		private short packing_size = -2;

		// Token: 0x040005E0 RID: 1504
		private int class_size = -2;

		// Token: 0x040005E1 RID: 1505
		private InterfaceImplementationCollection interfaces;

		// Token: 0x040005E2 RID: 1506
		private Collection<TypeDefinition> nested_types;

		// Token: 0x040005E3 RID: 1507
		private Collection<MethodDefinition> methods;

		// Token: 0x040005E4 RID: 1508
		private Collection<FieldDefinition> fields;

		// Token: 0x040005E5 RID: 1509
		private Collection<EventDefinition> events;

		// Token: 0x040005E6 RID: 1510
		private Collection<PropertyDefinition> properties;

		// Token: 0x040005E7 RID: 1511
		private Collection<CustomAttribute> custom_attributes;

		// Token: 0x040005E8 RID: 1512
		private Collection<SecurityDeclaration> security_declarations;

		// Token: 0x040005E9 RID: 1513
		internal Collection<CustomDebugInformation> custom_infos;
	}
}
