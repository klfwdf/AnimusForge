using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	// Token: 0x020004AA RID: 1194
	[NullableContext(2)]
	[Nullable(0)]
	internal static class MarshalEx
	{
		// Token: 0x06001ABD RID: 6845 RVA: 0x00057529 File Offset: 0x00055729
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetLastPInvokeError()
		{
			return Marshal.GetLastWin32Error();
		}

		// Token: 0x06001ABE RID: 6846 RVA: 0x00057530 File Offset: 0x00055730
		public static void SetLastPInvokeError(int error)
		{
			Action<int> marshal_SetLastWin32Error = MarshalEx.Marshal_SetLastWin32Error;
			if (marshal_SetLastWin32Error == null)
			{
				throw new PlatformNotSupportedException("Cannot set last P/Invoke error (no method Marshal.SetLastWin32Error or Marshal.SetLastPInvokeError)");
			}
			marshal_SetLastWin32Error(error);
		}

		// Token: 0x0400111B RID: 4379
		private static readonly MethodInfo Marshal_SetLastWin32Error_Meth = typeof(Marshal).GetMethod("SetLastPInvokeError", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? typeof(Marshal).GetMethod("SetLastWin32Error", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		// Token: 0x0400111C RID: 4380
		private static readonly Action<int> Marshal_SetLastWin32Error = ((MarshalEx.Marshal_SetLastWin32Error_Meth == null) ? null : ((Action<int>)Delegate.CreateDelegate(typeof(Action<int>), MarshalEx.Marshal_SetLastWin32Error_Meth)));
	}
}
