using System;
using System.Threading;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200023F RID: 575
	internal sealed class GenericParameter : TypeReference, ICustomAttributeProvider, IMetadataTokenProvider
	{
		// Token: 0x170002E3 RID: 739
		// (get) Token: 0x06000CE6 RID: 3302 RVA: 0x0002BA15 File Offset: 0x00029C15
		// (set) Token: 0x06000CE7 RID: 3303 RVA: 0x0002BA1D File Offset: 0x00029C1D
		public GenericParameterAttributes Attributes
		{
			get
			{
				return (GenericParameterAttributes)this.attributes;
			}
			set
			{
				this.attributes = (ushort)value;
			}
		}

		// Token: 0x170002E4 RID: 740
		// (get) Token: 0x06000CE8 RID: 3304 RVA: 0x0002BA26 File Offset: 0x00029C26
		public int Position
		{
			get
			{
				return this.position;
			}
		}

		// Token: 0x170002E5 RID: 741
		// (get) Token: 0x06000CE9 RID: 3305 RVA: 0x0002BA2E File Offset: 0x00029C2E
		public GenericParameterType Type
		{
			get
			{
				return this.type;
			}
		}

		// Token: 0x170002E6 RID: 742
		// (get) Token: 0x06000CEA RID: 3306 RVA: 0x0002BA36 File Offset: 0x00029C36
		public IGenericParameterProvider Owner
		{
			get
			{
				return this.owner;
			}
		}

		// Token: 0x170002E7 RID: 743
		// (get) Token: 0x06000CEB RID: 3307 RVA: 0x0002BA40 File Offset: 0x00029C40
		public bool HasConstraints
		{
			get
			{
				if (this.constraints != null)
				{
					return this.constraints.Count > 0;
				}
				if (base.HasImage)
				{
					return this.Module.Read<GenericParameter, bool>(this, (GenericParameter generic_parameter, MetadataReader reader) => reader.HasGenericConstraints(generic_parameter));
				}
				return false;
			}
		}

		// Token: 0x170002E8 RID: 744
		// (get) Token: 0x06000CEC RID: 3308 RVA: 0x0002BA9C File Offset: 0x00029C9C
		public Collection<GenericParameterConstraint> Constraints
		{
			get
			{
				if (this.constraints != null)
				{
					return this.constraints;
				}
				if (base.HasImage)
				{
					return this.Module.Read<GenericParameter, GenericParameterConstraintCollection>(ref this.constraints, this, (GenericParameter generic_parameter, MetadataReader reader) => reader.ReadGenericConstraints(generic_parameter));
				}
				Interlocked.CompareExchange<GenericParameterConstraintCollection>(ref this.constraints, new GenericParameterConstraintCollection(this), null);
				return this.constraints;
			}
		}

		// Token: 0x170002E9 RID: 745
		// (get) Token: 0x06000CED RID: 3309 RVA: 0x0002BB0B File Offset: 0x00029D0B
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

		// Token: 0x170002EA RID: 746
		// (get) Token: 0x06000CEE RID: 3310 RVA: 0x0002BB30 File Offset: 0x00029D30
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.Module);
			}
		}

		// Token: 0x170002EB RID: 747
		// (get) Token: 0x06000CEF RID: 3311 RVA: 0x0002BB4E File Offset: 0x00029D4E
		// (set) Token: 0x06000CF0 RID: 3312 RVA: 0x0001C627 File Offset: 0x0001A827
		public override IMetadataScope Scope
		{
			get
			{
				if (this.owner == null)
				{
					return null;
				}
				if (this.owner.GenericParameterType != GenericParameterType.Method)
				{
					return ((TypeReference)this.owner).Scope;
				}
				return ((MethodReference)this.owner).DeclaringType.Scope;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170002EC RID: 748
		// (get) Token: 0x06000CF1 RID: 3313 RVA: 0x0002BB8E File Offset: 0x00029D8E
		// (set) Token: 0x06000CF2 RID: 3314 RVA: 0x0001C627 File Offset: 0x0001A827
		public override TypeReference DeclaringType
		{
			get
			{
				return this.owner as TypeReference;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170002ED RID: 749
		// (get) Token: 0x06000CF3 RID: 3315 RVA: 0x0002BB9B File Offset: 0x00029D9B
		public MethodReference DeclaringMethod
		{
			get
			{
				return this.owner as MethodReference;
			}
		}

		// Token: 0x170002EE RID: 750
		// (get) Token: 0x06000CF4 RID: 3316 RVA: 0x0002BBA8 File Offset: 0x00029DA8
		public override ModuleDefinition Module
		{
			get
			{
				return this.module ?? this.owner.Module;
			}
		}

		// Token: 0x170002EF RID: 751
		// (get) Token: 0x06000CF5 RID: 3317 RVA: 0x0002BBC0 File Offset: 0x00029DC0
		public override string Name
		{
			get
			{
				if (!string.IsNullOrEmpty(base.Name))
				{
					return base.Name;
				}
				return base.Name = ((this.type == GenericParameterType.Method) ? "!!" : "!") + this.position.ToString();
			}
		}

		// Token: 0x170002F0 RID: 752
		// (get) Token: 0x06000CF6 RID: 3318 RVA: 0x0002A221 File Offset: 0x00028421
		// (set) Token: 0x06000CF7 RID: 3319 RVA: 0x0001C627 File Offset: 0x0001A827
		public override string Namespace
		{
			get
			{
				return string.Empty;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170002F1 RID: 753
		// (get) Token: 0x06000CF8 RID: 3320 RVA: 0x0002BC0F File Offset: 0x00029E0F
		public override string FullName
		{
			get
			{
				return this.Name;
			}
		}

		// Token: 0x170002F2 RID: 754
		// (get) Token: 0x06000CF9 RID: 3321 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsGenericParameter
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170002F3 RID: 755
		// (get) Token: 0x06000CFA RID: 3322 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool ContainsGenericParameter
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170002F4 RID: 756
		// (get) Token: 0x06000CFB RID: 3323 RVA: 0x0002BC17 File Offset: 0x00029E17
		public override MetadataType MetadataType
		{
			get
			{
				return (MetadataType)this.etype;
			}
		}

		// Token: 0x170002F5 RID: 757
		// (get) Token: 0x06000CFC RID: 3324 RVA: 0x0002BC1F File Offset: 0x00029E1F
		// (set) Token: 0x06000CFD RID: 3325 RVA: 0x0002BC2E File Offset: 0x00029E2E
		public bool IsNonVariant
		{
			get
			{
				return this.attributes.GetMaskedAttributes(3, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(3, 0U, value);
			}
		}

		// Token: 0x170002F6 RID: 758
		// (get) Token: 0x06000CFE RID: 3326 RVA: 0x0002BC44 File Offset: 0x00029E44
		// (set) Token: 0x06000CFF RID: 3327 RVA: 0x0002BC53 File Offset: 0x00029E53
		public bool IsCovariant
		{
			get
			{
				return this.attributes.GetMaskedAttributes(3, 1U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(3, 1U, value);
			}
		}

		// Token: 0x170002F7 RID: 759
		// (get) Token: 0x06000D00 RID: 3328 RVA: 0x0002BC69 File Offset: 0x00029E69
		// (set) Token: 0x06000D01 RID: 3329 RVA: 0x0002BC78 File Offset: 0x00029E78
		public bool IsContravariant
		{
			get
			{
				return this.attributes.GetMaskedAttributes(3, 2U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(3, 2U, value);
			}
		}

		// Token: 0x170002F8 RID: 760
		// (get) Token: 0x06000D02 RID: 3330 RVA: 0x0002BC8E File Offset: 0x00029E8E
		// (set) Token: 0x06000D03 RID: 3331 RVA: 0x0002BC9C File Offset: 0x00029E9C
		public bool HasReferenceTypeConstraint
		{
			get
			{
				return this.attributes.GetAttributes(4);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(4, value);
			}
		}

		// Token: 0x170002F9 RID: 761
		// (get) Token: 0x06000D04 RID: 3332 RVA: 0x0002BCB1 File Offset: 0x00029EB1
		// (set) Token: 0x06000D05 RID: 3333 RVA: 0x0002BCBF File Offset: 0x00029EBF
		public bool HasNotNullableValueTypeConstraint
		{
			get
			{
				return this.attributes.GetAttributes(8);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(8, value);
			}
		}

		// Token: 0x170002FA RID: 762
		// (get) Token: 0x06000D06 RID: 3334 RVA: 0x0002BCD4 File Offset: 0x00029ED4
		// (set) Token: 0x06000D07 RID: 3335 RVA: 0x0002BCE3 File Offset: 0x00029EE3
		public bool HasDefaultConstructorConstraint
		{
			get
			{
				return this.attributes.GetAttributes(16);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(16, value);
			}
		}

		// Token: 0x170002FB RID: 763
		// (get) Token: 0x06000D08 RID: 3336 RVA: 0x0002BCF9 File Offset: 0x00029EF9
		// (set) Token: 0x06000D09 RID: 3337 RVA: 0x0002BD08 File Offset: 0x00029F08
		public bool AllowByRefLikeConstraint
		{
			get
			{
				return this.attributes.GetAttributes(32);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(32, value);
			}
		}

		// Token: 0x06000D0A RID: 3338 RVA: 0x0002BD1E File Offset: 0x00029F1E
		public GenericParameter(IGenericParameterProvider owner)
			: this(string.Empty, owner)
		{
		}

		// Token: 0x06000D0B RID: 3339 RVA: 0x0002BD2C File Offset: 0x00029F2C
		public GenericParameter(string name, IGenericParameterProvider owner)
			: base(string.Empty, name)
		{
			if (owner == null)
			{
				throw new ArgumentNullException();
			}
			this.position = -1;
			this.owner = owner;
			this.type = owner.GenericParameterType;
			this.etype = GenericParameter.ConvertGenericParameterType(this.type);
			this.token = new MetadataToken(TokenType.GenericParam);
		}

		// Token: 0x06000D0C RID: 3340 RVA: 0x0002BD8C File Offset: 0x00029F8C
		internal GenericParameter(int position, GenericParameterType type, ModuleDefinition module)
			: base(string.Empty, string.Empty)
		{
			Mixin.CheckModule(module);
			this.position = position;
			this.type = type;
			this.etype = GenericParameter.ConvertGenericParameterType(type);
			this.module = module;
			this.token = new MetadataToken(TokenType.GenericParam);
		}

		// Token: 0x06000D0D RID: 3341 RVA: 0x0002BDE0 File Offset: 0x00029FE0
		private static ElementType ConvertGenericParameterType(GenericParameterType type)
		{
			if (type == GenericParameterType.Type)
			{
				return ElementType.Var;
			}
			if (type != GenericParameterType.Method)
			{
				throw new ArgumentOutOfRangeException();
			}
			return ElementType.MVar;
		}

		// Token: 0x06000D0E RID: 3342 RVA: 0x0002B871 File Offset: 0x00029A71
		public override TypeDefinition Resolve()
		{
			return null;
		}

		// Token: 0x040003DC RID: 988
		internal int position;

		// Token: 0x040003DD RID: 989
		internal GenericParameterType type;

		// Token: 0x040003DE RID: 990
		internal IGenericParameterProvider owner;

		// Token: 0x040003DF RID: 991
		private ushort attributes;

		// Token: 0x040003E0 RID: 992
		private GenericParameterConstraintCollection constraints;

		// Token: 0x040003E1 RID: 993
		private Collection<CustomAttribute> custom_attributes;
	}
}
