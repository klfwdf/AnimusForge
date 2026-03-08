using System;
using System.Collections;
using System.Globalization;

namespace System
{
	// Token: 0x02000077 RID: 119
	internal sealed class CultureAwareRandomizedComparer : StringComparer, IWellKnownStringEqualityComparer
	{
		// Token: 0x06000592 RID: 1426 RVA: 0x00013BE0 File Offset: 0x00011DE0
		internal CultureAwareRandomizedComparer(CompareInfo compareInfo, bool ignoreCase)
		{
			this._compareInfo = compareInfo;
			this._ignoreCase = ignoreCase;
			this._entropy = HashHelpers.GetEntropy();
		}

		// Token: 0x06000593 RID: 1427 RVA: 0x00013C01 File Offset: 0x00011E01
		public override int Compare(string x, string y)
		{
			if (x == y)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			return this._compareInfo.Compare(x, y, this._ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
		}

		// Token: 0x06000594 RID: 1428 RVA: 0x00013C2C File Offset: 0x00011E2C
		public override bool Equals(string x, string y)
		{
			return x == y || (x != null && y != null && this._compareInfo.Compare(x, y, this._ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None) == 0);
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x00013C58 File Offset: 0x00011E58
		public override int GetHashCode(string obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			CompareOptions compareOptions = CompareOptions.None;
			if (this._ignoreCase)
			{
				compareOptions |= CompareOptions.IgnoreCase;
			}
			return this._compareInfo.GetHashCodeOfString(obj, compareOptions, true, this._entropy);
		}

		// Token: 0x06000596 RID: 1430 RVA: 0x00013C98 File Offset: 0x00011E98
		public override bool Equals(object obj)
		{
			CultureAwareRandomizedComparer cultureAwareRandomizedComparer = obj as CultureAwareRandomizedComparer;
			return cultureAwareRandomizedComparer != null && (this._ignoreCase == cultureAwareRandomizedComparer._ignoreCase && this._compareInfo.Equals(cultureAwareRandomizedComparer._compareInfo)) && this._entropy == cultureAwareRandomizedComparer._entropy;
		}

		// Token: 0x06000597 RID: 1431 RVA: 0x00013CE4 File Offset: 0x00011EE4
		public override int GetHashCode()
		{
			int hashCode = this._compareInfo.GetHashCode();
			return (this._ignoreCase ? (~hashCode) : hashCode) ^ (int)(this._entropy & 2147483647L);
		}

		// Token: 0x06000598 RID: 1432 RVA: 0x00013D19 File Offset: 0x00011F19
		IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer()
		{
			return new CultureAwareRandomizedComparer(this._compareInfo, this._ignoreCase);
		}

		// Token: 0x06000599 RID: 1433 RVA: 0x00013D2C File Offset: 0x00011F2C
		IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization()
		{
			return new CultureAwareComparer(this._compareInfo, this._ignoreCase);
		}

		// Token: 0x04000296 RID: 662
		private CompareInfo _compareInfo;

		// Token: 0x04000297 RID: 663
		private bool _ignoreCase;

		// Token: 0x04000298 RID: 664
		private long _entropy;
	}
}
