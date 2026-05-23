using System;
using System.Collections.Generic;
using System.Text;

namespace System.Globalization
{
	// Token: 0x020003B3 RID: 947
	internal class DateTimeFormatInfoScanner
	{
		// Token: 0x17000660 RID: 1632
		// (get) Token: 0x06002F31 RID: 12081 RVA: 0x000B51E8 File Offset: 0x000B33E8
		private static Dictionary<string, string> KnownWords
		{
			get
			{
				if (DateTimeFormatInfoScanner.s_knownWords == null)
				{
					DateTimeFormatInfoScanner.s_knownWords = new Dictionary<string, string>
					{
						{
							"/",
							string.Empty
						},
						{
							"-",
							string.Empty
						},
						{
							".",
							string.Empty
						},
						{
							"年",
							string.Empty
						},
						{
							"月",
							string.Empty
						},
						{
							"日",
							string.Empty
						},
						{
							"년",
							string.Empty
						},
						{
							"월",
							string.Empty
						},
						{
							"일",
							string.Empty
						},
						{
							"시",
							string.Empty
						},
						{
							"분",
							string.Empty
						},
						{
							"초",
							string.Empty
						},
						{
							"時",
							string.Empty
						},
						{
							"时",
							string.Empty
						},
						{
							"分",
							string.Empty
						},
						{
							"秒",
							string.Empty
						}
					};
				}
				return DateTimeFormatInfoScanner.s_knownWords;
			}
		}

		// Token: 0x06002F32 RID: 12082 RVA: 0x000B5318 File Offset: 0x000B3518
		internal static int SkipWhiteSpacesAndNonLetter(string pattern, int currentIndex)
		{
			while (currentIndex < pattern.Length)
			{
				char c = pattern[currentIndex];
				if (c == '\\')
				{
					currentIndex++;
					if (currentIndex >= pattern.Length)
					{
						break;
					}
					c = pattern[currentIndex];
					if (c == '\'')
					{
						continue;
					}
				}
				if (char.IsLetter(c) || c == '\'' || c == '.')
				{
					break;
				}
				currentIndex++;
			}
			return currentIndex;
		}

		// Token: 0x06002F33 RID: 12083 RVA: 0x000B5370 File Offset: 0x000B3570
		internal void AddDateWordOrPostfix(string formatPostfix, string str)
		{
			if (str.Length > 0)
			{
				if (str.Equals("."))
				{
					this.AddIgnorableSymbols(".");
					return;
				}
				string text;
				if (!DateTimeFormatInfoScanner.KnownWords.TryGetValue(str, out text))
				{
					if (this.m_dateWords == null)
					{
						this.m_dateWords = new List<string>();
					}
					if (formatPostfix == "MMMM")
					{
						string item = "\ue000" + str;
						if (!this.m_dateWords.Contains(item))
						{
							this.m_dateWords.Add(item);
							return;
						}
					}
					else
					{
						if (!this.m_dateWords.Contains(str))
						{
							this.m_dateWords.Add(str);
						}
						if (str[str.Length - 1] == '.')
						{
							string item2 = str.Substring(0, str.Length - 1);
							if (!this.m_dateWords.Contains(item2))
							{
								this.m_dateWords.Add(item2);
							}
						}
					}
				}
			}
		}

		// Token: 0x06002F34 RID: 12084 RVA: 0x000B5454 File Offset: 0x000B3654
		internal int AddDateWords(string pattern, int index, string formatPostfix)
		{
			int num = DateTimeFormatInfoScanner.SkipWhiteSpacesAndNonLetter(pattern, index);
			if (num != index && formatPostfix != null)
			{
				formatPostfix = null;
			}
			index = num;
			StringBuilder stringBuilder = new StringBuilder();
			while (index < pattern.Length)
			{
				char c = pattern[index];
				if (c == '\'')
				{
					this.AddDateWordOrPostfix(formatPostfix, stringBuilder.ToString());
					index++;
					break;
				}
				if (c == '\\')
				{
					index++;
					if (index < pattern.Length)
					{
						stringBuilder.Append(pattern[index]);
						index++;
					}
				}
				else if (char.IsWhiteSpace(c))
				{
					this.AddDateWordOrPostfix(formatPostfix, stringBuilder.ToString());
					if (formatPostfix != null)
					{
						formatPostfix = null;
					}
					stringBuilder.Length = 0;
					index++;
				}
				else
				{
					stringBuilder.Append(c);
					index++;
				}
			}
			return index;
		}

