using System;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006A6 RID: 1702
	[Flags]
	internal enum HandlerFlags : uint
	{
		// Token: 0x04003587 RID: 13703
		None = 0U,
		// Token: 0x04003588 RID: 13704
		Xacquire = 1U,
		// Token: 0x04003589 RID: 13705
		Xrelease = 2U,
		// Token: 0x0400358A RID: 13706
		XacquireXreleaseNoLock = 4U,
		// Token: 0x0400358B RID: 13707
		Lock = 8U
	}
}
