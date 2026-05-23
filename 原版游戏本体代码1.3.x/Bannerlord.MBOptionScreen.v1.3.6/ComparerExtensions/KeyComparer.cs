using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComparerExtensions
{
	// Token: 0x0200000A RID: 10
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class KeyComparer<[Nullable(2)] T> : IComparer<T>, IComparer
	{
		// Token: 0x06000018 RID: 24
		public abstract int Compare(T x, T y);

		// Token: 0x06000019 RID: 25 RVA: 0x0000238C File Offset: 0x0000058C
		[NullableContext(2)]
		int IComparer.Compare(object x, object y)
		{
			if (x is T)
			{
				T x2 = (T)((object)x);
				if (y is T)
				{
					T y2 = (T)((object)y);
					return this.Compare(x2, y2);
				}
			}
			return 0;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000023C3 File Offset: 0x000005C3
		public static KeyComparer<T> OrderBy<[Nullable(2)] TKey>(Func<T, TKey> keySelector)
		{
			if (keySelector == null)
			{
				throw new ArgumentNullException("keySelector");
			}
			return new TypedKeyComparer<T, TKey>(keySelector, Comparer<TKey>.Default);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000023DE File Offset: 0x000005DE
		public static KeyComparer<T> OrderBy<[Nullable(2)] TKey>(Func<T, TKey> keySelector, IComparer<TKey> keyComparer)
		{
			if (keySelector == null)
			{
				throw new ArgumentNullException("keySelector");
			}
			if (keyComparer == null)
			{
				throw new ArgumentNullException("keyComparer");
			}
			return new TypedKeyComparer<T, TKey>(keySelector, keyComparer);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002404 File Offset: 0x00000604
		public static KeyComparer<T> OrderBy<[Nullable(2)] TKey>(Func<T, TKey> keySelector, Func<TKey, TKey, int> keyComparison)
		{
			if (keySelector == null)
			{
				throw new ArgumentNullException("keySelector");
			}
			if (keyComparison == null)
			{
				throw new ArgumentNullException("keyComparison");
			}
			IComparer<TKey> keyComparer = ComparisonWrapper<TKey>.GetComparer(keyComparison);
			return new TypedKeyComparer<T, TKey>(keySelector, keyComparer);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000243B File Offset: 0x0000063B
		public static KeyComparer<T> OrderByDescending<[Nullable(2)] TKey>(Func<T, TKey> keySelector)
		{
			if (keySelector == null)
			{
				throw new ArgumentNullException("keySelector");
			}
			return new TypedKeyComparer<T, TKey>(keySelector, Comparer<TKey>.Default)
			{
				Descending = true
			};
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000245D File Offset: 0x0000065D
		public static KeyComparer<T> OrderByDescending<[Nullable(2)] TKey>(Func<T, TKey> keySelector, IComparer<TKey> keyComparer)
		{
			if (keySelector == null)
			{
				throw new ArgumentNullException("keySelector");
			}
			if (keyComparer == null)
			{
				throw new ArgumentNullException("keyComparer");
			}
			return new TypedKeyComparer<T, TKey>(keySelector, keyComparer)
			{
				Descending = true
			};
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000248C File Offset: 0x0000068C
		public static KeyComparer<T> OrderByDescending<[Nullable(2)] TKey>(Func<T, TKey> keySelector, Func<TKey, TKey, int> keyComparison)
		{
			if (keySelector == null)
			{
				throw new ArgumentNullException("keySelector");
			}
			if (keyComparison == null)
			{
				throw new ArgumentNullException("keyComparison");
			}
			IComparer<TKey> keyComparer = ComparisonWrapper<TKey>.GetComparer(keyComparison);
			return new TypedKeyComparer<T, TKey>(keySelector, keyComparer)
			{
				Descending = true
			};
		}
	}
}