		// Token: 0x06002F35 RID: 12085 RVA: 0x000B550A File Offset: 0x000B370A
		internal static int ScanRepeatChar(string pattern, char ch, int index, out int count)
		{
			count = 1;
			while (++index < pattern.Length && pattern[index] == ch)
			{
				count++;
			}
			return index;
		}

		// Token: 0x06002F36 RID: 12086 RVA: 0x000B5530 File Offset: 0x000B3730
		internal void AddIgnorableSymbols(string text)
		{
			if (this.m_dateWords == null)
			{
				this.m_dateWords = new List<string>();
			}
			string item = "\ue001" + text;
			if (!this.m_dateWords.Contains(item))
			{
				this.m_dateWords.Add(item);
			}
		}

		// Token: 0x06002F37 RID: 12087 RVA: 0x000B5578 File Offset: 0x000B3778
		internal void ScanDateWord(string pattern)
		{
			this.m_ymdFlags = DateTimeFormatInfoScanner.FoundDatePattern.None;
			for (int i = 0; i < pattern.Length; i++)
			{
				char c = pattern[i];
				if (c <= 'M')
				{
					if (c == '\'')
					{
						i = this.AddDateWords(pattern, i + 1, null);
						continue;
					}
					if (c == '.')
					{
						if (this.m_ymdFlags == DateTimeFormatInfoScanner.FoundDatePattern.FoundYMDPatternFlag)
						{
							this.AddIgnorableSymbols(".");
							this.m_ymdFlags = DateTimeFormatInfoScanner.FoundDatePattern.None;
						}
						i++;
						continue;
					}
					if (c == 'M')
					{
						int num;
						i = DateTimeFormatInfoScanner.ScanRepeatChar(pattern, 'M', i, out num);
						if (num >= 4 && i < pattern.Length && pattern[i] == '\'')
						{
							i = this.AddDateWords(pattern, i + 1, "MMMM");
						}
						this.m_ymdFlags |= DateTimeFormatInfoScanner.FoundDatePattern.FoundMonthPatternFlag;
						continue;
					}
				}
				else
				{
					if (c == '\\')
					{
						i += 2;
						continue;
					}
					if (c != 'd')
					{
						if (c == 'y')
						{
							int num;
							i = DateTimeFormatInfoScanner.ScanRepeatChar(pattern, 'y', i, out num);
							this.m_ymdFlags |= DateTimeFormatInfoScanner.FoundDatePattern.FoundYearPatternFlag;
							continue;
						}
					}
					else
					{
						int num;
						i = DateTimeFormatInfoScanner.ScanRepeatChar(pattern, 'd', i, out num);
						if (num <= 2)
						{
							this.m_ymdFlags |= DateTimeFormatInfoScanner.FoundDatePattern.FoundDayPatternFlag;
							continue;
						}
						continue;
					}
				}
				if (this.m_ymdFlags == DateTimeFormatInfoScanner.FoundDatePattern.FoundYMDPatternFlag && !char.IsWhiteSpace(c))
				{
					this.m_ymdFlags = DateTimeFormatInfoScanner.FoundDatePattern.None;
				}
			}
		}

