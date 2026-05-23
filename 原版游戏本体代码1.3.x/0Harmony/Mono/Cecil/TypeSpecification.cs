using System;

namespace Mono.Cecil
{
	// Token: 0x020002AD RID: 685
	internal abstract class TypeSpecification : TypeReference
	{
		// Token: 0x170004AC RID: 1196
		// (get) Token: 0x06001170 RID: 4464 RVA: 0x00034B5A File Offset: 0x00032D5A
		public TypeReference ElementType
		{
			get
			{
				return this.element_type;
			}
		}

		// Token: 0x170004AD RID: 1197
		// (get) Token: 0x06001171 RID: 4465 RVA: 0x00034B62 File Offset: 0x00032D62
		// (set) Token: 0x06001172 RID: 4466 RVA: 0x0001C627 File Offset: 0x0001A827
		public override string Name
		{
			get
			{
				return this.element_type.Name;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170004AE RID: 1198
		// (get) Token: 0x06001173 RID: 4467 RVA: 0x00034B6F File Offset: 0x00032D6F
		// (set) Token: 0x06001174 RID: 4468 RVA: 0x0001C627 File Offset: 0x0001A827
		public override string Namespace
		{
			get
			{
				return this.element_type.Namespace;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170004AF RID: 1199
		// (get) Token: 0x06001175 RID: 4469 RVA: 0x00034B7C File Offset: 0x00032D7C
		// (set) Token: 0x06001176 RID: 4470 RVA: 0x0001C627 File Offset: 0x0001A827
		public override IMetadataScope Scope
		{
			get
			{
				return this.element_type.Scope;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170004B0 RID: 1200
		// (get) Token: 0x06001177 RID: 4471 RVA: 0x00034B89 File Offset: 0x00032D89
		public override ModuleDefinition Module
		{
			get
			{
				return this.element_type.Module;
			}
		}

		// Token: 0x170004B1 RID: 1201
		// (get) Token: 0x06001178 RID: 4472 RVA: 0x00034B96 File Offset: 0x00032D96
		public override string FullName
		{
			get
			{
				return this.element_type.FullName;
			}
		}

		// Token: 0x170004B2 RID: 1202
		// (get) Token: 0x06001179 RID: 4473 RVA: 0x00034BA3 File Offset: 0x00032DA3
		public override bool ContainsGenericParameter
		{
			get
			{
				return this.element_type.ContainsGenericParameter;
			}
		}

		// Token: 0x170004B3 RID: 1203
		// (get) Token: 0x0600117A RID: 4474 RVA: 0x0002BC17 File Offset: 0x00029E17
		public override MetadataType MetadataType
		{
			get
			{
				return (MetadataType)this.etype;
			}
		}

		// Token: 0x0600117B RID: 4475 RVA: 0x00034BB0 File Offset: 0x00032DB0
		internal TypeSpecification(TypeReference type)
			: base(null, null)
		{
			this.element_type = type;
			this.token = new MetadataToken(TokenType.TypeSpec);
		}

		// Token: 0x0600117C RID: 4476 RVA: 0x00034BD1 File Offset: 0x00032DD1
		public override TypeReference GetElementType()
		{
			return this.element_type.GetElementType();
		}

		// Token: 0x04000632 RID: 1586
		private readonly TypeReference element_type;
	}
}
