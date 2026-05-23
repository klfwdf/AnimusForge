using System;

namespace Mono.Cecil
{
	// Token: 0x02000265 RID: 613
	internal interface IMetadataResolver
	{
		// Token: 0x06000DE1 RID: 3553
		TypeDefinition Resolve(TypeReference type);

		// Token: 0x06000DE2 RID: 3554
		FieldDefinition Resolve(FieldReference field);

		// Token: 0x06000DE3 RID: 3555
		MethodDefinition Resolve(MethodReference method);
	}
}
