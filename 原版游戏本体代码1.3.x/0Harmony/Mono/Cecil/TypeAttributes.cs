using System;

namespace Mono.Cecil
{
	// Token: 0x020002A0 RID: 672
	[Flags]
	internal enum TypeAttributes : uint
	{
		// Token: 0x040005B9 RID: 1465
		VisibilityMask = 7U,
		// Token: 0x040005BA RID: 1466
		NotPublic = 0U,
		// Token: 0x040005BB RID: 1467
		Public = 1U,
		// Token: 0x040005BC RID: 1468
		NestedPublic = 2U,
		// Token: 0x040005BD RID: 1469
		NestedPrivate = 3U,
		// Token: 0x040005BE RID: 1470
		NestedFamily = 4U,
		// Token: 0x040005BF RID: 1471
		NestedAssembly = 5U,
		// Token: 0x040005C0 RID: 1472
		NestedFamANDAssem = 6U,
		// Token: 0x040005C1 RID: 1473
		NestedFamORAssem = 7U,
		// Token: 0x040005C2 RID: 1474
		LayoutMask = 24U,
		// Token: 0x040005C3 RID: 1475
		AutoLayout = 0U,
		// Token: 0x040005C4 RID: 1476
		SequentialLayout = 8U,
		// Token: 0x040005C5 RID: 1477
		ExplicitLayout = 16U,
		// Token: 0x040005C6 RID: 1478
		ClassSemanticMask = 32U,
		// Token: 0x040005C7 RID: 1479
		Class = 0U,
		// Token: 0x040005C8 RID: 1480
		Interface = 32U,
		// Token: 0x040005C9 RID: 1481
		Abstract = 128U,
		// Token: 0x040005CA RID: 1482
		Sealed = 256U,
		// Token: 0x040005CB RID: 1483
		SpecialName = 1024U,
		// Token: 0x040005CC RID: 1484
		Import = 4096U,
		// Token: 0x040005CD RID: 1485
		Serializable = 8192U,
		// Token: 0x040005CE RID: 1486
		WindowsRuntime = 16384U,
		// Token: 0x040005CF RID: 1487
		StringFormatMask = 196608U,
		// Token: 0x040005D0 RID: 1488
		AnsiClass = 0U,
		// Token: 0x040005D1 RID: 1489
		UnicodeClass = 65536U,
		// Token: 0x040005D2 RID: 1490
		AutoClass = 131072U,
		// Token: 0x040005D3 RID: 1491
		BeforeFieldInit = 1048576U,
		// Token: 0x040005D4 RID: 1492
		RTSpecialName = 2048U,
		// Token: 0x040005D5 RID: 1493
		HasSecurity = 262144U,
		// Token: 0x040005D6 RID: 1494
		Forwarder = 2097152U
	}
}
