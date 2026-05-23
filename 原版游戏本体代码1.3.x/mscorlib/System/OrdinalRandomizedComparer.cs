using System;
using System.Collections;
using System.Globalization;
using System.Security;

namespace System
{
	// Token: 0x02000079 RID: 121
	internal sealed class OrdinalRandomizedComparer : StringComparer, IWellKnownStringEqualityComparer
	{
		// Token: 0x060005A2 RID: 1442 RVA: 0x00013E36 File Offset: 0x00012036
		internal OrdinalRandomizedComparer(bool ignoreCase)
		{
			this._ignoreCase = ignoreCase;
			this._entropy = HashHelpers.GetEntropy();
		}

		// Token: 0x060005A3 RID: 1443 RVA: 0x00013E50 File Offset: 0x00012050
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
			if (this._ignoreCase)
			{
				return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
			}
			return string.CompareOrdinal(x, y);
		}

		// Token: 0x060005A4 RID: 1444 RVA: 0x00013E7A File Offset: 0x0001207A
		public override bool Equals(string x, string y)
		{
			if (x == y)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			if (this._ignoreCase)
			{
				return x.Length == y.Length && string.Compare(x, y, StringComparison.OrdinalIgnoreCase) == 0;
			}
			return x.Equals(y);
		}

		// Token: 0x060005A5 RID: 1445 RVA: 0x00013EB5 File Offset: 0x000120B5
		[SecuritySafeCritical]
		public override int GetHashCode(string obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			if (this._ignoreCase)
			{
				return TextInfo.GetHashCodeOrdinalIgnoreCase(obj, true, this._entropy);
			}
			return string.InternalMarvin32HashString(obj, obj.Length, this._entropy);
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x00013EF0 File Offset: 0x000120F0
		public override bool Equals(object obj)
		{
			OrdinalRandomizedComparer ordinalRandomizedComparer = obj as OrdinalRandomizedComparer;
			return ordinalRandomizedComparer != null && this._ignoreCase == ordinalRandomizedComparer._ignoreCase && this._entropy == ordinalRandomizedComparer._entropy;
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x00013F28 File Offset: 0x00012128
		public override int GetHashCode()
		{
			string text = "OrdinalRandomizedComparer";
			int hashCode = text.GetHashCode();
			return (this._ignoreCase ? (~hashCode) : hashCode) ^ (int)(this._entropy & 2147483647L);
		}

		// Token: 0x060005A8 RID: 1448 RVA: 0x00013F5E File Offset: 0x0001215E
		IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer()
		{
			return new OrdinalRandomizedComparer(this._ignoreCase);
		}

		// Token: 0x060005A9 RID: 1449 RVA: 0x00013F6B File Offset: 0x0001216B
		IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization()
		{
			return new OrdinalComparer(this._ignoreCase);
		}

		// Token: 0x0400029A RID: 666
		private bool _ignoreCase;

		// Token: 0x0400029B RID: 667
		private long _entropy;
	}
}
