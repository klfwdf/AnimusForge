using System;

namespace Mono.Cecil
{
	// Token: 0x020002BB RID: 699
	internal enum TokenType : uint
	{
		// Token: 0x04000680 RID: 1664
		Module,
		// Token: 0x04000681 RID: 1665
		TypeRef = 16777216U,
		// Token: 0x04000682 RID: 1666
		TypeDef = 33554432U,
		// Token: 0x04000683 RID: 1667
		Field = 67108864U,
		// Token: 0x04000684 RID: 1668
		Method = 100663296U,
		// Token: 0x04000685 RID: 1669
		Param = 134217728U,
		// Token: 0x04000686 RID: 1670
		InterfaceImpl = 150994944U,
		// Token: 0x04000687 RID: 1671
		MemberRef = 167772160U,
		// Token: 0x04000688 RID: 1672
		CustomAttribute = 201326592U,
		// Token: 0x04000689 RID: 1673
		Permission = 234881024U,
		// Token: 0x0400068A RID: 1674
		Signature = 285212672U,
		// Token: 0x0400068B RID: 1675
		Event = 335544320U,
		// Token: 0x0400068C RID: 1676
		Property = 385875968U,
		// Token: 0x0400068D RID: 1677
		ModuleRef = 436207616U,
		// Token: 0x0400068E RID: 1678
		TypeSpec = 452984832U,
		// Token: 0x0400068F RID: 1679
		Assembly = 536870912U,
		// Token: 0x04000690 RID: 1680
		AssemblyRef = 587202560U,
		// Token: 0x04000691 RID: 1681
		File = 637534208U,
		// Token: 0x04000692 RID: 1682
		ExportedType = 654311424U,
		// Token: 0x04000693 RID: 1683
		ManifestResource = 671088640U,
		// Token: 0x04000694 RID: 1684
		GenericParam = 704643072U,
		// Token: 0x04000695 RID: 1685
		MethodSpec = 721420288U,
		// Token: 0x04000696 RID: 1686
		GenericParamConstraint = 738197504U,
		// Token: 0x04000697 RID: 1687
		Document = 805306368U,
		// Token: 0x04000698 RID: 1688
		MethodDebugInformation = 822083584U,
		// Token: 0x04000699 RID: 1689
		LocalScope = 838860800U,
		// Token: 0x0400069A RID: 1690
		LocalVariable = 855638016U,
		// Token: 0x0400069B RID: 1691
		LocalConstant = 872415232U,
		// Token: 0x0400069C RID: 1692
		ImportScope = 889192448U,
		// Token: 0x0400069D RID: 1693
		StateMachineMethod = 905969664U,
		// Token: 0x0400069E RID: 1694
		CustomDebugInformation = 922746880U,
		// Token: 0x0400069F RID: 1695
		String = 1879048192U
	}
}
