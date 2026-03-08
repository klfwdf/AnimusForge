using System;

namespace Mono.Cecil
{
	// Token: 0x02000274 RID: 628
	internal interface IModifierType
	{
		// Token: 0x170003A3 RID: 931
		// (get) Token: 0x06000EED RID: 3821
		TypeReference ModifierType { get; }

		// Token: 0x170003A4 RID: 932
		// (get) Token: 0x06000EEE RID: 3822
		TypeReference ElementType { get; }
	}
}
