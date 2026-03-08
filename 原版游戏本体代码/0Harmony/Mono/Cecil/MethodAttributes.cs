using System;

namespace Mono.Cecil
{
	// Token: 0x0200026A RID: 618
	[Flags]
	internal enum MethodAttributes : ushort
	{
		// Token: 0x0400043E RID: 1086
		MemberAccessMask = 7,
		// Token: 0x0400043F RID: 1087
		CompilerControlled = 0,
		// Token: 0x04000440 RID: 1088
		Private = 1,
		// Token: 0x04000441 RID: 1089
		FamANDAssem = 2,
		// Token: 0x04000442 RID: 1090
		Assembly = 3,
		// Token: 0x04000443 RID: 1091
		Family = 4,
		// Token: 0x04000444 RID: 1092
		FamORAssem = 5,
		// Token: 0x04000445 RID: 1093
		Public = 6,
		// Token: 0x04000446 RID: 1094
		Static = 16,
		// Token: 0x04000447 RID: 1095
		Final = 32,
		// Token: 0x04000448 RID: 1096
		Virtual = 64,
		// Token: 0x04000449 RID: 1097
		HideBySig = 128,
		// Token: 0x0400044A RID: 1098
		VtableLayoutMask = 256,
		// Token: 0x0400044B RID: 1099
		ReuseSlot = 0,
		// Token: 0x0400044C RID: 1100
		NewSlot = 256,
		// Token: 0x0400044D RID: 1101
		CheckAccessOnOverride = 512,
		// Token: 0x0400044E RID: 1102
		Abstract = 1024,
		// Token: 0x0400044F RID: 1103
		SpecialName = 2048,
		// Token: 0x04000450 RID: 1104
		PInvokeImpl = 8192,
		// Token: 0x04000451 RID: 1105
		UnmanagedExport = 8,
		// Token: 0x04000452 RID: 1106
		RTSpecialName = 4096,
		// Token: 0x04000453 RID: 1107
		HasSecurity = 16384,
		// Token: 0x04000454 RID: 1108
		RequireSecObject = 32768
	}
}
