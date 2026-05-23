using System;

namespace Mono.Cecil.PE
{
	// Token: 0x020002C7 RID: 711
	internal enum TextSegment
	{
		// Token: 0x040006E2 RID: 1762
		ImportAddressTable,
		// Token: 0x040006E3 RID: 1763
		CLIHeader,
		// Token: 0x040006E4 RID: 1764
		Code,
		// Token: 0x040006E5 RID: 1765
		Resources,
		// Token: 0x040006E6 RID: 1766
		Data,
		// Token: 0x040006E7 RID: 1767
		StrongNameSignature,
		// Token: 0x040006E8 RID: 1768
		MetadataHeader,
		// Token: 0x040006E9 RID: 1769
		TableHeap,
		// Token: 0x040006EA RID: 1770
		StringHeap,
		// Token: 0x040006EB RID: 1771
		UserStringHeap,
		// Token: 0x040006EC RID: 1772
		GuidHeap,
		// Token: 0x040006ED RID: 1773
		BlobHeap,
		// Token: 0x040006EE RID: 1774
		PdbHeap,
		// Token: 0x040006EF RID: 1775
		DebugDirectory,
		// Token: 0x040006F0 RID: 1776
		ImportDirectory,
		// Token: 0x040006F1 RID: 1777
		ImportHintNameTable,
		// Token: 0x040006F2 RID: 1778
		StartupStub
	}
}
