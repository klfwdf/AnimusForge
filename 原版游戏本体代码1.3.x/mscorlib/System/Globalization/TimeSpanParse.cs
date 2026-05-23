using System;
using System.Text;

namespace System.Globalization
{
	// Token: 0x020003D8 RID: 984
	internal static class TimeSpanParse
	{
		// Token: 0x060031F4 RID: 12788 RVA: 0x000BF5D5 File Offset: 0x000BD7D5
		internal static void ValidateStyles(TimeSpanStyles style, string parameterName)
		{
			if (style != TimeSpanStyles.None && style != TimeSpanStyles.AssumeNegative)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTimeSpanStyles"), parameterName);
			}
		}

		// Token: 0x060031F5 RID: 12789 RVA: 0x000BF5F0 File Offset: 0x000BD7F0
		private static bool TryTimeToTicks(bool positive, TimeSpanParse.TimeSpanToken days, TimeSpanParse.TimeSpanToken hours, TimeSpanParse.TimeSpanToken minutes, TimeSpanParse.TimeSpanToken seconds, TimeSpanParse.TimeSpanToken fraction, out long result)
		{
			if (days.IsInvalidNumber(10675199, -1) || hours.IsInvalidNumber(23, -1) || minutes.IsInvalidNumber(59, -1) || seconds.IsInvalidNumber(59, -1) || fraction.IsInvalidNumber(9999999, 7))
			{
				result = 0L;
				return false;
			}
			long num = ((long)days.num * 3600L * 24L + (long)hours.num * 3600L + (long)minutes.num * 60L + (long)seconds.num) * 1000L;
			if (num > 922337203685477L || num < -922337203685477L)
			{
				result = 0L;
				return false;
			}
			long num2 = (long)fraction.num;
			if (num2 != 0L)
			{
				long num3 = 1000000L;
				if (fraction.zeroes > 0)
				{
					long num4 = (long)Math.Pow(10.0, (double)fraction.zeroes);
					num3 /= num4;
				}
				while (num2 < num3)
				{
					num2 *= 10L;
				}
			}
			result = num * 10000L + num2;
			if (positive && result < 0L)
			{
				result = 0L;
				return false;
			}
			return true;
		}

		// Token: 0x060031F6 RID: 12790 RVA: 0x000BF708 File Offset: 0x000BD908
		internal static TimeSpan Parse(string input, IFormatProvider formatProvider)
		{
			TimeSpanParse.TimeSpanResult timeSpanResult = default(TimeSpanParse.TimeSpanResult);
			timeSpanResult.Init(TimeSpanParse.TimeSpanThrowStyle.All);
			if (TimeSpanParse.TryParseTimeSpan(input, TimeSpanParse.TimeSpanStandardStyles.Any, formatProvider, ref timeSpanResult))
			{
				return timeSpanResult.parsedTimeSpan;
			}
			throw timeSpanResult.GetTimeSpanParseException();
		}

		// Token: 0x060031F7 RID: 12791 RVA: 0x000BF740 File Offset: 0x000BD940
		internal static bool TryParse(string input, IFormatProvider formatProvider, out TimeSpan result)
		{
			TimeSpanParse.TimeSpanResult timeSpanResult = default(TimeSpanParse.TimeSpanResult);
			timeSpanResult.Init(TimeSpanParse.TimeSpanThrowStyle.None);
			if (TimeSpanParse.TryParseTimeSpan(input, TimeSpanParse.TimeSpanStandardStyles.Any, formatProvider, ref timeSpanResult))
			{
				result = timeSpanResult.parsedTimeSpan;
				return true;
			}
			result = default(TimeSpan);
			return false;
		}

		// Token: 0x060031F8 RID: 12792 RVA: 0x000BF780 File Offset: 0x000BD980
		internal static TimeSpan ParseExact(string input, string format, IFormatProvider formatProvider, TimeSpanStyles styles)
		{
			TimeSpanParse.TimeSpanResult timeSpanResult = default(TimeSpanParse.TimeSpanResult);
			timeSpanResult.Init(TimeSpanParse.TimeSpanThrowStyle.All);
			if (TimeSpanParse.TryParseExactTimeSpan(input, format, formatProvider, styles, ref timeSpanResult))
			{
				return timeSpanResult.parsedTimeSpan;
			}
			throw timeSpanResult.GetTimeSpanParseException();
		}

		// Token: 0x060031F9 RID: 12793 RVA: 0x000BF7B8 File Offset: 0x000BD9B8
		internal static bool TryParseExact(string input, string format, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
		{
			TimeSpanParse.TimeSpanResult timeSpanResult = default(TimeSpanParse.TimeSpanResult);
			timeSpanResult.Init(TimeSpanParse.TimeSpanThrowStyle.None);
			if (TimeSpanParse.TryParseExactTimeSpan(input, format, formatProvider, styles, ref timeSpanResult))
			{
				result = timeSpanResult.parsedTimeSpan;
				return true;
			}
			result = default(TimeSpan);
			return false;
		}

		// Token: 0x060031FA RID: 12794 RVA: 0x000BF7FC File Offset: 0x000BD9FC
		internal static TimeSpan ParseExactMultiple(string input, string[] formats, IFormatProvider formatProvider, TimeSpanStyles styles)
		{
			TimeSpanParse.TimeSpanResult timeSpanResult = default(TimeSpanParse.TimeSpanResult);
			timeSpanResult.Init(TimeSpanParse.TimeSpanThrowStyle.All);
			if (TimeSpanParse.TryParseExactMultipleTimeSpan(input, formats, formatProvider, styles, ref timeSpanResult))
			{
				return timeSpanResult.parsedTimeSpan;
			}
			throw timeSpanResult.GetTimeSpanParseException();
		}

		// Token: 0x060031FB RID: 12795 RVA: 0x000BF834 File Offset: 0x000BDA34
		internal static bool TryParseExactMultiple(string input, string[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
		{
			TimeSpanParse.TimeSpanResult timeSpanResult = default(TimeSpanParse.TimeSpanResult);
			timeSpanResult.Init(TimeSpanParse.TimeSpanThrowStyle.None);
			if (TimeSpanParse.TryParseExactMultipleTimeSpan(input, formats, formatProvider, styles, ref timeSpanResult))
			{
				result = timeSpanResult.parsedTimeSpan;
				return true;
			}
			result = default(TimeSpan);
			return false;
		}

		// Token: 0x060031FC RID: 12796 RVA: 0x000BF878 File Offset: 0x000BDA78
		private static bool TryParseTimeSpan(string input, TimeSpanParse.TimeSpanStandardStyles style, IFormatProvider formatProvider, ref TimeSpanParse.TimeSpanResult result)
		{
			if (input == null)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "input");
				return false;
			}
			input = input.Trim();
			if (input == string.Empty)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			TimeSpanParse.TimeSpanTokenizer timeSpanTokenizer = default(TimeSpanParse.TimeSpanTokenizer);
			timeSpanTokenizer.Init(input);
			TimeSpanParse.TimeSpanRawInfo timeSpanRawInfo = default(TimeSpanParse.TimeSpanRawInfo);
			timeSpanRawInfo.Init(DateTimeFormatInfo.GetInstance(formatProvider));
			TimeSpanParse.TimeSpanToken nextToken = timeSpanTokenizer.GetNextToken();
			while (nextToken.ttt != TimeSpanParse.TTT.End)
			{
				if (!timeSpanRawInfo.ProcessToken(ref nextToken, ref result))
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
					return false;
				}
				nextToken = timeSpanTokenizer.GetNextToken();
			}
			if (!timeSpanTokenizer.EOL)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			if (!TimeSpanParse.ProcessTerminalState(ref timeSpanRawInfo, style, ref result))
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			return true;
		}

