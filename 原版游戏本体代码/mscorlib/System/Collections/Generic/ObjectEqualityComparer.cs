using System;

namespace System.Collections.Generic
{
	// Token: 0x020004C3 RID: 1219
	[Serializable]
	internal class ObjectEqualityComparer<T> : EqualityComparer<T>
	{
		// Token: 0x06003A88 RID: 14984 RVA: 0x000DF3C6 File Offset: 0x000DD5C6
		public override bool Equals(T x, T y)
		{
			if (x != null)
			{
				return y != null && x.Equals(y);
			}
			return y == null;
		}

		// Token: 0x06003A89 RID: 14985 RVA: 0x000DF3F9 File Offset: 0x000DD5F9
		public override int GetHashCode(T obj)
		{
			if (obj == null)
			{
				return 0;
			}
			return obj.GetHashCode();
		}

		// Token: 0x06003A8A RID: 14986 RVA: 0x000DF414 File Offset: 0x000DD614
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

		// Token: 0x06003A8B RID: 14987 RVA: 0x000DF488 File Offset: 0x000DD688
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

		// Token: 0x06003A8C RID: 14988 RVA: 0x000DF4FC File Offset: 0x000DD6FC
		public override bool Equals(object obj)
		{
			ObjectEqualityComparer<T> objectEqualityComparer = obj as ObjectEqualityComparer<T>;
			return objectEqualityComparer != null;
		}

		// Token: 0x06003A8D RID: 14989 RVA: 0x000DF514 File Offset: 0x000DD714
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
