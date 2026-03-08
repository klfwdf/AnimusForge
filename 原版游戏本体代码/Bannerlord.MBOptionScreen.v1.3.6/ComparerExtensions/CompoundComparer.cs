using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComparerExtensions
{
	// Token: 0x02000009 RID: 9
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class CompoundComparer<[Nullable(2)] T> : Comparer<T>
	{
		// Token: 0x06000013 RID: 19 RVA: 0x0000227C File Offset: 0x0000047C
		public static IComparer<T> GetComparer(IComparer<T> baseComparer, IComparer<T> nextComparer)
		{
			CompoundComparer<T> comparer = new CompoundComparer<T>();
			comparer.AppendComparison(baseComparer);
			comparer.AppendComparison(nextComparer);
			return comparer.Normalize();
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000022A3 File Offset: 0x000004A3
		public CompoundComparer()
		{
			this._comparers = new List<IComparer<T>>();
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000022B8 File Offset: 0x000004B8
		public void AppendComparison(IComparer<T> comparer)
		{
			if (comparer is NullComparer<T>)
			{
				return;
			}
			CompoundComparer<T> other = comparer as CompoundComparer<T>;
			if (other != null)
			{
				this._comparers.AddRange(other._comparers);
				return;
			}
			this._comparers.Add(comparer);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x000022F8 File Offset: 0x000004F8
		public override int Compare(T x, T y)
		{
			foreach (IComparer<T> comparer in this._comparers)
			{
				int result = comparer.Compare(x, y);
				if (result != 0)
				{
					return result;
				}
			}
			return 0;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002358 File Offset: 0x00000558
		public IComparer<T> Normalize()
		{
			if (this._comparers.Count == 0)
			{
				return NullComparer<T>.Default;
			}
			if (this._comparers.Count == 1)
			{
				return this._comparers[0];
			}
			return this;
		}

		// Token: 0x04000005 RID: 5
		private readonly List<IComparer<T>> _comparers;
	}
}
