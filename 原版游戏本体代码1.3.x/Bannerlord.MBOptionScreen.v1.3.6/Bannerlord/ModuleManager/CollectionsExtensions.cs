using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000062 RID: 98
	[NullableContext(1)]
	[Nullable(0)]
	internal static class CollectionsExtensions
	{
		// Token: 0x0600039B RID: 923 RVA: 0x0000D3D8 File Offset: 0x0000B5D8
		public static int IndexOf<[Nullable(2)] T>(this IReadOnlyList<T> self, T elementToFind)
		{
			int i = 0;
			foreach (T element in self)
			{
				if (object.Equals(element, elementToFind))
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x0600039C RID: 924 RVA: 0x0000D43C File Offset: 0x0000B63C
		public static int IndexOf<[Nullable(2)] T>(this IReadOnlyList<T> self, Func<T, bool> preficate)
		{
			int i = 0;
			foreach (T element in self)
			{
				if (preficate(element))
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x0600039D RID: 925 RVA: 0x0000D494 File Offset: 0x0000B694
		public static IEnumerable<TSource> DistinctBy<[Nullable(2)] TSource, [Nullable(2)] TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			return source.DistinctBy(keySelector, null);
		}

		// Token: 0x0600039E RID: 926 RVA: 0x0000D49E File Offset: 0x0000B69E
		public static IEnumerable<TSource> DistinctBy<[Nullable(2)] TSource, [Nullable(2)] TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, [Nullable(new byte[] { 2, 1 })] IEqualityComparer<TKey> comparer)
		{
			return CollectionsExtensions.DistinctByIterator<TSource, TKey>(source, keySelector, comparer);
		}

		// Token: 0x0600039F RID: 927 RVA: 0x0000D4A8 File Offset: 0x0000B6A8
		private static IEnumerable<TSource> DistinctByIterator<[Nullable(2)] TSource, [Nullable(2)] TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, [Nullable(new byte[] { 2, 1 })] IEqualityComparer<TKey> comparer)
		{
			CollectionsExtensions.<DistinctByIterator>d__4<TSource, TKey> <DistinctByIterator>d__ = new CollectionsExtensions.<DistinctByIterator>d__4<TSource, TKey>(-2);
			<DistinctByIterator>d__.<>3__source = source;
			<DistinctByIterator>d__.<>3__keySelector = keySelector;
			<DistinctByIterator>d__.<>3__comparer = comparer;
			return <DistinctByIterator>d__;
		}
	}
}
