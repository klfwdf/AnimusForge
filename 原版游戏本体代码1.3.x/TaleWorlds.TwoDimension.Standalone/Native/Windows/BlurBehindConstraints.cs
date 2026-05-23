using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x0200001A RID: 26
	[Flags]
	public enum BlurBehindConstraints : uint
	{
		// Token: 0x0400007A RID: 122
		Enable = 1U,
		// Token: 0x0400007B RID: 123
		BlurRegion = 2U,
		// Token: 0x0400007C RID: 124
		TransitionOnMaximized = 4U
	}
}
