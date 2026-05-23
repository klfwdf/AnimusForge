using System;

namespace Mono.Cecil
{
	// Token: 0x0200022B RID: 555
	internal struct CustomAttributeArgument
	{
		// Token: 0x17000260 RID: 608
		// (get) Token: 0x06000BE3 RID: 3043 RVA: 0x0002A2E9 File Offset: 0x000284E9
		public TypeReference Type
		{
			get
			{
				return this.type;
			}
		}

		// Token: 0x17000261 RID: 609
		// (get) Token: 0x06000BE4 RID: 3044 RVA: 0x0002A2F1 File Offset: 0x000284F1
		public object Value
		{
			get
			{
				return this.value;
			}
		}

		// Token: 0x06000BE5 RID: 3045 RVA: 0x0002A2F9 File Offset: 0x000284F9
		public CustomAttributeArgument(TypeReference type, object value)
		{
			Mixin.CheckType(type);
			this.type = type;
			this.value = value;
		}

		// Token: 0x04000390 RID: 912
		private readonly TypeReference type;

		// Token: 0x04000391 RID: 913
		private readonly object value;
	}
}
