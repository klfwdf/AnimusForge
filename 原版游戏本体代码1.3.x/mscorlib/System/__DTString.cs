using System;
using System.Globalization;
using System.Security;
using System.Threading;

namespace System
{
	// Token: 0x02000166 RID: 358
	internal struct __DTString
	{
		// Token: 0x0600164D RID: 5709 RVA: 0x0004644B File Offset: 0x0004464B
		internal __DTString(string str, DateTimeFormatInfo dtfi, bool checkDigitToken)
		{
			this = new __DTString(str, dtfi);
			this.m_checkDigitToken = checkDigitToken;
		}

		// Token: 0x0600164E RID: 5710 RVA: 0x0004645C File Offset: 0x0004465C
		internal __DTString(string str, DateTimeFormatInfo dtfi)
		{
			this.Index = -1;
			this.Value = str;
			this.len = this.Value.Length;
			this.m_current = '\0';
			if (dtfi != null)
			{
				this.m_info = dtfi.CompareInfo;
				this.m_checkDigitToken = (dtfi.FormatFlags & DateTimeFormatFlags.UseDigitPrefixInTokens) > DateTimeFormatFlags.None;
				return;
			}
			this.m_info = Thread.CurrentThread.CurrentCulture.CompareInfo;
			this.m_checkDigitToken = false;
		}

		// Token: 0x17000270 RID: 624
		// (get) Token: 0x0600164F RID: 5711 RVA: 0x000464CD File Offset: 0x000446CD
		internal CompareInfo CompareInfo
		{
			get
			{
				return this.m_info;
			}
		}

		// Token: 0x06001650 RID: 5712 RVA: 0x000464D5 File Offset: 0x000446D5
		internal bool GetNext()
		{
			this.Index++;
			if (this.Index < this.len)
			{
				this.m_current = this.Value[this.Index];
				return true;
			}
			return false;
		}

		// Token: 0x06001651 RID: 5713 RVA: 0x0004650D File Offset: 0x0004470D
		internal bool AtEnd()
		{
			return this.Index >= this.len;
		}

		// Token: 0x06001652 RID: 5714 RVA: 0x00046520 File Offset: 0x00044720
		internal bool Advance(int count)
		{
			this.Index += count;
			if (this.Index < this.len)
			{
				this.m_current = this.Value[this.Index];
				return true;
			}
			return false;
		}

		// Token: 0x06001653 RID: 5715 RVA: 0x00046558 File Offset: 0x00044758
		[SecurityCritical]
		internal void GetRegularToken(out TokenType tokenType, out int tokenValue, DateTimeFormatInfo dtfi)
		{
			tokenValue = 0;
			if (this.Index >= this.len)
			{
				tokenType = TokenType.EndOfString;
				return;
			}
			tokenType = TokenType.UnknownToken;
			IL_19:
			while (!DateTimeParse.IsDigit(this.m_current))
			{
				if (char.IsWhiteSpace(this.m_current))
				{
					for (;;)
					{
						int num = this.Index + 1;
						this.Index = num;
						if (num >= this.len)
						{
							break;
						}
						this.m_current = this.Value[this.Index];
						if (!char.IsWhiteSpace(this.m_current))
						{
							goto IL_19;
						}
					}
					tokenType = TokenType.EndOfString;
					return;
				}
				dtfi.Tokenize(TokenType.RegularTokenMask, out tokenType, out tokenValue, ref this);
				return;
			}
			tokenValue = (int)(this.m_current - '0');
			int index = this.Index;
			for (;;)
			{
				int num = this.Index + 1;
				this.Index = num;
				if (num >= this.len)
				{
					break;
				}
				this.m_current = this.Value[this.Index];
				int num2 = (int)(this.m_current - '0');
				if (num2 < 0 || num2 > 9)
				{
					break;
				}
				tokenValue = tokenValue * 10 + num2;
			}
			if (this.Index - index > 8)
			{
				tokenType = TokenType.NumberToken;
				tokenValue = -1;
			}
			else if (this.Index - index < 3)
			{
				tokenType = TokenType.NumberToken;
			}
			else
			{
				tokenType = TokenType.YearNumberToken;
			}
			if (!this.m_checkDigitToken)
			{
				return;
			}
			int index2 = this.Index;
			char current = this.m_current;
			this.Index = index;
			this.m_current = this.Value[this.Index];
			TokenType tokenType2;
			int num3;
			if (dtfi.Tokenize(TokenType.RegularTokenMask, out tokenType2, out num3, ref this))
			{
				tokenType = tokenType2;
				tokenValue = num3;
				return;
			}
			this.Index = index2;
			this.m_current = current;
		}

