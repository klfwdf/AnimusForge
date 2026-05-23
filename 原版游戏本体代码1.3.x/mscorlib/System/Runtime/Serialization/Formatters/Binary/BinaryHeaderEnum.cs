using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000768 RID: 1896
	[Serializable]
	internal enum BinaryHeaderEnum
	{
		// Token: 0x040024E6 RID: 9446
		SerializedStreamHeader,
		// Token: 0x040024E7 RID: 9447
		Object,
		// Token: 0x040024E8 RID: 9448
		ObjectWithMap,
		// Token: 0x040024E9 RID: 9449
		ObjectWithMapAssemId,
		// Token: 0x040024EA RID: 9450
		ObjectWithMapTyped,
		// Token: 0x040024EB RID: 9451
		ObjectWithMapTypedAssemId,
		// Token: 0x040024EC RID: 9452
		ObjectString,
		// Token: 0x040024ED RID: 9453
		Array,
		// Token: 0x040024EE RID: 9454
		MemberPrimitiveTyped,
		// Token: 0x040024EF RID: 9455
		MemberReference,
		// Token: 0x040024F0 RID: 9456
		ObjectNull,
		// Token: 0x040024F1 RID: 9457
		MessageEnd,
		// Token: 0x040024F2 RID: 9458
		Assembly,
		// Token: 0x040024F3 RID: 9459
		ObjectNullMultiple256,
		// Token: 0x040024F4 RID: 9460
		ObjectNullMultiple,
		// Token: 0x040024F5 RID: 9461
		ArraySinglePrimitive,
		// Token: 0x040024F6 RID: 9462
		ArraySingleObject,
		// Token: 0x040024F7 RID: 9463
		ArraySingleString,
		// Token: 0x040024F8 RID: 9464
		CrossAppDomainMap,
		// Token: 0x040024F9 RID: 9465
		CrossAppDomainString,
		// Token: 0x040024FA RID: 9466
		CrossAppDomainAssembly,
		// Token: 0x040024FB RID: 9467
		MethodCall,
		// Token: 0x040024FC RID: 9468
		MethodReturn
	}
}
