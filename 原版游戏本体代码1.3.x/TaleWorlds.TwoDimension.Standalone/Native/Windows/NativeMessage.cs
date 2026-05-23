using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x0200001F RID: 31
	public struct NativeMessage
	{
		// Token: 0x0400007F RID: 127
		public IntPtr handle;

		// Token: 0x04000080 RID: 128
		public WindowMessage msg;

		// Token: 0x04000081 RID: 129
		public IntPtr wParam;

		// Token: 0x04000082 RID: 130
		public IntPtr lParam;

		// Token: 0x04000083 RID: 131
		public uint time;

		// Token: 0x04000084 RID: 132
		public Point p;
	}
}
