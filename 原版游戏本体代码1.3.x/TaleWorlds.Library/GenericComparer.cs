using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x02000035 RID: 53
	public class GenericComparer<T> : Comparer<T> where T : IComparable<T>
	{
		// Token: 0x060001BA RID: 442 RVA: 0x00007060 File Offset: 0x00005260
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

		// Token: 0x060001BB RID: 443 RVA: 0x0000708E File Offset: 0x0000528E
		public override bool Equals(object obj)
		{
			return obj is GenericComparer<T>;
		}

		// Token: 0x060001BC RID: 444 RVA: 0x00007099 File Offset: 0x00005299
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
