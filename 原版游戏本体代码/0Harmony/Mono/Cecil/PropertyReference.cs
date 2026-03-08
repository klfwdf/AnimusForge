using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000290 RID: 656
	internal abstract class PropertyReference : MemberReference
	{
		// Token: 0x1700043A RID: 1082
		// (get) Token: 0x06001057 RID: 4183 RVA: 0x00031EF9 File Offset: 0x000300F9
		// (set) Token: 0x06001058 RID: 4184 RVA: 0x00031F01 File Offset: 0x00030101
		public TypeReference PropertyType
		{
			get
			{
				return this.property_type;
			}
			set
			{
				this.property_type = value;
			}
		}

		// Token: 0x1700043B RID: 1083
		// (get) Token: 0x06001059 RID: 4185
		public abstract Collection<ParameterDefinition> Parameters { get; }

		// Token: 0x0600105A RID: 4186 RVA: 0x00031F0A File Offset: 0x0003010A
		internal PropertyReference(string name, TypeReference propertyType)
			: base(name)
		{
			Mixin.CheckType(propertyType, Mixin.Argument.propertyType);
			this.property_type = propertyType;
		}

		// Token: 0x0600105B RID: 4187 RVA: 0x00031F22 File Offset: 0x00030122
		protected override IMemberDefinition ResolveDefinition()
		{
			return this.Resolve();
		}

		// Token: 0x0600105C RID: 4188
		public new abstract PropertyDefinition Resolve();

		// Token: 0x0400056F RID: 1391
		private TypeReference property_type;
	}
}
