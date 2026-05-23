using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;

namespace System.Collections.Generic
{
	// Token: 0x020004DF RID: 1247
	[TypeDependency("System.Collections.Generic.GenericArraySortHelper`1")]
	internal class ArraySortHelper<T> : IArraySortHelper<T>
	{
		// Token: 0x170008FD RID: 2301
		// (get) Token: 0x06003B39 RID: 15161 RVA: 0x000E0BE4 File Offset: 0x000DEDE4
		public static IArraySortHelper<T> Default
		{
			get
			{
				IArraySortHelper<T> arraySortHelper = ArraySortHelper<T>.defaultArraySortHelper;
				if (arraySortHelper == null)
				{
					arraySortHelper = ArraySortHelper<T>.CreateArraySortHelper();
				}
				return arraySortHelper;
			}
		}

		// Token: 0x06003B3A RID: 15162 RVA: 0x000E0C04 File Offset: 0x000DEE04
		[SecuritySafeCritical]
		private static IArraySortHelper<T> CreateArraySortHelper()
		{
			if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
			{
				ArraySortHelper<T>.defaultArraySortHelper = (IArraySortHelper<T>)RuntimeTypeHandle.Allocate(typeof(GenericArraySortHelper<string>).TypeHandle.Instantiate(new Type[] { typeof(T) }));
			}
			else
			{
				ArraySortHelper<T>.defaultArraySortHelper = new ArraySortHelper<T>();
			}
			return ArraySortHelper<T>.defaultArraySortHelper;
		}

