using System;

namespace System.Collections.Generic
{
	// Token: 0x020004BB RID: 1211
	[Serializable]
	internal class GenericComparer<T> : Comparer<T> where T : IComparable<T>
	{
		// Token: 0x06003A2C RID: 14892 RVA: 0x000DDE57 File Offset: 0x000DC057
		public override int Compare(T x, T y)
		{
			if (x != null)
			{
				if (y != null)
				{
					return x.CompareTo(y);
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

		// Token: 0x06003A2D RID: 14893 RVA: 0x000DDE88 File Offset: 0x000DC088
		public override bool Equals(object obj)
		{
			GenericComparer<T> genericComparer = obj as GenericComparer<T>;
			return genericComparer != null;
		}

		// Token: 0x06003A2E RID: 14894 RVA: 0x000DDEA0 File Offset: 0x000DC0A0
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