		// Token: 0x06002F38 RID: 12088 RVA: 0x000B56B0 File Offset: 0x000B38B0
		internal string[] GetDateWordsOfDTFI(DateTimeFormatInfo dtfi)
		{
			string[] allDateTimePatterns = dtfi.GetAllDateTimePatterns('D');
			for (int i = 0; i < allDateTimePatterns.Length; i++)
			{
				this.ScanDateWord(allDateTimePatterns[i]);
			}
			allDateTimePatterns = dtfi.GetAllDateTimePatterns('d');
			for (int i = 0; i < allDateTimePatterns.Length; i++)
			{
				this.ScanDateWord(allDateTimePatterns[i]);
			}
			allDateTimePatterns = dtfi.GetAllDateTimePatterns('y');
			for (int i = 0; i < allDateTimePatterns.Length; i++)
			{
				this.ScanDateWord(allDateTimePatterns[i]);
			}
			this.ScanDateWord(dtfi.MonthDayPattern);
			allDateTimePatterns = dtfi.GetAllDateTimePatterns('T');
			for (int i = 0; i < allDateTimePatterns.Length; i++)
			{
				this.ScanDateWord(allDateTimePatterns[i]);
			}
			allDateTimePatterns = dtfi.GetAllDateTimePatterns('t');
			for (int i = 0; i < allDateTimePatterns.Length; i++)
			{
				this.ScanDateWord(allDateTimePatterns[i]);
			}
			string[] array = null;
			if (this.m_dateWords != null && this.m_dateWords.Count > 0)
			{
				array = new string[this.m_dateWords.Count];
				for (int i = 0; i < this.m_dateWords.Count; i++)
				{
					array[i] = this.m_dateWords[i];
				}
			}
			return array;
		}

		// Token: 0x06002F39 RID: 12089 RVA: 0x000B57B8 File Offset: 0x000B39B8
		internal static FORMATFLAGS GetFormatFlagGenitiveMonth(string[] monthNames, string[] genitveMonthNames, string[] abbrevMonthNames, string[] genetiveAbbrevMonthNames)
		{
			if (DateTimeFormatInfoScanner.EqualStringArrays(monthNames, genitveMonthNames) && DateTimeFormatInfoScanner.EqualStringArrays(abbrevMonthNames, genetiveAbbrevMonthNames))
			{
				return FORMATFLAGS.None;
			}
			return FORMATFLAGS.UseGenitiveMonth;
		}

		// Token: 0x06002F3A RID: 12090 RVA: 0x000B57D0 File Offset: 0x000B39D0
		internal static FORMATFLAGS GetFormatFlagUseSpaceInMonthNames(string[] monthNames, string[] genitveMonthNames, string[] abbrevMonthNames, string[] genetiveAbbrevMonthNames)
		{
			FORMATFLAGS formatflags = FORMATFLAGS.None;
			formatflags |= ((DateTimeFormatInfoScanner.ArrayElementsBeginWithDigit(monthNames) || DateTimeFormatInfoScanner.ArrayElementsBeginWithDigit(genitveMonthNames) || DateTimeFormatInfoScanner.ArrayElementsBeginWithDigit(abbrevMonthNames) || DateTimeFormatInfoScanner.ArrayElementsBeginWithDigit(genetiveAbbrevMonthNames)) ? FORMATFLAGS.UseDigitPrefixInTokens : FORMATFLAGS.None);
			return formatflags | ((DateTimeFormatInfoScanner.ArrayElementsHaveSpace(monthNames) || DateTimeFormatInfoScanner.ArrayElementsHaveSpace(genitveMonthNames) || DateTimeFormatInfoScanner.ArrayElementsHaveSpace(abbrevMonthNames) || DateTimeFormatInfoScanner.ArrayElementsHaveSpace(genetiveAbbrevMonthNames)) ? FORMATFLAGS.UseSpacesInMonthNames : FORMATFLAGS.None);
		}

		// Token: 0x06002F3B RID: 12091 RVA: 0x000B582F File Offset: 0x000B3A2F
		internal static FORMATFLAGS GetFormatFlagUseSpaceInDayNames(string[] dayNames, string[] abbrevDayNames)
		{
			if (!DateTimeFormatInfoScanner.ArrayElementsHaveSpace(dayNames) && !DateTimeFormatInfoScanner.ArrayElementsHaveSpace(abbrevDayNames))
			{
				return FORMATFLAGS.None;
			}
			return FORMATFLAGS.UseSpacesInDayNames;
		}

		// Token: 0x06002F3C RID: 12092 RVA: 0x000B5845 File Offset: 0x000B3A45
		internal static FORMATFLAGS GetFormatFlagUseHebrewCalendar(int calID)
		{
			if (calID != 8)
			{
				return FORMATFLAGS.None;
			}
			return (FORMATFLAGS)10;
		}