		// Token: 0x06003B3B RID: 15163 RVA: 0x000E0C7C File Offset: 0x000DEE7C
		public void Sort(T[] keys, int index, int length, IComparer<T> comparer)
		{
			try
			{
				if (comparer == null)
				{
					comparer = Comparer<T>.Default;
				}
				if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
				{
					ArraySortHelper<T>.IntrospectiveSort(keys, index, length, comparer);
				}
				else
				{
					ArraySortHelper<T>.DepthLimitedQuickSort(keys, index, length + index - 1, comparer, 32);
				}
			}
			catch (IndexOutOfRangeException)
			{
				IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), innerException);
			}
		}

		// Token: 0x06003B3C RID: 15164 RVA: 0x000E0CF4 File Offset: 0x000DEEF4
		public int BinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
		{
			int result;
			try
			{
				if (comparer == null)
				{
					comparer = Comparer<T>.Default;
				}
				result = ArraySortHelper<T>.InternalBinarySearch(array, index, length, value, comparer);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), innerException);
			}
			return result;
		}

		// Token: 0x06003B3D RID: 15165 RVA: 0x000E0D40 File Offset: 0x000DEF40
		internal static int InternalBinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
		{
			int i = index;
			int num = index + length - 1;
			while (i <= num)
			{
				int num2 = i + (num - i >> 1);
				int num3 = comparer.Compare(array[num2], value);
				if (num3 == 0)
				{
					return num2;
				}
				if (num3 < 0)
				{
					i = num2 + 1;
				}
				else
				{
					num = num2 - 1;
				}
			}
			return ~i;
		}

		// Token: 0x06003B3E RID: 15166 RVA: 0x000E0D88 File Offset: 0x000DEF88
		private static void SwapIfGreater(T[] keys, IComparer<T> comparer, int a, int b)
		{
			if (a != b && comparer.Compare(keys[a], keys[b]) > 0)
			{
				T t = keys[a];
				keys[a] = keys[b];
				keys[b] = t;
			}
		}

		// Token: 0x06003B3F RID: 15167 RVA: 0x000E0DD0 File Offset: 0x000DEFD0
		private static void Swap(T[] a, int i, int j)
		{
			if (i != j)
			{
				T t = a[i];
				a[i] = a[j];
				a[j] = t;
			}
		}

		// Token: 0x06003B40 RID: 15168 RVA: 0x000E0E00 File Offset: 0x000DF000
		internal static void DepthLimitedQuickSort(T[] keys, int left, int right, IComparer<T> comparer, int depthLimit)
		{
			while (depthLimit != 0)
			{
				int num = left;
				int num2 = right;
				int num3 = num + (num2 - num >> 1);
				ArraySortHelper<T>.SwapIfGreater(keys, comparer, num, num3);
				ArraySortHelper<T>.SwapIfGreater(keys, comparer, num, num2);
				ArraySortHelper<T>.SwapIfGreater(keys, comparer, num3, num2);
				T t = keys[num3];
				for (;;)
				{
					if (comparer.Compare(keys[num], t) >= 0)
					{
						while (comparer.Compare(t, keys[num2]) < 0)
						{
							num2--;
						}
						if (num > num2)
						{
							break;
						}
						if (num < num2)
						{
							T t2 = keys[num];
							keys[num] = keys[num2];
							keys[num2] = t2;
						}
						num++;
						num2--;
						if (num > num2)
						{
							break;
						}
					}
					else
					{
						num++;
					}
				}
				depthLimit--;
				if (num2 - left <= right - num)
				{
					if (left < num2)
					{
						ArraySortHelper<T>.DepthLimitedQuickSort(keys, left, num2, comparer, depthLimit);
					}
					left = num;
				}
				else
				{
					if (num < right)
					{
						ArraySortHelper<T>.DepthLimitedQuickSort(keys, num, right, comparer, depthLimit);
					}
					right = num2;
				}
				if (left >= right)
				{
					return;
				}
			}
			ArraySortHelper<T>.Heapsort(keys, left, right, comparer);
		}

		// Token: 0x06003B41 RID: 15169 RVA: 0x000E0EE7 File Offset: 0x000DF0E7
		internal static void IntrospectiveSort(T[] keys, int left, int length, IComparer<T> comparer)
		{
			if (length < 2)
			{
				return;
			}
			ArraySortHelper<T>.IntroSort(keys, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length), comparer);
		}

		// Token: 0x06003B42 RID: 15170 RVA: 0x000E0F08 File Offset: 0x000DF108
		private static void IntroSort(T[] keys, int lo, int hi, int depthLimit, IComparer<T> comparer)
		{
			while (hi > lo)
			{
				int num = hi - lo + 1;
				if (num <= 16)
				{
					if (num == 1)
					{
						return;
					}
					if (num == 2)
					{
						ArraySortHelper<T>.SwapIfGreater(keys, comparer, lo, hi);
						return;
					}
					if (num == 3)
					{
						ArraySortHelper<T>.SwapIfGreater(keys, comparer, lo, hi - 1);
						ArraySortHelper<T>.SwapIfGreater(keys, comparer, lo, hi);
						ArraySortHelper<T>.SwapIfGreater(keys, comparer, hi - 1, hi);
						return;
					}
					ArraySortHelper<T>.InsertionSort(keys, lo, hi, comparer);
					return;
				}
				else
				{
					if (depthLimit == 0)
					{
						ArraySortHelper<T>.Heapsort(keys, lo, hi, comparer);
						return;
					}
					depthLimit--;
					int num2 = ArraySortHelper<T>.PickPivotAndPartition(keys, lo, hi, comparer);
					ArraySortHelper<T>.IntroSort(keys, num2 + 1, hi, depthLimit, comparer);
					hi = num2 - 1;
				}
			}
		}

		// Token: 0x06003B43 RID: 15171 RVA: 0x000E0FA4 File Offset: 0x000DF1A4
		private static int PickPivotAndPartition(T[] keys, int lo, int hi, IComparer<T> comparer)
		{
			int num = lo + (hi - lo) / 2;
			ArraySortHelper<T>.SwapIfGreater(keys, comparer, lo, num);
			ArraySortHelper<T>.SwapIfGreater(keys, comparer, lo, hi);
			ArraySortHelper<T>.SwapIfGreater(keys, comparer, num, hi);
			T t = keys[num];
			ArraySortHelper<T>.Swap(keys, num, hi - 1);
			int i = lo;
			int num2 = hi - 1;
			while (i < num2)
			{
				while (comparer.Compare(keys[++i], t) < 0)
				{
				}
				while (comparer.Compare(t, keys[--num2]) < 0)
				{
				}
				if (i >= num2)
				{
					break;
				}
				ArraySortHelper<T>.Swap(keys, i, num2);
			}
			ArraySortHelper<T>.Swap(keys, i, hi - 1);
			return i;
		}

		// Token: 0x06003B44 RID: 15172 RVA: 0x000E1034 File Offset: 0x000DF234
		private static void Heapsort(T[] keys, int lo, int hi, IComparer<T> comparer)
		{
			int num = hi - lo + 1;
			for (int i = num / 2; i >= 1; i--)
			{
				ArraySortHelper<T>.DownHeap(keys, i, num, lo, comparer);
			}
			for (int j = num; j > 1; j--)
			{
				ArraySortHelper<T>.Swap(keys, lo, lo + j - 1);
				ArraySortHelper<T>.DownHeap(keys, 1, j - 1, lo, comparer);
			}
		}

		// Token: 0x06003B45 RID: 15173 RVA: 0x000E1084 File Offset: 0x000DF284
		private static void DownHeap(T[] keys, int i, int n, int lo, IComparer<T> comparer)
		{
			T t = keys[lo + i - 1];
			while (i <= n / 2)
			{
				int num = 2 * i;
				if (num < n && comparer.Compare(keys[lo + num - 1], keys[lo + num]) < 0)
				{
					num++;
				}
				if (comparer.Compare(t, keys[lo + num - 1]) >= 0)
				{
					break;
				}
				keys[lo + i - 1] = keys[lo + num - 1];
				i = num;
			}
			keys[lo + i - 1] = t;
		}

		// Token: 0x06003B46 RID: 15174 RVA: 0x000E110C File Offset: 0x000DF30C
		private static void InsertionSort(T[] keys, int lo, int hi, IComparer<T> comparer)
		{
			for (int i = lo; i < hi; i++)
			{
				int num = i;
				T t = keys[i + 1];
				while (num >= lo && comparer.Compare(t, keys[num]) < 0)
				{
					keys[num + 1] = keys[num];
					num--;
				}
				keys[num + 1] = t;
			}
		}

		// Token: 0x04001963 RID: 6499
		private static volatile IArraySortHelper<T> defaultArraySortHelper;
	}
}
