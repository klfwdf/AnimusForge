using System;

namespace System
{
	// Token: 0x020000B9 RID: 185
	[Serializable]
	internal enum ConfigNodeType
	{
		// Token: 0x04000410 RID: 1040
		Element = 1,
		// Token: 0x04000411 RID: 1041
		Attribute,
		// Token: 0x04000412 RID: 1042
		Pi,
		// Token: 0x04000413 RID: 1043
		XmlDecl,
		// Token: 0x04000414 RID: 1044
		DocType,
		// Token: 0x04000415 RID: 1045
		DTDAttribute,
		// Token: 0x04000416 RID: 1046
		EntityDecl,
		// Token: 0x04000417 RID: 1047
		ElementDecl,
		// Token: 0x04000418 RID: 1048
		AttlistDecl,
		// Token: 0x04000419 RID: 1049
		Notation,
		// Token: 0x0400041A RID: 1050
		Group,
		// Token: 0x0400041B RID: 1051
		IncludeSect,
		// Token: 0x0400041C RID: 1052
		PCData,
		// Token: 0x0400041D RID: 1053
		CData,
		// Token: 0x0400041E RID: 1054
		IgnoreSect,
		// Token: 0x0400041F RID: 1055
		Comment,
		// Token: 0x04000420 RID: 1056
		EntityRef,
		// Token: 0x04000421 RID: 1057
		Whitespace,
		// Token: 0x04000422 RID: 1058
		Name,
		// Token: 0x04000423 RID: 1059
		NMToken,
		// Token: 0x04000424 RID: 1060
		String,
		// Token: 0x04000425 RID: 1061
		Peref,
		// Token: 0x04000426 RID: 1062
		Model,
		// Token: 0x04000427 RID: 1063
		ATTDef,
		// Token: 0x04000428 RID: 1064
		ATTType,
		// Token: 0x04000429 RID: 1065
		ATTPresence,
		// Token: 0x0400042A RID: 1066
		DTDSubset,
		// Token: 0x0400042B RID: 1067
		LastNodeType
	}
}
