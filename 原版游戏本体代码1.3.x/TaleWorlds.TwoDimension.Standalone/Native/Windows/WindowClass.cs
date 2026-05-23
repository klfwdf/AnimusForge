using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x02000027 RID: 39
	public struct WindowClass
	{
		// Token: 0x040000B9 RID: 185
		public uint style;

		// Token: 0x040000BA RID: 186
		[MarshalAs(UnmanagedType.FunctionPtr)]
		public WndProc lpfnWndProc;

		// Token: 0x040000BB RID: 187
		public int cbClsExtra;

		// Token: 0x040000BC RID: 188
		public int cbWndExtra;

		// Token: 0x040000BD RID: 189
		public IntPtr hInstance;

		// Token: 0x040000BE RID: 190
		public IntPtr hIcon;

		// Token: 0x040000BF RID: 191
		public IntPtr hCursor;

		// Token: 0x040000C0 RID: 192
		public IntPtr hbrBackground;

		// Token: 0x040000C1 RID: 193
		[MarshalAs(UnmanagedType.LPTStr)]
		public string lpszMenuName;

		// Token: 0x040000C2 RID: 194
		[MarshalAs(UnmanagedType.LPTStr)]
		public string lpszClassName;
	}
}
