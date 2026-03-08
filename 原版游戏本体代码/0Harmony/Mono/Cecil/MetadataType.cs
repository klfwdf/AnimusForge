using System;

namespace Mono.Cecil
{
	// Token: 0x020002A9 RID: 681
	internal enum MetadataType : byte
	{
		// Token: 0x0400060A RID: 1546
		Void = 1,
		// Token: 0x0400060B RID: 1547
		Boolean,
		// Token: 0x0400060C RID: 1548
		Char,
		// Token: 0x0400060D RID: 1549
		SByte,
		// Token: 0x0400060E RID: 1550
		Byte,
		// Token: 0x0400060F RID: 1551
		Int16,
		// Token: 0x04000610 RID: 1552
		UInt16,
		// Token: 0x04000611 RID: 1553
		Int32,
		// Token: 0x04000612 RID: 1554
		UInt32,
		// Token: 0x04000613 RID: 1555
		Int64,
		// Token: 0x04000614 RID: 1556
		UInt64,
		// Token: 0x04000615 RID: 1557
		Single,
		// Token: 0x04000616 RID: 1558
		Double,
		// Token: 0x04000617 RID: 1559
		String,
		// Token: 0x04000618 RID: 1560
		Pointer,
		// Token: 0x04000619 RID: 1561
		ByReference,
		// Token: 0x0400061A RID: 1562
		ValueType,
		// Token: 0x0400061B RID: 1563
		Class,
		// Token: 0x0400061C RID: 1564
		Var,
		// Token: 0x0400061D RID: 1565
		Array,
		// Token: 0x0400061E RID: 1566
		GenericInstance,
		// Token: 0x0400061F RID: 1567
		TypedByReference,
		// Token: 0x04000620 RID: 1568
		IntPtr = 24,
		// Token: 0x04000621 RID: 1569
		UIntPtr,
		// Token: 0x04000622 RID: 1570
		FunctionPointer = 27,
		// Token: 0x04000623 RID: 1571
		Object,
		// Token: 0x04000624 RID: 1572
		MVar = 30,
		// Token: 0x04000625 RID: 1573
		RequiredModifier,
		// Token: 0x04000626 RID: 1574
		OptionalModifier,
		// Token: 0x04000627 RID: 1575
		Sentinel = 65,
		// Token: 0x04000628 RID: 1576
		Pinned = 69
	}
}
