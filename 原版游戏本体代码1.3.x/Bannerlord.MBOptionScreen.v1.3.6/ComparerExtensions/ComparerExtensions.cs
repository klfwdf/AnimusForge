using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComparerExtensions
{
	// Token: 0x02000007 RID: 7
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ComparerExtensions
	{
		// Token: 0x06000007 RID: 7 RVA: 0x000020A8 File Offset: 0x000002A8
		public static IComparer<T> ThenBy<[Nullable(2)] T>(this IComparer<T> baseComparer, IComparer<T> comparer)
		{
			if (baseComparer == null)
			{
				throw new ArgumentNullException("baseComparer");
			}
			if (comparer == null)
			{
				throw new ArgumentNullException("comparer");
			}
			return CompoundComparer<T>.GetComparer(baseComparer, comparer);
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000020DC File Offset: 0x000002DC
		public static IComparer<T> ThenBy<[Nullable(2)] T>(this IComparer<T> baseComparer, Func<T, T, int> comparison)
		{
			if (baseComparer == null)
			{
				throw new ArgumentNullException("baseComparer");
			}
			if (comparison == null)
			{
				throw new ArgumentNullException("comparison");
			}
			IComparer<T> wrapper = ComparisonWrapper<T>.GetComparer(comparison);
			return CompoundComparer<T>.GetComparer(baseComparer, wrapper);
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002118 File Offset: 0x00000318
		public static IComparer<T> ThenBy<[Nullable(2)] T, [Nullable(2)] TKey>(this IComparer<T> baseComparer, Func<T, TKey> keySelector)
		{
			if (baseComparer == null)
			{
				throw new ArgumentNullException("baseComparer");
			}
			KeyComparer<T> comparer = KeyComparer<T>.OrderBy<TKey>(keySelector);
			return CompoundComparer<T>.GetComparer(baseComparer, comparer);
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002144 File Offset: 0x00000344
		public static IComparer<T> ThenBy<[Nullable(2)] T, [Nullable(2)] TKey>(this IComparer<T> baseComparer, Func<T, TKey> keySelector, IComparer<TKey> keyComparer)
		{
			if (baseComparer == null)
			{
				throw new ArgumentNullException("baseComparer");
			}
			KeyComparer<T> comparer = KeyComparer<T>.OrderBy<TKey>(keySelector, keyComparer);
			return CompoundComparer<T>.GetComparer(baseComparer, comparer);
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002170 File Offset: 0x00000370
		public static IComparer<T> ThenBy<[Nullable(2)] T, [Nullable(2)] TKey>(this IComparer<T> baseComparer, Func<T, TKey> keySelector, Func<TKey, TKey, int> keyComparison)
		{
			if (baseComparer == null)
			{
				throw new ArgumentNullException("baseComparer");
			}
			KeyComparer<T> comparer = KeyComparer<T>.OrderBy<TKey>(keySelector, keyComparison);
			return CompoundComparer<T>.GetComparer(baseComparer, comparer);
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000219C File Offset: 0x0000039C
		public static IComparer<T> ThenByDescending<[Nullable(2)] T, [Nullable(2)] TKey>(this IComparer<T> baseComparer, Func<T, TKey> keySelector)
		{
			if (baseComparer == null)
			{
				throw new ArgumentNullException("baseComparer");
			}
			KeyComparer<T> comparer = KeyComparer<T>.OrderByDescending<TKey>(keySelector);
			return CompoundComparer<T>.GetComparer(baseComparer, comparer);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000021C8 File Offset: 0x000003C8
		public static IComparer<T> ThenByDescending<[Nullable(2)] T, [Nullable(2)] TKey>(this IComparer<T> baseComparer, Func<T, TKey> keySelector, IComparer<TKey> keyComparer)
		{
			if (baseComparer == null)
			{
				throw new ArgumentNullException("baseComparer");
			}
			KeyComparer<T> comparer = KeyComparer<T>.OrderByDescending<TKey>(keySelector, keyComparer);
			return CompoundComparer<T>.GetComparer(baseComparer, comparer);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000021F4 File Offset: 0x000003F4
		public static IComparer<T> ThenByDescending<[Nullable(2)] T, [Nullable(2)] TKey>(this IComparer<T> baseComparer, Func<T, TKey> keySelector, Func<TKey, TKey, int> keyComparison)
		{
			if (baseComparer == null)
			{
				throw new ArgumentNullException("baseComparer");
			}
			KeyComparer<T> comparer = KeyComparer<T>.OrderByDescending<TKey>(keySelector, keyComparison);
			return CompoundComparer<T>.GetComparer(baseComparer, comparer);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002220 File Offset: 0x00000420
		public static IComparer<T> ToComparer<[Nullable(2)] T>(this Func<T, T, int> comparison)
		{
			if (comparison == null)
			{
				throw new ArgumentNullException("comparison");
			}
			return ComparisonWrapper<T>.GetComparer(comparison);
		}
	}
}
