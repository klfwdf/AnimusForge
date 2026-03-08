using System;

namespace System.Collections
{
	// Token: 0x020004A8 RID: 1192
	[Serializable]
	internal class StructuralEqualityComparer : IEqualityComparer
	{
		// Token: 0x060038FF RID: 14591 RVA: 0x000DA5AC File Offset: 0x000D87AC
		public bool Equals(object x, object y)
		{
			if (x == null)
			{
				return y == null;
			}
			IStructuralEquatable structuralEquatable = x as IStructuralEquatable;
			if (structuralEquatable != null)
			{
				return structuralEquatable.Equals(y, this);
			}
			return y != null && x.Equals(y);
		}

		// Token: 0x06003900 RID: 14592 RVA: 0x000DA5E4 File Offset: 0x000D87E4
		public int GetHashCode(object obj)
		{
			if (obj == null)
			{
				return 0;
			}
			IStructuralEquatable structuralEquatable = obj as IStructuralEquatable;
			if (structuralEquatable != null)
			{
				return structuralEquatable.GetHashCode(this);
			}
			return obj.GetHashCode();
		}
	}
}
