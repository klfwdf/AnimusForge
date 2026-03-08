using System;
using System.Collections;
using System.Globalization;

namespace System.Security.Util
{
	// Token: 0x0200037C RID: 892
	[Serializable]
	internal class SiteString
	{
		// Token: 0x06002C2F RID: 11311 RVA: 0x000A477C File Offset: 0x000A297C
		protected internal SiteString()
		{
		}

		// Token: 0x06002C30 RID: 11312 RVA: 0x000A4784 File Offset: 0x000A2984
		public SiteString(string site)
		{
			this.m_separatedSite = SiteString.CreateSeparatedSite(site);
			this.m_site = site;
		}

		// Token: 0x06002C31 RID: 11313 RVA: 0x000A479F File Offset: 0x000A299F
		private SiteString(string site, ArrayList separatedSite)
		{
			this.m_separatedSite = separatedSite;
			this.m_site = site;
		}

		// Token: 0x06002C32 RID: 11314 RVA: 0x000A47B8 File Offset: 0x000A29B8
		private static ArrayList CreateSeparatedSite(string site)
		{
			if (site == null || site.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
			}
			ArrayList arrayList = new ArrayList();
			int num = -1;
			int num2 = site.IndexOf('[');
			if (num2 == 0)
			{
				num = site.IndexOf(']', num2 + 1);
			}
			if (num != -1)
			{
				string value = site.Substring(num2 + 1, num - num2 - 1);
				arrayList.Add(value);
				return arrayList;
			}
			string[] array = site.Split(SiteString.m_separators);
			for (int i = array.Length - 1; i > -1; i--)
			{
				if (array[i] == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
				}
				if (array[i].Equals(""))
				{
					if (i != array.Length - 1)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
					}
				}
				else if (array[i].Equals("*"))
				{
					if (i != 0)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
					}
					arrayList.Add(array[i]);
				}
				else
				{
					if (!SiteString.AllLegalCharacters(array[i]))
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
					}
					arrayList.Add(array[i]);
				}
			}
			return arrayList;
		}

		// Token: 0x06002C33 RID: 11315 RVA: 0x000A48E0 File Offset: 0x000A2AE0
		private static bool AllLegalCharacters(string str)
		{
			foreach (char c in str)
			{
				if (!SiteString.IsLegalDNSChar(c) && !SiteString.IsNetbiosSplChar(c))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002C34 RID: 11316 RVA: 0x000A4919 File Offset: 0x000A2B19
		private static bool IsLegalDNSChar(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-';
		}

		// Token: 0x06002C35 RID: 11317 RVA: 0x000A4944 File Offset: 0x000A2B44
		private static bool IsNetbiosSplChar(char c)
		{
			if (c <= '@')
			{
				switch (c)
				{
				case '!':
				case '#':
				case '$':
				case '%':
				case '&':
				case '\'':
				case '(':
				case ')':
				case '-':
				case '.':
					break;
				case '"':
				case '*':
				case '+':
				case ',':
					return false;
				default:
					if (c != '@')
					{
						return false;
					}
					break;
				}
			}
			else if (c != '^' && c != '_')
			{
				switch (c)
				{
				case '{':
				case '}':
				case '~':
					break;
				case '|':
					return false;
				default:
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002C36 RID: 11318 RVA: 0x000A49C6 File Offset: 0x000A2BC6
		public override string ToString()
		{
			return this.m_site;
		}

		// Token: 0x06002C37 RID: 11319 RVA: 0x000A49CE File Offset: 0x000A2BCE
		public override bool Equals(object o)
		{
			return o != null && o is SiteString && this.Equals((SiteString)o, true);
		}

		// Token: 0x06002C38 RID: 11320 RVA: 0x000A49EC File Offset: 0x000A2BEC
		public override int GetHashCode()
		{
			TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
			return textInfo.GetCaseInsensitiveHashCode(this.m_site);
		}

		// Token: 0x06002C39 RID: 11321 RVA: 0x000A4A10 File Offset: 0x000A2C10
		internal bool Equals(SiteString ss, bool ignoreCase)
		{
			if (this.m_site == null)
			{
				return ss.m_site == null;
			}
			return ss.m_site != null && this.IsSubsetOf(ss, ignoreCase) && ss.IsSubsetOf(this, ignoreCase);
		}

		// Token: 0x06002C3A RID: 11322 RVA: 0x000A4A42 File Offset: 0x000A2C42
		public virtual SiteString Copy()
		{
			return new SiteString(this.m_site, this.m_separatedSite);
		}

		// Token: 0x06002C3B RID: 11323 RVA: 0x000A4A55 File Offset: 0x000A2C55
		public virtual bool IsSubsetOf(SiteString operand)
		{
			return this.IsSubsetOf(operand, true);
		}

		// Token: 0x06002C3C RID: 11324 RVA: 0x000A4A60 File Offset: 0x000A2C60
		public virtual bool IsSubsetOf(SiteString operand, bool ignoreCase)
		{
			StringComparison comparisonType = (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			if (operand == null)
			{
				return false;
			}
			if (this.m_separatedSite.Count == operand.m_separatedSite.Count && this.m_separatedSite.Count == 0)
			{
				return true;
			}
			if (this.m_separatedSite.Count < operand.m_separatedSite.Count - 1)
			{
				return false;
			}
			if (this.m_separatedSite.Count > operand.m_separatedSite.Count && operand.m_separatedSite.Count > 0 && !operand.m_separatedSite[operand.m_separatedSite.Count - 1].Equals("*"))
			{
				return false;
			}
			if (string.Compare(this.m_site, operand.m_site, comparisonType) == 0)
			{
				return true;
			}
			for (int i = 0; i < operand.m_separatedSite.Count - 1; i++)
			{
				if (string.Compare((string)this.m_separatedSite[i], (string)operand.m_separatedSite[i], comparisonType) != 0)
				{
					return false;
				}
			}
			if (this.m_separatedSite.Count < operand.m_separatedSite.Count)
			{
				return operand.m_separatedSite[operand.m_separatedSite.Count - 1].Equals("*");
			}
			return this.m_separatedSite.Count != operand.m_separatedSite.Count || string.Compare((string)this.m_separatedSite[this.m_separatedSite.Count - 1], (string)operand.m_separatedSite[this.m_separatedSite.Count - 1], comparisonType) == 0 || operand.m_separatedSite[operand.m_separatedSite.Count - 1].Equals("*");
		}

		// Token: 0x06002C3D RID: 11325 RVA: 0x000A4C1E File Offset: 0x000A2E1E
		public virtual SiteString Intersect(SiteString operand)
		{
			if (operand == null)
			{
				return null;
			}
			if (this.IsSubsetOf(operand))
			{
				return this.Copy();
			}
			if (operand.IsSubsetOf(this))
			{
				return operand.Copy();
			}
			return null;
		}

		// Token: 0x06002C3E RID: 11326 RVA: 0x000A4C46 File Offset: 0x000A2E46
		public virtual SiteString Union(SiteString operand)
		{
			if (operand == null)
			{
				return this;
			}
			if (this.IsSubsetOf(operand))
			{
				return operand.Copy();
			}
			if (operand.IsSubsetOf(this))
			{
				return this.Copy();
			}
			return null;
		}

		// Token: 0x040011C6 RID: 4550
		protected string m_site;

		// Token: 0x040011C7 RID: 4551
		protected ArrayList m_separatedSite;

		// Token: 0x040011C8 RID: 4552
		protected static char[] m_separators = new char[] { '.' };
	}
}
