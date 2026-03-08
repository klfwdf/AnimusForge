using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002A4 RID: 676
	internal sealed class InterfaceImplementation : ICustomAttributeProvider, IMetadataTokenProvider
	{
		// Token: 0x1700048E RID: 1166
		// (get) Token: 0x060010F7 RID: 4343 RVA: 0x00032F5F File Offset: 0x0003115F
		// (set) Token: 0x060010F8 RID: 4344 RVA: 0x00032F67 File Offset: 0x00031167
		public TypeReference InterfaceType
		{
			get
			{
				return this.interface_type;
			}
			set
			{
				this.interface_type = value;
			}
		}

		// Token: 0x1700048F RID: 1167
		// (get) Token: 0x060010F9 RID: 4345 RVA: 0x00032F70 File Offset: 0x00031170
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.type != null && this.GetHasCustomAttributes(this.type.Module);
			}
		}

		// Token: 0x17000490 RID: 1168
		// (get) Token: 0x060010FA RID: 4346 RVA: 0x00032FA4 File Offset: 0x000311A4
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				if (this.type == null)
				{
					if (this.custom_attributes == null)
					{
						Interlocked.CompareExchange<Collection<CustomAttribute>>(ref this.custom_attributes, new Collection<CustomAttribute>(), null);
					}
					return this.custom_attributes;
				}
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.type.Module);
			}
		}

		// Token: 0x17000491 RID: 1169
		// (get) Token: 0x060010FB RID: 4347 RVA: 0x00032FFB File Offset: 0x000311FB
		// (set) Token: 0x060010FC RID: 4348 RVA: 0x00033003 File Offset: 0x00031203
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

		// Token: 0x060010FD RID: 4349 RVA: 0x0003300C File Offset: 0x0003120C
		internal InterfaceImplementation(TypeReference interfaceType, MetadataToken token)
		{
			this.interface_type = interfaceType;
			this.token = token;
		}

		// Token: 0x060010FE RID: 4350 RVA: 0x00033022 File Offset: 0x00031222
		public InterfaceImplementation(TypeReference interfaceType)
		{
			Mixin.CheckType(interfaceType, Mixin.Argument.interfaceType);
			this.interface_type = interfaceType;
			this.token = new MetadataToken(TokenType.InterfaceImpl);
		}

		// Token: 0x040005F6 RID: 1526
		internal TypeDefinition type;

		// Token: 0x040005F7 RID: 1527
		internal MetadataToken token;

		// Token: 0x040005F8 RID: 1528
		private TypeReference interface_type;

		// Token: 0x040005F9 RID: 1529
		private Collection<CustomAttribute> custom_attributes;
	}
}
