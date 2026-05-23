using System;

namespace System.Collections.Generic
{
	// Token: 0x020004BC RID: 1212
	[Serializable]
	internal class NullableComparer<T> : Comparer<T?> where T : struct, IComparable<T>
	{
		// Token: 0x06003A30 RID: 14896 RVA: 0x000DDEBA File Offset: 0x000DC0BA
		public override int Compare(T? x, T? y)
		{
			if (x != null)
			{
				if (y != null)
				{
					return x.value.CompareTo(y.value);
				}
				return 1;
			}
			else
			{
				if (y != null)
				{
					return -1;
				}
				return 0;
			}
		}

		// Token: 0x06003A31 RID: 14897 RVA: 0x000DDEF8 File Offset: 0x000DC0F8
		public override bool Equals(object obj)
		{
			NullableComparer<T> nullableComparer = obj as NullableComparer<T>;
			return nullableComparer != null;
		}

		// Token: 0x06003A32 RID: 14898 RVA: 0x000DDF10 File Offset: 0x000DC110
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