		// Token: 0x06001654 RID: 5716 RVA: 0x000466DC File Offset: 0x000448DC
		[SecurityCritical]
		internal TokenType GetSeparatorToken(DateTimeFormatInfo dtfi, out int indexBeforeSeparator, out char charBeforeSeparator)
		{
			indexBeforeSeparator = this.Index;
			charBeforeSeparator = this.m_current;
			if (!this.SkipWhiteSpaceCurrent())
			{
				return TokenType.SEP_End;
			}
			TokenType result;
			if (!DateTimeParse.IsDigit(this.m_current))
			{
				int num;
				if (!dtfi.Tokenize(TokenType.SeparatorTokenMask, out result, out num, ref this))
				{
					result = TokenType.SEP_Space;
				}
			}
			else
			{
				result = TokenType.SEP_Space;
			}
			return result;
		}

		// Token: 0x06001655 RID: 5717 RVA: 0x00046737 File Offset: 0x00044937
		internal bool MatchSpecifiedWord(string target)
		{
			return this.MatchSpecifiedWord(target, target.Length + this.Index);
		}

		// Token: 0x06001656 RID: 5718 RVA: 0x00046750 File Offset: 0x00044950
		internal bool MatchSpecifiedWord(string target, int endIndex)
		{
			int num = endIndex - this.Index;
			return num == target.Length && this.Index + num <= this.len && this.m_info.Compare(this.Value, this.Index, num, target, 0, num, CompareOptions.IgnoreCase) == 0;
		}

