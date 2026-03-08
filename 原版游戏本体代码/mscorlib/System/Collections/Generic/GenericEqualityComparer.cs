using System;

namespace System.Collections.Generic
{
	// Token: 0x020004C1 RID: 1217
	[Serializable]
	internal class GenericEqualityComparer<T> : EqualityComparer<T> where T : IEquatable<T>
	{
		// Token: 0x06003A7A RID: 14970 RVA: 0x000DF104 File Offset: 0x000DD304
		public override bool Equals(T x, T y)
		{
			if (x != null)
			{
				return y != null && x.Equals(y);
			}
			return y == null;
		}

		// Token: 0x06003A7B RID: 14971 RVA: 0x000DF132 File Offset: 0x000DD332
		public override int GetHashCode(T obj)
		{
			if (obj == null)
			{
				return 0;
			}
			return obj.GetHashCode();
		}

		// Token: 0x06003A7C RID: 14972 RVA: 0x000DF14C File Offset: 0x000DD34C
		internal override int IndexOf(T[] array, T value, int startIndex, int count)
		{
			int num = startIndex + count;
			if (value == null)
			{
				for (int i = startIndex; i < num; i++)
				{
					if (array[i] == null)
					{
						return i;
					}
				}
			}
			else
			{
				for (int j = startIndex; j < num; j++)
				{
					if (array[j] != null && array[j].Equals(value))
					{
						return j;
					}
				}
			}
			return -1;
		}

		// Token: 0x06003A7D RID: 14973 RVA: 0x000DF1B8 File Offset: 0x000DD3B8
		internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
		{
			int num = startIndex - count + 1;
			if (value == null)
			{
				for (int i = startIndex; i >= num; i--)
				{
					if (array[i] == null)
					{
						return i;
					}
				}
			}
			else
			{
				for (int j = startIndex; j >= num; j--)
				{
					if (array[j] != null && array[j].Equals(value))
					{
						return j;
					}
				}
			}
			return -1;
		}

		// Token: 0x06003A7E RID: 14974 RVA: 0x000DF228 File Offset: 0x000DD428
		public override bool Equals(object obj)
		{
			GenericEqualityComparer<T> genericEqualityComparer = obj as GenericEqualityComparer<T>;
			return genericEqualityComparer != null;
		}

		// Token: 0x06003A7F RID: 14975 RVA: 0x000DF240 File Offset: 0x000DD440
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
