using System;

namespace System.Collections.Generic
{
	// Token: 0x020004C2 RID: 1218
	[Serializable]
	internal class NullableEqualityComparer<T> : EqualityComparer<T?> where T : struct, IEquatable<T>
	{
		// Token: 0x06003A81 RID: 14977 RVA: 0x000DF25A File Offset: 0x000DD45A
		public override bool Equals(T? x, T? y)
		{
			if (x != null)
			{
				return y != null && x.value.Equals(y.value);
			}
			return y == null;
		}

		// Token: 0x06003A82 RID: 14978 RVA: 0x000DF295 File Offset: 0x000DD495
		public override int GetHashCode(T? obj)
		{
			return obj.GetHashCode();
		}

		// Token: 0x06003A83 RID: 14979 RVA: 0x000DF2A4 File Offset: 0x000DD4A4
		internal override int IndexOf(T?[] array, T? value, int startIndex, int count)
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
					if (array[j] != null && array[j].value.Equals(value.value))
					{
						return j;
					}
				}
			}
			return -1;
		}

		// Token: 0x06003A84 RID: 14980 RVA: 0x000DF31C File Offset: 0x000DD51C
		internal override int LastIndexOf(T?[] array, T? value, int startIndex, int count)
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
					if (array[j] != null && array[j].value.Equals(value.value))
					{
						return j;
					}
				}
			}
			return -1;
		}

		// Token: 0x06003A85 RID: 14981 RVA: 0x000DF394 File Offset: 0x000DD594
		public override bool Equals(object obj)
		{
			NullableEqualityComparer<T> nullableEqualityComparer = obj as NullableEqualityComparer<T>;
			return nullableEqualityComparer != null;
		}

		// Token: 0x06003A86 RID: 14982 RVA: 0x000DF3AC File Offset: 0x000DD5AC
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
