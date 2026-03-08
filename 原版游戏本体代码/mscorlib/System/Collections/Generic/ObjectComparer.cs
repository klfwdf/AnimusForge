using System;

namespace System.Collections.Generic
{
	// Token: 0x020004BD RID: 1213
	[Serializable]
	internal class ObjectComparer<T> : Comparer<T>
	{
		// Token: 0x06003A34 RID: 14900 RVA: 0x000DDF2A File Offset: 0x000DC12A
		public override int Compare(T x, T y)
		{
			return Comparer.Default.Compare(x, y);
		}

		// Token: 0x06003A35 RID: 14901 RVA: 0x000DDF44 File Offset: 0x000DC144
		public override bool Equals(object obj)
		{
			ObjectComparer<T> objectComparer = obj as ObjectComparer<T>;
			return objectComparer != null;
		}

		// Token: 0x06003A36 RID: 14902 RVA: 0x000DDF5C File Offset: 0x000DC15C
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
