using System;

namespace Mono.Cecil
{
	// Token: 0x02000284 RID: 644
	internal enum NativeType
	{
		// Token: 0x04000510 RID: 1296
		None = 102,
		// Token: 0x04000511 RID: 1297
		Boolean = 2,
		// Token: 0x04000512 RID: 1298
		I1,
		// Token: 0x04000513 RID: 1299
		U1,
		// Token: 0x04000514 RID: 1300
		I2,
		// Token: 0x04000515 RID: 1301
		U2,
		// Token: 0x04000516 RID: 1302
		I4,
		// Token: 0x04000517 RID: 1303
		U4,
		// Token: 0x04000518 RID: 1304
		I8,
		// Token: 0x04000519 RID: 1305
		U8,
		// Token: 0x0400051A RID: 1306
		R4,
		// Token: 0x0400051B RID: 1307
		R8,
		// Token: 0x0400051C RID: 1308
		LPStr = 20,
		// Token: 0x0400051D RID: 1309
		Int = 31,
		// Token: 0x0400051E RID: 1310
		UInt,
		// Token: 0x0400051F RID: 1311
		Func = 38,
		// Token: 0x04000520 RID: 1312
		Array = 42,
		// Token: 0x04000521 RID: 1313
		Currency = 15,
		// Token: 0x04000522 RID: 1314
		BStr = 19,
		// Token: 0x04000523 RID: 1315
		LPWStr = 21,
		// Token: 0x04000524 RID: 1316
		LPTStr,
		// Token: 0x04000525 RID: 1317
		FixedSysString,
		// Token: 0x04000526 RID: 1318
		IUnknown = 25,
		// Token: 0x04000527 RID: 1319
		IDispatch,
		// Token: 0x04000528 RID: 1320
		Struct,
		// Token: 0x04000529 RID: 1321
		IntF,
		// Token: 0x0400052A RID: 1322
		SafeArray,
		// Token: 0x0400052B RID: 1323
		FixedArray,
		// Token: 0x0400052C RID: 1324
		ByValStr = 34,
		// Token: 0x0400052D RID: 1325
		ANSIBStr,
		// Token: 0x0400052E RID: 1326
		TBStr,
		// Token: 0x0400052F RID: 1327
		VariantBool,
		// Token: 0x04000530 RID: 1328
		ASAny = 40,
		// Token: 0x04000531 RID: 1329
		LPStruct = 43,
		// Token: 0x04000532 RID: 1330
		CustomMarshaler,
		// Token: 0x04000533 RID: 1331
		Error,
		// Token: 0x04000534 RID: 1332
		Max = 80
	}
}
