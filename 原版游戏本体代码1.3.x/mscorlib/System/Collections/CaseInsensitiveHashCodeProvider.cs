using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x0200048B RID: 1163
	[Obsolete("Please use StringComparer instead.")]
	[ComVisible(true)]
	[Serializable]
	public class CaseInsensitiveHashCodeProvider : IHashCodeProvider
	{
		// Token: 0x06003773 RID: 14195 RVA: 0x000D5678 File Offset: 0x000D3878
		public CaseInsensitiveHashCodeProvider()
		{
			this.m_text = CultureInfo.CurrentCulture.TextInfo;
		}

		// Token: 0x06003774 RID: 14196 RVA: 0x000D5690 File Offset: 0x000D3890
		public CaseInsensitiveHashCodeProvider(CultureInfo culture)
		{
			if (culture == null)
			{
				throw new ArgumentNullException("culture");
			}
			this.m_text = culture.TextInfo;
		}

		// Token: 0x17000821 RID: 2081
		// (get) Token: 0x06003775 RID: 14197 RVA: 0x000D56B2 File Offset: 0x000D38B2
		public static CaseInsensitiveHashCodeProvider Default
		{
			get
			{
				return new CaseInsensitiveHashCodeProvider(CultureInfo.CurrentCulture);
			}
		}

		// Token: 0x17000822 RID: 2082
		// (get) Token: 0x06003776 RID: 14198 RVA: 0x000D56BE File Offset: 0x000D38BE
		public static CaseInsensitiveHashCodeProvider DefaultInvariant
		{
			get
			{
				if (CaseInsensitiveHashCodeProvider.m_InvariantCaseInsensitiveHashCodeProvider == null)
				{
					CaseInsensitiveHashCodeProvider.m_InvariantCaseInsensitiveHashCodeProvider = new CaseInsensitiveHashCodeProvider(CultureInfo.InvariantCulture);
				}
				return CaseInsensitiveHashCodeProvider.m_InvariantCaseInsensitiveHashCodeProvider;
			}
		}

		// Token: 0x06003777 RID: 14199 RVA: 0x000D56E4 File Offset: 0x000D38E4
		public int GetHashCode(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			string text = obj as string;
			if (text == null)
			{
				return obj.GetHashCode();
			}
			return this.m_text.GetCaseInsensitiveHashCode(text);
		}

		// Token: 0x040018B7 RID: 6327
		private TextInfo m_text;

		// Token: 0x040018B8 RID: 6328
		private static volatile CaseInsensitiveHashCodeProvider m_InvariantCaseInsensitiveHashCodeProvider;
	}
}
