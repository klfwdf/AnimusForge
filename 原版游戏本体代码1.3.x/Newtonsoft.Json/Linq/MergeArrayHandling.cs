using System;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies how JSON arrays are merged together.
	/// </summary>
	// Token: 0x020000CE RID: 206
	public enum MergeArrayHandling
	{
		/// <summary>Concatenate arrays.</summary>
		// Token: 0x040003C4 RID: 964
		Concat,
		/// <summary>Union arrays, skipping items that already exist.</summary>
		// Token: 0x040003C5 RID: 965
		Union,
		/// <summary>Replace all array items.</summary>
		// Token: 0x040003C6 RID: 966
		Replace,
		/// <summary>Merge array items together, matched by index.</summary>
		// Token: 0x040003C7 RID: 967
		Merge
	}
}
