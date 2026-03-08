using System;

namespace System.Reflection
{
	// Token: 0x020005F6 RID: 1526
	[Serializable]
	internal enum CorElementType : byte
	{
		// Token: 0x04001CE1 RID: 7393
		End,
		// Token: 0x04001CE2 RID: 7394
		Void,
		// Token: 0x04001CE3 RID: 7395
		Boolean,
		// Token: 0x04001CE4 RID: 7396
		Char,
		// Token: 0x04001CE5 RID: 7397
		I1,
		// Token: 0x04001CE6 RID: 7398
		U1,
		// Token: 0x04001CE7 RID: 7399
		I2,
		// Token: 0x04001CE8 RID: 7400
		U2,
		// Token: 0x04001CE9 RID: 7401
		I4,
		// Token: 0x04001CEA RID: 7402
		U4,
		// Token: 0x04001CEB RID: 7403
		I8,
		// Token: 0x04001CEC RID: 7404
		U8,
		// Token: 0x04001CED RID: 7405
		R4,
		// Token: 0x04001CEE RID: 7406
		R8,
		// Token: 0x04001CEF RID: 7407
		String,
		// Token: 0x04001CF0 RID: 7408
		Ptr,
		// Token: 0x04001CF1 RID: 7409
		ByRef,
		// Token: 0x04001CF2 RID: 7410
		ValueType,
		// Token: 0x04001CF3 RID: 7411
		Class,
		// Token: 0x04001CF4 RID: 7412
		Var,
		// Token: 0x04001CF5 RID: 7413
		Array,
		// Token: 0x04001CF6 RID: 7414
		GenericInst,
		// Token: 0x04001CF7 RID: 7415
		TypedByRef,
		// Token: 0x04001CF8 RID: 7416
		I = 24,
		// Token: 0x04001CF9 RID: 7417
		U,
		// Token: 0x04001CFA RID: 7418
		FnPtr = 27,
		// Token: 0x04001CFB RID: 7419
		Object,
		// Token: 0x04001CFC RID: 7420
		SzArray,
		// Token: 0x04001CFD RID: 7421
		MVar,
		// Token: 0x04001CFE RID: 7422
		CModReqd,
		// Token: 0x04001CFF RID: 7423
		CModOpt,
		// Token: 0x04001D00 RID: 7424
		Internal,
		// Token: 0x04001D01 RID: 7425
		Max,
		// Token: 0x04001D02 RID: 7426
		Modifier = 64,
		// Token: 0x04001D03 RID: 7427
		Sentinel,
		// Token: 0x04001D04 RID: 7428
		Pinned = 69
	}
}
