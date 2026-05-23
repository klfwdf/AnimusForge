using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000020 RID: 32
	[NullableContext(1)]
	[Nullable(0)]
	internal static class CollectionsExtensions
	{
		// Token: 0x06000193 RID: 403 RVA: 0x0000865C File Offset: 0x0000685C
		public static int IndexOf<[Nullable(2)] T>(this IReadOnlyList<T> self, T elementToFind)
		{
			int i = 0;
			foreach (T element in self)
			{
				bool flag = object.Equals(element, elementToFind);
				if (flag)
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x06000194 RID: 404 RVA: 0x000086CC File Offset: 0x000068CC
		public static int IndexOf<[Nullable(2)] T>(this IReadOnlyList<T> self, Func<T, bool> preficate)
		{
			int i = 0;
			foreach (T element in self)
			{
				bool flag = preficate(element);
				if (flag)
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00008730 File Offset: 0x00006930
		public static IEnumerable<TSource> DistinctBy<[Nullable(2)] TSource, [Nullable(2)] TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			return source.DistinctBy(keySelector, null);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x0000873A File Offset: 0x0000693A
		public static IEnumerable<TSource> DistinctBy<[Nullable(2)] TSource, [Nullable(2)] TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, [Nullable(new byte[] { 2, 1 })] IEqualityComparer<TKey> comparer)
		{
			return CollectionsExtensions.DistinctByIterator<TSource, TKey>(source, keySelector, comparer);
		}

		// Token: 0x06000197 RID: 407 RVA: 0x00008744 File Offset: 0x00006944
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
