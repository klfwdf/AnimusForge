using System;

namespace Mono.Cecil
{
	// Token: 0x02000252 RID: 594
	internal interface IMetadataImporterProvider
	{
		// Token: 0x06000D5F RID: 3423
		IMetadataImporter GetMetadataImporter(ModuleDefinition module);
	}
}
