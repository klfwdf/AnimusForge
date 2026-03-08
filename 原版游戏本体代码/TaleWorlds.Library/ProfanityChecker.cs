using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TaleWorlds.Library
{
	// Token: 0x02000083 RID: 131
	public class ProfanityChecker
	{
		// Token: 0x060004A4 RID: 1188 RVA: 0x00010524 File Offset: 0x0000E724
		public ProfanityChecker(string[] profanityList, string[] allowList)
		{
			this.ProfanityList = profanityList;
			this.AllowList = allowList;
			for (int i = 0; i < this.ProfanityList.Length; i++)
			{
				this.ProfanityList[i] = this.ProfanityList[i].ToLower();
			}
			for (int j = 0; j < this.AllowList.Length; j++)
			{
				this.AllowList[j] = this.AllowList[j].ToLower();
			}
		}

		// Token: 0x060004A5 RID: 1189 RVA: 0x00010595 File Offset: 0x0000E795
		public bool IsProfane(string word)
		{
			if (string.IsNullOrEmpty(word) || word.Length == 0)
			{
				return false;
			}
			word = word.ToLower();
			return !this.AllowList.Contains(word) && this.ProfanityList.Contains(word);
		}

		// Token: 0x060004A6 RID: 1190 RVA: 0x000105D0 File Offset: 0x0000E7D0
		public bool ContainsProfanity(string text, ProfanityChecker.ProfanityChechkerType checkType)
		{
			if (string.IsNullOrEmpty(text) || text.Length == 0)
			{
				return false;
			}
			List<string> list = new List<string>();
			foreach (string text2 in this.ProfanityList)
			{
				if (text.Length >= text2.Length)
				{
					list.Add(text2);
				}
			}
			if (list.Count == 0)
			{
				return false;
			}
			text = text.ToLower();
			if (checkType == ProfanityChecker.ProfanityChechkerType.FalsePositive)
			{
				using (IEnumerator enumerator = new Regex(string.Format("(?:{0})", string.Join("|", list).Replace("$", "\\$"), RegexOptions.IgnoreCase)).Matches(text).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						object value = enumerator.Current;
						if (!this.AllowList.Contains(value))
						{
							return true;
						}
					}
					return false;
				}
			}
			if (checkType == ProfanityChecker.ProfanityChechkerType.FalseNegative)
			{
				foreach (object obj in new Regex("\\w(?<!\\d)[\\w'-]*", RegexOptions.IgnoreCase).Matches(text))
				{
					string value2 = obj.ToString();
					if (this.ProfanityList.Contains(value2) && !this.AllowList.Contains(value2))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060004A7 RID: 1191 RVA: 0x0001074C File Offset: 0x0000E94C
		public string CensorText(string text)
		{
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = text.ToLower();
				StringBuilder stringBuilder = new StringBuilder(text);
				string[] array = text.Split(new char[] { ' ' });
				for (int i = 0; i < array.Length; i++)
				{
					string text3 = array[i].ToLower();
					foreach (string text4 in this.ProfanityList)
					{
						string text5 = text3;
						while (text3.Contains(text4) && !this.AllowList.Contains(text3))
						{
							string text6 = stringBuilder.ToString().ToLower();
							int num = text6.IndexOf(text4, StringComparison.Ordinal);
							if (num < 0)
							{
								num = text2.IndexOf(text4, StringComparison.Ordinal);
								text6.Substring(num, text4.Length);
							}
							int startIndex = text3.IndexOf(text4, StringComparison.Ordinal);
							text3 = text3.Remove(startIndex, text4.Length);
							for (int k = num; k < num + text4.Length; k++)
							{
								stringBuilder[k] = '*';
							}
						}
						text3 = text5;
					}
				}
				return stringBuilder.ToString();
			}
			return string.Empty;
		}

		// Token: 0x0400016E RID: 366
		private readonly string[] ProfanityList;

		// Token: 0x0400016F RID: 367
		private readonly string[] AllowList;

		// Token: 0x020000E0 RID: 224
		public enum ProfanityChechkerType
		{
			// Token: 0x040002E1 RID: 737
			FalsePositive,
			// Token: 0x040002E2 RID: 738
			FalseNegative
		}
	}
}
