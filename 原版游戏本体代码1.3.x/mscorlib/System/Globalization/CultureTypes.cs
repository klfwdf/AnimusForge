using System;
using System.Runtime.InteropServices;

namespace System.Globalization
{
	// Token: 0x020003AB RID: 939
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum CultureTypes
	{
		// Token: 0x0400135A RID: 4954
		NeutralCultures = 1,
		// Token: 0x0400135B RID: 4955
		SpecificCultures = 2,
		// Token: 0x0400135C RID: 4956
		InstalledWin32Cultures = 4,
		// Token: 0x0400135D RID: 4957
		AllCultures = 7,
		// Token: 0x0400135E RID: 4958
		UserCustomCulture = 8,
		// Token: 0x0400135F RID: 4959
		ReplacementCultures = 16,
		// Token: 0x04001360 RID: 4960
		[Obsolete("This value has been deprecated.  Please use other values in CultureTypes.")]
		WindowsOnlyCultures = 32,
		// Token: 0x04001361 RID: 4961
		[Obsolete("This value has been deprecated.  Please use other values in CultureTypes.")]
		FrameworkCultures = 64
	}
}
