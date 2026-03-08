using System;

namespace Mono.Cecil
{
	// Token: 0x02000282 RID: 642
	[Flags]
	internal enum ModuleCharacteristics
	{
		// Token: 0x04000507 RID: 1287
		HighEntropyVA = 32,
		// Token: 0x04000508 RID: 1288
		DynamicBase = 64,
		// Token: 0x04000509 RID: 1289
		NoSEH = 1024,
		// Token: 0x0400050A RID: 1290
		NXCompat = 256,
		// Token: 0x0400050B RID: 1291
		AppContainer = 4096,
		// Token: 0x0400050C RID: 1292
		TerminalServerAware = 32768
	}
}
