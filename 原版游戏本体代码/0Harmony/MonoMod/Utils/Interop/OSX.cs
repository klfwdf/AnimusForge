using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoMod.Utils.Interop
{
	// Token: 0x020008EA RID: 2282
	internal static class OSX
	{
		// Token: 0x06002F7E RID: 12158
		[DllImport("libSystem", CallingConvention = CallingConvention.Cdecl, EntryPoint = "uname", SetLastError = true)]
		public unsafe static extern int Uname(byte* buf);

		// Token: 0x04003BB4 RID: 15284
		[Nullable(1)]
		public const string LibSystem = "libSystem";
	}
}
