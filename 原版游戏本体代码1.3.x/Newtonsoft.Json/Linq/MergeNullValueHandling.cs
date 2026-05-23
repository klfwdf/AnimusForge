using System;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies how null value properties are merged.
	/// </summary>
	// Token: 0x020000CF RID: 207
	[Flags]
	public enum MergeNullValueHandling
	{
		/// <summary>
		/// The content's null value properties will be ignored during merging.
		/// </summary>
		// Token: 0x040003C9 RID: 969
		Ignore = 0,
		/// <summary>
		/// The content's null value properties will be merged.
		/// </summary>
		// Token: 0x040003CA RID: 970
		Merge = 1
	}
}
