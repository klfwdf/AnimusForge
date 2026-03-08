using System;
using System.Runtime.CompilerServices;

namespace MonoMod
{
	// Token: 0x02000008 RID: 8
	public static class ILHelpers
	{
		// Token: 0x06000009 RID: 9 RVA: 0x000020B8 File Offset: 0x000002B8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T TailCallDelegatePtr<T>(IntPtr source)
		{
			return calli(T(), source);
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000020C0 File Offset: 0x000002C0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T TailCallFunc<T>(Func<T> func)
		{
			return func();
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000020CC File Offset: 0x000002CC
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
