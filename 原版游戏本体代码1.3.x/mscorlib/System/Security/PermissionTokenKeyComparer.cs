using System;
using System.Collections;
using System.Globalization;

namespace System.Security
{
	// Token: 0x020001E0 RID: 480
	[Serializable]
	internal sealed class PermissionTokenKeyComparer : IEqualityComparer
	{
		// Token: 0x06001D23 RID: 7459 RVA: 0x00064FB6 File Offset: 0x000631B6
		public PermissionTokenKeyComparer()
		{
			this._caseSensitiveComparer = new Comparer(CultureInfo.InvariantCulture);
			this._info = CultureInfo.InvariantCulture.TextInfo;
		}

		// Token: 0x06001D24 RID: 7460 RVA: 0x00064FE0 File Offset: 0x000631E0
		[SecuritySafeCritical]
		public int Compare(object a, object b)
		{
			string text = a as string;
			string text2 = b as string;
			if (text == null || text2 == null)
			{
				return this._caseSensitiveComparer.Compare(a, b);
			}
			int num = this._caseSensitiveComparer.Compare(a, b);
			if (num == 0)
			{
				return 0;
			}
			if (SecurityManager.IsSameType(text, text2))
			{
				return 0;
			}
			return num;
		}

		// Token: 0x06001D25 RID: 7461 RVA: 0x0006502E File Offset: 0x0006322E
		public bool Equals(object a, object b)
		{
			return a == b || (a != null && b != null && this.Compare(a, b) == 0);
		}

		// Token: 0x06001D26 RID: 7462 RVA: 0x0006504C File Offset: 0x0006324C
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
			int num = text.IndexOf(',');
			if (num == -1)
			{
				num = text.Length;
			}
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				num2 = (num2 << 7) ^ (int)text[i] ^ (num2 >> 25);
			}
			return num2;
		}

		// Token: 0x04000A2C RID: 2604
		private Comparer _caseSensitiveComparer;

		// Token: 0x04000A2D RID: 2605
		private TextInfo _info;
	}
}
