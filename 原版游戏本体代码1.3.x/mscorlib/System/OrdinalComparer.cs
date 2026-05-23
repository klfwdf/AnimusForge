using System;
using System.Collections;
using System.Globalization;

namespace System
{
	// Token: 0x02000078 RID: 120
	[Serializable]
	internal sealed class OrdinalComparer : StringComparer, IWellKnownStringEqualityComparer
	{
		// Token: 0x0600059A RID: 1434 RVA: 0x00013D3F File Offset: 0x00011F3F
		internal OrdinalComparer(bool ignoreCase)
		{
			this._ignoreCase = ignoreCase;
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x00013D4E File Offset: 0x00011F4E
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

		// Token: 0x0600059C RID: 1436 RVA: 0x00013D78 File Offset: 0x00011F78
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

		// Token: 0x0600059D RID: 1437 RVA: 0x00013DB3 File Offset: 0x00011FB3
		public override int GetHashCode(string obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			if (this._ignoreCase)
			{
				return TextInfo.GetHashCodeOrdinalIgnoreCase(obj);
			}
			return obj.GetHashCode();
		}

		// Token: 0x0600059E RID: 1438 RVA: 0x00013DD8 File Offset: 0x00011FD8
		public override bool Equals(object obj)
		{
			OrdinalComparer ordinalComparer = obj as OrdinalComparer;
			return ordinalComparer != null && this._ignoreCase == ordinalComparer._ignoreCase;
		}

		// Token: 0x0600059F RID: 1439 RVA: 0x00013E00 File Offset: 0x00012000
		public override int GetHashCode()
		{
			string text = "OrdinalComparer";
			int hashCode = text.GetHashCode();
			if (!this._ignoreCase)
			{
				return hashCode;
			}
			return ~hashCode;
		}

		// Token: 0x060005A0 RID: 1440 RVA: 0x00013E26 File Offset: 0x00012026
		IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer()
		{
			return new OrdinalRandomizedComparer(this._ignoreCase);
		}

		// Token: 0x060005A1 RID: 1441 RVA: 0x00013E33 File Offset: 0x00012033
		IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization()
		{
			return this;
		}

		// Token: 0x04000299 RID: 665
		private bool _ignoreCase;
	}
}
