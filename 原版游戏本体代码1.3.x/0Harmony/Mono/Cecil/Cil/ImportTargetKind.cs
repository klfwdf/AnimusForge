using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000311 RID: 785
	internal enum ImportTargetKind : byte
	{
		// Token: 0x04000A2E RID: 2606
		ImportNamespace = 1,
		// Token: 0x04000A2F RID: 2607
		ImportNamespaceInAssembly,
		// Token: 0x04000A30 RID: 2608
		ImportType,
		// Token: 0x04000A31 RID: 2609
		ImportXmlNamespaceWithAlias,
		// Token: 0x04000A32 RID: 2610
		ImportAlias,
		// Token: 0x04000A33 RID: 2611
		DefineAssemblyAlias,
		// Token: 0x04000A34 RID: 2612
		DefineNamespaceAlias,
		// Token: 0x04000A35 RID: 2613
		DefineNamespaceInAssemblyAlias,
		// Token: 0x04000A36 RID: 2614
		DefineTypeAlias
	}
}
