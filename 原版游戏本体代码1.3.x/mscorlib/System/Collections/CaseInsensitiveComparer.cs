using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x0200048A RID: 1162
	[ComVisible(true)]
	[Serializable]
	public class CaseInsensitiveComparer : IComparer
	{
		// Token: 0x0600376E RID: 14190 RVA: 0x000D55D2 File Offset: 0x000D37D2
		public CaseInsensitiveComparer()
		{
			this.m_compareInfo = CultureInfo.CurrentCulture.CompareInfo;
		}

		// Token: 0x0600376F RID: 14191 RVA: 0x000D55EA File Offset: 0x000D37EA
		public CaseInsensitiveComparer(CultureInfo culture)
		{
			if (culture == null)
			{
				throw new ArgumentNullException("culture");
			}
			this.m_compareInfo = culture.CompareInfo;
		}

		// Token: 0x1700081F RID: 2079
		// (get) Token: 0x06003770 RID: 14192 RVA: 0x000D560C File Offset: 0x000D380C
		public static CaseInsensitiveComparer Default
		{
			get
			{
				return new CaseInsensitiveComparer(CultureInfo.CurrentCulture);
			}
		}

		// Token: 0x17000820 RID: 2080
		// (get) Token: 0x06003771 RID: 14193 RVA: 0x000D5618 File Offset: 0x000D3818
		public static CaseInsensitiveComparer DefaultInvariant
		{
			get
			{
				if (CaseInsensitiveComparer.m_InvariantCaseInsensitiveComparer == null)
				{
					CaseInsensitiveComparer.m_InvariantCaseInsensitiveComparer = new CaseInsensitiveComparer(CultureInfo.InvariantCulture);
				}
				return CaseInsensitiveComparer.m_InvariantCaseInsensitiveComparer;
			}
		}

		// Token: 0x06003772 RID: 14194 RVA: 0x000D563C File Offset: 0x000D383C
		public int Compare(object a, object b)
		{
			string text = a as string;
			string text2 = b as string;
			if (text != null && text2 != null)
			{
				return this.m_compareInfo.Compare(text, text2, CompareOptions.IgnoreCase);
			}
			return Comparer.Default.Compare(a, b);
		}

		// Token: 0x040018B5 RID: 6325
		private CompareInfo m_compareInfo;

		// Token: 0x040018B6 RID: 6326
		private static volatile CaseInsensitiveComparer m_InvariantCaseInsensitiveComparer;
	}
}
