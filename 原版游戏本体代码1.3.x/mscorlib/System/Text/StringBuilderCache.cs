using System;

namespace System.Text
{
	// Token: 0x02000A59 RID: 2649
	internal static class StringBuilderCache
	{
		// Token: 0x06006736 RID: 26422 RVA: 0x0015C28C File Offset: 0x0015A48C
		public static StringBuilder Acquire(int capacity = 16)
		{
			if (capacity <= 360)
			{
				StringBuilder cachedInstance = StringBuilderCache.CachedInstance;
				if (cachedInstance != null && capacity <= cachedInstance.Capacity)
				{
					StringBuilderCache.CachedInstance = null;
					cachedInstance.Clear();
					return cachedInstance;
				}
			}
			return new StringBuilder(capacity);
		}

		// Token: 0x06006737 RID: 26423 RVA: 0x0015C2C8 File Offset: 0x0015A4C8
		public static void Release(StringBuilder sb)
		{
			if (sb.Capacity <= 360)
			{
				StringBuilderCache.CachedInstance = sb;
			}
		}

		// Token: 0x06006738 RID: 26424 RVA: 0x0015C2E0 File Offset: 0x0015A4E0
		public static string GetStringAndRelease(StringBuilder sb)
		{
			string result = sb.ToString();
			StringBuilderCache.Release(sb);
			return result;
		}

		// Token: 0x04002E28 RID: 11816
		internal const int MAX_BUILDER_SIZE = 360;

		// Token: 0x04002E29 RID: 11817
		[ThreadStatic]
		private static StringBuilder CachedInstance;
	}
}
