using System;

namespace Mono.Cecil
{
	// Token: 0x02000272 RID: 626
	[Flags]
	internal enum MethodSemanticsAttributes : ushort
	{
		// Token: 0x0400048D RID: 1165
		None = 0,
		// Token: 0x0400048E RID: 1166
		Setter = 1,
		// Token: 0x0400048F RID: 1167
		Getter = 2,
		// Token: 0x04000490 RID: 1168
		Other = 4,
		// Token: 0x04000491 RID: 1169
		AddOn = 8,
		// Token: 0x04000492 RID: 1170
		RemoveOn = 16,
		// Token: 0x04000493 RID: 1171
		Fire = 32
	}
}