		// Token: 0x06001657 RID: 5719 RVA: 0x000467A4 File Offset: 0x000449A4
		internal bool MatchSpecifiedWords(string target, bool checkWordBoundary, ref int matchLength)
		{
			int num = this.Value.Length - this.Index;
			matchLength = target.Length;
			if (matchLength > num || this.m_info.Compare(this.Value, this.Index, matchLength, target, 0, matchLength, CompareOptions.IgnoreCase) != 0)
			{
				int num2 = 0;
				int num3 = this.Index;
				int num4 = target.IndexOfAny(__DTString.WhiteSpaceChecks, num2);
				if (num4 == -1)
				{
					return false;
				}
				for (;;)
				{
					int num5 = num4 - num2;
					if (num3 >= this.Value.Length - num5)
					{
						break;
					}
					if (num5 == 0)
					{
						matchLength--;
					}
					else
					{
						if (!char.IsWhiteSpace(this.Value[num3 + num5]))
						{
							return false;
						}
						if (this.m_info.Compare(this.Value, num3, num5, target, num2, num5, CompareOptions.IgnoreCase) != 0)
						{
							return false;
						}
						num3 = num3 + num5 + 1;
					}
					num2 = num4 + 1;
					while (num3 < this.Value.Length && char.IsWhiteSpace(this.Value[num3]))
					{
						num3++;
						matchLength++;
					}
					if ((num4 = target.IndexOfAny(__DTString.WhiteSpaceChecks, num2)) < 0)
					{
						goto Block_8;
					}
				}
				return false;
				Block_8:
				if (num2 < target.Length)
				{
					int num6 = target.Length - num2;
					if (num3 > this.Value.Length - num6)
					{
						return false;
					}
					if (this.m_info.Compare(this.Value, num3, num6, target, num2, num6, CompareOptions.IgnoreCase) != 0)
					{
						return false;
					}
				}
			}
			if (checkWordBoundary)
			{
				int num7 = this.Index + matchLength;
				if (num7 < this.Value.Length && char.IsLetter(this.Value[num7]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001658 RID: 5720 RVA: 0x0004692C File Offset: 0x00044B2C
		internal bool Match(string str)
		{
			int num = this.Index + 1;
			this.Index = num;
			if (num >= this.len)
			{
				return false;
			}
			if (str.Length > this.Value.Length - this.Index)
			{
				return false;
			}
			if (this.m_info.Compare(this.Value, this.Index, str.Length, str, 0, str.Length, CompareOptions.Ordinal) == 0)
			{
				this.Index += str.Length - 1;
				return true;
			}
			return false;
		}

		// Token: 0x06001659 RID: 5721 RVA: 0x000469B4 File Offset: 0x00044BB4
		internal bool Match(char ch)
		{
			int num = this.Index + 1;
			this.Index = num;
			if (num >= this.len)
			{
				return false;
			}
			if (this.Value[this.Index] == ch)
			{
				this.m_current = ch;
				return true;
			}
			this.Index--;
			return false;
		}

		// Token: 0x0600165A RID: 5722 RVA: 0x00046A08 File Offset: 0x00044C08
		internal int MatchLongestWords(string[] words, ref int maxMatchStrLen)
		{
			int result = -1;
			for (int i = 0; i < words.Length; i++)
			{
				string text = words[i];
				int length = text.Length;
				if (this.MatchSpecifiedWords(text, false, ref length) && length > maxMatchStrLen)
				{
					maxMatchStrLen = length;
					result = i;
				}
			}
			return result;
		}

		// Token: 0x0600165B RID: 5723 RVA: 0x00046A48 File Offset: 0x00044C48
		internal int GetRepeatCount()
		{
			char c = this.Value[this.Index];
			int num = this.Index + 1;
			while (num < this.len && this.Value[num] == c)
			{
				num++;
			}
			int result = num - this.Index;
			this.Index = num - 1;
			return result;
		}

		// Token: 0x0600165C RID: 5724 RVA: 0x00046AA4 File Offset: 0x00044CA4
		internal bool GetNextDigit()
		{
			int num = this.Index + 1;
			this.Index = num;
			return num < this.len && DateTimeParse.IsDigit(this.Value[this.Index]);
		}

		// Token: 0x0600165D RID: 5725 RVA: 0x00046AE2 File Offset: 0x00044CE2
		internal char GetChar()
		{
			return this.Value[this.Index];
		}

		// Token: 0x0600165E RID: 5726 RVA: 0x00046AF5 File Offset: 0x00044CF5
		internal int GetDigit()
		{
			return (int)(this.Value[this.Index] - '0');
		}

		// Token: 0x0600165F RID: 5727 RVA: 0x00046B0C File Offset: 0x00044D0C
		internal void SkipWhiteSpaces()
		{
			while (this.Index + 1 < this.len)
			{
				char c = this.Value[this.Index + 1];
				if (!char.IsWhiteSpace(c))
				{
					return;
				}
				this.Index++;
			}
		}

		// Token: 0x06001660 RID: 5728 RVA: 0x00046B58 File Offset: 0x00044D58
		internal bool SkipWhiteSpaceCurrent()
		{
			if (this.Index >= this.len)
			{
				return false;
			}
			if (!char.IsWhiteSpace(this.m_current))
			{
				return true;
			}
			do
			{
				int num = this.Index + 1;
				this.Index = num;
				if (num >= this.len)
				{
					return false;
				}
				this.m_current = this.Value[this.Index];
			}
			while (char.IsWhiteSpace(this.m_current));
			return true;
		}

		// Token: 0x06001661 RID: 5729 RVA: 0x00046BC4 File Offset: 0x00044DC4
		internal void TrimTail()
		{
			int num = this.len - 1;
			while (num >= 0 && char.IsWhiteSpace(this.Value[num]))
			{
				num--;
			}
			this.Value = this.Value.Substring(0, num + 1);
			this.len = this.Value.Length;
		}

		// Token: 0x06001662 RID: 5730 RVA: 0x00046C20 File Offset: 0x00044E20
		internal void RemoveTrailingInQuoteSpaces()
		{
			int num = this.len - 1;
			if (num <= 1)
			{
				return;
			}
			char c = this.Value[num];
			if ((c == '\'' || c == '"') && char.IsWhiteSpace(this.Value[num - 1]))
			{
				num--;
				while (num >= 1 && char.IsWhiteSpace(this.Value[num - 1]))
				{
					num--;
				}
				this.Value = this.Value.Remove(num, this.Value.Length - 1 - num);
				this.len = this.Value.Length;
			}
		}

		// Token: 0x06001663 RID: 5731 RVA: 0x00046CBC File Offset: 0x00044EBC
		internal void RemoveLeadingInQuoteSpaces()
		{
			if (this.len <= 2)
			{
				return;
			}
			int num = 0;
			char c = this.Value[num];
			if (c != '\'')
			{
				if (c != '"')
				{
					return;
				}
			}
			while (num + 1 < this.len && char.IsWhiteSpace(this.Value[num + 1]))
			{
				num++;
			}
			if (num != 0)
			{
				this.Value = this.Value.Remove(1, num);
				this.len = this.Value.Length;
			}
		}

		// Token: 0x06001664 RID: 5732 RVA: 0x00046D3C File Offset: 0x00044F3C
		internal DTSubString GetSubString()
		{
			DTSubString dtsubString = default(DTSubString);
			dtsubString.index = this.Index;
			dtsubString.s = this.Value;
			while (this.Index + dtsubString.length < this.len)
			{
				char c = this.Value[this.Index + dtsubString.length];
				DTSubStringType dtsubStringType;
				if (c >= '0' && c <= '9')
				{
					dtsubStringType = DTSubStringType.Number;
				}
				else
				{
					dtsubStringType = DTSubStringType.Other;
				}
				if (dtsubString.length == 0)
				{
					dtsubString.type = dtsubStringType;
				}
				else if (dtsubString.type != dtsubStringType)
				{
					break;
				}
				dtsubString.length++;
				if (dtsubStringType != DTSubStringType.Number)
				{
					break;
				}
				if (dtsubString.length > 8)
				{
					dtsubString.type = DTSubStringType.Invalid;
					return dtsubString;
				}
				int num = (int)(c - '0');
				dtsubString.value = dtsubString.value * 10 + num;
			}
			if (dtsubString.length == 0)
			{
				dtsubString.type = DTSubStringType.End;
				return dtsubString;
			}
			return dtsubString;
		}

		// Token: 0x06001665 RID: 5733 RVA: 0x00046E16 File Offset: 0x00045016
		internal void ConsumeSubString(DTSubString sub)
		{
			this.Index = sub.index + sub.length;
			if (this.Index < this.len)
			{
				this.m_current = this.Value[this.Index];
			}
		}

		// Token: 0x04000765 RID: 1893
		internal string Value;

		// Token: 0x04000766 RID: 1894
		internal int Index;

		// Token: 0x04000767 RID: 1895
		internal int len;

		// Token: 0x04000768 RID: 1896
		internal char m_current;

		// Token: 0x04000769 RID: 1897
		private CompareInfo m_info;

		// Token: 0x0400076A RID: 1898
		private bool m_checkDigitToken;

		// Token: 0x0400076B RID: 1899
		private static char[] WhiteSpaceChecks = new char[] { ' ', '\u00a0' };
	}
}
