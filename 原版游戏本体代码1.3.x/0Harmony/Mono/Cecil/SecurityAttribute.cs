using System;
using System.Diagnostics;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000296 RID: 662
	[DebuggerDisplay("{AttributeType}")]
	internal sealed class SecurityAttribute : ICustomAttribute
	{
		// Token: 0x17000447 RID: 1095
		// (get) Token: 0x0600106F RID: 4207 RVA: 0x00031FE7 File Offset: 0x000301E7
		// (set) Token: 0x06001070 RID: 4208 RVA: 0x00031FEF File Offset: 0x000301EF
		public TypeReference AttributeType
		{
			get
			{
				return this.attribute_type;
			}
			set
			{
				this.attribute_type = value;
			}
		}

		// Token: 0x17000448 RID: 1096
		// (get) Token: 0x06001071 RID: 4209 RVA: 0x00031FF8 File Offset: 0x000301F8
		public bool HasFields
		{
			get
			{
				return !this.fields.IsNullOrEmpty<CustomAttributeNamedArgument>();
			}
		}

		// Token: 0x17000449 RID: 1097
		// (get) Token: 0x06001072 RID: 4210 RVA: 0x00032008 File Offset: 0x00030208
		public Collection<CustomAttributeNamedArgument> Fields
		{
			get
			{
				if (this.fields == null)
				{
					Interlocked.CompareExchange<Collection<CustomAttributeNamedArgument>>(ref this.fields, new Collection<CustomAttributeNamedArgument>(), null);
				}
				return this.fields;
			}
		}

		// Token: 0x1700044A RID: 1098
		// (get) Token: 0x06001073 RID: 4211 RVA: 0x0003202A File Offset: 0x0003022A
		public bool HasProperties
		{
			get
			{
				return !this.properties.IsNullOrEmpty<CustomAttributeNamedArgument>();
			}
		}

		// Token: 0x1700044B RID: 1099
		// (get) Token: 0x06001074 RID: 4212 RVA: 0x0003203A File Offset: 0x0003023A
		public Collection<CustomAttributeNamedArgument> Properties
		{
			get
			{
				if (this.properties == null)
				{
					Interlocked.CompareExchange<Collection<CustomAttributeNamedArgument>>(ref this.properties, new Collection<CustomAttributeNamedArgument>(), null);
				}
				return this.properties;
			}
		}

		// Token: 0x06001075 RID: 4213 RVA: 0x0003205C File Offset: 0x0003025C
		public SecurityAttribute(TypeReference attributeType)
		{
			this.attribute_type = attributeType;
		}

		// Token: 0x1700044C RID: 1100
		// (get) Token: 0x06001076 RID: 4214 RVA: 0x0001B69F File Offset: 0x0001989F
		bool ICustomAttribute.HasConstructorArguments
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700044D RID: 1101
		// (get) Token: 0x06001077 RID: 4215 RVA: 0x00003BBE File Offset: 0x00001DBE
		Collection<CustomAttributeArgument> ICustomAttribute.ConstructorArguments
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x04000586 RID: 1414
		private TypeReference attribute_type;

		// Token: 0x04000587 RID: 1415
		internal Collection<CustomAttributeNamedArgument> fields;

		// Token: 0x04000588 RID: 1416
		internal Collection<CustomAttributeNamedArgument> properties;
	}
}
