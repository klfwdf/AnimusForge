using System;

namespace Mono.Cecil
{
	// Token: 0x02000254 RID: 596
	internal interface IReflectionImporterProvider
	{
		// Token: 0x06000D64 RID: 3428
		IReflectionImporter GetReflectionImporter(ModuleDefinition module);
	}
}
