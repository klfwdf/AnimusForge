using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000295 RID: 661
	internal interface ISecurityDeclarationProvider : IMetadataTokenProvider
	{
		// Token: 0x17000445 RID: 1093
		// (get) Token: 0x0600106D RID: 4205
		bool HasSecurityDeclarations { get; }

		// Token: 0x17000446 RID: 1094
		// (get) Token: 0x0600106E RID: 4206
		Collection<SecurityDeclaration> SecurityDeclarations { get; }
	}
}
