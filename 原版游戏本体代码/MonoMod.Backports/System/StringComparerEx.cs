using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200001C RID: 28
	public static class StringComparerEx
	{
		// Token: 0x0600011E RID: 286 RVA: 0x000090D8 File Offset: 0x000072D8
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
