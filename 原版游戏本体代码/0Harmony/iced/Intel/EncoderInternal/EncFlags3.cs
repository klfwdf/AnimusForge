using System;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000683 RID: 1667
	[Flags]
	internal enum EncFlags3 : uint
	{
		// Token: 0x040034ED RID: 13549
		None = 0U,
		// Token: 0x040034EE RID: 13550
		EncodingShift = 0U,
		// Token: 0x040034EF RID: 13551
		EncodingMask = 7U,
		// Token: 0x040034F0 RID: 13552
		OperandSizeShift = 3U,
		// Token: 0x040034F1 RID: 13553
		OperandSizeMask = 3U,
		// Token: 0x040034F2 RID: 13554
		AddressSizeShift = 5U,
		// Token: 0x040034F3 RID: 13555
		AddressSizeMask = 3U,
		// Token: 0x040034F4 RID: 13556
		TupleTypeShift = 7U,
		// Token: 0x040034F5 RID: 13557
		TupleTypeMask = 31U,
		// Token: 0x040034F6 RID: 13558
		DefaultOpSize64 = 4096U,
		// Token: 0x040034F7 RID: 13559
		HasRmGroupIndex = 8192U,
		// Token: 0x040034F8 RID: 13560
		IntelForceOpSize64 = 16384U,
		// Token: 0x040034F9 RID: 13561
		Fwait = 32768U,
		// Token: 0x040034FA RID: 13562
		Bit16or32 = 65536U,
		// Token: 0x040034FB RID: 13563
		Bit64 = 131072U,
		// Token: 0x040034FC RID: 13564
		Lock = 262144U,
		// Token: 0x040034FD RID: 13565
		Xacquire = 524288U,
		// Token: 0x040034FE RID: 13566
		Xrelease = 1048576U,
		// Token: 0x040034FF RID: 13567
		Rep = 2097152U,
		// Token: 0x04003500 RID: 13568
		Repne = 4194304U,
		// Token: 0x04003501 RID: 13569
		Bnd = 8388608U,
		// Token: 0x04003502 RID: 13570
		HintTaken = 16777216U,
		// Token: 0x04003503 RID: 13571
		Notrack = 33554432U,
		// Token: 0x04003504 RID: 13572
		Broadcast = 67108864U,
		// Token: 0x04003505 RID: 13573
		RoundingControl = 134217728U,
		// Token: 0x04003506 RID: 13574
		SuppressAllExceptions = 268435456U,
		// Token: 0x04003507 RID: 13575
		OpMaskRegister = 536870912U,
		// Token: 0x04003508 RID: 13576
		ZeroingMasking = 1073741824U,
		// Token: 0x04003509 RID: 13577
		RequireOpMaskRegister = 2147483648U
	}
}
