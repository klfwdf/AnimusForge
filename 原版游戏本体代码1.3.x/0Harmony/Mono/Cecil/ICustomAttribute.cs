using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200022D RID: 557
	internal interface ICustomAttribute
	{
		// Token: 0x17000264 RID: 612
		// (get) Token: 0x06000BE9 RID: 3049
		TypeReference AttributeType { get; }

		// Token: 0x17000265 RID: 613
		// (get) Token: 0x06000BEA RID: 3050
		bool HasFields { get; }

		// Token: 0x17000266 RID: 614
		// (get) Token: 0x06000BEB RID: 3051
		bool HasProperties { get; }

		// Token: 0x17000267 RID: 615
		// (get) Token: 0x06000BEC RID: 3052
		bool HasConstructorArguments { get; }

		// Token: 0x17000268 RID: 616
		// (get) Token: 0x06000BED RID: 3053
		Collection<CustomAttributeNamedArgument> Fields { get; }

		// Token: 0x17000269 RID: 617
		// (get) Token: 0x06000BEE RID: 3054
		Collection<CustomAttributeNamedArgument> Properties { get; }

		// Token: 0x1700026A RID: 618
		// (get) Token: 0x06000BEF RID: 3055
		Collection<CustomAttributeArgument> ConstructorArguments { get; }
	}
}
