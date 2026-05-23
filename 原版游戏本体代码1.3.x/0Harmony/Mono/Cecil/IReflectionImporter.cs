using System;
using System.Reflection;

namespace Mono.Cecil
{
	// Token: 0x02000255 RID: 597
	internal interface IReflectionImporter
	{
		// Token: 0x06000D65 RID: 3429
		AssemblyNameReference ImportReference(AssemblyName reference);

		// Token: 0x06000D66 RID: 3430
		TypeReference ImportReference(Type type, IGenericParameterProvider context);

		// Token: 0x06000D67 RID: 3431
		FieldReference ImportReference(FieldInfo field, IGenericParameterProvider context);

		// Token: 0x06000D68 RID: 3432
		MethodReference ImportReference(MethodBase method, IGenericParameterProvider context);
	}
}
