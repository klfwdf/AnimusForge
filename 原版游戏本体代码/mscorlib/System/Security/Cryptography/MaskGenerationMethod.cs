using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000273 RID: 627
	[ComVisible(true)]
	public abstract class MaskGenerationMethod
	{
		// Token: 0x0600222F RID: 8751
		[ComVisible(true)]
		public abstract byte[] GenerateMask(byte[] rgbSeed, int cbReturn);
	}
}
