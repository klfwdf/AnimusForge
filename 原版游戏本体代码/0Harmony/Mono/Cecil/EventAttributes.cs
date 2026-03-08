using System;

namespace Mono.Cecil
{
	// Token: 0x02000232 RID: 562
	[Flags]
	internal enum EventAttributes : ushort
	{
		// Token: 0x040003A4 RID: 932
		None = 0,
		// Token: 0x040003A5 RID: 933
		SpecialName = 512,
		// Token: 0x040003A6 RID: 934
		RTSpecialName = 1024
	}
}
