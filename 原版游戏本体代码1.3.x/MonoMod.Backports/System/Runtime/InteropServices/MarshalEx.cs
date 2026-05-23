using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200003A RID: 58
	[NullableContext(2)]
	[Nullable(0)]
	public static class MarshalEx
	{
		// Token: 0x06000242 RID: 578 RVA: 0x0000BE33 File Offset: 0x0000A033
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetLastPInvokeError()
		{
			return Marshal.GetLastWin32Error();
		}

		// Token: 0x06000243 RID: 579 RVA: 0x0000BE3A File Offset: 0x0000A03A
		public static void SetLastPInvokeError(int error)
		{
			Action<int> marshal_SetLastWin32Error = MarshalEx.Marshal_SetLastWin32Error;
			if (marshal_SetLastWin32Error == null)
			{
				throw new PlatformNotSupportedException("Cannot set last P/Invoke error (no method Marshal.SetLastWin32Error or Marshal.SetLastPInvokeError)");
			}
			marshal_SetLastWin32Error(error);
		}

		// Token: 0x04000078 RID: 120
		private static readonly MethodInfo Marshal_SetLastWin32Error_Meth = typeof(Marshal).GetMethod("SetLastPInvokeError", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? typeof(Marshal).GetMethod("SetLastWin32Error", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		// Token: 0x04000079 RID: 121
		private static readonly Action<int> Marshal_SetLastWin32Error = ((MarshalEx.Marshal_SetLastWin32Error_Meth == null) ? null : ((Action<int>)Delegate.CreateDelegate(typeof(Action<int>), MarshalEx.Marshal_SetLastWin32Error_Meth)));
	}
}
