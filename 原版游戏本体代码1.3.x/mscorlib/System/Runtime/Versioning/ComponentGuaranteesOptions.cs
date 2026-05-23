using System;

namespace System.Runtime.Versioning
{
	// Token: 0x0200071D RID: 1821
	[Flags]
	[Serializable]
	public enum ComponentGuaranteesOptions
	{
		// Token: 0x0400240B RID: 9227
		None = 0,
		// Token: 0x0400240C RID: 9228
		Exchange = 1,
		// Token: 0x0400240D RID: 9229
		Stable = 2,
		// Token: 0x0400240E RID: 9230
		SideBySide = 4
	}
}
