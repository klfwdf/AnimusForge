using System;
using System.Collections.Generic;

namespace Mono
{
	// Token: 0x020001DC RID: 476
	internal class MergeSort<T>
	{
		// Token: 0x06000891 RID: 2193 RVA: 0x0001B8A8 File Offset: 0x00019AA8
		private MergeSort(T[] elements, IComparer<T> comparer)
		{
			this.elements = elements;
			this.buffer = new T[elements.Length];
			Array.Copy(this.elements, this.buffer, elements.Length);
			this.comparer = comparer;
		}

		// Token: 0x06000892 RID: 2194 RVA: 0x0001B8E0 File Offset: 0x00019AE0
		public static void Sort(T[] source, IComparer<T> comparer)
		{
			MergeSort<T>.Sort(source, 0, source.Length, comparer);
		}

		// Token: 0x06000893 RID: 2195 RVA: 0x0001B8ED File Offset: 0x00019AED
		public static void Sort(T[] source, int start, int length, IComparer<T> comparer)
		{
			new MergeSort<T>(source, comparer).Sort(start, length);
		}

		// Token: 0x06000894 RID: 2196 RVA: 0x0001B8FD File Offset: 0x00019AFD
		private void Sort(int start, int length)
		{
			this.TopDownSplitMerge(this.buffer, this.elements, start, length);
		}

		// Token: 0x06000895 RID: 2197 RVA: 0x0001B914 File Offset: 0x00019B14
		private void TopDownSplitMerge(T[] a, T[] b, int start, int end)
		{
			if (end - start < 2)
			{
				return;
			}
			int middle = (end + start) / 2;
			this.TopDownSplitMerge(b, a, start, middle);
			this.TopDownSplitMerge(b, a, middle, end);
			this.TopDownMerge(a, b, start, middle, end);
		}

		// Token: 0x06000896 RID: 2198 RVA: 0x0001B954 File Offset: 0x00019B54
		private void TopDownMerge(T[] a, T[] b, int start, int middle, int end)
		{
			int i = start;
			int j = middle;
			for (int k = start; k < end; k++)
			{
				if (i < middle && (j >= end || this.comparer.Compare(a[i], a[j]) <= 0))
				{
					b[k] = a[i++];
				}
				else
				{
					b[k] = a[j++];
				}
			}
		}

		// Token: 0x040002E2 RID: 738
		private readonly T[] elements;

		// Token: 0x040002E3 RID: 739
		private readonly T[] buffer;

		// Token: 0x040002E4 RID: 740
		private readonly IComparer<T> comparer;
	}
}
