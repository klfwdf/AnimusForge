using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000057 RID: 87
	[NullableContext(1)]
	[Nullable(0)]
	internal class EnumInfo
	{
		// Token: 0x06000511 RID: 1297 RVA: 0x00015763 File Offset: 0x00013963
		public EnumInfo(bool isFlags, ulong[] values, string[] names, string[] resolvedNames)
		{
			this.IsFlags = isFlags;
			this.Values = values;
			this.Names = names;
			this.ResolvedNames = resolvedNames;
		}

		// Token: 0x040001DD RID: 477
		public readonly bool IsFlags;

		// Token: 0x040001DE RID: 478
		public readonly ulong[] Values;

		// Token: 0x040001DF RID: 479
		public readonly string[] Names;

		// Token: 0x040001E0 RID: 480
		public readonly string[] ResolvedNames;
	}
}
