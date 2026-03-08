using System;

namespace Mono.Cecil
{
	// Token: 0x02000253 RID: 595
	internal interface IMetadataImporter
	{
		// Token: 0x06000D60 RID: 3424
		AssemblyNameReference ImportReference(AssemblyNameReference reference);

		// Token: 0x06000D61 RID: 3425
		TypeReference ImportReference(TypeReference type, IGenericParameterProvider context);

		// Token: 0x06000D62 RID: 3426
		FieldReference ImportReference(FieldReference field, IGenericParameterProvider context);

		// Token: 0x06000D63 RID: 3427
		MethodReference ImportReference(MethodReference method, IGenericParameterProvider context);
	}
}
