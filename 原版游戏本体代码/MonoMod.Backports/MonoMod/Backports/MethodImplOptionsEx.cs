using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Backports
{
	// Token: 0x0200000C RID: 12
	public static class MethodImplOptionsEx
	{
		// Token: 0x04000009 RID: 9
		public const MethodImplOptions Unmanaged = MethodImplOptions.Unmanaged;

		// Token: 0x0400000A RID: 10
		public const MethodImplOptions NoInlining = MethodImplOptions.NoInlining;

		// Token: 0x0400000B RID: 11
		public const MethodImplOptions ForwardRef = MethodImplOptions.ForwardRef;

		// Token: 0x0400000C RID: 12
		public const MethodImplOptions Synchronized = MethodImplOptions.Synchronized;

		// Token: 0x0400000D RID: 13
		public const MethodImplOptions NoOptimization = MethodImplOptions.NoOptimization;

		// Token: 0x0400000E RID: 14
		public const MethodImplOptions PreserveSig = MethodImplOptions.PreserveSig;

		// Token: 0x0400000F RID: 15
		public const MethodImplOptions AggressiveInlining = MethodImplOptions.AggressiveInlining;

		// Token: 0x04000010 RID: 16
		public const MethodImplOptions AggressiveOptimization = (MethodImplOptions)512;

		// Token: 0x04000011 RID: 17
		public const MethodImplOptions InternalCall = MethodImplOptions.InternalCall;
	}
}
