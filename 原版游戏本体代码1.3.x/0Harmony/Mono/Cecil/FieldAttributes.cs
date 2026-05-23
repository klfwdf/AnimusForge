using System;

namespace Mono.Cecil
{
	// Token: 0x02000237 RID: 567
	[Flags]
	internal enum FieldAttributes : ushort
	{
		// Token: 0x040003B9 RID: 953
		FieldAccessMask = 7,
		// Token: 0x040003BA RID: 954
		CompilerControlled = 0,
		// Token: 0x040003BB RID: 955
		Private = 1,
		// Token: 0x040003BC RID: 956
		FamANDAssem = 2,
		// Token: 0x040003BD RID: 957
		Assembly = 3,
		// Token: 0x040003BE RID: 958
		Family = 4,
		// Token: 0x040003BF RID: 959
		FamORAssem = 5,
		// Token: 0x040003C0 RID: 960
		Public = 6,
		// Token: 0x040003C1 RID: 961
		Static = 16,
		// Token: 0x040003C2 RID: 962
		InitOnly = 32,
		// Token: 0x040003C3 RID: 963
		Literal = 64,
		// Token: 0x040003C4 RID: 964
		NotSerialized = 128,
		// Token: 0x040003C5 RID: 965
		SpecialName = 512,
		// Token: 0x040003C6 RID: 966
		PInvokeImpl = 8192,
		// Token: 0x040003C7 RID: 967
		RTSpecialName = 1024,
		// Token: 0x040003C8 RID: 968
		HasFieldMarshal = 4096,
		// Token: 0x040003C9 RID: 969
		HasDefault = 32768,
		// Token: 0x040003CA RID: 970
		HasFieldRVA = 256
	}
}
