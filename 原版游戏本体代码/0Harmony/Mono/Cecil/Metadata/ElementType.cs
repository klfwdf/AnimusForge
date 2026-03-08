using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002D5 RID: 725
	internal enum ElementType : byte
	{
		// Token: 0x04000712 RID: 1810
		None,
		// Token: 0x04000713 RID: 1811
		Void,
		// Token: 0x04000714 RID: 1812
		Boolean,
		// Token: 0x04000715 RID: 1813
		Char,
		// Token: 0x04000716 RID: 1814
		I1,
		// Token: 0x04000717 RID: 1815
		U1,
		// Token: 0x04000718 RID: 1816
		I2,
		// Token: 0x04000719 RID: 1817
		U2,
		// Token: 0x0400071A RID: 1818
		I4,
		// Token: 0x0400071B RID: 1819
		U4,
		// Token: 0x0400071C RID: 1820
		I8,
		// Token: 0x0400071D RID: 1821
		U8,
		// Token: 0x0400071E RID: 1822
		R4,
		// Token: 0x0400071F RID: 1823
		R8,
		// Token: 0x04000720 RID: 1824
		String,
		// Token: 0x04000721 RID: 1825
		Ptr,
		// Token: 0x04000722 RID: 1826
		ByRef,
		// Token: 0x04000723 RID: 1827
		ValueType,
		// Token: 0x04000724 RID: 1828
		Class,
		// Token: 0x04000725 RID: 1829
		Var,
		// Token: 0x04000726 RID: 1830
		Array,
		// Token: 0x04000727 RID: 1831
		GenericInst,
		// Token: 0x04000728 RID: 1832
		TypedByRef,
		// Token: 0x04000729 RID: 1833
		I = 24,
		// Token: 0x0400072A RID: 1834
		U,
		// Token: 0x0400072B RID: 1835
		FnPtr = 27,
		// Token: 0x0400072C RID: 1836
		Object,
		// Token: 0x0400072D RID: 1837
		SzArray,
		// Token: 0x0400072E RID: 1838
		MVar,
		// Token: 0x0400072F RID: 1839
		CModReqD,
		// Token: 0x04000730 RID: 1840
		CModOpt,
		// Token: 0x04000731 RID: 1841
		Internal,
		// Token: 0x04000732 RID: 1842
		Modifier = 64,
		// Token: 0x04000733 RID: 1843
		Sentinel,
		// Token: 0x04000734 RID: 1844
		Pinned = 69,
		// Token: 0x04000735 RID: 1845
		Type = 80,
		// Token: 0x04000736 RID: 1846
		Boxed,
		// Token: 0x04000737 RID: 1847
		Enum = 85
	}
}
