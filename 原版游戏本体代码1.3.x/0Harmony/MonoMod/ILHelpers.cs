using System;
using System.Runtime.CompilerServices;

namespace MonoMod
{
	// Token: 0x02000805 RID: 2053
	internal static class ILHelpers
	{
		// Token: 0x06002703 RID: 9987 RVA: 0x00087451 File Offset: 0x00085651
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T TailCallDelegatePtr<T>(IntPtr source)
		{
			return calli(T(), source);
		}

		// Token: 0x06002704 RID: 9988 RVA: 0x00087459 File Offset: 0x00085659
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T TailCallFunc<T>(Func<T> func)
		{
			return func();
		}

		// Token: 0x06002705 RID: 9989 RVA: 0x00087464 File Offset: 0x00085664
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static ref T ObjectAsRef<T>(object obj)
		{
			fixed (object obj2 = obj)
			{
				T** ptr = (T**)(&obj2);
				return *(IntPtr*)ptr;
			}
		}
	}
}
