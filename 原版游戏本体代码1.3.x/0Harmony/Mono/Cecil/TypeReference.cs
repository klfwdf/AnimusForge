using System;
using System.Threading;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002AA RID: 682
	internal class TypeReference : MemberReference, IGenericParameterProvider, IMetadataTokenProvider, IGenericContext
	{
		// Token: 0x17000492 RID: 1170
		// (get) Token: 0x06001130 RID: 4400 RVA: 0x0002E8CD File Offset: 0x0002CACD
		// (set) Token: 0x06001131 RID: 4401 RVA: 0x00033C44 File Offset: 0x00031E44
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
					throw new InvalidOperationException("Projected type reference name can't be changed.");
				}
				base.Name = value;
				this.ClearFullName();
			}
		}

		// Token: 0x17000493 RID: 1171
		// (get) Token: 0x06001132 RID: 4402 RVA: 0x00033C74 File Offset: 0x00031E74
		// (set) Token: 0x06001133 RID: 4403 RVA: 0x00033C7C File Offset: 0x00031E7C
		public virtual string Namespace
		{
			get
			{
				return this.@namespace;
			}
			set
			{
				if (base.IsWindowsRuntimeProjection && value != this.@namespace)
				{
					throw new InvalidOperationException("Projected type reference namespace can't be changed.");
				}
				this.@namespace = value;
				this.ClearFullName();
			}
		}

		// Token: 0x17000494 RID: 1172
		// (get) Token: 0x06001134 RID: 4404 RVA: 0x00033CAC File Offset: 0x00031EAC
		// (set) Token: 0x06001135 RID: 4405 RVA: 0x00033CB4 File Offset: 0x00031EB4
		public virtual bool IsValueType
		{
			get
			{
				return this.value_type;
			}
			set
			{
				this.value_type = value;
			}
		}

		// Token: 0x17000495 RID: 1173
		// (get) Token: 0x06001136 RID: 4406 RVA: 0x00033CC0 File Offset: 0x00031EC0
		public override ModuleDefinition Module
		{
			get
			{
				if (this.module != null)
				{
					return this.module;
				}
				TypeReference declaring_type = this.DeclaringType;
				if (declaring_type != null)
				{
					return declaring_type.Module;
				}
				return null;
			}
		}

		// Token: 0x17000496 RID: 1174
		// (get) Token: 0x06001137 RID: 4407 RVA: 0x00033CEE File Offset: 0x00031EEE
		// (set) Token: 0x06001138 RID: 4408 RVA: 0x0002B182 File Offset: 0x00029382
		internal TypeReferenceProjection WindowsRuntimeProjection
		{
			get
			{
				return (TypeReferenceProjection)this.projection;
			}
			set
			{
				this.projection = value;
			}
		}

		// Token: 0x17000497 RID: 1175
		// (get) Token: 0x06001139 RID: 4409 RVA: 0x0001B6A2 File Offset: 0x000198A2
		IGenericParameterProvider IGenericContext.Type
		{
			get
			{
				return this;
			}
		}

		// Token: 0x17000498 RID: 1176
		// (get) Token: 0x0600113A RID: 4410 RVA: 0x0002B871 File Offset: 0x00029A71
		IGenericParameterProvider IGenericContext.Method
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000499 RID: 1177
		// (get) Token: 0x0600113B RID: 4411 RVA: 0x0001B69F File Offset: 0x0001989F
		GenericParameterType IGenericParameterProvider.GenericParameterType
		{
			get
			{
				return GenericParameterType.Type;
			}
		}

		// Token: 0x1700049A RID: 1178
		// (get) Token: 0x0600113C RID: 4412 RVA: 0x00033CFB File Offset: 0x00031EFB
		public virtual bool HasGenericParameters
		{
			get
			{
				return !this.generic_parameters.IsNullOrEmpty<GenericParameter>();
			}
		}

		// Token: 0x1700049B RID: 1179
		// (get) Token: 0x0600113D RID: 4413 RVA: 0x00033D0B File Offset: 0x00031F0B
		public virtual Collection<GenericParameter> GenericParameters
		{
			get
			{
				if (this.generic_parameters == null)
				{
					Interlocked.CompareExchange<Collection<GenericParameter>>(ref this.generic_parameters, new GenericParameterCollection(this), null);
				}
				return this.generic_parameters;
			}
		}

		// Token: 0x1700049C RID: 1180
		// (get) Token: 0x0600113E RID: 4414 RVA: 0x00033D30 File Offset: 0x00031F30
		// (set) Token: 0x0600113F RID: 4415 RVA: 0x00033D54 File Offset: 0x00031F54
		public virtual IMetadataScope Scope
		{
			get
			{
				TypeReference declaring_type = this.DeclaringType;
				if (declaring_type != null)
				{
					return declaring_type.Scope;
				}
				return this.scope;
			}
			set
			{
				TypeReference declaring_type = this.DeclaringType;
				if (declaring_type != null)
				{
					if (base.IsWindowsRuntimeProjection && value != declaring_type.Scope)
					{
						throw new InvalidOperationException("Projected type scope can't be changed.");
					}
					declaring_type.Scope = value;
					return;
				}
				else
				{
					if (base.IsWindowsRuntimeProjection && value != this.scope)
					{
						throw new InvalidOperationException("Projected type scope can't be changed.");
					}
					this.scope = value;
					return;
				}
			}
		}

		// Token: 0x1700049D RID: 1181
		// (get) Token: 0x06001140 RID: 4416 RVA: 0x00033DB2 File Offset: 0x00031FB2
		public bool IsNested
		{
			get
			{
				return this.DeclaringType != null;
			}
		}

		// Token: 0x1700049E RID: 1182
		// (get) Token: 0x06001141 RID: 4417 RVA: 0x00033DBD File Offset: 0x00031FBD
		// (set) Token: 0x06001142 RID: 4418 RVA: 0x00033DC5 File Offset: 0x00031FC5
		public override TypeReference DeclaringType
		{
			get
			{
				return base.DeclaringType;
			}
			set
			{
				if (base.IsWindowsRuntimeProjection && value != base.DeclaringType)
				{
					throw new InvalidOperationException("Projected type declaring type can't be changed.");
				}
				base.DeclaringType = value;
				this.ClearFullName();
			}
		}

		// Token: 0x1700049F RID: 1183
		// (get) Token: 0x06001143 RID: 4419 RVA: 0x00033DF0 File Offset: 0x00031FF0
		public override string FullName
		{
			get
			{
				if (this.fullname != null)
				{
					return this.fullname;
				}
				string new_fullname = this.TypeFullName();
				if (this.IsNested)
				{
					new_fullname = this.DeclaringType.FullName + "/" + new_fullname;
				}
				Interlocked.CompareExchange<string>(ref this.fullname, new_fullname, null);
				return this.fullname;
			}
		}

		// Token: 0x170004A0 RID: 1184
		// (get) Token: 0x06001144 RID: 4420 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsByReference
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A1 RID: 1185
		// (get) Token: 0x06001145 RID: 4421 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsPointer
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A2 RID: 1186
		// (get) Token: 0x06001146 RID: 4422 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsSentinel
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A3 RID: 1187
		// (get) Token: 0x06001147 RID: 4423 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsArray
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A4 RID: 1188
		// (get) Token: 0x06001148 RID: 4424 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsGenericParameter
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A5 RID: 1189
		// (get) Token: 0x06001149 RID: 4425 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsGenericInstance
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A6 RID: 1190
		// (get) Token: 0x0600114A RID: 4426 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsRequiredModifier
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A7 RID: 1191
		// (get) Token: 0x0600114B RID: 4427 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsOptionalModifier
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A8 RID: 1192
		// (get) Token: 0x0600114C RID: 4428 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsPinned
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004A9 RID: 1193
		// (get) Token: 0x0600114D RID: 4429 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsFunctionPointer
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170004AA RID: 1194
		// (get) Token: 0x0600114E RID: 4430 RVA: 0x00033E46 File Offset: 0x00032046
		public virtual bool IsPrimitive
		{
			get
			{
				return this.etype.IsPrimitive();
			}
		}

		// Token: 0x170004AB RID: 1195
		// (get) Token: 0x0600114F RID: 4431 RVA: 0x00033E53 File Offset: 0x00032053
		public virtual MetadataType MetadataType
		{
			get
			{
				if (this.etype != ElementType.None)
				{
					return (MetadataType)this.etype;
				}
				if (!this.IsValueType)
				{
					return MetadataType.Class;
				}
				return MetadataType.ValueType;
			}
		}

		// Token: 0x06001150 RID: 4432 RVA: 0x00033E72 File Offset: 0x00032072
		protected TypeReference(string @namespace, string name)
			: base(name)
		{
			this.@namespace = @namespace ?? string.Empty;
			this.token = new MetadataToken(TokenType.TypeRef, 0);
		}

		// Token: 0x06001151 RID: 4433 RVA: 0x00033E9C File Offset: 0x0003209C
		public TypeReference(string @namespace, string name, ModuleDefinition module, IMetadataScope scope)
			: this(@namespace, name)
		{
			this.module = module;
			this.scope = scope;
		}

		// Token: 0x06001152 RID: 4434 RVA: 0x00033EB5 File Offset: 0x000320B5
		public TypeReference(string @namespace, string name, ModuleDefinition module, IMetadataScope scope, bool valueType)
			: this(@namespace, name, module, scope)
		{
			this.value_type = valueType;
		}

		// Token: 0x06001153 RID: 4435 RVA: 0x00033ECA File Offset: 0x000320CA
		protected virtual void ClearFullName()
		{
			this.fullname = null;
		}

		// Token: 0x06001154 RID: 4436 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public virtual TypeReference GetElementType()
		{
			return this;
		}

		// Token: 0x06001155 RID: 4437 RVA: 0x00033ED3 File Offset: 0x000320D3
		protected override IMemberDefinition ResolveDefinition()
		{
			return this.Resolve();
		}

		// Token: 0x06001156 RID: 4438 RVA: 0x00033EDB File Offset: 0x000320DB
		public new virtual TypeDefinition Resolve()
		{
			ModuleDefinition moduleDefinition = this.Module;
			if (moduleDefinition == null)
			{
				throw new NotSupportedException();
			}
			return moduleDefinition.Resolve(this);
		}

		// Token: 0x04000629 RID: 1577
		private string @namespace;

		// Token: 0x0400062A RID: 1578
		private bool value_type;

		// Token: 0x0400062B RID: 1579
		internal IMetadataScope scope;

		// Token: 0x0400062C RID: 1580
		internal ModuleDefinition module;

		// Token: 0x0400062D RID: 1581
		internal ElementType etype;

		// Token: 0x0400062E RID: 1582
		private string fullname;

		// Token: 0x0400062F RID: 1583
		protected Collection<GenericParameter> generic_parameters;
	}
}