		// Token: 0x060031FD RID: 12797 RVA: 0x000BF94C File Offset: 0x000BDB4C
		private static bool ProcessTerminalState(ref TimeSpanParse.TimeSpanRawInfo raw, TimeSpanParse.TimeSpanStandardStyles style, ref TimeSpanParse.TimeSpanResult result)
		{
			if (raw.lastSeenTTT == TimeSpanParse.TTT.Num)
			{
				TimeSpanParse.TimeSpanToken timeSpanToken = default(TimeSpanParse.TimeSpanToken);
				timeSpanToken.ttt = TimeSpanParse.TTT.Sep;
				timeSpanToken.sep = string.Empty;
				if (!raw.ProcessToken(ref timeSpanToken, ref result))
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
					return false;
				}
			}
			switch (raw.NumCount)
			{
			case 1:
				return TimeSpanParse.ProcessTerminal_D(ref raw, style, ref result);
			case 2:
				return TimeSpanParse.ProcessTerminal_HM(ref raw, style, ref result);
			case 3:
				return TimeSpanParse.ProcessTerminal_HM_S_D(ref raw, style, ref result);
			case 4:
				return TimeSpanParse.ProcessTerminal_HMS_F_D(ref raw, style, ref result);
			case 5:
				return TimeSpanParse.ProcessTerminal_DHMSF(ref raw, style, ref result);
			default:
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
		}

