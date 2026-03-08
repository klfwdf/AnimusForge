using System;
using System.Security;

namespace System.Collections.Generic
{
	// Token: 0x020004CA RID: 1226
	internal sealed class RandomizedObjectEqualityComparer : IEqualityComparer, IWellKnownStringEqualityComparer
	{
		// Token: 0x06003AB3 RID: 15027 RVA: 0x000DF8CA File Offset: 0x000DDACA
		public RandomizedObjectEqualityComparer()
		{
			this._entropy = HashHelpers.GetEntropy();
		}

		// Token: 0x06003AB4 RID: 15028 RVA: 0x000DF8DD File Offset: 0x000DDADD
		public bool Equals(object x, object y)
		{
			if (x != null)
			{
				return y != null && x.Equals(y);
			}
			return y == null;
		}

		// Token: 0x06003AB5 RID: 15029 RVA: 0x000DF8F8 File Offset: 0x000DDAF8
		[SecuritySafeCritical]
		public int GetHashCode(object obj)
		{
			if (obj == null)
			{
				return 0;
			}
			string text = obj as string;
			if (text != null)
			{
				return string.InternalMarvin32HashString(text, text.Length, this._entropy);
			}
			return obj.GetHashCode();
		}

		// Token: 0x06003AB6 RID: 15030 RVA: 0x000DF930 File Offset: 0x000DDB30
		public override bool Equals(object obj)
		{
			RandomizedObjectEqualityComparer randomizedObjectEqualityComparer = obj as RandomizedObjectEqualityComparer;
			return randomizedObjectEqualityComparer != null && this._entropy == randomizedObjectEqualityComparer._entropy;
		}

		// Token: 0x06003AB7 RID: 15031 RVA: 0x000DF957 File Offset: 0x000DDB57
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode() ^ (int)(this._entropy & 2147483647L);
		}

		// Token: 0x06003AB8 RID: 15032 RVA: 0x000DF978 File Offset: 0x000DDB78
		IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer()
		{
			return new RandomizedObjectEqualityComparer();
		}

		// Token: 0x06003AB9 RID: 15033 RVA: 0x000DF97F File Offset: 0x000DDB7F
		IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization()
		{
			return null;
		}

		// Token: 0x04001953 RID: 6483
		private long _entropy;
	}
}
