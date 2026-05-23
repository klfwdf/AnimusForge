using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Backports
{
	// Token: 0x02000466 RID: 1126
	internal static class MethodImplOptionsEx
	{
		// Token: 0x04001078 RID: 4216
		public const MethodImplOptions Unmanaged = MethodImplOptions.Unmanaged;

		// Token: 0x04001079 RID: 4217
		public const MethodImplOptions NoInlining = MethodImplOptions.NoInlining;

		// Token: 0x0400107A RID: 4218
		public const MethodImplOptions ForwardRef = MethodImplOptions.ForwardRef;

		// Token: 0x0400107B RID: 4219
		public const MethodImplOptions Synchronized = MethodImplOptions.Synchronized;

		// Token: 0x0400107C RID: 4220
		public const MethodImplOptions NoOptimization = MethodImplOptions.NoOptimization;

		// Token: 0x0400107D RID: 4221
		public const MethodImplOptions PreserveSig = MethodImplOptions.PreserveSig;

		// Token: 0x0400107E RID: 4222
		public const MethodImplOptions AggressiveInlining = MethodImplOptions.AggressiveInlining;

		// Token: 0x0400107F RID: 4223
		public const MethodImplOptions AggressiveOptimization = (MethodImplOptions)512;

		// Token: 0x04001080 RID: 4224
		public const MethodImplOptions InternalCall = MethodImplOptions.InternalCall;
	}
}
