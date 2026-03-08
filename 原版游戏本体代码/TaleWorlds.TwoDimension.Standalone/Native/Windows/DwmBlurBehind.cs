using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x02000019 RID: 25
	internal struct DwmBlurBehind
	{
		// Token: 0x04000075 RID: 117
		public BlurBehindConstraints dwFlags;

		// Token: 0x04000076 RID: 118
		[MarshalAs(UnmanagedType.Bool)]
		public bool fEnable;

		// Token: 0x04000077 RID: 119
		public IntPtr hRgnBlur;

		// Token: 0x04000078 RID: 120
		[MarshalAs(UnmanagedType.Bool)]
		public bool fTransitionOnMaximized;
	}
}
