using System;
using System.Collections;

namespace System.Security.Util
{
	// Token: 0x02000382 RID: 898
	[Serializable]
	internal class LocalSiteString : SiteString
	{
		// Token: 0x06002CA5 RID: 11429 RVA: 0x000A7384 File Offset: 0x000A5584
		public LocalSiteString(string site)
		{
			this.m_site = site.Replace('|', ':');
			if (this.m_site.Length > 2 && this.m_site.IndexOf(':') != -1)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
			}
			this.m_separatedSite = this.CreateSeparatedString(this.m_site);
		}

		// Token: 0x06002CA6 RID: 11430 RVA: 0x000A73E8 File Offset: 0x000A55E8
		private ArrayList CreateSeparatedString(string directory)
		{
			if (directory == null || directory.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
			}
			ArrayList arrayList = new ArrayList();
			string[] array = directory.Split(LocalSiteString.m_separators);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == null || array[i].Equals(""))
				{
					if (i < 2 && directory[i] == '/')
					{
						arrayList.Add("//");
					}
					else if (i != array.Length - 1)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
					}
				}
				else if (array[i].Equals("*"))
				{
					if (i != array.Length - 1)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
					}
					arrayList.Add(array[i]);
				}
				else
				{
					arrayList.Add(array[i]);
				}
			}
			return arrayList;
		}

		// Token: 0x06002CA7 RID: 11431 RVA: 0x000A74BD File Offset: 0x000A56BD
		public virtual bool IsSubsetOf(LocalSiteString operand)
		{
			return this.IsSubsetOf(operand, true);
		}

		// Token: 0x06002CA8 RID: 11432 RVA: 0x000A74C8 File Offset: 0x000A56C8
		public virtual bool IsSubsetOf(LocalSiteString operand, bool ignoreCase)
		{
			if (operand == null)
			{
				return false;
			}
			if (operand.m_separatedSite.Count == 0)
			{
				return this.m_separatedSite.Count == 0 || (this.m_separatedSite.Count > 0 && string.Compare((string)this.m_separatedSite[0], "*", StringComparison.Ordinal) == 0);
			}
			if (this.m_separatedSite.Count == 0)
			{
				return string.Compare((string)operand.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
			}
			return base.IsSubsetOf(operand, ignoreCase);
		}

		// Token: 0x040011EB RID: 4587
		private new static char[] m_separators = new char[] { '/' };
	}
}
