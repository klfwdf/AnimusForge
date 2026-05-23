using System;

namespace System.Collections
{
	// Token: 0x02000494 RID: 1172
	[Serializable]
	internal class CompatibleComparer : IEqualityComparer
	{
		// Token: 0x06003838 RID: 14392 RVA: 0x000D7E34 File Offset: 0x000D6034
		internal CompatibleComparer(IComparer comparer, IHashCodeProvider hashCodeProvider)
		{
			this._comparer = comparer;
			this._hcp = hashCodeProvider;
		}

		// Token: 0x06003839 RID: 14393 RVA: 0x000D7E4C File Offset: 0x000D604C
		public int Compare(object a, object b)
		{
			if (a == b)
			{
				return 0;
			}
			if (a == null)
			{
				return -1;
			}
			if (b == null)
			{
				return 1;
			}
			if (this._comparer != null)
			{
				return this._comparer.Compare(a, b);
			}
			IComparable comparable = a as IComparable;
			if (comparable != null)
			{
				return comparable.CompareTo(b);
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_ImplementIComparable"));
		}

		// Token: 0x0600383A RID: 14394 RVA: 0x000D7EA0 File Offset: 0x000D60A0
		public bool Equals(object a, object b)
		{
			return this.Compare(a, b) == 0;
		}

		// Token: 0x0600383B RID: 14395 RVA: 0x000D7EAD File Offset: 0x000D60AD
		public int GetHashCode(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			if (this._hcp != null)
			{
				return this._hcp.GetHashCode(obj);
			}
			return obj.GetHashCode();
		}

		// Token: 0x1700084D RID: 2125
		// (get) Token: 0x0600383C RID: 14396 RVA: 0x000D7ED8 File Offset: 0x000D60D8
		internal IComparer Comparer
		{
			get
			{
				return this._comparer;
			}
		}

		// Token: 0x1700084E RID: 2126
		// (get) Token: 0x0600383D RID: 14397 RVA: 0x000D7EE0 File Offset: 0x000D60E0
		internal IHashCodeProvider HashCodeProvider
		{
			get
			{
				return this._hcp;
			}
		}

		// Token: 0x040018DC RID: 6364
		private IComparer _comparer;

		// Token: 0x040018DD RID: 6365
		private IHashCodeProvider _hcp;
	}
}
