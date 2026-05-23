using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace System
{
	// Token: 0x02000165 RID: 357
	internal static class DateTimeParse
	{
		// Token: 0x060015F6 RID: 5622 RVA: 0x000417DA File Offset: 0x0003F9DA
		[SecuritySafeCritical]
		internal static bool GetAmPmParseFlag()
		{
			return DateTime.EnableAmPmParseAdjustment();
		}

		// Token: 0x060015F7 RID: 5623 RVA: 0x000417E4 File Offset: 0x0003F9E4
		internal static DateTime ParseExact(string s, string format, DateTimeFormatInfo dtfi, DateTimeStyles style)
		{
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			if (DateTimeParse.TryParseExact(s, format, dtfi, style, ref dateTimeResult))
			{
				return dateTimeResult.parsedDate;
			}
			throw DateTimeParse.GetDateTimeParseException(ref dateTimeResult);
		}

		// Token: 0x060015F8 RID: 5624 RVA: 0x0004181C File Offset: 0x0003FA1C
		internal static DateTime ParseExact(string s, string format, DateTimeFormatInfo dtfi, DateTimeStyles style, out TimeSpan offset)
		{
			DateTimeResult dateTimeResult = default(DateTimeResult);
			offset = TimeSpan.Zero;
			dateTimeResult.Init();
			dateTimeResult.flags |= ParseFlags.CaptureOffset;
			if (DateTimeParse.TryParseExact(s, format, dtfi, style, ref dateTimeResult))
			{
				offset = dateTimeResult.timeZoneOffset;
				return dateTimeResult.parsedDate;
			}
			throw DateTimeParse.GetDateTimeParseException(ref dateTimeResult);
		}

		// Token: 0x060015F9 RID: 5625 RVA: 0x0004187C File Offset: 0x0003FA7C
		internal static bool TryParseExact(string s, string format, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result)
		{
			result = DateTime.MinValue;
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			if (DateTimeParse.TryParseExact(s, format, dtfi, style, ref dateTimeResult))
			{
				result = dateTimeResult.parsedDate;
				return true;
			}
			return false;
		}

		// Token: 0x060015FA RID: 5626 RVA: 0x000418C4 File Offset: 0x0003FAC4
		internal static bool TryParseExact(string s, string format, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result, out TimeSpan offset)
		{
			result = DateTime.MinValue;
			offset = TimeSpan.Zero;
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			dateTimeResult.flags |= ParseFlags.CaptureOffset;
			if (DateTimeParse.TryParseExact(s, format, dtfi, style, ref dateTimeResult))
			{
				result = dateTimeResult.parsedDate;
				offset = dateTimeResult.timeZoneOffset;
				return true;
			}
			return false;
		}

		// Token: 0x060015FB RID: 5627 RVA: 0x00041934 File Offset: 0x0003FB34
		internal static bool TryParseExact(string s, string format, DateTimeFormatInfo dtfi, DateTimeStyles style, ref DateTimeResult result)
		{
			if (s == null)
			{
				result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "s");
				return false;
			}
			if (format == null)
			{
				result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "format");
				return false;
			}
			if (s.Length == 0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (format.Length == 0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
				return false;
			}
			return DateTimeParse.DoStrictParse(s, format, style, dtfi, ref result);
		}

		// Token: 0x060015FC RID: 5628 RVA: 0x000419AC File Offset: 0x0003FBAC
		internal static DateTime ParseExactMultiple(string s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style)
		{
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			if (DateTimeParse.TryParseExactMultiple(s, formats, dtfi, style, ref dateTimeResult))
			{
				return dateTimeResult.parsedDate;
			}
			throw DateTimeParse.GetDateTimeParseException(ref dateTimeResult);
		}

		// Token: 0x060015FD RID: 5629 RVA: 0x000419E4 File Offset: 0x0003FBE4
		internal static DateTime ParseExactMultiple(string s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out TimeSpan offset)
		{
			DateTimeResult dateTimeResult = default(DateTimeResult);
			offset = TimeSpan.Zero;
			dateTimeResult.Init();
			dateTimeResult.flags |= ParseFlags.CaptureOffset;
			if (DateTimeParse.TryParseExactMultiple(s, formats, dtfi, style, ref dateTimeResult))
			{
				offset = dateTimeResult.timeZoneOffset;
				return dateTimeResult.parsedDate;
			}
			throw DateTimeParse.GetDateTimeParseException(ref dateTimeResult);
		}

		// Token: 0x060015FE RID: 5630 RVA: 0x00041A44 File Offset: 0x0003FC44
		internal static bool TryParseExactMultiple(string s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result, out TimeSpan offset)
		{
			result = DateTime.MinValue;
			offset = TimeSpan.Zero;
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			dateTimeResult.flags |= ParseFlags.CaptureOffset;
			if (DateTimeParse.TryParseExactMultiple(s, formats, dtfi, style, ref dateTimeResult))
			{
				result = dateTimeResult.parsedDate;
				offset = dateTimeResult.timeZoneOffset;
				return true;
			}
			return false;
		}

		// Token: 0x060015FF RID: 5631 RVA: 0x00041AB4 File Offset: 0x0003FCB4
		internal static bool TryParseExactMultiple(string s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result)
		{
			result = DateTime.MinValue;
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			if (DateTimeParse.TryParseExactMultiple(s, formats, dtfi, style, ref dateTimeResult))
			{
				result = dateTimeResult.parsedDate;
				return true;
			}
			return false;
		}

		// Token: 0x06001600 RID: 5632 RVA: 0x00041AFC File Offset: 0x0003FCFC
		internal static bool TryParseExactMultiple(string s, string[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, ref DateTimeResult result)
		{
			if (s == null)
			{
				result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "s");
				return false;
			}
			if (formats == null)
			{
				result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "formats");
				return false;
			}
			if (s.Length == 0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (formats.Length == 0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
				return false;
			}
			for (int i = 0; i < formats.Length; i++)
			{
				if (formats[i] == null || formats[i].Length == 0)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
					return false;
				}
				DateTimeResult dateTimeResult = default(DateTimeResult);
				dateTimeResult.Init();
				dateTimeResult.flags = result.flags;
				if (DateTimeParse.TryParseExact(s, formats[i], dtfi, style, ref dateTimeResult))
				{
					result.parsedDate = dateTimeResult.parsedDate;
					result.timeZoneOffset = dateTimeResult.timeZoneOffset;
					return true;
				}
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x06001601 RID: 5633 RVA: 0x00041BEC File Offset: 0x0003FDEC
		private static bool MatchWord(ref __DTString str, string target)
		{
			int length = target.Length;
			if (length > str.Value.Length - str.Index)
			{
				return false;
			}
			if (str.CompareInfo.Compare(str.Value, str.Index, length, target, 0, length, CompareOptions.IgnoreCase) != 0)
			{
				return false;
			}
			int num = str.Index + target.Length;
			if (num < str.Value.Length)
			{
				char c = str.Value[num];
				if (char.IsLetter(c))
				{
					return false;
				}
			}
			str.Index = num;
			if (str.Index < str.len)
			{
				str.m_current = str.Value[str.Index];
			}
			return true;
		}

		// Token: 0x06001602 RID: 5634 RVA: 0x00041C97 File Offset: 0x0003FE97
		private static bool GetTimeZoneName(ref __DTString str)
		{
			return DateTimeParse.MatchWord(ref str, "GMT") || DateTimeParse.MatchWord(ref str, "Z");
		}

		// Token: 0x06001603 RID: 5635 RVA: 0x00041CB8 File Offset: 0x0003FEB8
		internal static bool IsDigit(char ch)
		{
			return ch >= '0' && ch <= '9';
		}

		// Token: 0x06001604 RID: 5636 RVA: 0x00041CCC File Offset: 0x0003FECC
		private static bool ParseFraction(ref __DTString str, out double result)
		{
			result = 0.0;
			double num = 0.1;
			int num2 = 0;
			char current;
			while (str.GetNext() && DateTimeParse.IsDigit(current = str.m_current))
			{
				result += (double)(current - '0') * num;
				num *= 0.1;
				num2++;
			}
			return num2 > 0;
		}

		// Token: 0x06001605 RID: 5637 RVA: 0x00041D2C File Offset: 0x0003FF2C
		private static bool ParseTimeZone(ref __DTString str, ref TimeSpan result)
		{
			int num = 0;
			DTSubString subString = str.GetSubString();
			if (subString.length != 1)
			{
				return false;
			}
			char c = subString[0];
			if (c != '+' && c != '-')
			{
				return false;
			}
			str.ConsumeSubString(subString);
			subString = str.GetSubString();
			if (subString.type != DTSubStringType.Number)
			{
				return false;
			}
			int value = subString.value;
			int length = subString.length;
			int hours;
			if (length == 1 || length == 2)
			{
				hours = value;
				str.ConsumeSubString(subString);
				subString = str.GetSubString();
				if (subString.length == 1 && subString[0] == ':')
				{
					str.ConsumeSubString(subString);
					subString = str.GetSubString();
					if (subString.type != DTSubStringType.Number || subString.length < 1 || subString.length > 2)
					{
						return false;
					}
					num = subString.value;
					str.ConsumeSubString(subString);
				}
			}
			else
			{
				if (length != 3 && length != 4)
				{
					return false;
				}
				hours = value / 100;
				num = value % 100;
				str.ConsumeSubString(subString);
			}
			if (num < 0 || num >= 60)
			{
				return false;
			}
			result = new TimeSpan(hours, num, 0);
			if (c == '-')
			{
				result = result.Negate();
			}
			return true;
		}

		// Token: 0x06001606 RID: 5638 RVA: 0x00041E44 File Offset: 0x00040044
		private static bool HandleTimeZone(ref __DTString str, ref DateTimeResult result)
		{
			if (str.Index < str.len - 1)
			{
				char c = str.Value[str.Index];
				int num = 0;
				while (char.IsWhiteSpace(c) && str.Index + num < str.len - 1)
				{
					num++;
					c = str.Value[str.Index + num];
				}
				if (c == '+' || c == '-')
				{
					str.Index += num;
					if ((result.flags & ParseFlags.TimeZoneUsed) != (ParseFlags)0)
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					result.flags |= ParseFlags.TimeZoneUsed;
					if (!DateTimeParse.ParseTimeZone(ref str, ref result.timeZoneOffset))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06001607 RID: 5639 RVA: 0x00041F0C File Offset: 0x0004010C
		[SecuritySafeCritical]
		private static bool Lex(DateTimeParse.DS dps, ref __DTString str, ref DateTimeToken dtok, ref DateTimeRawInfo raw, ref DateTimeResult result, ref DateTimeFormatInfo dtfi, DateTimeStyles styles)
		{
			dtok.dtt = DateTimeParse.DTT.Unk;
			TokenType tokenType;
			int num;
			str.GetRegularToken(out tokenType, out num, dtfi);
			switch (tokenType)
			{
			case TokenType.NumberToken:
			case TokenType.YearNumberToken:
				if (raw.numCount == 3 || num == -1)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				if (dps == DateTimeParse.DS.T_NNt && str.Index < str.len - 1)
				{
					char c = str.Value[str.Index];
					if (c == '.')
					{
						DateTimeParse.ParseFraction(ref str, out raw.fraction);
					}
				}
				if ((dps == DateTimeParse.DS.T_NNt || dps == DateTimeParse.DS.T_Nt) && str.Index < str.len - 1 && !DateTimeParse.HandleTimeZone(ref str, ref result))
				{
					return false;
				}
				dtok.num = num;
				if (tokenType != TokenType.YearNumberToken)
				{
					int index;
					char current;
					TokenType separatorToken;
					TokenType tokenType2 = (separatorToken = str.GetSeparatorToken(dtfi, out index, out current));
					if (separatorToken > TokenType.SEP_YearSuff)
					{
						if (separatorToken <= TokenType.SEP_HourSuff)
						{
							if (separatorToken == TokenType.SEP_MonthSuff || separatorToken == TokenType.SEP_DaySuff)
							{
								dtok.dtt = DateTimeParse.DTT.NumDatesuff;
								dtok.suffix = tokenType2;
								break;
							}
							if (separatorToken != TokenType.SEP_HourSuff)
							{
								goto IL_5DE;
							}
						}
						else if (separatorToken <= TokenType.SEP_SecondSuff)
						{
							if (separatorToken != TokenType.SEP_MinuteSuff && separatorToken != TokenType.SEP_SecondSuff)
							{
								goto IL_5DE;
							}
						}
						else
						{
							if (separatorToken == TokenType.SEP_LocalTimeMark)
							{
								dtok.dtt = DateTimeParse.DTT.NumLocalTimeMark;
								raw.AddNumber(dtok.num);
								break;
							}
							if (separatorToken != TokenType.SEP_DateOrOffset)
							{
								goto IL_5DE;
							}
							if (DateTimeParse.dateParsingStates[(int)dps][4] == DateTimeParse.DS.ERROR && DateTimeParse.dateParsingStates[(int)dps][3] > DateTimeParse.DS.ERROR)
							{
								str.Index = index;
								str.m_current = current;
								dtok.dtt = DateTimeParse.DTT.NumSpace;
							}
							else
							{
								dtok.dtt = DateTimeParse.DTT.NumDatesep;
							}
							raw.AddNumber(dtok.num);
							break;
						}
						dtok.dtt = DateTimeParse.DTT.NumTimesuff;
						dtok.suffix = tokenType2;
						break;
					}
					if (separatorToken <= TokenType.SEP_Am)
					{
						if (separatorToken == TokenType.SEP_End)
						{
							dtok.dtt = DateTimeParse.DTT.NumEnd;
							raw.AddNumber(dtok.num);
							break;
						}
						if (separatorToken == TokenType.SEP_Space)
						{
							dtok.dtt = DateTimeParse.DTT.NumSpace;
							raw.AddNumber(dtok.num);
							break;
						}
						if (separatorToken != TokenType.SEP_Am)
						{
							goto IL_5DE;
						}
					}
					else if (separatorToken <= TokenType.SEP_Date)
					{
						if (separatorToken != TokenType.SEP_Pm)
						{
							if (separatorToken != TokenType.SEP_Date)
							{
								goto IL_5DE;
							}
							dtok.dtt = DateTimeParse.DTT.NumDatesep;
							raw.AddNumber(dtok.num);
							break;
						}
					}
					else if (separatorToken != TokenType.SEP_Time)
					{
						if (separatorToken != TokenType.SEP_YearSuff)
						{
							goto IL_5DE;
						}
						try
						{
							dtok.num = dtfi.Calendar.ToFourDigitYear(num);
						}
						catch (ArgumentOutOfRangeException failureMessageFormatArgument)
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", failureMessageFormatArgument);
							return false;
						}
						dtok.dtt = DateTimeParse.DTT.NumDatesuff;
						dtok.suffix = tokenType2;
						break;
					}
					else
					{
						if (raw.hasSameDateAndTimeSeparators && (dps == DateTimeParse.DS.D_Y || dps == DateTimeParse.DS.D_YN || dps == DateTimeParse.DS.D_YNd || dps == DateTimeParse.DS.D_YM || dps == DateTimeParse.DS.D_YMd))
						{
							dtok.dtt = DateTimeParse.DTT.NumDatesep;
							raw.AddNumber(dtok.num);
							break;
						}
						dtok.dtt = DateTimeParse.DTT.NumTimesep;
						raw.AddNumber(dtok.num);
						break;
					}
					if (raw.timeMark != DateTimeParse.TM.NotSet)
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						break;
					}
					raw.timeMark = ((tokenType2 == TokenType.SEP_Am) ? DateTimeParse.TM.AM : DateTimeParse.TM.PM);
					dtok.dtt = DateTimeParse.DTT.NumAmpm;
					if (dps == DateTimeParse.DS.D_NN && DateTimeParse.enableAmPmParseAdjustment && !DateTimeParse.ProcessTerminaltState(DateTimeParse.DS.DX_NN, ref result, ref styles, ref raw, dtfi))
					{
						return false;
					}
					raw.AddNumber(dtok.num);
					if ((dps == DateTimeParse.DS.T_NNt || dps == DateTimeParse.DS.T_Nt) && !DateTimeParse.HandleTimeZone(ref str, ref result))
					{
						return false;
					}
					break;
					IL_5DE:
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				if (raw.year == -1)
				{
					raw.year = num;
					int index;
					char current;
					TokenType separatorToken2;
					TokenType tokenType2 = (separatorToken2 = str.GetSeparatorToken(dtfi, out index, out current));
					if (separatorToken2 <= TokenType.SEP_Time)
					{
						if (separatorToken2 <= TokenType.SEP_Am)
						{
							if (separatorToken2 == TokenType.SEP_End)
							{
								dtok.dtt = DateTimeParse.DTT.YearEnd;
								return true;
							}
							if (separatorToken2 == TokenType.SEP_Space)
							{
								dtok.dtt = DateTimeParse.DTT.YearSpace;
								return true;
							}
							if (separatorToken2 != TokenType.SEP_Am)
							{
								goto IL_2CF;
							}
						}
						else if (separatorToken2 != TokenType.SEP_Pm)
						{
							if (separatorToken2 == TokenType.SEP_Date)
							{
								dtok.dtt = DateTimeParse.DTT.YearDateSep;
								return true;
							}
							if (separatorToken2 != TokenType.SEP_Time)
							{
								goto IL_2CF;
							}
							if (!raw.hasSameDateAndTimeSeparators)
							{
								result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
								return false;
							}
							dtok.dtt = DateTimeParse.DTT.YearDateSep;
							return true;
						}
						if (raw.timeMark == DateTimeParse.TM.NotSet)
						{
							raw.timeMark = ((tokenType2 == TokenType.SEP_Am) ? DateTimeParse.TM.AM : DateTimeParse.TM.PM);
							dtok.dtt = DateTimeParse.DTT.YearSpace;
							return true;
						}
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return true;
					}
					else
					{
						if (separatorToken2 > TokenType.SEP_DaySuff)
						{
							if (separatorToken2 <= TokenType.SEP_MinuteSuff)
							{
								if (separatorToken2 != TokenType.SEP_HourSuff && separatorToken2 != TokenType.SEP_MinuteSuff)
								{
									goto IL_2CF;
								}
							}
							else if (separatorToken2 != TokenType.SEP_SecondSuff)
							{
								if (separatorToken2 != TokenType.SEP_DateOrOffset)
								{
									goto IL_2CF;
								}
								if (DateTimeParse.dateParsingStates[(int)dps][13] == DateTimeParse.DS.ERROR && DateTimeParse.dateParsingStates[(int)dps][12] > DateTimeParse.DS.ERROR)
								{
									str.Index = index;
									str.m_current = current;
									dtok.dtt = DateTimeParse.DTT.YearSpace;
									return true;
								}
								dtok.dtt = DateTimeParse.DTT.YearDateSep;
								return true;
							}
							dtok.dtt = DateTimeParse.DTT.NumTimesuff;
							dtok.suffix = tokenType2;
							return true;
						}
						if (separatorToken2 == TokenType.SEP_YearSuff || separatorToken2 == TokenType.SEP_MonthSuff || separatorToken2 == TokenType.SEP_DaySuff)
						{
							dtok.dtt = DateTimeParse.DTT.NumDatesuff;
							dtok.suffix = tokenType2;
							return true;
						}
					}
					IL_2CF:
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			case TokenType.Am:
			case TokenType.Pm:
				if (raw.timeMark != DateTimeParse.TM.NotSet)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				raw.timeMark = (DateTimeParse.TM)num;
				break;
			case TokenType.MonthToken:
			{
				if (raw.month != -1)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				int index;
				char current;
				TokenType separatorToken3;
				TokenType tokenType2 = (separatorToken3 = str.GetSeparatorToken(dtfi, out index, out current));
				if (separatorToken3 <= TokenType.SEP_Space)
				{
					if (separatorToken3 == TokenType.SEP_End)
					{
						dtok.dtt = DateTimeParse.DTT.MonthEnd;
						goto IL_867;
					}
					if (separatorToken3 == TokenType.SEP_Space)
					{
						dtok.dtt = DateTimeParse.DTT.MonthSpace;
						goto IL_867;
					}
				}
				else
				{
					if (separatorToken3 == TokenType.SEP_Date)
					{
						dtok.dtt = DateTimeParse.DTT.MonthDatesep;
						goto IL_867;
					}
					if (separatorToken3 != TokenType.SEP_Time)
					{
						if (separatorToken3 == TokenType.SEP_DateOrOffset)
						{
							if (DateTimeParse.dateParsingStates[(int)dps][8] == DateTimeParse.DS.ERROR && DateTimeParse.dateParsingStates[(int)dps][7] > DateTimeParse.DS.ERROR)
							{
								str.Index = index;
								str.m_current = current;
								dtok.dtt = DateTimeParse.DTT.MonthSpace;
								goto IL_867;
							}
							dtok.dtt = DateTimeParse.DTT.MonthDatesep;
							goto IL_867;
						}
					}
					else
					{
						if (!raw.hasSameDateAndTimeSeparators)
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						dtok.dtt = DateTimeParse.DTT.MonthDatesep;
						goto IL_867;
					}
				}
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
				IL_867:
				raw.month = num;
				break;
			}
			case TokenType.EndOfString:
				dtok.dtt = DateTimeParse.DTT.End;
				break;
			case TokenType.DayOfWeekToken:
				if (raw.dayOfWeek != -1)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				raw.dayOfWeek = num;
				dtok.dtt = DateTimeParse.DTT.DayOfWeek;
				break;
			case TokenType.TimeZoneToken:
				if ((result.flags & ParseFlags.TimeZoneUsed) != (ParseFlags)0)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				dtok.dtt = DateTimeParse.DTT.TimeZone;
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = new TimeSpan(0L);
				result.flags |= ParseFlags.TimeZoneUtc;
				break;
			case TokenType.EraToken:
				if (result.era == -1)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				result.era = num;
				dtok.dtt = DateTimeParse.DTT.Era;
				break;
			case TokenType.UnknownToken:
				if (char.IsLetter(str.m_current))
				{
					result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_UnknowDateTimeWord", str.Index);
					return false;
				}
				if (Environment.GetCompatibilityFlag(CompatibilityFlag.DateTimeParseIgnorePunctuation) && (result.flags & ParseFlags.CaptureOffset) == (ParseFlags)0)
				{
					str.GetNext();
					return true;
				}
				if ((str.m_current == '-' || str.m_current == '+') && (result.flags & ParseFlags.TimeZoneUsed) == (ParseFlags)0)
				{
					int index2 = str.Index;
					if (DateTimeParse.ParseTimeZone(ref str, ref result.timeZoneOffset))
					{
						result.flags |= ParseFlags.TimeZoneUsed;
						return true;
					}
					str.Index = index2;
				}
				if (DateTimeParse.VerifyValidPunctuation(ref str))
				{
					return true;
				}
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			case TokenType.HebrewNumber:
			{
				int index;
				char current;
				TokenType tokenType2;
				if (num < 100)
				{
					dtok.num = num;
					raw.AddNumber(dtok.num);
					TokenType separatorToken4;
					tokenType2 = (separatorToken4 = str.GetSeparatorToken(dtfi, out index, out current));
					if (separatorToken4 <= TokenType.SEP_Space)
					{
						if (separatorToken4 == TokenType.SEP_End)
						{
							dtok.dtt = DateTimeParse.DTT.NumEnd;
							break;
						}
						if (separatorToken4 != TokenType.SEP_Space)
						{
							goto IL_749;
						}
					}
					else if (separatorToken4 != TokenType.SEP_Date)
					{
						if (separatorToken4 != TokenType.SEP_DateOrOffset)
						{
							goto IL_749;
						}
						if (DateTimeParse.dateParsingStates[(int)dps][4] == DateTimeParse.DS.ERROR && DateTimeParse.dateParsingStates[(int)dps][3] > DateTimeParse.DS.ERROR)
						{
							str.Index = index;
							str.m_current = current;
							dtok.dtt = DateTimeParse.DTT.NumSpace;
							break;
						}
						dtok.dtt = DateTimeParse.DTT.NumDatesep;
						break;
					}
					dtok.dtt = DateTimeParse.DTT.NumDatesep;
					break;
					IL_749:
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				if (raw.year != -1)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				raw.year = num;
				TokenType separatorToken5;
				tokenType2 = (separatorToken5 = str.GetSeparatorToken(dtfi, out index, out current));
				if (separatorToken5 != TokenType.SEP_End)
				{
					if (separatorToken5 != TokenType.SEP_Space)
					{
						if (separatorToken5 == TokenType.SEP_DateOrOffset)
						{
							if (DateTimeParse.dateParsingStates[(int)dps][12] > DateTimeParse.DS.ERROR)
							{
								str.Index = index;
								str.m_current = current;
								dtok.dtt = DateTimeParse.DTT.YearSpace;
								break;
							}
						}
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					dtok.dtt = DateTimeParse.DTT.YearSpace;
				}
				else
				{
					dtok.dtt = DateTimeParse.DTT.YearEnd;
				}
				break;
			}
			case TokenType.JapaneseEraToken:
				result.calendar = JapaneseCalendar.GetDefaultInstance();
				dtfi = DateTimeFormatInfo.GetJapaneseCalendarDTFI();
				if (result.era == -1)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				result.era = num;
				dtok.dtt = DateTimeParse.DTT.Era;
				break;
			case TokenType.TEraToken:
				result.calendar = TaiwanCalendar.GetDefaultInstance();
				dtfi = DateTimeFormatInfo.GetTaiwanCalendarDTFI();
				if (result.era == -1)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				result.era = num;
				dtok.dtt = DateTimeParse.DTT.Era;
				break;
			}
			return true;
		}

		// Token: 0x06001608 RID: 5640 RVA: 0x000429A8 File Offset: 0x00040BA8
		private static bool VerifyValidPunctuation(ref __DTString str)
		{
			char c = str.Value[str.Index];
			if (c == '#')
			{
				bool flag = false;
				bool flag2 = false;
				for (int i = 0; i < str.len; i++)
				{
					c = str.Value[i];
					if (c == '#')
					{
						if (flag)
						{
							if (flag2)
							{
								return false;
							}
							flag2 = true;
						}
						else
						{
							flag = true;
						}
					}
					else if (c == '\0')
					{
						if (!flag2)
						{
							return false;
						}
					}
					else if (!char.IsWhiteSpace(c) && (!flag || flag2))
					{
						return false;
					}
				}
				if (!flag2)
				{
					return false;
				}
				str.GetNext();
				return true;
			}
			else
			{
				if (c == '\0')
				{
					for (int j = str.Index; j < str.len; j++)
					{
						if (str.Value[j] != '\0')
						{
							return false;
						}
					}
					str.Index = str.len;
					return true;
				}
				return false;
			}
		}

		// Token: 0x06001609 RID: 5641 RVA: 0x00042A68 File Offset: 0x00040C68
		private static bool GetYearMonthDayOrder(string datePattern, DateTimeFormatInfo dtfi, out int order)
		{
			int num = -1;
			int num2 = -1;
			int num3 = -1;
			int num4 = 0;
			bool flag = false;
			int num5 = 0;
			while (num5 < datePattern.Length && num4 < 3)
			{
				char c = datePattern[num5];
				if (c == '\\' || c == '%')
				{
					num5++;
				}
				else
				{
					if (c == '\'' || c == '"')
					{
						flag = !flag;
					}
					if (!flag)
					{
						if (c == 'y')
						{
							num = num4++;
							while (num5 + 1 < datePattern.Length)
							{
								if (datePattern[num5 + 1] != 'y')
								{
									break;
								}
								num5++;
							}
						}
						else if (c == 'M')
						{
							num2 = num4++;
							while (num5 + 1 < datePattern.Length)
							{
								if (datePattern[num5 + 1] != 'M')
								{
									break;
								}
								num5++;
							}
						}
						else if (c == 'd')
						{
							int num6 = 1;
							while (num5 + 1 < datePattern.Length && datePattern[num5 + 1] == 'd')
							{
								num6++;
								num5++;
							}
							if (num6 <= 2)
							{
								num3 = num4++;
							}
						}
					}
				}
				num5++;
			}
			if (num == 0 && num2 == 1 && num3 == 2)
			{
				order = 0;
				return true;
			}
			if (num2 == 0 && num3 == 1 && num == 2)
			{
				order = 1;
				return true;
			}
			if (num3 == 0 && num2 == 1 && num == 2)
			{
				order = 2;
				return true;
			}
			if (num == 0 && num3 == 1 && num2 == 2)
			{
				order = 3;
				return true;
			}
			order = -1;
			return false;
		}

		// Token: 0x0600160A RID: 5642 RVA: 0x00042BBC File Offset: 0x00040DBC
		private static bool GetYearMonthOrder(string pattern, DateTimeFormatInfo dtfi, out int order)
		{
			int num = -1;
			int num2 = -1;
			int num3 = 0;
			bool flag = false;
			int num4 = 0;
			while (num4 < pattern.Length && num3 < 2)
			{
				char c = pattern[num4];
				if (c == '\\' || c == '%')
				{
					num4++;
				}
				else
				{
					if (c == '\'' || c == '"')
					{
						flag = !flag;
					}
					if (!flag)
					{
						if (c == 'y')
						{
							num = num3++;
							while (num4 + 1 < pattern.Length)
							{
								if (pattern[num4 + 1] != 'y')
								{
									break;
								}
								num4++;
							}
						}
						else if (c == 'M')
						{
							num2 = num3++;
							while (num4 + 1 < pattern.Length && pattern[num4 + 1] == 'M')
							{
								num4++;
							}
						}
					}
				}
				num4++;
			}
			if (num == 0 && num2 == 1)
			{
				order = 4;
				return true;
			}
			if (num2 == 0 && num == 1)
			{
				order = 5;
				return true;
			}
			order = -1;
			return false;
		}

		// Token: 0x0600160B RID: 5643 RVA: 0x00042C9C File Offset: 0x00040E9C
		private static bool GetMonthDayOrder(string pattern, DateTimeFormatInfo dtfi, out int order)
		{
			int num = -1;
			int num2 = -1;
			int num3 = 0;
			bool flag = false;
			int num4 = 0;
			while (num4 < pattern.Length && num3 < 2)
			{
				char c = pattern[num4];
				if (c == '\\' || c == '%')
				{
					num4++;
				}
				else
				{
					if (c == '\'' || c == '"')
					{
						flag = !flag;
					}
					if (!flag)
					{
						if (c == 'd')
						{
							int num5 = 1;
							while (num4 + 1 < pattern.Length && pattern[num4 + 1] == 'd')
							{
								num5++;
								num4++;
							}
							if (num5 <= 2)
							{
								num2 = num3++;
							}
						}
						else if (c == 'M')
						{
							num = num3++;
							while (num4 + 1 < pattern.Length && pattern[num4 + 1] == 'M')
							{
								num4++;
							}
						}
					}
				}
				num4++;
			}
			if (num == 0 && num2 == 1)
			{
				order = 6;
				return true;
			}
			if (num2 == 0 && num == 1)
			{
				order = 7;
				return true;
			}
			order = -1;
			return false;
		}

		// Token: 0x0600160C RID: 5644 RVA: 0x00042D90 File Offset: 0x00040F90
		private static bool TryAdjustYear(ref DateTimeResult result, int year, out int adjustedYear)
		{
			if (year < 100)
			{
				try
				{
					year = result.calendar.ToFourDigitYear(year);
				}
				catch (ArgumentOutOfRangeException)
				{
					adjustedYear = -1;
					return false;
				}
			}
			adjustedYear = year;
			return true;
		}

		// Token: 0x0600160D RID: 5645 RVA: 0x00042DD0 File Offset: 0x00040FD0
		private static bool SetDateYMD(ref DateTimeResult result, int year, int month, int day)
		{
			if (result.calendar.IsValidDay(year, month, day, result.era))
			{
				result.SetDate(year, month, day);
				return true;
			}
			return false;
		}

		// Token: 0x0600160E RID: 5646 RVA: 0x00042DF4 File Offset: 0x00040FF4
		private static bool SetDateMDY(ref DateTimeResult result, int month, int day, int year)
		{
			return DateTimeParse.SetDateYMD(ref result, year, month, day);
		}

		// Token: 0x0600160F RID: 5647 RVA: 0x00042DFF File Offset: 0x00040FFF
		private static bool SetDateDMY(ref DateTimeResult result, int day, int month, int year)
		{
			return DateTimeParse.SetDateYMD(ref result, year, month, day);
		}

		// Token: 0x06001610 RID: 5648 RVA: 0x00042E0A File Offset: 0x0004100A
		private static bool SetDateYDM(ref DateTimeResult result, int year, int day, int month)
		{
			return DateTimeParse.SetDateYMD(ref result, year, month, day);
		}

		// Token: 0x06001611 RID: 5649 RVA: 0x00042E15 File Offset: 0x00041015
		private static void GetDefaultYear(ref DateTimeResult result, ref DateTimeStyles styles)
		{
			result.Year = result.calendar.GetYear(DateTimeParse.GetDateTimeNow(ref result, ref styles));
			result.flags |= ParseFlags.YearDefault;
		}

		// Token: 0x06001612 RID: 5650 RVA: 0x00042E40 File Offset: 0x00041040
		private static bool GetDayOfNN(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			int number = raw.GetNumber(0);
			int number2 = raw.GetNumber(1);
			DateTimeParse.GetDefaultYear(ref result, ref styles);
			int num;
			if (!DateTimeParse.GetMonthDayOrder(dtfi.MonthDayPattern, dtfi, out num))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
				return false;
			}
			if (num == 6)
			{
				if (DateTimeParse.SetDateYMD(ref result, result.Year, number, number2))
				{
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			else if (DateTimeParse.SetDateYMD(ref result, result.Year, number2, number))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x06001613 RID: 5651 RVA: 0x00042EFC File Offset: 0x000410FC
		private static bool GetDayOfNNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			int number = raw.GetNumber(0);
			int number2 = raw.GetNumber(1);
			int number3 = raw.GetNumber(2);
			int num;
			if (!DateTimeParse.GetYearMonthDayOrder(dtfi.ShortDatePattern, dtfi, out num))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
				return false;
			}
			int year;
			if (num == 0)
			{
				if (DateTimeParse.TryAdjustYear(ref result, number, out year) && DateTimeParse.SetDateYMD(ref result, year, number2, number3))
				{
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			else if (num == 1)
			{
				if (DateTimeParse.TryAdjustYear(ref result, number3, out year) && DateTimeParse.SetDateMDY(ref result, number, number2, year))
				{
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			else if (num == 2)
			{
				if (DateTimeParse.TryAdjustYear(ref result, number3, out year) && DateTimeParse.SetDateDMY(ref result, number, number2, year))
				{
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			else if (num == 3 && DateTimeParse.TryAdjustYear(ref result, number, out year) && DateTimeParse.SetDateYDM(ref result, year, number2, number3))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x06001614 RID: 5652 RVA: 0x00043028 File Offset: 0x00041228
		private static bool GetDayOfMN(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			int num;
			if (!DateTimeParse.GetMonthDayOrder(dtfi.MonthDayPattern, dtfi, out num))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
				return false;
			}
			if (num == 7)
			{
				int num2;
				if (!DateTimeParse.GetYearMonthOrder(dtfi.YearMonthPattern, dtfi, out num2))
				{
					result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.YearMonthPattern);
					return false;
				}
				if (num2 == 5)
				{
					int year;
					if (!DateTimeParse.TryAdjustYear(ref result, raw.GetNumber(0), out year) || !DateTimeParse.SetDateYMD(ref result, year, raw.month, 1))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					return true;
				}
			}
			DateTimeParse.GetDefaultYear(ref result, ref styles);
			if (!DateTimeParse.SetDateYMD(ref result, result.Year, raw.month, raw.GetNumber(0)))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			return true;
		}

		// Token: 0x06001615 RID: 5653 RVA: 0x00043108 File Offset: 0x00041308
		private static bool GetHebrewDayOfNM(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			int num;
			if (!DateTimeParse.GetMonthDayOrder(dtfi.MonthDayPattern, dtfi, out num))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
				return false;
			}
			result.Month = raw.month;
			if ((num == 7 || num == 6) && result.calendar.IsValidDay(result.Year, result.Month, raw.GetNumber(0), result.era))
			{
				result.Day = raw.GetNumber(0);
				return true;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x06001616 RID: 5654 RVA: 0x00043190 File Offset: 0x00041390
		private static bool GetDayOfNM(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			int num;
			if (!DateTimeParse.GetMonthDayOrder(dtfi.MonthDayPattern, dtfi, out num))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
				return false;
			}
			if (num == 6)
			{
				int num2;
				if (!DateTimeParse.GetYearMonthOrder(dtfi.YearMonthPattern, dtfi, out num2))
				{
					result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.YearMonthPattern);
					return false;
				}
				if (num2 == 4)
				{
					int year;
					if (!DateTimeParse.TryAdjustYear(ref result, raw.GetNumber(0), out year) || !DateTimeParse.SetDateYMD(ref result, year, raw.month, 1))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					return true;
				}
			}
			DateTimeParse.GetDefaultYear(ref result, ref styles);
			if (!DateTimeParse.SetDateYMD(ref result, result.Year, raw.month, raw.GetNumber(0)))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			return true;
		}

		// Token: 0x06001617 RID: 5655 RVA: 0x00043270 File Offset: 0x00041470
		private static bool GetDayOfMNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			int number = raw.GetNumber(0);
			int number2 = raw.GetNumber(1);
			int num;
			if (!DateTimeParse.GetYearMonthDayOrder(dtfi.ShortDatePattern, dtfi, out num))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
				return false;
			}
			if (num == 1)
			{
				int year;
				if (DateTimeParse.TryAdjustYear(ref result, number2, out year) && result.calendar.IsValidDay(year, raw.month, number, result.era))
				{
					result.SetDate(year, raw.month, number);
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
				if (DateTimeParse.TryAdjustYear(ref result, number, out year) && result.calendar.IsValidDay(year, raw.month, number2, result.era))
				{
					result.SetDate(year, raw.month, number2);
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			else if (num == 0)
			{
				int year;
				if (DateTimeParse.TryAdjustYear(ref result, number, out year) && result.calendar.IsValidDay(year, raw.month, number2, result.era))
				{
					result.SetDate(year, raw.month, number2);
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
				if (DateTimeParse.TryAdjustYear(ref result, number2, out year) && result.calendar.IsValidDay(year, raw.month, number, result.era))
				{
					result.SetDate(year, raw.month, number);
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			else if (num == 2)
			{
				int year;
				if (DateTimeParse.TryAdjustYear(ref result, number2, out year) && result.calendar.IsValidDay(year, raw.month, number, result.era))
				{
					result.SetDate(year, raw.month, number);
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
				if (DateTimeParse.TryAdjustYear(ref result, number, out year) && result.calendar.IsValidDay(year, raw.month, number2, result.era))
				{
					result.SetDate(year, raw.month, number2);
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x06001618 RID: 5656 RVA: 0x0004349C File Offset: 0x0004169C
		private static bool GetDayOfYNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			int number = raw.GetNumber(0);
			int number2 = raw.GetNumber(1);
			string shortDatePattern = dtfi.ShortDatePattern;
			int num;
			if (DateTimeParse.GetYearMonthDayOrder(shortDatePattern, dtfi, out num) && num == 3)
			{
				if (DateTimeParse.SetDateYMD(ref result, raw.year, number2, number))
				{
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			else if (DateTimeParse.SetDateYMD(ref result, raw.year, number, number2))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x06001619 RID: 5657 RVA: 0x0004353C File Offset: 0x0004173C
		private static bool GetDayOfNNY(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			int number = raw.GetNumber(0);
			int number2 = raw.GetNumber(1);
			int num;
			if (!DateTimeParse.GetYearMonthDayOrder(dtfi.ShortDatePattern, dtfi, out num))
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
				return false;
			}
			if (num == 1 || num == 0)
			{
				if (DateTimeParse.SetDateYMD(ref result, raw.year, number, number2))
				{
					result.flags |= ParseFlags.HaveDate;
					return true;
				}
			}
			else if (DateTimeParse.SetDateYMD(ref result, raw.year, number2, number))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x0600161A RID: 5658 RVA: 0x000435F4 File Offset: 0x000417F4
		private static bool GetDayOfYMN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (DateTimeParse.SetDateYMD(ref result, raw.year, raw.month, raw.GetNumber(0)))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x0600161B RID: 5659 RVA: 0x00043658 File Offset: 0x00041858
		private static bool GetDayOfYN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (DateTimeParse.SetDateYMD(ref result, raw.year, raw.GetNumber(0), 1))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x0600161C RID: 5660 RVA: 0x000436B8 File Offset: 0x000418B8
		private static bool GetDayOfYM(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveDate) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (DateTimeParse.SetDateYMD(ref result, raw.year, raw.month, 1))
			{
				result.flags |= ParseFlags.HaveDate;
				return true;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x0600161D RID: 5661 RVA: 0x00043718 File Offset: 0x00041918
		private static void AdjustTimeMark(DateTimeFormatInfo dtfi, ref DateTimeRawInfo raw)
		{
			if (raw.timeMark == DateTimeParse.TM.NotSet && dtfi.AMDesignator != null && dtfi.PMDesignator != null)
			{
				if (dtfi.AMDesignator.Length == 0 && dtfi.PMDesignator.Length != 0)
				{
					raw.timeMark = DateTimeParse.TM.AM;
				}
				if (dtfi.PMDesignator.Length == 0 && dtfi.AMDesignator.Length != 0)
				{
					raw.timeMark = DateTimeParse.TM.PM;
				}
			}
		}

		// Token: 0x0600161E RID: 5662 RVA: 0x00043780 File Offset: 0x00041980
		private static bool AdjustHour(ref int hour, DateTimeParse.TM timeMark)
		{
			if (timeMark != DateTimeParse.TM.NotSet)
			{
				if (timeMark == DateTimeParse.TM.AM)
				{
					if (hour < 0 || hour > 12)
					{
						return false;
					}
					hour = ((hour == 12) ? 0 : hour);
				}
				else
				{
					if (hour < 0 || hour > 23)
					{
						return false;
					}
					if (hour < 12)
					{
						hour += 12;
					}
				}
			}
			return true;
		}

		// Token: 0x0600161F RID: 5663 RVA: 0x000437C0 File Offset: 0x000419C0
		private static bool GetTimeOfN(DateTimeFormatInfo dtfi, ref DateTimeResult result, ref DateTimeRawInfo raw)
		{
			if ((result.flags & ParseFlags.HaveTime) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (raw.timeMark == DateTimeParse.TM.NotSet)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			result.Hour = raw.GetNumber(0);
			result.flags |= ParseFlags.HaveTime;
			return true;
		}

		// Token: 0x06001620 RID: 5664 RVA: 0x0004381C File Offset: 0x00041A1C
		private static bool GetTimeOfNN(DateTimeFormatInfo dtfi, ref DateTimeResult result, ref DateTimeRawInfo raw)
		{
			if ((result.flags & ParseFlags.HaveTime) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			result.Hour = raw.GetNumber(0);
			result.Minute = raw.GetNumber(1);
			result.flags |= ParseFlags.HaveTime;
			return true;
		}

		// Token: 0x06001621 RID: 5665 RVA: 0x0004386C File Offset: 0x00041A6C
		private static bool GetTimeOfNNN(DateTimeFormatInfo dtfi, ref DateTimeResult result, ref DateTimeRawInfo raw)
		{
			if ((result.flags & ParseFlags.HaveTime) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			result.Hour = raw.GetNumber(0);
			result.Minute = raw.GetNumber(1);
			result.Second = raw.GetNumber(2);
			result.flags |= ParseFlags.HaveTime;
			return true;
		}

		// Token: 0x06001622 RID: 5666 RVA: 0x000438C7 File Offset: 0x00041AC7
		private static bool GetDateOfDSN(ref DateTimeResult result, ref DateTimeRawInfo raw)
		{
			if (raw.numCount != 1 || result.Day != -1)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			result.Day = raw.GetNumber(0);
			return true;
		}

		// Token: 0x06001623 RID: 5667 RVA: 0x000438F8 File Offset: 0x00041AF8
		private static bool GetDateOfNDS(ref DateTimeResult result, ref DateTimeRawInfo raw)
		{
			if (result.Month == -1)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (result.Year != -1)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (!DateTimeParse.TryAdjustYear(ref result, raw.GetNumber(0), out result.Year))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			result.Day = 1;
			return true;
		}

		// Token: 0x06001624 RID: 5668 RVA: 0x00043964 File Offset: 0x00041B64
		private static bool GetDateOfNNDS(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			if ((result.flags & ParseFlags.HaveYear) != (ParseFlags)0)
			{
				if ((result.flags & ParseFlags.HaveMonth) == (ParseFlags)0 && (result.flags & ParseFlags.HaveDay) == (ParseFlags)0 && DateTimeParse.TryAdjustYear(ref result, raw.year, out result.Year) && DateTimeParse.SetDateYMD(ref result, result.Year, raw.GetNumber(0), raw.GetNumber(1)))
				{
					return true;
				}
			}
			else if ((result.flags & ParseFlags.HaveMonth) != (ParseFlags)0 && (result.flags & ParseFlags.HaveYear) == (ParseFlags)0 && (result.flags & ParseFlags.HaveDay) == (ParseFlags)0)
			{
				int num;
				if (!DateTimeParse.GetYearMonthDayOrder(dtfi.ShortDatePattern, dtfi, out num))
				{
					result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
					return false;
				}
				int year;
				if (num == 0)
				{
					if (DateTimeParse.TryAdjustYear(ref result, raw.GetNumber(0), out year) && DateTimeParse.SetDateYMD(ref result, year, result.Month, raw.GetNumber(1)))
					{
						return true;
					}
				}
				else if (DateTimeParse.TryAdjustYear(ref result, raw.GetNumber(1), out year) && DateTimeParse.SetDateYMD(ref result, year, result.Month, raw.GetNumber(0)))
				{
					return true;
				}
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
		}

		// Token: 0x06001625 RID: 5669 RVA: 0x00043A78 File Offset: 0x00041C78
		private static bool ProcessDateTimeSuffix(ref DateTimeResult result, ref DateTimeRawInfo raw, ref DateTimeToken dtok)
		{
			TokenType suffix = dtok.suffix;
			if (suffix <= TokenType.SEP_DaySuff)
			{
				if (suffix != TokenType.SEP_YearSuff)
				{
					if (suffix != TokenType.SEP_MonthSuff)
					{
						if (suffix == TokenType.SEP_DaySuff)
						{
							if ((result.flags & ParseFlags.HaveDay) != (ParseFlags)0)
							{
								return false;
							}
							result.flags |= ParseFlags.HaveDay;
							result.Day = dtok.num;
						}
					}
					else
					{
						if ((result.flags & ParseFlags.HaveMonth) != (ParseFlags)0)
						{
							return false;
						}
						result.flags |= ParseFlags.HaveMonth;
						result.Month = (raw.month = dtok.num);
					}
				}
				else
				{
					if ((result.flags & ParseFlags.HaveYear) != (ParseFlags)0)
					{
						return false;
					}
					result.flags |= ParseFlags.HaveYear;
					result.Year = (raw.year = dtok.num);
				}
			}
			else if (suffix != TokenType.SEP_HourSuff)
			{
				if (suffix != TokenType.SEP_MinuteSuff)
				{
					if (suffix == TokenType.SEP_SecondSuff)
					{
						if ((result.flags & ParseFlags.HaveSecond) != (ParseFlags)0)
						{
							return false;
						}
						result.flags |= ParseFlags.HaveSecond;
						result.Second = dtok.num;
					}
				}
				else
				{
					if ((result.flags & ParseFlags.HaveMinute) != (ParseFlags)0)
					{
						return false;
					}
					result.flags |= ParseFlags.HaveMinute;
					result.Minute = dtok.num;
				}
			}
			else
			{
				if ((result.flags & ParseFlags.HaveHour) != (ParseFlags)0)
				{
					return false;
				}
				result.flags |= ParseFlags.HaveHour;
				result.Hour = dtok.num;
			}
			return true;
		}

		// Token: 0x06001626 RID: 5670 RVA: 0x00043BD4 File Offset: 0x00041DD4
		internal static bool ProcessHebrewTerminalState(DateTimeParse.DS dps, ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			switch (dps)
			{
			case DateTimeParse.DS.DX_MN:
			case DateTimeParse.DS.DX_NM:
				DateTimeParse.GetDefaultYear(ref result, ref styles);
				if (!dtfi.YearMonthAdjustment(ref result.Year, ref raw.month, true))
				{
					result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
					return false;
				}
				if (!DateTimeParse.GetHebrewDayOfNM(ref result, ref raw, dtfi))
				{
					return false;
				}
				goto IL_15E;
			case DateTimeParse.DS.DX_MNN:
				raw.year = raw.GetNumber(1);
				if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, true))
				{
					result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
					return false;
				}
				if (!DateTimeParse.GetDayOfMNN(ref result, ref raw, dtfi))
				{
					return false;
				}
				goto IL_15E;
			case DateTimeParse.DS.DX_YMN:
				if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, true))
				{
					result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
					return false;
				}
				if (!DateTimeParse.GetDayOfYMN(ref result, ref raw, dtfi))
				{
					return false;
				}
				goto IL_15E;
			case DateTimeParse.DS.DX_YM:
				if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, true))
				{
					result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
					return false;
				}
				if (!DateTimeParse.GetDayOfYM(ref result, ref raw, dtfi))
				{
					return false;
				}
				goto IL_15E;
			case DateTimeParse.DS.TX_N:
				if (!DateTimeParse.GetTimeOfN(dtfi, ref result, ref raw))
				{
					return false;
				}
				goto IL_15E;
			case DateTimeParse.DS.TX_NN:
				if (!DateTimeParse.GetTimeOfNN(dtfi, ref result, ref raw))
				{
					return false;
				}
				goto IL_15E;
			case DateTimeParse.DS.TX_NNN:
				if (!DateTimeParse.GetTimeOfNNN(dtfi, ref result, ref raw))
				{
					return false;
				}
				goto IL_15E;
			}
			result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
			return false;
			IL_15E:
			if (dps > DateTimeParse.DS.ERROR)
			{
				raw.numCount = 0;
			}
			return true;
		}

		// Token: 0x06001627 RID: 5671 RVA: 0x00043D4C File Offset: 0x00041F4C
		internal static bool ProcessTerminaltState(DateTimeParse.DS dps, ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
		{
			bool flag = true;
			switch (dps)
			{
			case DateTimeParse.DS.DX_NN:
				flag = DateTimeParse.GetDayOfNN(ref result, ref styles, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_NNN:
				flag = DateTimeParse.GetDayOfNNN(ref result, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_MN:
				flag = DateTimeParse.GetDayOfMN(ref result, ref styles, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_NM:
				flag = DateTimeParse.GetDayOfNM(ref result, ref styles, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_MNN:
				flag = DateTimeParse.GetDayOfMNN(ref result, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_DS:
				flag = true;
				break;
			case DateTimeParse.DS.DX_DSN:
				flag = DateTimeParse.GetDateOfDSN(ref result, ref raw);
				break;
			case DateTimeParse.DS.DX_NDS:
				flag = DateTimeParse.GetDateOfNDS(ref result, ref raw);
				break;
			case DateTimeParse.DS.DX_NNDS:
				flag = DateTimeParse.GetDateOfNNDS(ref result, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_YNN:
				flag = DateTimeParse.GetDayOfYNN(ref result, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_YMN:
				flag = DateTimeParse.GetDayOfYMN(ref result, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_YN:
				flag = DateTimeParse.GetDayOfYN(ref result, ref raw, dtfi);
				break;
			case DateTimeParse.DS.DX_YM:
				flag = DateTimeParse.GetDayOfYM(ref result, ref raw, dtfi);
				break;
			case DateTimeParse.DS.TX_N:
				flag = DateTimeParse.GetTimeOfN(dtfi, ref result, ref raw);
				break;
			case DateTimeParse.DS.TX_NN:
				flag = DateTimeParse.GetTimeOfNN(dtfi, ref result, ref raw);
				break;
			case DateTimeParse.DS.TX_NNN:
				flag = DateTimeParse.GetTimeOfNNN(dtfi, ref result, ref raw);
				break;
			case DateTimeParse.DS.TX_TS:
				flag = true;
				break;
			case DateTimeParse.DS.DX_NNY:
				flag = DateTimeParse.GetDayOfNNY(ref result, ref raw, dtfi);
				break;
			}
			if (!flag)
			{
				return false;
			}
			if (dps > DateTimeParse.DS.ERROR)
			{
				raw.numCount = 0;
			}
			return true;
		}

		// Token: 0x06001628 RID: 5672 RVA: 0x00043E9C File Offset: 0x0004209C
		internal static DateTime Parse(string s, DateTimeFormatInfo dtfi, DateTimeStyles styles)
		{
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			if (DateTimeParse.TryParse(s, dtfi, styles, ref dateTimeResult))
			{
				return dateTimeResult.parsedDate;
			}
			throw DateTimeParse.GetDateTimeParseException(ref dateTimeResult);
		}

		// Token: 0x06001629 RID: 5673 RVA: 0x00043ED4 File Offset: 0x000420D4
		internal static DateTime Parse(string s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out TimeSpan offset)
		{
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			dateTimeResult.flags |= ParseFlags.CaptureOffset;
			if (DateTimeParse.TryParse(s, dtfi, styles, ref dateTimeResult))
			{
				offset = dateTimeResult.timeZoneOffset;
				return dateTimeResult.parsedDate;
			}
			throw DateTimeParse.GetDateTimeParseException(ref dateTimeResult);
		}

		// Token: 0x0600162A RID: 5674 RVA: 0x00043F28 File Offset: 0x00042128
		internal static bool TryParse(string s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out DateTime result)
		{
			result = DateTime.MinValue;
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			if (DateTimeParse.TryParse(s, dtfi, styles, ref dateTimeResult))
			{
				result = dateTimeResult.parsedDate;
				return true;
			}
			return false;
		}

		// Token: 0x0600162B RID: 5675 RVA: 0x00043F6C File Offset: 0x0004216C
		internal static bool TryParse(string s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out DateTime result, out TimeSpan offset)
		{
			result = DateTime.MinValue;
			offset = TimeSpan.Zero;
			DateTimeResult dateTimeResult = default(DateTimeResult);
			dateTimeResult.Init();
			dateTimeResult.flags |= ParseFlags.CaptureOffset;
			if (DateTimeParse.TryParse(s, dtfi, styles, ref dateTimeResult))
			{
				result = dateTimeResult.parsedDate;
				offset = dateTimeResult.timeZoneOffset;
				return true;
			}
			return false;
		}

		// Token: 0x0600162C RID: 5676 RVA: 0x00043FD8 File Offset: 0x000421D8
		[SecuritySafeCritical]
		internal unsafe static bool TryParse(string s, DateTimeFormatInfo dtfi, DateTimeStyles styles, ref DateTimeResult result)
		{
			if (s == null)
			{
				result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "s");
				return false;
			}
			if (s.Length == 0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			DateTimeParse.DS ds = DateTimeParse.DS.BEGIN;
			bool flag = false;
			DateTimeToken dateTimeToken = default(DateTimeToken);
			dateTimeToken.suffix = TokenType.SEP_Unk;
			DateTimeRawInfo dateTimeRawInfo = default(DateTimeRawInfo);
			int* numberBuffer = stackalloc int[(UIntPtr)12];
			dateTimeRawInfo.Init(numberBuffer);
			dateTimeRawInfo.hasSameDateAndTimeSeparators = dtfi.DateSeparator.Equals(dtfi.TimeSeparator, StringComparison.Ordinal);
			result.calendar = dtfi.Calendar;
			result.era = 0;
			__DTString _DTString = new __DTString(s, dtfi);
			_DTString.GetNext();
			while (DateTimeParse.Lex(ds, ref _DTString, ref dateTimeToken, ref dateTimeRawInfo, ref result, ref dtfi, styles))
			{
				if (dateTimeToken.dtt != DateTimeParse.DTT.Unk)
				{
					if (dateTimeToken.suffix != TokenType.SEP_Unk)
					{
						if (!DateTimeParse.ProcessDateTimeSuffix(ref result, ref dateTimeRawInfo, ref dateTimeToken))
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						dateTimeToken.suffix = TokenType.SEP_Unk;
					}
					if (dateTimeToken.dtt == DateTimeParse.DTT.NumLocalTimeMark)
					{
						if (ds == DateTimeParse.DS.D_YNd || ds == DateTimeParse.DS.D_YN)
						{
							return DateTimeParse.ParseISO8601(ref dateTimeRawInfo, ref _DTString, styles, ref result);
						}
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					else
					{
						if (dateTimeRawInfo.hasSameDateAndTimeSeparators)
						{
							if (dateTimeToken.dtt == DateTimeParse.DTT.YearEnd || dateTimeToken.dtt == DateTimeParse.DTT.YearSpace || dateTimeToken.dtt == DateTimeParse.DTT.YearDateSep)
							{
								if (ds == DateTimeParse.DS.T_Nt)
								{
									ds = DateTimeParse.DS.D_Nd;
								}
								if (ds == DateTimeParse.DS.T_NNt)
								{
									ds = DateTimeParse.DS.D_NNd;
								}
							}
							bool flag2 = _DTString.AtEnd();
							if (DateTimeParse.dateParsingStates[(int)ds][(int)dateTimeToken.dtt] == DateTimeParse.DS.ERROR || flag2)
							{
								DateTimeParse.DTT dtt = dateTimeToken.dtt;
								switch (dtt)
								{
								case DateTimeParse.DTT.NumDatesep:
									dateTimeToken.dtt = (flag2 ? DateTimeParse.DTT.NumEnd : DateTimeParse.DTT.NumSpace);
									break;
								case DateTimeParse.DTT.NumTimesep:
									dateTimeToken.dtt = (flag2 ? DateTimeParse.DTT.NumEnd : DateTimeParse.DTT.NumSpace);
									break;
								case DateTimeParse.DTT.MonthEnd:
								case DateTimeParse.DTT.MonthSpace:
									break;
								case DateTimeParse.DTT.MonthDatesep:
									dateTimeToken.dtt = (flag2 ? DateTimeParse.DTT.MonthEnd : DateTimeParse.DTT.MonthSpace);
									break;
								default:
									if (dtt == DateTimeParse.DTT.YearDateSep)
									{
										dateTimeToken.dtt = (flag2 ? DateTimeParse.DTT.YearEnd : DateTimeParse.DTT.YearSpace);
									}
									break;
								}
							}
						}
						ds = DateTimeParse.dateParsingStates[(int)ds][(int)dateTimeToken.dtt];
						if (ds == DateTimeParse.DS.ERROR)
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						if (ds > DateTimeParse.DS.ERROR)
						{
							if ((dtfi.FormatFlags & DateTimeFormatFlags.UseHebrewRule) != DateTimeFormatFlags.None)
							{
								if (!DateTimeParse.ProcessHebrewTerminalState(ds, ref result, ref styles, ref dateTimeRawInfo, dtfi))
								{
									return false;
								}
							}
							else if (!DateTimeParse.ProcessTerminaltState(ds, ref result, ref styles, ref dateTimeRawInfo, dtfi))
							{
								return false;
							}
							flag = true;
							ds = DateTimeParse.DS.BEGIN;
						}
					}
				}
				if (dateTimeToken.dtt == DateTimeParse.DTT.End || dateTimeToken.dtt == DateTimeParse.DTT.NumEnd || dateTimeToken.dtt == DateTimeParse.DTT.MonthEnd)
				{
					if (!flag)
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					DateTimeParse.AdjustTimeMark(dtfi, ref dateTimeRawInfo);
					if (!DateTimeParse.AdjustHour(ref result.Hour, dateTimeRawInfo.timeMark))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					bool bTimeOnly = result.Year == -1 && result.Month == -1 && result.Day == -1;
					if (!DateTimeParse.CheckDefaultDateTime(ref result, ref result.calendar, styles))
					{
						return false;
					}
					DateTime dateTime;
					if (!result.calendar.TryToDateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, 0, result.era, out dateTime))
					{
						result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
						return false;
					}
					if (dateTimeRawInfo.fraction > 0.0)
					{
						dateTime = dateTime.AddTicks((long)Math.Round(dateTimeRawInfo.fraction * 10000000.0));
					}
					if (dateTimeRawInfo.dayOfWeek != -1 && dateTimeRawInfo.dayOfWeek != (int)result.calendar.GetDayOfWeek(dateTime))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDayOfWeek", null);
						return false;
					}
					result.parsedDate = dateTime;
					return DateTimeParse.DetermineTimeZoneAdjustments(ref result, styles, bTimeOnly);
				}
			}
			return false;
		}

		// Token: 0x0600162D RID: 5677 RVA: 0x0004436C File Offset: 0x0004256C
		private static bool DetermineTimeZoneAdjustments(ref DateTimeResult result, DateTimeStyles styles, bool bTimeOnly)
		{
			if ((result.flags & ParseFlags.CaptureOffset) != (ParseFlags)0)
			{
				return DateTimeParse.DateTimeOffsetTimeZonePostProcessing(ref result, styles);
			}
			if ((result.flags & ParseFlags.TimeZoneUsed) == (ParseFlags)0)
			{
				if ((styles & DateTimeStyles.AssumeLocal) != DateTimeStyles.None)
				{
					if ((styles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.None)
					{
						result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Local);
						return true;
					}
					result.flags |= ParseFlags.TimeZoneUsed;
					result.timeZoneOffset = TimeZoneInfo.GetLocalUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime);
				}
				else
				{
					if ((styles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.None)
					{
						return true;
					}
					if ((styles & DateTimeStyles.AdjustToUniversal) != DateTimeStyles.None)
					{
						result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Utc);
						return true;
					}
					result.flags |= ParseFlags.TimeZoneUsed;
					result.timeZoneOffset = TimeSpan.Zero;
				}
			}
			if ((styles & DateTimeStyles.RoundtripKind) != DateTimeStyles.None && (result.flags & ParseFlags.TimeZoneUtc) != (ParseFlags)0)
			{
				result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Utc);
				return true;
			}
			if ((styles & DateTimeStyles.AdjustToUniversal) != DateTimeStyles.None)
			{
				return DateTimeParse.AdjustTimeZoneToUniversal(ref result);
			}
			return DateTimeParse.AdjustTimeZoneToLocal(ref result, bTimeOnly);
		}

		// Token: 0x0600162E RID: 5678 RVA: 0x00044460 File Offset: 0x00042660
		private static bool DateTimeOffsetTimeZonePostProcessing(ref DateTimeResult result, DateTimeStyles styles)
		{
			if ((result.flags & ParseFlags.TimeZoneUsed) == (ParseFlags)0)
			{
				if ((styles & DateTimeStyles.AssumeUniversal) != DateTimeStyles.None)
				{
					result.timeZoneOffset = TimeSpan.Zero;
				}
				else
				{
					result.timeZoneOffset = TimeZoneInfo.GetLocalUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime);
				}
			}
			long ticks = result.timeZoneOffset.Ticks;
			long num = result.parsedDate.Ticks - ticks;
			if (num < 0L || num > 3155378975999999999L)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_UTCOutOfRange", null);
				return false;
			}
			if (ticks < -504000000000L || ticks > 504000000000L)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_OffsetOutOfRange", null);
				return false;
			}
			if ((styles & DateTimeStyles.AdjustToUniversal) != DateTimeStyles.None)
			{
				if ((result.flags & ParseFlags.TimeZoneUsed) == (ParseFlags)0 && (styles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.None)
				{
					bool result2 = DateTimeParse.AdjustTimeZoneToUniversal(ref result);
					result.timeZoneOffset = TimeSpan.Zero;
					return result2;
				}
				result.parsedDate = new DateTime(num, DateTimeKind.Utc);
				result.timeZoneOffset = TimeSpan.Zero;
			}
			return true;
		}

		// Token: 0x0600162F RID: 5679 RVA: 0x00044548 File Offset: 0x00042748
		private static bool AdjustTimeZoneToUniversal(ref DateTimeResult result)
		{
			long num = result.parsedDate.Ticks;
			num -= result.timeZoneOffset.Ticks;
			if (num < 0L)
			{
				num += 864000000000L;
			}
			if (num < 0L || num > 3155378975999999999L)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_DateOutOfRange", null);
				return false;
			}
			result.parsedDate = new DateTime(num, DateTimeKind.Utc);
			return true;
		}

		// Token: 0x06001630 RID: 5680 RVA: 0x000445B0 File Offset: 0x000427B0
		private static bool AdjustTimeZoneToLocal(ref DateTimeResult result, bool bTimeOnly)
		{
			long num = result.parsedDate.Ticks;
			TimeZoneInfo local = TimeZoneInfo.Local;
			bool isAmbiguousDst = false;
			if (num < 864000000000L)
			{
				num -= result.timeZoneOffset.Ticks;
				num += local.GetUtcOffset(bTimeOnly ? DateTime.Now : result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
				if (num < 0L)
				{
					num += 864000000000L;
				}
			}
			else
			{
				num -= result.timeZoneOffset.Ticks;
				if (num < 0L || num > 3155378975999999999L)
				{
					num += local.GetUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
				}
				else
				{
					DateTime time = new DateTime(num, DateTimeKind.Utc);
					bool flag = false;
					num += TimeZoneInfo.GetUtcOffsetFromUtc(time, TimeZoneInfo.Local, out flag, out isAmbiguousDst).Ticks;
				}
			}
			if (num < 0L || num > 3155378975999999999L)
			{
				result.parsedDate = DateTime.MinValue;
				result.SetFailure(ParseFailureKind.Format, "Format_DateOutOfRange", null);
				return false;
			}
			result.parsedDate = new DateTime(num, DateTimeKind.Local, isAmbiguousDst);
			return true;
		}

		// Token: 0x06001631 RID: 5681 RVA: 0x000446BC File Offset: 0x000428BC
		private static bool ParseISO8601(ref DateTimeRawInfo raw, ref __DTString str, DateTimeStyles styles, ref DateTimeResult result)
		{
			if (raw.year >= 0 && raw.GetNumber(0) >= 0)
			{
				raw.GetNumber(1);
			}
			str.Index--;
			int second = 0;
			double num = 0.0;
			str.SkipWhiteSpaces();
			int hour;
			if (!DateTimeParse.ParseDigits(ref str, 2, out hour))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			str.SkipWhiteSpaces();
			if (!str.Match(':'))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			str.SkipWhiteSpaces();
			int minute;
			if (!DateTimeParse.ParseDigits(ref str, 2, out minute))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			str.SkipWhiteSpaces();
			if (str.Match(':'))
			{
				str.SkipWhiteSpaces();
				if (!DateTimeParse.ParseDigits(ref str, 2, out second))
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				if (str.Match('.'))
				{
					if (!DateTimeParse.ParseFraction(ref str, out num))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					str.Index--;
				}
				str.SkipWhiteSpaces();
			}
			if (str.GetNext())
			{
				char @char = str.GetChar();
				if (@char == '+' || @char == '-')
				{
					result.flags |= ParseFlags.TimeZoneUsed;
					if (!DateTimeParse.ParseTimeZone(ref str, ref result.timeZoneOffset))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
				}
				else if (@char == 'Z' || @char == 'z')
				{
					result.flags |= ParseFlags.TimeZoneUsed;
					result.timeZoneOffset = TimeSpan.Zero;
					result.flags |= ParseFlags.TimeZoneUtc;
				}
				else
				{
					str.Index--;
				}
				str.SkipWhiteSpaces();
				if (str.Match('#'))
				{
					if (!DateTimeParse.VerifyValidPunctuation(ref str))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					str.SkipWhiteSpaces();
				}
				if (str.Match('\0') && !DateTimeParse.VerifyValidPunctuation(ref str))
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				if (str.GetNext())
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
			}
			Calendar defaultInstance = GregorianCalendar.GetDefaultInstance();
			DateTime parsedDate;
			if (!defaultInstance.TryToDateTime(raw.year, raw.GetNumber(0), raw.GetNumber(1), hour, minute, second, 0, result.era, out parsedDate))
			{
				result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
				return false;
			}
			parsedDate = parsedDate.AddTicks((long)Math.Round(num * 10000000.0));
			result.parsedDate = parsedDate;
			return DateTimeParse.DetermineTimeZoneAdjustments(ref result, styles, false);
		}

		// Token: 0x06001632 RID: 5682 RVA: 0x00044920 File Offset: 0x00042B20
		internal static bool MatchHebrewDigits(ref __DTString str, int digitLen, out int number)
		{
			number = 0;
			HebrewNumberParsingContext hebrewNumberParsingContext = new HebrewNumberParsingContext(0);
			HebrewNumberParsingState hebrewNumberParsingState = HebrewNumberParsingState.ContinueParsing;
			while (hebrewNumberParsingState == HebrewNumberParsingState.ContinueParsing && str.GetNext())
			{
				hebrewNumberParsingState = HebrewNumber.ParseByChar(str.GetChar(), ref hebrewNumberParsingContext);
			}
			if (hebrewNumberParsingState == HebrewNumberParsingState.FoundEndOfHebrewNumber)
			{
				number = hebrewNumberParsingContext.result;
				return true;
			}
			return false;
		}

		// Token: 0x06001633 RID: 5683 RVA: 0x00044965 File Offset: 0x00042B65
		internal static bool ParseDigits(ref __DTString str, int digitLen, out int result)
		{
			if (digitLen == 1)
			{
				return DateTimeParse.ParseDigits(ref str, 1, 2, out result);
			}
			return DateTimeParse.ParseDigits(ref str, digitLen, digitLen, out result);
		}

		// Token: 0x06001634 RID: 5684 RVA: 0x00044980 File Offset: 0x00042B80
		internal static bool ParseDigits(ref __DTString str, int minDigitLen, int maxDigitLen, out int result)
		{
			result = 0;
			int index = str.Index;
			int i;
			for (i = 0; i < maxDigitLen; i++)
			{
				if (!str.GetNextDigit())
				{
					str.Index--;
					break;
				}
				result = result * 10 + str.GetDigit();
			}
			if (i < minDigitLen)
			{
				str.Index = index;
				return false;
			}
			return true;
		}

		// Token: 0x06001635 RID: 5685 RVA: 0x000449D4 File Offset: 0x00042BD4
		private static bool ParseFractionExact(ref __DTString str, int maxDigitLen, ref double result)
		{
			if (!str.GetNextDigit())
			{
				str.Index--;
				return false;
			}
			result = (double)str.GetDigit();
			int i;
			for (i = 1; i < maxDigitLen; i++)
			{
				if (!str.GetNextDigit())
				{
					str.Index--;
					break;
				}
				result = result * 10.0 + (double)str.GetDigit();
			}
			result /= Math.Pow(10.0, (double)i);
			return i == maxDigitLen;
		}

		// Token: 0x06001636 RID: 5686 RVA: 0x00044A50 File Offset: 0x00042C50
		private static bool ParseSign(ref __DTString str, ref bool result)
		{
			if (!str.GetNext())
			{
				return false;
			}
			char @char = str.GetChar();
			if (@char == '+')
			{
				result = true;
				return true;
			}
			if (@char == '-')
			{
				result = false;
				return true;
			}
			return false;
		}

		// Token: 0x06001637 RID: 5687 RVA: 0x00044A84 File Offset: 0x00042C84
		private static bool ParseTimeZoneOffset(ref __DTString str, int len, ref TimeSpan result)
		{
			bool flag = true;
			int num = 0;
			int hours;
			if (len - 1 <= 1)
			{
				if (!DateTimeParse.ParseSign(ref str, ref flag))
				{
					return false;
				}
				if (!DateTimeParse.ParseDigits(ref str, len, out hours))
				{
					return false;
				}
			}
			else
			{
				if (!DateTimeParse.ParseSign(ref str, ref flag))
				{
					return false;
				}
				if (!DateTimeParse.ParseDigits(ref str, 1, out hours))
				{
					return false;
				}
				if (str.Match(":"))
				{
					if (!DateTimeParse.ParseDigits(ref str, 2, out num))
					{
						return false;
					}
				}
				else
				{
					str.Index--;
					if (!DateTimeParse.ParseDigits(ref str, 2, out num))
					{
						return false;
					}
				}
			}
			if (num < 0 || num >= 60)
			{
				return false;
			}
			result = new TimeSpan(hours, num, 0);
			if (!flag)
			{
				result = result.Negate();
			}
			return true;
		}

		// Token: 0x06001638 RID: 5688 RVA: 0x00044B28 File Offset: 0x00042D28
		private static bool MatchAbbreviatedMonthName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
		{
			int num = 0;
			result = -1;
			if (str.GetNext())
			{
				int num2 = ((dtfi.GetMonthName(13).Length == 0) ? 12 : 13);
				for (int i = 1; i <= num2; i++)
				{
					string abbreviatedMonthName = dtfi.GetAbbreviatedMonthName(i);
					int length = abbreviatedMonthName.Length;
					if ((dtfi.HasSpacesInMonthNames ? str.MatchSpecifiedWords(abbreviatedMonthName, false, ref length) : str.MatchSpecifiedWord(abbreviatedMonthName)) && length > num)
					{
						num = length;
						result = i;
					}
				}
				if ((dtfi.FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != DateTimeFormatFlags.None)
				{
					int num3 = str.MatchLongestWords(dtfi.internalGetLeapYearMonthNames(), ref num);
					if (num3 >= 0)
					{
						result = num3 + 1;
					}
				}
			}
			if (result > 0)
			{
				str.Index += num - 1;
				return true;
			}
			return false;
		}

		// Token: 0x06001639 RID: 5689 RVA: 0x00044BD8 File Offset: 0x00042DD8
		private static bool MatchMonthName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
		{
			int num = 0;
			result = -1;
			if (str.GetNext())
			{
				int num2 = ((dtfi.GetMonthName(13).Length == 0) ? 12 : 13);
				for (int i = 1; i <= num2; i++)
				{
					string monthName = dtfi.GetMonthName(i);
					int length = monthName.Length;
					if ((dtfi.HasSpacesInMonthNames ? str.MatchSpecifiedWords(monthName, false, ref length) : str.MatchSpecifiedWord(monthName)) && length > num)
					{
						num = length;
						result = i;
					}
				}
				if ((dtfi.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != DateTimeFormatFlags.None)
				{
					int num3 = str.MatchLongestWords(dtfi.MonthGenitiveNames, ref num);
					if (num3 >= 0)
					{
						result = num3 + 1;
					}
				}
				if ((dtfi.FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != DateTimeFormatFlags.None)
				{
					int num4 = str.MatchLongestWords(dtfi.internalGetLeapYearMonthNames(), ref num);
					if (num4 >= 0)
					{
						result = num4 + 1;
					}
				}
			}
			if (result > 0)
			{
				str.Index += num - 1;
				return true;
			}
			return false;
		}

		// Token: 0x0600163A RID: 5690 RVA: 0x00044CB0 File Offset: 0x00042EB0
		private static bool MatchAbbreviatedDayName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
		{
			int num = 0;
			result = -1;
			if (str.GetNext())
			{
				for (DayOfWeek dayOfWeek = DayOfWeek.Sunday; dayOfWeek <= DayOfWeek.Saturday; dayOfWeek++)
				{
					string abbreviatedDayName = dtfi.GetAbbreviatedDayName(dayOfWeek);
					int length = abbreviatedDayName.Length;
					if ((dtfi.HasSpacesInDayNames ? str.MatchSpecifiedWords(abbreviatedDayName, false, ref length) : str.MatchSpecifiedWord(abbreviatedDayName)) && length > num)
					{
						num = length;
						result = (int)dayOfWeek;
					}
				}
			}
			if (result >= 0)
			{
				str.Index += num - 1;
				return true;
			}
			return false;
		}

		// Token: 0x0600163B RID: 5691 RVA: 0x00044D20 File Offset: 0x00042F20
		private static bool MatchDayName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
		{
			int num = 0;
			result = -1;
			if (str.GetNext())
			{
				for (DayOfWeek dayOfWeek = DayOfWeek.Sunday; dayOfWeek <= DayOfWeek.Saturday; dayOfWeek++)
				{
					string dayName = dtfi.GetDayName(dayOfWeek);
					int length = dayName.Length;
					if ((dtfi.HasSpacesInDayNames ? str.MatchSpecifiedWords(dayName, false, ref length) : str.MatchSpecifiedWord(dayName)) && length > num)
					{
						num = length;
						result = (int)dayOfWeek;
					}
				}
			}
			if (result >= 0)
			{
				str.Index += num - 1;
				return true;
			}
			return false;
		}

		// Token: 0x0600163C RID: 5692 RVA: 0x00044D90 File Offset: 0x00042F90
		private static bool MatchEraName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
		{
			if (str.GetNext())
			{
				int[] eras = dtfi.Calendar.Eras;
				if (eras != null)
				{
					for (int i = 0; i < eras.Length; i++)
					{
						string text = dtfi.GetEraName(eras[i]);
						if (str.MatchSpecifiedWord(text))
						{
							str.Index += text.Length - 1;
							result = eras[i];
							return true;
						}
						text = dtfi.GetAbbreviatedEraName(eras[i]);
						if (str.MatchSpecifiedWord(text))
						{
							str.Index += text.Length - 1;
							result = eras[i];
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x0600163D RID: 5693 RVA: 0x00044E1C File Offset: 0x0004301C
		private static bool MatchTimeMark(ref __DTString str, DateTimeFormatInfo dtfi, ref DateTimeParse.TM result)
		{
			result = DateTimeParse.TM.NotSet;
			if (dtfi.AMDesignator.Length == 0)
			{
				result = DateTimeParse.TM.AM;
			}
			if (dtfi.PMDesignator.Length == 0)
			{
				result = DateTimeParse.TM.PM;
			}
			if (str.GetNext())
			{
				string text = dtfi.AMDesignator;
				if (text.Length > 0 && str.MatchSpecifiedWord(text))
				{
					str.Index += text.Length - 1;
					result = DateTimeParse.TM.AM;
					return true;
				}
				text = dtfi.PMDesignator;
				if (text.Length > 0 && str.MatchSpecifiedWord(text))
				{
					str.Index += text.Length - 1;
					result = DateTimeParse.TM.PM;
					return true;
				}
				str.Index--;
			}
			return result != DateTimeParse.TM.NotSet;
		}

		// Token: 0x0600163E RID: 5694 RVA: 0x00044EC7 File Offset: 0x000430C7
		private static bool MatchAbbreviatedTimeMark(ref __DTString str, DateTimeFormatInfo dtfi, ref DateTimeParse.TM result)
		{
			if (str.GetNext())
			{
				if (str.GetChar() == dtfi.AMDesignator[0])
				{
					result = DateTimeParse.TM.AM;
					return true;
				}
				if (str.GetChar() == dtfi.PMDesignator[0])
				{
					result = DateTimeParse.TM.PM;
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600163F RID: 5695 RVA: 0x00044F04 File Offset: 0x00043104
		private static bool CheckNewValue(ref int currentValue, int newValue, char patternChar, ref DateTimeResult result)
		{
			if (currentValue == -1)
			{
				currentValue = newValue;
				return true;
			}
			if (newValue != currentValue)
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", patternChar);
				return false;
			}
			return true;
		}

		// Token: 0x06001640 RID: 5696 RVA: 0x00044F2C File Offset: 0x0004312C
		private static DateTime GetDateTimeNow(ref DateTimeResult result, ref DateTimeStyles styles)
		{
			if ((result.flags & ParseFlags.CaptureOffset) != (ParseFlags)0)
			{
				if ((result.flags & ParseFlags.TimeZoneUsed) != (ParseFlags)0)
				{
					return new DateTime(DateTime.UtcNow.Ticks + result.timeZoneOffset.Ticks, DateTimeKind.Unspecified);
				}
				if ((styles & DateTimeStyles.AssumeUniversal) != DateTimeStyles.None)
				{
					return DateTime.UtcNow;
				}
			}
			return DateTime.Now;
		}

		// Token: 0x06001641 RID: 5697 RVA: 0x00044F88 File Offset: 0x00043188
		private static bool CheckDefaultDateTime(ref DateTimeResult result, ref Calendar cal, DateTimeStyles styles)
		{
			if ((result.flags & ParseFlags.CaptureOffset) != (ParseFlags)0 && (result.Month != -1 || result.Day != -1) && (result.Year == -1 || (result.flags & ParseFlags.YearDefault) != (ParseFlags)0) && (result.flags & ParseFlags.TimeZoneUsed) != (ParseFlags)0)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_MissingIncompleteDate", null);
				return false;
			}
			if (result.Year == -1 || result.Month == -1 || result.Day == -1)
			{
				DateTime dateTimeNow = DateTimeParse.GetDateTimeNow(ref result, ref styles);
				if (result.Month == -1 && result.Day == -1)
				{
					if (result.Year == -1)
					{
						if ((styles & DateTimeStyles.NoCurrentDateDefault) != DateTimeStyles.None)
						{
							cal = GregorianCalendar.GetDefaultInstance();
							result.Year = (result.Month = (result.Day = 1));
						}
						else
						{
							result.Year = cal.GetYear(dateTimeNow);
							result.Month = cal.GetMonth(dateTimeNow);
							result.Day = cal.GetDayOfMonth(dateTimeNow);
						}
					}
					else
					{
						result.Month = 1;
						result.Day = 1;
					}
				}
				else
				{
					if (result.Year == -1)
					{
						result.Year = cal.GetYear(dateTimeNow);
					}
					if (result.Month == -1)
					{
						result.Month = 1;
					}
					if (result.Day == -1)
					{
						result.Day = 1;
					}
				}
			}
			if (result.Hour == -1)
			{
				result.Hour = 0;
			}
			if (result.Minute == -1)
			{
				result.Minute = 0;
			}
			if (result.Second == -1)
			{
				result.Second = 0;
			}
			if (result.era == -1)
			{
				result.era = 0;
			}
			return true;
		}

		// Token: 0x06001642 RID: 5698 RVA: 0x00045108 File Offset: 0x00043308
		private static string ExpandPredefinedFormat(string format, ref DateTimeFormatInfo dtfi, ref ParsingInfo parseInfo, ref DateTimeResult result)
		{
			char c = format[0];
			if (c <= 'R')
			{
				if (c != 'O')
				{
					if (c != 'R')
					{
						goto IL_151;
					}
					goto IL_65;
				}
			}
			else if (c != 'U')
			{
				switch (c)
				{
				case 'o':
					break;
				case 'p':
				case 'q':
				case 't':
					goto IL_151;
				case 'r':
					goto IL_65;
				case 's':
					dtfi = DateTimeFormatInfo.InvariantInfo;
					parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
					goto IL_151;
				case 'u':
					parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
					dtfi = DateTimeFormatInfo.InvariantInfo;
					if ((result.flags & ParseFlags.CaptureOffset) != (ParseFlags)0)
					{
						result.flags |= ParseFlags.UtcSortPattern;
						goto IL_151;
					}
					goto IL_151;
				default:
					goto IL_151;
				}
			}
			else
			{
				parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = new TimeSpan(0L);
				result.flags |= ParseFlags.TimeZoneUtc;
				if (dtfi.Calendar.GetType() != typeof(GregorianCalendar))
				{
					dtfi = (DateTimeFormatInfo)dtfi.Clone();
					dtfi.Calendar = GregorianCalendar.GetDefaultInstance();
					goto IL_151;
				}
				goto IL_151;
			}
			parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
			dtfi = DateTimeFormatInfo.InvariantInfo;
			goto IL_151;
			IL_65:
			parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
			dtfi = DateTimeFormatInfo.InvariantInfo;
			if ((result.flags & ParseFlags.CaptureOffset) != (ParseFlags)0)
			{
				result.flags |= ParseFlags.Rfc1123Pattern;
			}
			IL_151:
			return DateTimeFormat.GetRealFormat(format, dtfi);
		}

		// Token: 0x06001643 RID: 5699 RVA: 0x00045270 File Offset: 0x00043470
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ParseJapaneseEraStart(ref __DTString str, DateTimeFormatInfo dtfi)
		{
			if (AppContextSwitches.EnforceLegacyJapaneseDateParsing || dtfi.Calendar.ID != 3 || !str.GetNext())
			{
				return false;
			}
			if (str.m_current != "元"[0])
			{
				str.Index--;
				return false;
			}
			return true;
		}

		// Token: 0x06001644 RID: 5700 RVA: 0x000452C0 File Offset: 0x000434C0
		private static bool ParseByFormat(ref __DTString str, ref __DTString format, ref ParsingInfo parseInfo, DateTimeFormatInfo dtfi, ref DateTimeResult result)
		{
			int num = 0;
			int newValue = 0;
			int newValue2 = 0;
			int newValue3 = 0;
			int newValue4 = 0;
			int newValue5 = 0;
			int newValue6 = 0;
			int newValue7 = 0;
			double num2 = 0.0;
			DateTimeParse.TM tm = DateTimeParse.TM.AM;
			char @char = format.GetChar();
			if (@char <= 'K')
			{
				if (@char <= '.')
				{
					if (@char <= '%')
					{
						if (@char != '"')
						{
							if (@char != '%')
							{
								goto IL_9DB;
							}
							if (format.Index >= format.Value.Length - 1 || format.Value[format.Index + 1] == '%')
							{
								result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
								return false;
							}
							return true;
						}
					}
					else if (@char != '\'')
					{
						if (@char != '.')
						{
							goto IL_9DB;
						}
						if (str.Match(@char))
						{
							return true;
						}
						if (format.GetNext() && format.Match('F'))
						{
							format.GetRepeatCount();
							return true;
						}
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					StringBuilder stringBuilder = new StringBuilder();
					if (!DateTimeParse.TryParseQuoteString(format.Value, format.Index, stringBuilder, out num))
					{
						result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadQuote", @char);
						return false;
					}
					format.Index += num - 1;
					string text = stringBuilder.ToString();
					for (int i = 0; i < text.Length; i++)
					{
						if (text[i] == ' ' && parseInfo.fAllowInnerWhite)
						{
							str.SkipWhiteSpaces();
						}
						else if (!str.Match(text[i]))
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
					}
					if ((result.flags & ParseFlags.CaptureOffset) == (ParseFlags)0)
					{
						return true;
					}
					if ((result.flags & ParseFlags.Rfc1123Pattern) != (ParseFlags)0 && text == "GMT")
					{
						result.flags |= ParseFlags.TimeZoneUsed;
						result.timeZoneOffset = TimeSpan.Zero;
						return true;
					}
					if ((result.flags & ParseFlags.UtcSortPattern) != (ParseFlags)0 && text == "Z")
					{
						result.flags |= ParseFlags.TimeZoneUsed;
						result.timeZoneOffset = TimeSpan.Zero;
						return true;
					}
					return true;
				}
				else if (@char <= ':')
				{
					if (@char != '/')
					{
						if (@char != ':')
						{
							goto IL_9DB;
						}
						if (((dtfi.TimeSeparator.Length > 1 && dtfi.TimeSeparator[0] == ':') || !str.Match(':')) && !str.Match(dtfi.TimeSeparator))
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						return true;
					}
					else
					{
						if (((dtfi.DateSeparator.Length > 1 && dtfi.DateSeparator[0] == '/') || !str.Match('/')) && !str.Match(dtfi.DateSeparator))
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						return true;
					}
				}
				else if (@char != 'F')
				{
					if (@char != 'H')
					{
						if (@char != 'K')
						{
							goto IL_9DB;
						}
						if (str.Match('Z'))
						{
							if ((result.flags & ParseFlags.TimeZoneUsed) != (ParseFlags)0 && result.timeZoneOffset != TimeSpan.Zero)
							{
								result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'K');
								return false;
							}
							result.flags |= ParseFlags.TimeZoneUsed;
							result.timeZoneOffset = new TimeSpan(0L);
							result.flags |= ParseFlags.TimeZoneUtc;
							return true;
						}
						else
						{
							if (!str.Match('+') && !str.Match('-'))
							{
								return true;
							}
							str.Index--;
							TimeSpan timeSpan = new TimeSpan(0L);
							if (!DateTimeParse.ParseTimeZoneOffset(ref str, 3, ref timeSpan))
							{
								result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
								return false;
							}
							if ((result.flags & ParseFlags.TimeZoneUsed) != (ParseFlags)0 && timeSpan != result.timeZoneOffset)
							{
								result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'K');
								return false;
							}
							result.timeZoneOffset = timeSpan;
							result.flags |= ParseFlags.TimeZoneUsed;
							return true;
						}
					}
					else
					{
						num = format.GetRepeatCount();
						if (!DateTimeParse.ParseDigits(ref str, (num < 2) ? 1 : 2, out newValue5))
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						if (!DateTimeParse.CheckNewValue(ref result.Hour, newValue5, @char, ref result))
						{
							return false;
						}
						return true;
					}
				}
			}
			else if (@char <= 'h')
			{
				if (@char <= 'Z')
				{
					if (@char != 'M')
					{
						if (@char != 'Z')
						{
							goto IL_9DB;
						}
						if ((result.flags & ParseFlags.TimeZoneUsed) != (ParseFlags)0 && result.timeZoneOffset != TimeSpan.Zero)
						{
							result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'Z');
							return false;
						}
						result.flags |= ParseFlags.TimeZoneUsed;
						result.timeZoneOffset = new TimeSpan(0L);
						result.flags |= ParseFlags.TimeZoneUtc;
						str.Index++;
						if (!DateTimeParse.GetTimeZoneName(ref str))
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						str.Index--;
						return true;
					}
					else
					{
						num = format.GetRepeatCount();
						if (num <= 2)
						{
							if (!DateTimeParse.ParseDigits(ref str, num, out newValue2) && (!parseInfo.fCustomNumberParser || !parseInfo.parseNumberDelegate(ref str, num, out newValue2)))
							{
								result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
								return false;
							}
						}
						else
						{
							if (num == 3)
							{
								if (!DateTimeParse.MatchAbbreviatedMonthName(ref str, dtfi, ref newValue2))
								{
									result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
									return false;
								}
							}
							else if (!DateTimeParse.MatchMonthName(ref str, dtfi, ref newValue2))
							{
								result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
								return false;
							}
							result.flags |= ParseFlags.ParsedMonthName;
						}
						if (!DateTimeParse.CheckNewValue(ref result.Month, newValue2, @char, ref result))
						{
							return false;
						}
						return true;
					}
				}
				else if (@char != '\\')
				{
					switch (@char)
					{
					case 'd':
						num = format.GetRepeatCount();
						if (num <= 2)
						{
							if (!DateTimeParse.ParseDigits(ref str, num, out newValue3) && (!parseInfo.fCustomNumberParser || !parseInfo.parseNumberDelegate(ref str, num, out newValue3)))
							{
								result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
								return false;
							}
							if (!DateTimeParse.CheckNewValue(ref result.Day, newValue3, @char, ref result))
							{
								return false;
							}
							return true;
						}
						else
						{
							if (num == 3)
							{
								if (!DateTimeParse.MatchAbbreviatedDayName(ref str, dtfi, ref newValue4))
								{
									result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
									return false;
								}
							}
							else if (!DateTimeParse.MatchDayName(ref str, dtfi, ref newValue4))
							{
								result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
								return false;
							}
							if (!DateTimeParse.CheckNewValue(ref parseInfo.dayOfWeek, newValue4, @char, ref result))
							{
								return false;
							}
							return true;
						}
						break;
					case 'e':
						goto IL_9DB;
					case 'f':
						break;
					case 'g':
						num = format.GetRepeatCount();
						if (!DateTimeParse.MatchEraName(ref str, dtfi, ref result.era))
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						return true;
					case 'h':
						parseInfo.fUseHour12 = true;
						num = format.GetRepeatCount();
						if (!DateTimeParse.ParseDigits(ref str, (num < 2) ? 1 : 2, out newValue5))
						{
							result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
							return false;
						}
						if (!DateTimeParse.CheckNewValue(ref result.Hour, newValue5, @char, ref result))
						{
							return false;
						}
						return true;
					default:
						goto IL_9DB;
					}
				}
				else
				{
					if (!format.GetNext())
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
						return false;
					}
					if (!str.Match(format.GetChar()))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					return true;
				}
			}
			else if (@char <= 's')
			{
				if (@char != 'm')
				{
					if (@char != 's')
					{
						goto IL_9DB;
					}
					num = format.GetRepeatCount();
					if (!DateTimeParse.ParseDigits(ref str, (num < 2) ? 1 : 2, out newValue7))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					if (!DateTimeParse.CheckNewValue(ref result.Second, newValue7, @char, ref result))
					{
						return false;
					}
					return true;
				}
				else
				{
					num = format.GetRepeatCount();
					if (!DateTimeParse.ParseDigits(ref str, (num < 2) ? 1 : 2, out newValue6))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					if (!DateTimeParse.CheckNewValue(ref result.Minute, newValue6, @char, ref result))
					{
						return false;
					}
					return true;
				}
			}
			else if (@char != 't')
			{
				if (@char != 'y')
				{
					if (@char != 'z')
					{
						goto IL_9DB;
					}
					num = format.GetRepeatCount();
					TimeSpan timeSpan2 = new TimeSpan(0L);
					if (!DateTimeParse.ParseTimeZoneOffset(ref str, num, ref timeSpan2))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					if ((result.flags & ParseFlags.TimeZoneUsed) != (ParseFlags)0 && timeSpan2 != result.timeZoneOffset)
					{
						result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'z');
						return false;
					}
					result.timeZoneOffset = timeSpan2;
					result.flags |= ParseFlags.TimeZoneUsed;
					return true;
				}
				else
				{
					num = format.GetRepeatCount();
					bool flag;
					if (DateTimeParse.ParseJapaneseEraStart(ref str, dtfi))
					{
						newValue = 1;
						flag = true;
					}
					else if (dtfi.HasForceTwoDigitYears)
					{
						flag = DateTimeParse.ParseDigits(ref str, 1, 4, out newValue);
					}
					else
					{
						if (num <= 2)
						{
							parseInfo.fUseTwoDigitYear = true;
						}
						flag = DateTimeParse.ParseDigits(ref str, num, out newValue);
					}
					if (!flag && parseInfo.fCustomNumberParser)
					{
						flag = parseInfo.parseNumberDelegate(ref str, num, out newValue);
					}
					if (!flag)
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
					if (!DateTimeParse.CheckNewValue(ref result.Year, newValue, @char, ref result))
					{
						return false;
					}
					return true;
				}
			}
			else
			{
				num = format.GetRepeatCount();
				if (num == 1)
				{
					if (!DateTimeParse.MatchAbbreviatedTimeMark(ref str, dtfi, ref tm))
					{
						result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
						return false;
					}
				}
				else if (!DateTimeParse.MatchTimeMark(ref str, dtfi, ref tm))
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				if (parseInfo.timeMark == DateTimeParse.TM.NotSet)
				{
					parseInfo.timeMark = tm;
					return true;
				}
				if (parseInfo.timeMark != tm)
				{
					result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", @char);
					return false;
				}
				return true;
			}
			num = format.GetRepeatCount();
			if (num > 7)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (!DateTimeParse.ParseFractionExact(ref str, num, ref num2) && @char == 'f')
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (result.fraction < 0.0)
			{
				result.fraction = num2;
				return true;
			}
			if (num2 != result.fraction)
			{
				result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", @char);
				return false;
			}
			return true;
			IL_9DB:
			if (@char == ' ')
			{
				if (!parseInfo.fAllowInnerWhite && !str.Match(@char))
				{
					if (parseInfo.fAllowTrailingWhite && format.GetNext() && DateTimeParse.ParseByFormat(ref str, ref format, ref parseInfo, dtfi, ref result))
					{
						return true;
					}
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
			}
			else if (format.MatchSpecifiedWord("GMT"))
			{
				format.Index += "GMT".Length - 1;
				result.flags |= ParseFlags.TimeZoneUsed;
				result.timeZoneOffset = TimeSpan.Zero;
				if (!str.Match("GMT"))
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
			}
			else if (!str.Match(@char))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			return true;
		}

		// Token: 0x06001645 RID: 5701 RVA: 0x00045D6C File Offset: 0x00043F6C
		internal static bool TryParseQuoteString(string format, int pos, StringBuilder result, out int returnValue)
		{
			returnValue = 0;
			int length = format.Length;
			int num = pos;
			char c = format[pos++];
			bool flag = false;
			while (pos < length)
			{
				char c2 = format[pos++];
				if (c2 == c)
				{
					flag = true;
					break;
				}
				if (c2 == '\\')
				{
					if (pos >= length)
					{
						return false;
					}
					result.Append(format[pos++]);
				}
				else
				{
					result.Append(c2);
				}
			}
			if (!flag)
			{
				return false;
			}
			returnValue = pos - num;
			return true;
		}

		// Token: 0x06001646 RID: 5702 RVA: 0x00045DE8 File Offset: 0x00043FE8
		private static bool DoStrictParse(string s, string formatParam, DateTimeStyles styles, DateTimeFormatInfo dtfi, ref DateTimeResult result)
		{
			ParsingInfo parsingInfo = default(ParsingInfo);
			parsingInfo.Init();
			parsingInfo.calendar = dtfi.Calendar;
			parsingInfo.fAllowInnerWhite = (styles & DateTimeStyles.AllowInnerWhite) > DateTimeStyles.None;
			parsingInfo.fAllowTrailingWhite = (styles & DateTimeStyles.AllowTrailingWhite) > DateTimeStyles.None;
			if (formatParam.Length == 1)
			{
				if ((result.flags & ParseFlags.CaptureOffset) != (ParseFlags)0 && formatParam[0] == 'U')
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
					return false;
				}
				formatParam = DateTimeParse.ExpandPredefinedFormat(formatParam, ref dtfi, ref parsingInfo, ref result);
			}
			result.calendar = parsingInfo.calendar;
			if (parsingInfo.calendar.ID == 8)
			{
				parsingInfo.parseNumberDelegate = DateTimeParse.m_hebrewNumberParser;
				parsingInfo.fCustomNumberParser = true;
			}
			result.Hour = (result.Minute = (result.Second = -1));
			__DTString _DTString = new __DTString(formatParam, dtfi, false);
			__DTString _DTString2 = new __DTString(s, dtfi, false);
			if (parsingInfo.fAllowTrailingWhite)
			{
				_DTString.TrimTail();
				_DTString.RemoveTrailingInQuoteSpaces();
				_DTString2.TrimTail();
			}
			if ((styles & DateTimeStyles.AllowLeadingWhite) != DateTimeStyles.None)
			{
				_DTString.SkipWhiteSpaces();
				_DTString.RemoveLeadingInQuoteSpaces();
				_DTString2.SkipWhiteSpaces();
			}
			while (_DTString.GetNext())
			{
				if (parsingInfo.fAllowInnerWhite)
				{
					_DTString2.SkipWhiteSpaces();
				}
				if (!DateTimeParse.ParseByFormat(ref _DTString2, ref _DTString, ref parsingInfo, dtfi, ref result))
				{
					return false;
				}
			}
			if (_DTString2.Index < _DTString2.Value.Length - 1)
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			if (parsingInfo.fUseTwoDigitYear && (dtfi.FormatFlags & DateTimeFormatFlags.UseHebrewRule) == DateTimeFormatFlags.None)
			{
				if (result.Year >= 100)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				try
				{
					result.Year = parsingInfo.calendar.ToFourDigitYear(result.Year);
				}
				catch (ArgumentOutOfRangeException failureMessageFormatArgument)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", failureMessageFormatArgument);
					return false;
				}
			}
			if (parsingInfo.fUseHour12)
			{
				if (parsingInfo.timeMark == DateTimeParse.TM.NotSet)
				{
					parsingInfo.timeMark = DateTimeParse.TM.AM;
				}
				if (result.Hour > 12)
				{
					result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
					return false;
				}
				if (parsingInfo.timeMark == DateTimeParse.TM.AM)
				{
					if (result.Hour == 12)
					{
						result.Hour = 0;
					}
				}
				else
				{
					result.Hour = ((result.Hour == 12) ? 12 : (result.Hour + 12));
				}
			}
			else if ((parsingInfo.timeMark == DateTimeParse.TM.AM && result.Hour >= 12) || (parsingInfo.timeMark == DateTimeParse.TM.PM && result.Hour < 12))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
				return false;
			}
			bool flag = result.Year == -1 && result.Month == -1 && result.Day == -1;
			if (!DateTimeParse.CheckDefaultDateTime(ref result, ref parsingInfo.calendar, styles))
			{
				return false;
			}
			if (!flag && dtfi.HasYearMonthAdjustment && !dtfi.YearMonthAdjustment(ref result.Year, ref result.Month, (result.flags & ParseFlags.ParsedMonthName) > (ParseFlags)0))
			{
				result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
				return false;
			}
			if (!parsingInfo.calendar.TryToDateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, 0, result.era, out result.parsedDate))
			{
				result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
				return false;
			}
			if (result.fraction > 0.0)
			{
				result.parsedDate = result.parsedDate.AddTicks((long)Math.Round(result.fraction * 10000000.0));
			}
			if (parsingInfo.dayOfWeek != -1 && parsingInfo.dayOfWeek != (int)parsingInfo.calendar.GetDayOfWeek(result.parsedDate))
			{
				result.SetFailure(ParseFailureKind.Format, "Format_BadDayOfWeek", null);
				return false;
			}
			return DateTimeParse.DetermineTimeZoneAdjustments(ref result, styles, flag);
		}

		// Token: 0x06001647 RID: 5703 RVA: 0x000461C8 File Offset: 0x000443C8
		private static Exception GetDateTimeParseException(ref DateTimeResult result)
		{
			switch (result.failure)
			{
			case ParseFailureKind.ArgumentNull:
				return new ArgumentNullException(result.failureArgumentName, Environment.GetResourceString(result.failureMessageID));
			case ParseFailureKind.Format:
				return new FormatException(Environment.GetResourceString(result.failureMessageID));
			case ParseFailureKind.FormatWithParameter:
				return new FormatException(Environment.GetResourceString(result.failureMessageID, new object[] { result.failureMessageFormatArgument }));
			case ParseFailureKind.FormatBadDateTimeCalendar:
				return new FormatException(Environment.GetResourceString(result.failureMessageID, new object[] { result.calendar }));
			default:
				return null;
			}
		}

		// Token: 0x06001648 RID: 5704 RVA: 0x0004625F File Offset: 0x0004445F
		[Conditional("_LOGGING")]
		internal static void LexTraceExit(string message, DateTimeParse.DS dps)
		{
		}

		// Token: 0x06001649 RID: 5705 RVA: 0x00046261 File Offset: 0x00044461
		[Conditional("_LOGGING")]
		internal static void PTSTraceExit(DateTimeParse.DS dps, bool passed)
		{
		}

		// Token: 0x0600164A RID: 5706 RVA: 0x00046263 File Offset: 0x00044463
		[Conditional("_LOGGING")]
		internal static void TPTraceExit(string message, DateTimeParse.DS dps)
		{
		}

		// Token: 0x0600164B RID: 5707 RVA: 0x00046265 File Offset: 0x00044465
		[Conditional("_LOGGING")]
		internal static void DTFITrace(DateTimeFormatInfo dtfi)
		{
		}

		// Token: 0x04000757 RID: 1879
		internal const int MaxDateTimeNumberDigits = 8;

		// Token: 0x04000758 RID: 1880
		internal static DateTimeParse.MatchNumberDelegate m_hebrewNumberParser = new DateTimeParse.MatchNumberDelegate(DateTimeParse.MatchHebrewDigits);

		// Token: 0x04000759 RID: 1881
		internal static bool enableAmPmParseAdjustment = DateTimeParse.GetAmPmParseFlag();

		// Token: 0x0400075A RID: 1882
		private static DateTimeParse.DS[][] dateParsingStates = new DateTimeParse.DS[][]
		{
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.BEGIN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.TX_N,
				DateTimeParse.DS.N,
				DateTimeParse.DS.D_Nd,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_M,
				DateTimeParse.DS.D_M,
				DateTimeParse.DS.D_S,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.BEGIN,
				DateTimeParse.DS.D_Y,
				DateTimeParse.DS.D_Y,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.BEGIN,
				DateTimeParse.DS.BEGIN,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_NN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.NN,
				DateTimeParse.DS.D_NNd,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_NM,
				DateTimeParse.DS.D_NM,
				DateTimeParse.DS.D_MNd,
				DateTimeParse.DS.D_NDS,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.N,
				DateTimeParse.DS.D_YN,
				DateTimeParse.DS.D_YNd,
				DateTimeParse.DS.DX_YN,
				DateTimeParse.DS.N,
				DateTimeParse.DS.N,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.DX_NN,
				DateTimeParse.DS.DX_NNN,
				DateTimeParse.DS.TX_N,
				DateTimeParse.DS.DX_NNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.NN,
				DateTimeParse.DS.DX_NNY,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_NNY,
				DateTimeParse.DS.NN,
				DateTimeParse.DS.NN,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_NN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_NN,
				DateTimeParse.DS.D_NNd,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_NM,
				DateTimeParse.DS.D_MN,
				DateTimeParse.DS.D_MNd,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_Nd,
				DateTimeParse.DS.D_YN,
				DateTimeParse.DS.D_YNd,
				DateTimeParse.DS.DX_YN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_Nd,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.DX_NN,
				DateTimeParse.DS.DX_NNN,
				DateTimeParse.DS.TX_N,
				DateTimeParse.DS.DX_NNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_DS,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.D_NN,
				DateTimeParse.DS.DX_NNY,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_NNY,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_NN,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_NNN,
				DateTimeParse.DS.DX_NNN,
				DateTimeParse.DS.DX_NNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_DS,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_NNd,
				DateTimeParse.DS.DX_NNY,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_NNY,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_NNd,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_MN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_MN,
				DateTimeParse.DS.D_MNd,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_M,
				DateTimeParse.DS.D_YM,
				DateTimeParse.DS.D_YMd,
				DateTimeParse.DS.DX_YM,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_M,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.DX_MN,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_DS,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.D_MN,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_MN,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.DX_NM,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_DS,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.D_NM,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_NM,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_MNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_MNd,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_MNd,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.DX_NDS,
				DateTimeParse.DS.DX_NNDS,
				DateTimeParse.DS.DX_NNDS,
				DateTimeParse.DS.DX_NNDS,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_NDS,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.D_NDS,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_NDS,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YN,
				DateTimeParse.DS.D_YNd,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YM,
				DateTimeParse.DS.D_YM,
				DateTimeParse.DS.D_YMd,
				DateTimeParse.DS.D_YM,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_Y,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_Y,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.DX_YN,
				DateTimeParse.DS.DX_YNN,
				DateTimeParse.DS.DX_YNN,
				DateTimeParse.DS.DX_YNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YN,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YNN,
				DateTimeParse.DS.DX_YNN,
				DateTimeParse.DS.DX_YNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YN,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.DX_YM,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YM,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YM,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.DX_YMN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YM,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_YM,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.DX_DS,
				DateTimeParse.DS.DX_DSN,
				DateTimeParse.DS.TX_N,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_S,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.D_S,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_S,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.TX_TS,
				DateTimeParse.DS.TX_TS,
				DateTimeParse.DS.TX_TS,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.D_Nd,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.D_S,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.ERROR
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.TX_NN,
				DateTimeParse.DS.TX_NN,
				DateTimeParse.DS.TX_NN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_NNt,
				DateTimeParse.DS.DX_NM,
				DateTimeParse.DS.D_NM,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.T_Nt,
				DateTimeParse.DS.TX_NN
			},
			new DateTimeParse.DS[]
			{
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.TX_NNN,
				DateTimeParse.DS.TX_NNN,
				DateTimeParse.DS.TX_NNN,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_S,
				DateTimeParse.DS.T_NNt,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.ERROR,
				DateTimeParse.DS.T_NNt,
				DateTimeParse.DS.T_NNt,
				DateTimeParse.DS.TX_NNN
			}
		};

		// Token: 0x0400075B RID: 1883
		internal const string GMTName = "GMT";

		// Token: 0x0400075C RID: 1884
		internal const string ZuluName = "Z";

		// Token: 0x0400075D RID: 1885
		private const int ORDER_YMD = 0;

		// Token: 0x0400075E RID: 1886
		private const int ORDER_MDY = 1;

		// Token: 0x0400075F RID: 1887
		private const int ORDER_DMY = 2;

		// Token: 0x04000760 RID: 1888
		private const int ORDER_YDM = 3;

		// Token: 0x04000761 RID: 1889
		private const int ORDER_YM = 4;

		// Token: 0x04000762 RID: 1890
		private const int ORDER_MY = 5;

		// Token: 0x04000763 RID: 1891
		private const int ORDER_MD = 6;

		// Token: 0x04000764 RID: 1892
		private const int ORDER_DM = 7;

		// Token: 0x02000B06 RID: 2822
		// (Invoke) Token: 0x06006A89 RID: 27273
		internal delegate bool MatchNumberDelegate(ref __DTString str, int digitLen, out int result);

		// Token: 0x02000B07 RID: 2823
		internal enum DTT
		{
			// Token: 0x04003238 RID: 12856
			End,
			// Token: 0x04003239 RID: 12857
			NumEnd,
			// Token: 0x0400323A RID: 12858
			NumAmpm,
			// Token: 0x0400323B RID: 12859
			NumSpace,
			// Token: 0x0400323C RID: 12860
			NumDatesep,
			// Token: 0x0400323D RID: 12861
			NumTimesep,
			// Token: 0x0400323E RID: 12862
			MonthEnd,
			// Token: 0x0400323F RID: 12863
			MonthSpace,
			// Token: 0x04003240 RID: 12864
			MonthDatesep,
			// Token: 0x04003241 RID: 12865
			NumDatesuff,
			// Token: 0x04003242 RID: 12866
			NumTimesuff,
			// Token: 0x04003243 RID: 12867
			DayOfWeek,
			// Token: 0x04003244 RID: 12868
			YearSpace,
			// Token: 0x04003245 RID: 12869
			YearDateSep,
			// Token: 0x04003246 RID: 12870
			YearEnd,
			// Token: 0x04003247 RID: 12871
			TimeZone,
			// Token: 0x04003248 RID: 12872
			Era,
			// Token: 0x04003249 RID: 12873
			NumUTCTimeMark,
			// Token: 0x0400324A RID: 12874
			Unk,
			// Token: 0x0400324B RID: 12875
			NumLocalTimeMark,
			// Token: 0x0400324C RID: 12876
			Max
		}

		// Token: 0x02000B08 RID: 2824
		internal enum TM
		{
			// Token: 0x0400324E RID: 12878
			NotSet = -1,
			// Token: 0x0400324F RID: 12879
			AM,
			// Token: 0x04003250 RID: 12880
			PM
		}

		// Token: 0x02000B09 RID: 2825
		internal enum DS
		{
			// Token: 0x04003252 RID: 12882
			BEGIN,
			// Token: 0x04003253 RID: 12883
			N,
			// Token: 0x04003254 RID: 12884
			NN,
			// Token: 0x04003255 RID: 12885
			D_Nd,
			// Token: 0x04003256 RID: 12886
			D_NN,
			// Token: 0x04003257 RID: 12887
			D_NNd,
			// Token: 0x04003258 RID: 12888
			D_M,
			// Token: 0x04003259 RID: 12889
			D_MN,
			// Token: 0x0400325A RID: 12890
			D_NM,
			// Token: 0x0400325B RID: 12891
			D_MNd,
			// Token: 0x0400325C RID: 12892
			D_NDS,
			// Token: 0x0400325D RID: 12893
			D_Y,
			// Token: 0x0400325E RID: 12894
			D_YN,
			// Token: 0x0400325F RID: 12895
			D_YNd,
			// Token: 0x04003260 RID: 12896
			D_YM,
			// Token: 0x04003261 RID: 12897
			D_YMd,
			// Token: 0x04003262 RID: 12898
			D_S,
			// Token: 0x04003263 RID: 12899
			T_S,
			// Token: 0x04003264 RID: 12900
			T_Nt,
			// Token: 0x04003265 RID: 12901
			T_NNt,
			// Token: 0x04003266 RID: 12902
			ERROR,
			// Token: 0x04003267 RID: 12903
			DX_NN,
			// Token: 0x04003268 RID: 12904
			DX_NNN,
			// Token: 0x04003269 RID: 12905
			DX_MN,
			// Token: 0x0400326A RID: 12906
			DX_NM,
			// Token: 0x0400326B RID: 12907
			DX_MNN,
			// Token: 0x0400326C RID: 12908
			DX_DS,
			// Token: 0x0400326D RID: 12909
			DX_DSN,
			// Token: 0x0400326E RID: 12910
			DX_NDS,
			// Token: 0x0400326F RID: 12911
			DX_NNDS,
			// Token: 0x04003270 RID: 12912
			DX_YNN,
			// Token: 0x04003271 RID: 12913
			DX_YMN,
			// Token: 0x04003272 RID: 12914
			DX_YN,
			// Token: 0x04003273 RID: 12915
			DX_YM,
			// Token: 0x04003274 RID: 12916
			TX_N,
			// Token: 0x04003275 RID: 12917
			TX_NN,
			// Token: 0x04003276 RID: 12918
			TX_NNN,
			// Token: 0x04003277 RID: 12919
			TX_TS,
			// Token: 0x04003278 RID: 12920
			DX_NNY
		}
	}
}
