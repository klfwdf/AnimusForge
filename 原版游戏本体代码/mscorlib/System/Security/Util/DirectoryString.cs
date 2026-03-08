using System;
using System.Collections;

namespace System.Security.Util
{
	// Token: 0x02000381 RID: 897
	[Serializable]
	internal class DirectoryString : SiteString
	{
		// Token: 0x06002C9F RID: 11423 RVA: 0x000A71B7 File Offset: 0x000A53B7
		public DirectoryString()
		{
			this.m_site = "";
			this.m_separatedSite = new ArrayList();
		}

		// Token: 0x06002CA0 RID: 11424 RVA: 0x000A71D5 File Offset: 0x000A53D5
		public DirectoryString(string directory, bool checkForIllegalChars)
		{
			this.m_site = directory;
			this.m_checkForIllegalChars = checkForIllegalChars;
			this.m_separatedSite = this.CreateSeparatedString(directory);
		}

		// Token: 0x06002CA1 RID: 11425 RVA: 0x000A71F8 File Offset: 0x000A53F8
		private ArrayList CreateSeparatedString(string directory)
		{
			if (directory == null || directory.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
			}
			ArrayList arrayList = new ArrayList();
			string[] array = directory.Split(DirectoryString.m_separators);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != null && !array[i].Equals(""))
				{
					if (array[i].Equals("*"))
					{
						if (i != array.Length - 1)
						{
							throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
						}
						arrayList.Add(array[i]);
					}
					else
					{
						if (this.m_checkForIllegalChars && array[i].IndexOfAny(DirectoryString.m_illegalDirectoryCharacters) != -1)
						{
							throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
						}
						arrayList.Add(array[i]);
					}
				}
			}
			return arrayList;
		}

		// Token: 0x06002CA2 RID: 11426 RVA: 0x000A72BD File Offset: 0x000A54BD
		public virtual bool IsSubsetOf(DirectoryString operand)
		{
			return this.IsSubsetOf(operand, true);
		}

		// Token: 0x06002CA3 RID: 11427 RVA: 0x000A72C8 File Offset: 0x000A54C8
		public virtual bool IsSubsetOf(DirectoryString operand, bool ignoreCase)
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

		// Token: 0x040011E8 RID: 4584
		private bool m_checkForIllegalChars;

		// Token: 0x040011E9 RID: 4585
		private new static char[] m_separators = new char[] { '/' };

		// Token: 0x040011EA RID: 4586
		protected static char[] m_illegalDirectoryCharacters = new char[] { '\\', ':', '*', '?', '"', '<', '>', '|' };
	}
}