		// Token: 0x060031FE RID: 12798 RVA: 0x000BF9F8 File Offset: 0x000BDBF8
		private static bool ProcessTerminal_DHMSF(ref TimeSpanParse.TimeSpanRawInfo raw, TimeSpanParse.TimeSpanStandardStyles style, ref TimeSpanParse.TimeSpanResult result)
		{
			if (raw.SepCount != 6 || raw.NumCount != 5)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			bool flag = (style & TimeSpanParse.TimeSpanStandardStyles.Invariant) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag2 = (style & TimeSpanParse.TimeSpanStandardStyles.Localized) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag3 = false;
			bool flag4 = false;
			if (flag)
			{
				if (raw.FullMatch(raw.PositiveInvariant))
				{
					flag4 = true;
					flag3 = true;
				}
				if (!flag4 && raw.FullMatch(raw.NegativeInvariant))
				{
					flag4 = true;
					flag3 = false;
				}
			}
			if (flag2)
			{
				if (!flag4 && raw.FullMatch(raw.PositiveLocalized))
				{
					flag4 = true;
					flag3 = true;
				}
				if (!flag4 && raw.FullMatch(raw.NegativeLocalized))
				{
					flag4 = true;
					flag3 = false;
				}
			}
			if (!flag4)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			long num;
			if (!TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], raw.numbers[4], out num))
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
				return false;
			}
			if (!flag3)
			{
				num = -num;
				if (num > 0L)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
					return false;
				}
			}
			result.parsedTimeSpan._ticks = num;
			return true;
		}

		// Token: 0x060031FF RID: 12799 RVA: 0x000BFB20 File Offset: 0x000BDD20
		private static bool ProcessTerminal_HMS_F_D(ref TimeSpanParse.TimeSpanRawInfo raw, TimeSpanParse.TimeSpanStandardStyles style, ref TimeSpanParse.TimeSpanResult result)
		{
			if (raw.SepCount != 5 || raw.NumCount != 4 || (style & TimeSpanParse.TimeSpanStandardStyles.RequireFull) != TimeSpanParse.TimeSpanStandardStyles.None)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			bool flag = (style & TimeSpanParse.TimeSpanStandardStyles.Invariant) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag2 = (style & TimeSpanParse.TimeSpanStandardStyles.Localized) > TimeSpanParse.TimeSpanStandardStyles.None;
			long num = 0L;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			if (flag)
			{
				if (raw.FullHMSFMatch(raw.PositiveInvariant))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullDHMSMatch(raw.PositiveInvariant))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullAppCompatMatch(raw.PositiveInvariant))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, raw.numbers[3], out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullHMSFMatch(raw.NegativeInvariant))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullDHMSMatch(raw.NegativeInvariant))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullAppCompatMatch(raw.NegativeInvariant))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, raw.numbers[3], out num);
					flag5 = flag5 || !flag4;
				}
			}
			if (flag2)
			{
				if (!flag4 && raw.FullHMSFMatch(raw.PositiveLocalized))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullDHMSMatch(raw.PositiveLocalized))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullAppCompatMatch(raw.PositiveLocalized))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, raw.numbers[3], out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullHMSFMatch(raw.NegativeLocalized))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullDHMSMatch(raw.NegativeLocalized))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullAppCompatMatch(raw.NegativeLocalized))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, raw.numbers[3], out num);
					flag5 = flag5 || !flag4;
				}
			}
			if (flag4)
			{
				if (!flag3)
				{
					num = -num;
					if (num > 0L)
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
						return false;
					}
				}
				result.parsedTimeSpan._ticks = num;
				return true;
			}
			if (flag5)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
				return false;
			}
			result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
			return false;
		}

		// Token: 0x06003200 RID: 12800 RVA: 0x000C004C File Offset: 0x000BE24C
		private static bool ProcessTerminal_HM_S_D(ref TimeSpanParse.TimeSpanRawInfo raw, TimeSpanParse.TimeSpanStandardStyles style, ref TimeSpanParse.TimeSpanResult result)
		{
			if (raw.SepCount != 4 || raw.NumCount != 3 || (style & TimeSpanParse.TimeSpanStandardStyles.RequireFull) != TimeSpanParse.TimeSpanStandardStyles.None)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			bool flag = (style & TimeSpanParse.TimeSpanStandardStyles.Invariant) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag2 = (style & TimeSpanParse.TimeSpanStandardStyles.Localized) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			long num = 0L;
			if (flag)
			{
				if (raw.FullHMSMatch(raw.PositiveInvariant))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullDHMMatch(raw.PositiveInvariant))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.PartialAppCompatMatch(raw.PositiveInvariant))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], TimeSpanParse.zero, raw.numbers[2], out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullHMSMatch(raw.NegativeInvariant))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullDHMMatch(raw.NegativeInvariant))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.PartialAppCompatMatch(raw.NegativeInvariant))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], TimeSpanParse.zero, raw.numbers[2], out num);
					flag5 = flag5 || !flag4;
				}
			}
			if (flag2)
			{
				if (!flag4 && raw.FullHMSMatch(raw.PositiveLocalized))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullDHMMatch(raw.PositiveLocalized))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.PartialAppCompatMatch(raw.PositiveLocalized))
				{
					flag3 = true;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], TimeSpanParse.zero, raw.numbers[2], out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullHMSMatch(raw.NegativeLocalized))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.FullDHMMatch(raw.NegativeLocalized))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], raw.numbers[1], raw.numbers[2], TimeSpanParse.zero, TimeSpanParse.zero, out num);
					flag5 = flag5 || !flag4;
				}
				if (!flag4 && raw.PartialAppCompatMatch(raw.NegativeLocalized))
				{
					flag3 = false;
					flag4 = TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], TimeSpanParse.zero, raw.numbers[2], out num);
					flag5 = flag5 || !flag4;
				}
			}
			if (flag4)
			{
				if (!flag3)
				{
					num = -num;
					if (num > 0L)
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
						return false;
					}
				}
				result.parsedTimeSpan._ticks = num;
				return true;
			}
			if (flag5)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
				return false;
			}
			result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
			return false;
		}

		// Token: 0x06003201 RID: 12801 RVA: 0x000C0504 File Offset: 0x000BE704
		private static bool ProcessTerminal_HM(ref TimeSpanParse.TimeSpanRawInfo raw, TimeSpanParse.TimeSpanStandardStyles style, ref TimeSpanParse.TimeSpanResult result)
		{
			if (raw.SepCount != 3 || raw.NumCount != 2 || (style & TimeSpanParse.TimeSpanStandardStyles.RequireFull) != TimeSpanParse.TimeSpanStandardStyles.None)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			bool flag = (style & TimeSpanParse.TimeSpanStandardStyles.Invariant) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag2 = (style & TimeSpanParse.TimeSpanStandardStyles.Localized) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag3 = false;
			bool flag4 = false;
			if (flag)
			{
				if (raw.FullHMMatch(raw.PositiveInvariant))
				{
					flag4 = true;
					flag3 = true;
				}
				if (!flag4 && raw.FullHMMatch(raw.NegativeInvariant))
				{
					flag4 = true;
					flag3 = false;
				}
			}
			if (flag2)
			{
				if (!flag4 && raw.FullHMMatch(raw.PositiveLocalized))
				{
					flag4 = true;
					flag3 = true;
				}
				if (!flag4 && raw.FullHMMatch(raw.NegativeLocalized))
				{
					flag4 = true;
					flag3 = false;
				}
			}
			long num = 0L;
			if (!flag4)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			if (!TimeSpanParse.TryTimeToTicks(flag3, TimeSpanParse.zero, raw.numbers[0], raw.numbers[1], TimeSpanParse.zero, TimeSpanParse.zero, out num))
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
				return false;
			}
			if (!flag3)
			{
				num = -num;
				if (num > 0L)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
					return false;
				}
			}
			result.parsedTimeSpan._ticks = num;
			return true;
		}

		// Token: 0x06003202 RID: 12802 RVA: 0x000C0620 File Offset: 0x000BE820
		private static bool ProcessTerminal_D(ref TimeSpanParse.TimeSpanRawInfo raw, TimeSpanParse.TimeSpanStandardStyles style, ref TimeSpanParse.TimeSpanResult result)
		{
			if (raw.SepCount != 2 || raw.NumCount != 1 || (style & TimeSpanParse.TimeSpanStandardStyles.RequireFull) != TimeSpanParse.TimeSpanStandardStyles.None)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			bool flag = (style & TimeSpanParse.TimeSpanStandardStyles.Invariant) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag2 = (style & TimeSpanParse.TimeSpanStandardStyles.Localized) > TimeSpanParse.TimeSpanStandardStyles.None;
			bool flag3 = false;
			bool flag4 = false;
			if (flag)
			{
				if (raw.FullDMatch(raw.PositiveInvariant))
				{
					flag4 = true;
					flag3 = true;
				}
				if (!flag4 && raw.FullDMatch(raw.NegativeInvariant))
				{
					flag4 = true;
					flag3 = false;
				}
			}
			if (flag2)
			{
				if (!flag4 && raw.FullDMatch(raw.PositiveLocalized))
				{
					flag4 = true;
					flag3 = true;
				}
				if (!flag4 && raw.FullDMatch(raw.NegativeLocalized))
				{
					flag4 = true;
					flag3 = false;
				}
			}
			long num = 0L;
			if (!flag4)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			if (!TimeSpanParse.TryTimeToTicks(flag3, raw.numbers[0], TimeSpanParse.zero, TimeSpanParse.zero, TimeSpanParse.zero, TimeSpanParse.zero, out num))
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
				return false;
			}
			if (!flag3)
			{
				num = -num;
				if (num > 0L)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
					return false;
				}
			}
			result.parsedTimeSpan._ticks = num;
			return true;
		}

		// Token: 0x06003203 RID: 12803 RVA: 0x000C0734 File Offset: 0x000BE934
		private static bool TryParseExactTimeSpan(string input, string format, IFormatProvider formatProvider, TimeSpanStyles styles, ref TimeSpanParse.TimeSpanResult result)
		{
			if (input == null)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "input");
				return false;
			}
			if (format == null)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "format");
				return false;
			}
			if (format.Length == 0)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadFormatSpecifier");
				return false;
			}
			if (format.Length != 1)
			{
				return TimeSpanParse.TryParseByFormat(input, format, styles, ref result);
			}
			if (format[0] == 'c' || format[0] == 't' || format[0] == 'T')
			{
				return TimeSpanParse.TryParseTimeSpanConstant(input, ref result);
			}
			TimeSpanParse.TimeSpanStandardStyles style;
			if (format[0] == 'g')
			{
				style = TimeSpanParse.TimeSpanStandardStyles.Localized;
			}
			else
			{
				if (format[0] != 'G')
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadFormatSpecifier");
					return false;
				}
				style = TimeSpanParse.TimeSpanStandardStyles.Localized | TimeSpanParse.TimeSpanStandardStyles.RequireFull;
			}
			return TimeSpanParse.TryParseTimeSpan(input, style, formatProvider, ref result);
		}

		// Token: 0x06003204 RID: 12804 RVA: 0x000C0800 File Offset: 0x000BEA00
		private static bool TryParseByFormat(string input, string format, TimeSpanStyles styles, ref TimeSpanParse.TimeSpanResult result)
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			int number = 0;
			int number2 = 0;
			int number3 = 0;
			int number4 = 0;
			int leadingZeroes = 0;
			int number5 = 0;
			int i = 0;
			int num = 0;
			TimeSpanParse.TimeSpanTokenizer timeSpanTokenizer = default(TimeSpanParse.TimeSpanTokenizer);
			timeSpanTokenizer.Init(input, -1);
			while (i < format.Length)
			{
				char c = format[i];
				if (c <= 'F')
				{
					if (c <= '%')
					{
						if (c != '"')
						{
							if (c != '%')
							{
								goto IL_2C5;
							}
							int num2 = DateTimeFormat.ParseNextChar(format, i);
							if (num2 >= 0 && num2 != 37)
							{
								num = 1;
								goto IL_2D3;
							}
							result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
							return false;
						}
					}
					else if (c != '\'')
					{
						if (c != 'F')
						{
							goto IL_2C5;
						}
						num = DateTimeFormat.ParseRepeatPattern(format, i, c);
						if (num > 7 || flag5)
						{
							result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
							return false;
						}
						TimeSpanParse.ParseExactDigits(ref timeSpanTokenizer, num, num, out leadingZeroes, out number5);
						flag5 = true;
						goto IL_2D3;
					}
					StringBuilder stringBuilder = new StringBuilder();
					if (!DateTimeParse.TryParseQuoteString(format, i, stringBuilder, out num))
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.FormatWithParameter, "Format_BadQuote", c);
						return false;
					}
					if (!TimeSpanParse.ParseExactLiteral(ref timeSpanTokenizer, stringBuilder))
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
						return false;
					}
				}
				else if (c <= 'h')
				{
					if (c != '\\')
					{
						switch (c)
						{
						case 'd':
						{
							num = DateTimeFormat.ParseRepeatPattern(format, i, c);
							int num3 = 0;
							if (num > 8 || flag || !TimeSpanParse.ParseExactDigits(ref timeSpanTokenizer, (num < 2) ? 1 : num, (num < 2) ? 8 : num, out num3, out number))
							{
								result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
								return false;
							}
							flag = true;
							break;
						}
						case 'e':
						case 'g':
							goto IL_2C5;
						case 'f':
							num = DateTimeFormat.ParseRepeatPattern(format, i, c);
							if (num > 7 || flag5 || !TimeSpanParse.ParseExactDigits(ref timeSpanTokenizer, num, num, out leadingZeroes, out number5))
							{
								result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
								return false;
							}
							flag5 = true;
							break;
						case 'h':
							num = DateTimeFormat.ParseRepeatPattern(format, i, c);
							if (num > 2 || flag2 || !TimeSpanParse.ParseExactDigits(ref timeSpanTokenizer, num, out number2))
							{
								result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
								return false;
							}
							flag2 = true;
							break;
						default:
							goto IL_2C5;
						}
					}
					else
					{
						int num2 = DateTimeFormat.ParseNextChar(format, i);
						if (num2 < 0 || timeSpanTokenizer.NextChar != (char)num2)
						{
							result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
							return false;
						}
						num = 2;
					}
				}
				else if (c != 'm')
				{
					if (c != 's')
					{
						goto IL_2C5;
					}
					num = DateTimeFormat.ParseRepeatPattern(format, i, c);
					if (num > 2 || flag4 || !TimeSpanParse.ParseExactDigits(ref timeSpanTokenizer, num, out number4))
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
						return false;
					}
					flag4 = true;
				}
				else
				{
					num = DateTimeFormat.ParseRepeatPattern(format, i, c);
					if (num > 2 || flag3 || !TimeSpanParse.ParseExactDigits(ref timeSpanTokenizer, num, out number3))
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
						return false;
					}
					flag3 = true;
				}
				IL_2D3:
				i += num;
				continue;
				IL_2C5:
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_InvalidString");
				return false;
			}
			if (!timeSpanTokenizer.EOL)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			long num4 = 0L;
			bool flag6 = (styles & TimeSpanStyles.AssumeNegative) == TimeSpanStyles.None;
			if (TimeSpanParse.TryTimeToTicks(flag6, new TimeSpanParse.TimeSpanToken(number), new TimeSpanParse.TimeSpanToken(number2), new TimeSpanParse.TimeSpanToken(number3), new TimeSpanParse.TimeSpanToken(number4), new TimeSpanParse.TimeSpanToken(leadingZeroes, number5), out num4))
			{
				if (!flag6)
				{
					num4 = -num4;
				}
				result.parsedTimeSpan._ticks = num4;
				return true;
			}
			result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
			return false;
		}

		// Token: 0x06003205 RID: 12805 RVA: 0x000C0B6C File Offset: 0x000BED6C
		private static bool ParseExactDigits(ref TimeSpanParse.TimeSpanTokenizer tokenizer, int minDigitLength, out int result)
		{
			result = 0;
			int num = 0;
			int maxDigitLength = ((minDigitLength == 1) ? 2 : minDigitLength);
			return TimeSpanParse.ParseExactDigits(ref tokenizer, minDigitLength, maxDigitLength, out num, out result);
		}

		// Token: 0x06003206 RID: 12806 RVA: 0x000C0B94 File Offset: 0x000BED94
		private static bool ParseExactDigits(ref TimeSpanParse.TimeSpanTokenizer tokenizer, int minDigitLength, int maxDigitLength, out int zeroes, out int result)
		{
			result = 0;
			zeroes = 0;
			int i;
			for (i = 0; i < maxDigitLength; i++)
			{
				char nextChar = tokenizer.NextChar;
				if (nextChar < '0' || nextChar > '9')
				{
					tokenizer.BackOne();
					break;
				}
				result = result * 10 + (int)(nextChar - '0');
				if (result == 0)
				{
					zeroes++;
				}
			}
			return i >= minDigitLength;
		}

		// Token: 0x06003207 RID: 12807 RVA: 0x000C0BF0 File Offset: 0x000BEDF0
		private static bool ParseExactLiteral(ref TimeSpanParse.TimeSpanTokenizer tokenizer, StringBuilder enquotedString)
		{
			for (int i = 0; i < enquotedString.Length; i++)
			{
				if (enquotedString[i] != tokenizer.NextChar)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06003208 RID: 12808 RVA: 0x000C0C20 File Offset: 0x000BEE20
		private static bool TryParseTimeSpanConstant(string input, ref TimeSpanParse.TimeSpanResult result)
		{
			return default(TimeSpanParse.StringParser).TryParse(input, ref result);
		}

		// Token: 0x06003209 RID: 12809 RVA: 0x000C0C40 File Offset: 0x000BEE40
		private static bool TryParseExactMultipleTimeSpan(string input, string[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, ref TimeSpanParse.TimeSpanResult result)
		{
			if (input == null)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "input");
				return false;
			}
			if (formats == null)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "formats");
				return false;
			}
			if (input.Length == 0)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
				return false;
			}
			if (formats.Length == 0)
			{
				result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadFormatSpecifier");
				return false;
			}
			for (int i = 0; i < formats.Length; i++)
			{
				if (formats[i] == null || formats[i].Length == 0)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadFormatSpecifier");
					return false;
				}
				TimeSpanParse.TimeSpanResult timeSpanResult = default(TimeSpanParse.TimeSpanResult);
				timeSpanResult.Init(TimeSpanParse.TimeSpanThrowStyle.None);
				if (TimeSpanParse.TryParseExactTimeSpan(input, formats[i], formatProvider, styles, ref timeSpanResult))
				{
					result.parsedTimeSpan = timeSpanResult.parsedTimeSpan;
					return true;
				}
			}
			result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
			return false;
		}

		// Token: 0x04001546 RID: 5446
		internal const int unlimitedDigits = -1;

		// Token: 0x04001547 RID: 5447
		internal const int maxFractionDigits = 7;

		// Token: 0x04001548 RID: 5448
		internal const int maxDays = 10675199;

		// Token: 0x04001549 RID: 5449
		internal const int maxHours = 23;

		// Token: 0x0400154A RID: 5450
		internal const int maxMinutes = 59;

		// Token: 0x0400154B RID: 5451
		internal const int maxSeconds = 59;

		// Token: 0x0400154C RID: 5452
		internal const int maxFraction = 9999999;

		// Token: 0x0400154D RID: 5453
		private static readonly TimeSpanParse.TimeSpanToken zero = new TimeSpanParse.TimeSpanToken(0);

		// Token: 0x02000B75 RID: 2933
		private enum TimeSpanThrowStyle
		{
			// Token: 0x04003485 RID: 13445
			None,
			// Token: 0x04003486 RID: 13446
			All
		}

		// Token: 0x02000B76 RID: 2934
		private enum ParseFailureKind
		{
			// Token: 0x04003488 RID: 13448
			None,
			// Token: 0x04003489 RID: 13449
			ArgumentNull,
			// Token: 0x0400348A RID: 13450
			Format,
			// Token: 0x0400348B RID: 13451
			FormatWithParameter,
			// Token: 0x0400348C RID: 13452
			Overflow
		}

		// Token: 0x02000B77 RID: 2935
		[Flags]
		private enum TimeSpanStandardStyles
		{
			// Token: 0x0400348E RID: 13454
			None = 0,
			// Token: 0x0400348F RID: 13455
			Invariant = 1,
			// Token: 0x04003490 RID: 13456
			Localized = 2,
			// Token: 0x04003491 RID: 13457
			RequireFull = 4,
			// Token: 0x04003492 RID: 13458
			Any = 3
		}

		// Token: 0x02000B78 RID: 2936
		private enum TTT
		{
			// Token: 0x04003494 RID: 13460
			None,
			// Token: 0x04003495 RID: 13461
			End,
			// Token: 0x04003496 RID: 13462
			Num,
			// Token: 0x04003497 RID: 13463
			Sep,
			// Token: 0x04003498 RID: 13464
			NumOverflow
		}

		// Token: 0x02000B79 RID: 2937
		private struct TimeSpanToken
		{
			// Token: 0x06006C36 RID: 27702 RVA: 0x00176824 File Offset: 0x00174A24
			public TimeSpanToken(int number)
			{
				this.ttt = TimeSpanParse.TTT.Num;
				this.num = number;
				this.zeroes = 0;
				this.sep = null;
			}

			// Token: 0x06006C37 RID: 27703 RVA: 0x00176842 File Offset: 0x00174A42
			public TimeSpanToken(int leadingZeroes, int number)
			{
				this.ttt = TimeSpanParse.TTT.Num;
				this.num = number;
				this.zeroes = leadingZeroes;
				this.sep = null;
			}

			// Token: 0x06006C38 RID: 27704 RVA: 0x00176860 File Offset: 0x00174A60
			public bool IsInvalidNumber(int maxValue, int maxPrecision)
			{
				return this.num > maxValue || (maxPrecision != -1 && (this.zeroes > maxPrecision || (this.num != 0 && this.zeroes != 0 && (long)this.num >= (long)maxValue / (long)Math.Pow(10.0, (double)(this.zeroes - 1)))));
			}

			// Token: 0x04003499 RID: 13465
			internal TimeSpanParse.TTT ttt;

			// Token: 0x0400349A RID: 13466
			internal int num;

			// Token: 0x0400349B RID: 13467
			internal int zeroes;

			// Token: 0x0400349C RID: 13468
			internal string sep;
		}

		// Token: 0x02000B7A RID: 2938
		private struct TimeSpanTokenizer
		{
			// Token: 0x06006C39 RID: 27705 RVA: 0x001768C2 File Offset: 0x00174AC2
			internal void Init(string input)
			{
				this.Init(input, 0);
			}

			// Token: 0x06006C3A RID: 27706 RVA: 0x001768CC File Offset: 0x00174ACC
			internal void Init(string input, int startPosition)
			{
				this.m_pos = startPosition;
				this.m_value = input;
			}

			// Token: 0x06006C3B RID: 27707 RVA: 0x001768DC File Offset: 0x00174ADC
			internal TimeSpanParse.TimeSpanToken GetNextToken()
			{
				TimeSpanParse.TimeSpanToken timeSpanToken = default(TimeSpanParse.TimeSpanToken);
				char c = this.CurrentChar;
				if (c == '\0')
				{
					timeSpanToken.ttt = TimeSpanParse.TTT.End;
					return timeSpanToken;
				}
				if (c >= '0' && c <= '9')
				{
					timeSpanToken.ttt = TimeSpanParse.TTT.Num;
					timeSpanToken.num = 0;
					timeSpanToken.zeroes = 0;
					while (((long)timeSpanToken.num & (long)((ulong)(-268435456))) == 0L)
					{
						timeSpanToken.num = timeSpanToken.num * 10 + (int)c - 48;
						if (timeSpanToken.num == 0)
						{
							timeSpanToken.zeroes++;
						}
						if (timeSpanToken.num < 0)
						{
							timeSpanToken.ttt = TimeSpanParse.TTT.NumOverflow;
							return timeSpanToken;
						}
						c = this.NextChar;
						if (c < '0' || c > '9')
						{
							return timeSpanToken;
						}
					}
					timeSpanToken.ttt = TimeSpanParse.TTT.NumOverflow;
					return timeSpanToken;
				}
				timeSpanToken.ttt = TimeSpanParse.TTT.Sep;
				int pos = this.m_pos;
				int num = 0;
				while (c != '\0' && (c < '0' || '9' < c))
				{
					c = this.NextChar;
					num++;
				}
				timeSpanToken.sep = this.m_value.Substring(pos, num);
				return timeSpanToken;
			}

			// Token: 0x17001254 RID: 4692
			// (get) Token: 0x06006C3C RID: 27708 RVA: 0x001769D6 File Offset: 0x00174BD6
			internal bool EOL
			{
				get
				{
					return this.m_pos >= this.m_value.Length - 1;
				}
			}

			// Token: 0x06006C3D RID: 27709 RVA: 0x001769F0 File Offset: 0x00174BF0
			internal void BackOne()
			{
				if (this.m_pos > 0)
				{
					this.m_pos--;
				}
			}

			// Token: 0x17001255 RID: 4693
			// (get) Token: 0x06006C3E RID: 27710 RVA: 0x00176A09 File Offset: 0x00174C09
			internal char NextChar
			{
				get
				{
					this.m_pos++;
					return this.CurrentChar;
				}
			}

			// Token: 0x17001256 RID: 4694
			// (get) Token: 0x06006C3F RID: 27711 RVA: 0x00176A1F File Offset: 0x00174C1F
			internal char CurrentChar
			{
				get
				{
					if (this.m_pos > -1 && this.m_pos < this.m_value.Length)
					{
						return this.m_value[this.m_pos];
					}
					return '\0';
				}
			}

			// Token: 0x0400349D RID: 13469
			private int m_pos;

			// Token: 0x0400349E RID: 13470
			private string m_value;
		}

		// Token: 0x02000B7B RID: 2939
		private struct TimeSpanRawInfo
		{
			// Token: 0x17001257 RID: 4695
			// (get) Token: 0x06006C40 RID: 27712 RVA: 0x00176A50 File Offset: 0x00174C50
			internal TimeSpanFormat.FormatLiterals PositiveInvariant
			{
				get
				{
					return TimeSpanFormat.PositiveInvariantFormatLiterals;
				}
			}

			// Token: 0x17001258 RID: 4696
			// (get) Token: 0x06006C41 RID: 27713 RVA: 0x00176A57 File Offset: 0x00174C57
			internal TimeSpanFormat.FormatLiterals NegativeInvariant
			{
				get
				{
					return TimeSpanFormat.NegativeInvariantFormatLiterals;
				}
			}

			// Token: 0x17001259 RID: 4697
			// (get) Token: 0x06006C42 RID: 27714 RVA: 0x00176A5E File Offset: 0x00174C5E
			internal TimeSpanFormat.FormatLiterals PositiveLocalized
			{
				get
				{
					if (!this.m_posLocInit)
					{
						this.m_posLoc = default(TimeSpanFormat.FormatLiterals);
						this.m_posLoc.Init(this.m_fullPosPattern, false);
						this.m_posLocInit = true;
					}
					return this.m_posLoc;
				}
			}

			// Token: 0x1700125A RID: 4698
			// (get) Token: 0x06006C43 RID: 27715 RVA: 0x00176A93 File Offset: 0x00174C93
			internal TimeSpanFormat.FormatLiterals NegativeLocalized
			{
				get
				{
					if (!this.m_negLocInit)
					{
						this.m_negLoc = default(TimeSpanFormat.FormatLiterals);
						this.m_negLoc.Init(this.m_fullNegPattern, false);
						this.m_negLocInit = true;
					}
					return this.m_negLoc;
				}
			}

			// Token: 0x06006C44 RID: 27716 RVA: 0x00176AC8 File Offset: 0x00174CC8
			internal bool FullAppCompatMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 5 && this.NumCount == 4 && pattern.Start == this.literals[0] && pattern.DayHourSep == this.literals[1] && pattern.HourMinuteSep == this.literals[2] && pattern.AppCompatLiteral == this.literals[3] && pattern.End == this.literals[4];
			}

			// Token: 0x06006C45 RID: 27717 RVA: 0x00176B54 File Offset: 0x00174D54
			internal bool PartialAppCompatMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 4 && this.NumCount == 3 && pattern.Start == this.literals[0] && pattern.HourMinuteSep == this.literals[1] && pattern.AppCompatLiteral == this.literals[2] && pattern.End == this.literals[3];
			}

			// Token: 0x06006C46 RID: 27718 RVA: 0x00176BCC File Offset: 0x00174DCC
			internal bool FullMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 6 && this.NumCount == 5 && pattern.Start == this.literals[0] && pattern.DayHourSep == this.literals[1] && pattern.HourMinuteSep == this.literals[2] && pattern.MinuteSecondSep == this.literals[3] && pattern.SecondFractionSep == this.literals[4] && pattern.End == this.literals[5];
			}

			// Token: 0x06006C47 RID: 27719 RVA: 0x00176C75 File Offset: 0x00174E75
			internal bool FullDMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 2 && this.NumCount == 1 && pattern.Start == this.literals[0] && pattern.End == this.literals[1];
			}

			// Token: 0x06006C48 RID: 27720 RVA: 0x00176CB8 File Offset: 0x00174EB8
			internal bool FullHMMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 3 && this.NumCount == 2 && pattern.Start == this.literals[0] && pattern.HourMinuteSep == this.literals[1] && pattern.End == this.literals[2];
			}

			// Token: 0x06006C49 RID: 27721 RVA: 0x00176D1C File Offset: 0x00174F1C
			internal bool FullDHMMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 4 && this.NumCount == 3 && pattern.Start == this.literals[0] && pattern.DayHourSep == this.literals[1] && pattern.HourMinuteSep == this.literals[2] && pattern.End == this.literals[3];
			}

			// Token: 0x06006C4A RID: 27722 RVA: 0x00176D94 File Offset: 0x00174F94
			internal bool FullHMSMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 4 && this.NumCount == 3 && pattern.Start == this.literals[0] && pattern.HourMinuteSep == this.literals[1] && pattern.MinuteSecondSep == this.literals[2] && pattern.End == this.literals[3];
			}

			// Token: 0x06006C4B RID: 27723 RVA: 0x00176E0C File Offset: 0x0017500C
			internal bool FullDHMSMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 5 && this.NumCount == 4 && pattern.Start == this.literals[0] && pattern.DayHourSep == this.literals[1] && pattern.HourMinuteSep == this.literals[2] && pattern.MinuteSecondSep == this.literals[3] && pattern.End == this.literals[4];
			}

			// Token: 0x06006C4C RID: 27724 RVA: 0x00176E9C File Offset: 0x0017509C
			internal bool FullHMSFMatch(TimeSpanFormat.FormatLiterals pattern)
			{
				return this.SepCount == 5 && this.NumCount == 4 && pattern.Start == this.literals[0] && pattern.HourMinuteSep == this.literals[1] && pattern.MinuteSecondSep == this.literals[2] && pattern.SecondFractionSep == this.literals[3] && pattern.End == this.literals[4];
			}

			// Token: 0x06006C4D RID: 27725 RVA: 0x00176F2C File Offset: 0x0017512C
			internal void Init(DateTimeFormatInfo dtfi)
			{
				this.lastSeenTTT = TimeSpanParse.TTT.None;
				this.tokenCount = 0;
				this.SepCount = 0;
				this.NumCount = 0;
				this.literals = new string[6];
				this.numbers = new TimeSpanParse.TimeSpanToken[5];
				this.m_fullPosPattern = dtfi.FullTimeSpanPositivePattern;
				this.m_fullNegPattern = dtfi.FullTimeSpanNegativePattern;
				this.m_posLocInit = false;
				this.m_negLocInit = false;
			}

			// Token: 0x06006C4E RID: 27726 RVA: 0x00176F94 File Offset: 0x00175194
			internal bool ProcessToken(ref TimeSpanParse.TimeSpanToken tok, ref TimeSpanParse.TimeSpanResult result)
			{
				if (tok.ttt == TimeSpanParse.TTT.NumOverflow)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge", null);
					return false;
				}
				if (tok.ttt != TimeSpanParse.TTT.Sep && tok.ttt != TimeSpanParse.TTT.Num)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan", null);
					return false;
				}
				TimeSpanParse.TTT ttt = tok.ttt;
				if (ttt != TimeSpanParse.TTT.Num)
				{
					if (ttt == TimeSpanParse.TTT.Sep && !this.AddSep(tok.sep, ref result))
					{
						return false;
					}
				}
				else
				{
					if (this.tokenCount == 0 && !this.AddSep(string.Empty, ref result))
					{
						return false;
					}
					if (!this.AddNum(tok, ref result))
					{
						return false;
					}
				}
				this.lastSeenTTT = tok.ttt;
				return true;
			}

			// Token: 0x06006C4F RID: 27727 RVA: 0x00177030 File Offset: 0x00175230
			private bool AddSep(string sep, ref TimeSpanParse.TimeSpanResult result)
			{
				if (this.SepCount >= 6 || this.tokenCount >= 11)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan", null);
					return false;
				}
				string[] array = this.literals;
				int sepCount = this.SepCount;
				this.SepCount = sepCount + 1;
				array[sepCount] = sep;
				this.tokenCount++;
				return true;
			}

			// Token: 0x06006C50 RID: 27728 RVA: 0x00177088 File Offset: 0x00175288
			private bool AddNum(TimeSpanParse.TimeSpanToken num, ref TimeSpanParse.TimeSpanResult result)
			{
				if (this.NumCount >= 5 || this.tokenCount >= 11)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan", null);
					return false;
				}
				TimeSpanParse.TimeSpanToken[] array = this.numbers;
				int numCount = this.NumCount;
				this.NumCount = numCount + 1;
				array[numCount] = num;
				this.tokenCount++;
				return true;
			}

			// Token: 0x0400349F RID: 13471
			internal TimeSpanParse.TTT lastSeenTTT;

			// Token: 0x040034A0 RID: 13472
			internal int tokenCount;

			// Token: 0x040034A1 RID: 13473
			internal int SepCount;

			// Token: 0x040034A2 RID: 13474
			internal int NumCount;

			// Token: 0x040034A3 RID: 13475
			internal string[] literals;

			// Token: 0x040034A4 RID: 13476
			internal TimeSpanParse.TimeSpanToken[] numbers;

			// Token: 0x040034A5 RID: 13477
			private TimeSpanFormat.FormatLiterals m_posLoc;

			// Token: 0x040034A6 RID: 13478
			private TimeSpanFormat.FormatLiterals m_negLoc;

			// Token: 0x040034A7 RID: 13479
			private bool m_posLocInit;

			// Token: 0x040034A8 RID: 13480
			private bool m_negLocInit;

			// Token: 0x040034A9 RID: 13481
			private string m_fullPosPattern;

			// Token: 0x040034AA RID: 13482
			private string m_fullNegPattern;

			// Token: 0x040034AB RID: 13483
			private const int MaxTokens = 11;

			// Token: 0x040034AC RID: 13484
			private const int MaxLiteralTokens = 6;

			// Token: 0x040034AD RID: 13485
			private const int MaxNumericTokens = 5;
		}

		// Token: 0x02000B7C RID: 2940
		private struct TimeSpanResult
		{
			// Token: 0x06006C51 RID: 27729 RVA: 0x001770E3 File Offset: 0x001752E3
			internal void Init(TimeSpanParse.TimeSpanThrowStyle canThrow)
			{
				this.parsedTimeSpan = default(TimeSpan);
				this.throwStyle = canThrow;
			}

			// Token: 0x06006C52 RID: 27730 RVA: 0x001770F8 File Offset: 0x001752F8
			internal void SetFailure(TimeSpanParse.ParseFailureKind failure, string failureMessageID)
			{
				this.SetFailure(failure, failureMessageID, null, null);
			}

			// Token: 0x06006C53 RID: 27731 RVA: 0x00177104 File Offset: 0x00175304
			internal void SetFailure(TimeSpanParse.ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument)
			{
				this.SetFailure(failure, failureMessageID, failureMessageFormatArgument, null);
			}

			// Token: 0x06006C54 RID: 27732 RVA: 0x00177110 File Offset: 0x00175310
			internal void SetFailure(TimeSpanParse.ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument, string failureArgumentName)
			{
				this.m_failure = failure;
				this.m_failureMessageID = failureMessageID;
				this.m_failureMessageFormatArgument = failureMessageFormatArgument;
				this.m_failureArgumentName = failureArgumentName;
				if (this.throwStyle != TimeSpanParse.TimeSpanThrowStyle.None)
				{
					throw this.GetTimeSpanParseException();
				}
			}

			// Token: 0x06006C55 RID: 27733 RVA: 0x00177140 File Offset: 0x00175340
			internal Exception GetTimeSpanParseException()
			{
				switch (this.m_failure)
				{
				case TimeSpanParse.ParseFailureKind.ArgumentNull:
					return new ArgumentNullException(this.m_failureArgumentName, Environment.GetResourceString(this.m_failureMessageID));
				case TimeSpanParse.ParseFailureKind.Format:
					return new FormatException(Environment.GetResourceString(this.m_failureMessageID));
				case TimeSpanParse.ParseFailureKind.FormatWithParameter:
					return new FormatException(Environment.GetResourceString(this.m_failureMessageID, new object[] { this.m_failureMessageFormatArgument }));
				case TimeSpanParse.ParseFailureKind.Overflow:
					return new OverflowException(Environment.GetResourceString(this.m_failureMessageID));
				default:
					return new FormatException(Environment.GetResourceString("Format_InvalidString"));
				}
			}

			// Token: 0x040034AE RID: 13486
			internal TimeSpan parsedTimeSpan;

			// Token: 0x040034AF RID: 13487
			internal TimeSpanParse.TimeSpanThrowStyle throwStyle;

			// Token: 0x040034B0 RID: 13488
			internal TimeSpanParse.ParseFailureKind m_failure;

			// Token: 0x040034B1 RID: 13489
			internal string m_failureMessageID;

			// Token: 0x040034B2 RID: 13490
			internal object m_failureMessageFormatArgument;

			// Token: 0x040034B3 RID: 13491
			internal string m_failureArgumentName;
		}

		// Token: 0x02000B7D RID: 2941
		private struct StringParser
		{
			// Token: 0x06006C56 RID: 27734 RVA: 0x001771D8 File Offset: 0x001753D8
			internal void NextChar()
			{
				if (this.pos < this.len)
				{
					this.pos++;
				}
				this.ch = ((this.pos < this.len) ? this.str[this.pos] : '\0');
			}

			// Token: 0x06006C57 RID: 27735 RVA: 0x0017722C File Offset: 0x0017542C
			internal char NextNonDigit()
			{
				for (int i = this.pos; i < this.len; i++)
				{
					char c = this.str[i];
					if (c < '0' || c > '9')
					{
						return c;
					}
				}
				return '\0';
			}

			// Token: 0x06006C58 RID: 27736 RVA: 0x0017726C File Offset: 0x0017546C
			internal bool TryParse(string input, ref TimeSpanParse.TimeSpanResult result)
			{
				result.parsedTimeSpan._ticks = 0L;
				if (input == null)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "input");
					return false;
				}
				this.str = input;
				this.len = input.Length;
				this.pos = -1;
				this.NextChar();
				this.SkipBlanks();
				bool flag = false;
				if (this.ch == '-')
				{
					flag = true;
					this.NextChar();
				}
				long num;
				if (this.NextNonDigit() == ':')
				{
					if (!this.ParseTime(out num, ref result))
					{
						return false;
					}
				}
				else
				{
					int num2;
					if (!this.ParseInt(10675199, out num2, ref result))
					{
						return false;
					}
					num = (long)num2 * 864000000000L;
					if (this.ch == '.')
					{
						this.NextChar();
						long num3;
						if (!this.ParseTime(out num3, ref result))
						{
							return false;
						}
						num += num3;
					}
				}
				if (flag)
				{
					num = -num;
					if (num > 0L)
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
						return false;
					}
				}
				else if (num < 0L)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
					return false;
				}
				this.SkipBlanks();
				if (this.pos < this.len)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
					return false;
				}
				result.parsedTimeSpan._ticks = num;
				return true;
			}

			// Token: 0x06006C59 RID: 27737 RVA: 0x0017738C File Offset: 0x0017558C
			internal bool ParseInt(int max, out int i, ref TimeSpanParse.TimeSpanResult result)
			{
				i = 0;
				int num = this.pos;
				while (this.ch >= '0' && this.ch <= '9')
				{
					if (((long)i & (long)((ulong)(-268435456))) != 0L)
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
						return false;
					}
					i = i * 10 + (int)this.ch - 48;
					if (i < 0)
					{
						result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
						return false;
					}
					this.NextChar();
				}
				if (num == this.pos)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
					return false;
				}
				if (i > max)
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
					return false;
				}
				return true;
			}

			// Token: 0x06006C5A RID: 27738 RVA: 0x00177428 File Offset: 0x00175628
			internal bool ParseTime(out long time, ref TimeSpanParse.TimeSpanResult result)
			{
				time = 0L;
				int num;
				if (!this.ParseInt(23, out num, ref result))
				{
					return false;
				}
				time = (long)num * 36000000000L;
				if (this.ch != ':')
				{
					result.SetFailure(TimeSpanParse.ParseFailureKind.Format, "Format_BadTimeSpan");
					return false;
				}
				this.NextChar();
				if (!this.ParseInt(59, out num, ref result))
				{
					return false;
				}
				time += (long)num * 600000000L;
				if (this.ch == ':')
				{
					this.NextChar();
					if (this.ch != '.')
					{
						if (!this.ParseInt(59, out num, ref result))
						{
							return false;
						}
						time += (long)num * 10000000L;
					}
					if (this.ch == '.')
					{
						this.NextChar();
						int num2 = 10000000;
						while (num2 > 1 && this.ch >= '0' && this.ch <= '9')
						{
							num2 /= 10;
							time += (long)((int)(this.ch - '0') * num2);
							this.NextChar();
						}
					}
				}
				return true;
			}

			// Token: 0x06006C5B RID: 27739 RVA: 0x00177515 File Offset: 0x00175715
			internal void SkipBlanks()
			{
				while (this.ch == ' ' || this.ch == '\t')
				{
					this.NextChar();
				}
			}

			// Token: 0x040034B4 RID: 13492
			private string str;

			// Token: 0x040034B5 RID: 13493
			private char ch;

			// Token: 0x040034B6 RID: 13494
			private int pos;

			// Token: 0x040034B7 RID: 13495
			private int len;
		}
	}
}
