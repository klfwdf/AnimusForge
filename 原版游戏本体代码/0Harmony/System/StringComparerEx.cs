using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200047E RID: 1150
	internal static class StringComparerEx
	{
		// Token: 0x0600196F RID: 6511 RVA: 0x00054180 File Offset: 0x00052380
		[NullableContext(1)]
		public static StringComparer FromComparison(StringComparison comparisonType)
		{
			StringComparer result;
			switch (comparisonType)
			{
			case StringComparison.CurrentCulture:
				result = StringComparer.CurrentCulture;
				break;
			case StringComparison.CurrentCultureIgnoreCase:
				result = StringComparer.CurrentCultureIgnoreCase;
				break;
			case StringComparison.InvariantCulture:
				result = StringComparer.InvariantCulture;
				break;
			case StringComparison.InvariantCultureIgnoreCase:
				result = StringComparer.InvariantCultureIgnoreCase;
				break;
			case StringComparison.Ordinal:
				result = StringComparer.Ordinal;
				break;
			case StringComparison.OrdinalIgnoreCase:
				result = StringComparer.OrdinalIgnoreCase;
				break;
			default:
				throw new ArgumentException("Invalid StringComparison value", "comparisonType");
			}
			return result;
		}
	}
}