		// Token: 0x06002F3D RID: 12093 RVA: 0x000B5850 File Offset: 0x000B3A50
		private static bool EqualStringArrays(string[] array1, string[] array2)
		{
			if (array1 == array2)
			{
				return true;
			}
			if (array1.Length != array2.Length)
			{
				return false;
			}
			for (int i = 0; i < array1.Length; i++)
			{
				if (!array1[i].Equals(array2[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002F3E RID: 12094 RVA: 0x000B588C File Offset: 0x000B3A8C
		private static bool ArrayElementsHaveSpace(string[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				for (int j = 0; j < array[i].Length; j++)
				{
					if (char.IsWhiteSpace(array[i][j]))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06002F3F RID: 12095 RVA: 0x000B58D0 File Offset: 0x000B3AD0
		private static bool ArrayElementsBeginWithDigit(string[] array)
		{
			int i = 0;
			while (i < array.Length)
			{
				if (array[i].Length > 0 && array[i][0] >= '0' && array[i][0] <= '9')
				{
					int num = 1;
					while (num < array[i].Length && array[i][num] >= '0' && array[i][num] <= '9')
					{
						num++;
					}
					if (num == array[i].Length)
					{
						return false;
					}
					if (num == array[i].Length - 1)
					{
						char c = array[i][num];
						if (c == '月' || c == '월')
						{
							return false;
						}
					}
					return num != array[i].Length - 4 || array[i][num] != '\'' || array[i][num + 1] != ' ' || array[i][num + 2] != '月' || array[i][num + 3] != '\'';
				}
				else
				{
					i++;
				}
			}
			return false;
		}

		// Token: 0x040013F4 RID: 5108
		internal const char MonthPostfixChar = '\ue000';

		// Token: 0x040013F5 RID: 5109
		internal const char IgnorableSymbolChar = '\ue001';

		// Token: 0x040013F6 RID: 5110
		internal const string CJKYearSuff = "年";

		// Token: 0x040013F7 RID: 5111
		internal const string CJKMonthSuff = "月";

		// Token: 0x040013F8 RID: 5112
		internal const string CJKDaySuff = "日";

		// Token: 0x040013F9 RID: 5113
		internal const string KoreanYearSuff = "년";

		// Token: 0x040013FA RID: 5114
		internal const string KoreanMonthSuff = "월";

		// Token: 0x040013FB RID: 5115
		internal const string KoreanDaySuff = "일";

		// Token: 0x040013FC RID: 5116
		internal const string KoreanHourSuff = "시";

		// Token: 0x040013FD RID: 5117
		internal const string KoreanMinuteSuff = "분";

		// Token: 0x040013FE RID: 5118
		internal const string KoreanSecondSuff = "초";

		// Token: 0x040013FF RID: 5119
		internal const string CJKHourSuff = "時";

		// Token: 0x04001400 RID: 5120
		internal const string ChineseHourSuff = "时";

		// Token: 0x04001401 RID: 5121
		internal const string CJKMinuteSuff = "分";

		// Token: 0x04001402 RID: 5122
		internal const string CJKSecondSuff = "秒";

		// Token: 0x04001403 RID: 5123
		internal List<string> m_dateWords = new List<string>();

		// Token: 0x04001404 RID: 5124
		private static volatile Dictionary<string, string> s_knownWords;

		// Token: 0x04001405 RID: 5125
		private DateTimeFormatInfoScanner.FoundDatePattern m_ymdFlags;

		// Token: 0x02000B6D RID: 2925
		private enum FoundDatePattern
		{
			// Token: 0x04003462 RID: 13410
			None,
			// Token: 0x04003463 RID: 13411
			FoundYearPatternFlag,
			// Token: 0x04003464 RID: 13412
			FoundMonthPatternFlag,
			// Token: 0x04003465 RID: 13413
			FoundDayPatternFlag = 4,
			// Token: 0x04003466 RID: 13414
			FoundYMDPatternFlag = 7
		}
	}
}
