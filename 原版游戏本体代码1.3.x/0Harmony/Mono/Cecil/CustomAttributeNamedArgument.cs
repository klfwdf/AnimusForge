using System;

namespace Mono.Cecil
{
	// Token: 0x0200022C RID: 556
	internal struct CustomAttributeNamedArgument
	{
		// Token: 0x17000262 RID: 610
		// (get) Token: 0x06000BE6 RID: 3046 RVA: 0x0002A30F File Offset: 0x0002850F
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		// Token: 0x17000263 RID: 611
		// (get) Token: 0x06000BE7 RID: 3047 RVA: 0x0002A317 File Offset: 0x00028517
		public CustomAttributeArgument Argument
		{
			get
			{
				return this.argument;
			}
		}

		// Token: 0x06000BE8 RID: 3048 RVA: 0x0002A31F File Offset: 0x0002851F
		public CustomAttributeNamedArgument(string name, CustomAttributeArgument argument)
		{
			Mixin.CheckName(name);
			this.name = name;
			this.argument = argument;
		}

		// Token: 0x04000392 RID: 914
		private readonly string name;

		// Token: 0x04000393 RID: 915
		private readonly CustomAttributeArgument argument;
	}
}
