using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000242 RID: 578
	internal sealed class GenericParameterConstraint : ICustomAttributeProvider, IMetadataTokenProvider
	{
		// Token: 0x170002FC RID: 764
		// (get) Token: 0x06000D1A RID: 3354 RVA: 0x0002BEE1 File Offset: 0x0002A0E1
		// (set) Token: 0x06000D1B RID: 3355 RVA: 0x0002BEE9 File Offset: 0x0002A0E9
		public TypeReference ConstraintType
		{
			get
			{
				return this.constraint_type;
			}
			set
			{
				this.constraint_type = value;
			}
		}

		// Token: 0x170002FD RID: 765
		// (get) Token: 0x06000D1C RID: 3356 RVA: 0x0002BEF2 File Offset: 0x0002A0F2
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.generic_parameter != null && this.GetHasCustomAttributes(this.generic_parameter.Module);
			}
		}

		// Token: 0x170002FE RID: 766
		// (get) Token: 0x06000D1D RID: 3357 RVA: 0x0002BF28 File Offset: 0x0002A128
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				if (this.generic_parameter == null)
				{
					if (this.custom_attributes == null)
					{
						Interlocked.CompareExchange<Collection<CustomAttribute>>(ref this.custom_attributes, new Collection<CustomAttribute>(), null);
					}
					return this.custom_attributes;
				}
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.generic_parameter.Module);
			}
		}

		// Token: 0x170002FF RID: 767
		// (get) Token: 0x06000D1E RID: 3358 RVA: 0x0002BF7F File Offset: 0x0002A17F
		// (set) Token: 0x06000D1F RID: 3359 RVA: 0x0002BF87 File Offset: 0x0002A187
		public MetadataToken MetadataToken
		{
			get
			{
				return this.token;
			}
			set
			{
				this.token = value;
			}
		}

		// Token: 0x06000D20 RID: 3360 RVA: 0x0002BF90 File Offset: 0x0002A190
		internal GenericParameterConstraint(TypeReference constraintType, MetadataToken token)
		{
			this.constraint_type = constraintType;
			this.token = token;
		}

		// Token: 0x06000D21 RID: 3361 RVA: 0x0002BFA6 File Offset: 0x0002A1A6
		public GenericParameterConstraint(TypeReference constraintType)
		{
			Mixin.CheckType(constraintType, Mixin.Argument.constraintType);
			this.constraint_type = constraintType;
			this.token = new MetadataToken(TokenType.GenericParamConstraint);
		}

		// Token: 0x040003E6 RID: 998
		internal GenericParameter generic_parameter;

		// Token: 0x040003E7 RID: 999
		internal MetadataToken token;

		// Token: 0x040003E8 RID: 1000
		private TypeReference constraint_type;

		// Token: 0x040003E9 RID: 1001
		private Collection<CustomAttribute> custom_attributes;
	}
}
