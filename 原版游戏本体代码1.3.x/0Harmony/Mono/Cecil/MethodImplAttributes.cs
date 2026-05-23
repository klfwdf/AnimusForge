using System;

namespace Mono.Cecil
{
	// Token: 0x0200026E RID: 622
	[Flags]
	internal enum MethodImplAttributes : ushort
	{
		// Token: 0x04000471 RID: 1137
		CodeTypeMask = 3,
		// Token: 0x04000472 RID: 1138
		IL = 0,
		// Token: 0x04000473 RID: 1139
		Native = 1,
		// Token: 0x04000474 RID: 1140
		OPTIL = 2,
		// Token: 0x04000475 RID: 1141
		Runtime = 3,
		// Token: 0x04000476 RID: 1142
		ManagedMask = 4,
		// Token: 0x04000477 RID: 1143
		Unmanaged = 4,
		// Token: 0x04000478 RID: 1144
		Managed = 0,
		// Token: 0x04000479 RID: 1145
		ForwardRef = 16,
		// Token: 0x0400047A RID: 1146
		PreserveSig = 128,
		// Token: 0x0400047B RID: 1147
		InternalCall = 4096,
		// Token: 0x0400047C RID: 1148
		Synchronized = 32,
		// Token: 0x0400047D RID: 1149
		NoOptimization = 64,
		// Token: 0x0400047E RID: 1150
		NoInlining = 8,
		// Token: 0x0400047F RID: 1151
		AggressiveInlining = 256,
		// Token: 0x04000480 RID: 1152
		AggressiveOptimization = 512
	}
}
