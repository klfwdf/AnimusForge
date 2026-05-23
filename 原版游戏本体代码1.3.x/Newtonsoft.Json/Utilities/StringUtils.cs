using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x0200006D RID: 109
	[NullableContext(1)]
	[Nullable(0)]
	internal static class StringUtils
	{
		// Token: 0x060005E2 RID: 1506 RVA: 0x00019473 File Offset: 0x00017673
		[NullableContext(2)]
		public static bool IsNullOrEmpty([NotNullWhen(false)] string value)
		{
			return string.IsNullOrEmpty(value);
		}

		// Token: 0x060005E3 RID: 1507 RVA: 0x0001947B File Offset: 0x0001767B
		public static string FormatWith(this string format, IFormatProvider provider, [Nullable(2)] object arg0)
		{
			return format.FormatWith(provider, new object[] { arg0 });
		}

		// Token: 0x060005E4 RID: 1508 RVA: 0x0001948E File Offset: 0x0001768E
		public static string FormatWith(this string format, IFormatProvider provider, [Nullable(2)] object arg0, [Nullable(2)] object arg1)
		{
			return format.FormatWith(provider, new object[] { arg0, arg1 });
		}

		// Token: 0x060005E5 RID: 1509 RVA: 0x000194A5 File Offset: 0x000176A5
		public static string FormatWith(this string format, IFormatProvider provider, [Nullable(2)] object arg0, [Nullable(2)] object arg1, [Nullable(2)] object arg2)
		{
			return format.FormatWith(provider, new object[] { arg0, arg1, arg2 });
		}

		// Token: 0x060005E6 RID: 1510 RVA: 0x000194C1 File Offset: 0x000176C1
		[NullableContext(2)]
		[return: Nullable(1)]
		public static string FormatWith([Nullable(1)] this string format, [Nullable(1)] IFormatProvider provider, object arg0, object arg1, object arg2, object arg3)
		{
			return format.FormatWith(provider, new object[] { arg0, arg1, arg2, arg3 });
		}

		// Token: 0x060005E7 RID: 1511 RVA: 0x000194E2 File Offset: 0x000176E2
		private static string FormatWith(this string format, IFormatProvider provider, [Nullable(new byte[] { 1, 2 })] params object[] args)
		{
			ValidationUtils.ArgumentNotNull(format, "format");
			return string.Format(provider, format, args);
		}

		/// <summary>
		/// Determines whether the string is all white space. Empty string will return <c>false</c>.
		/// </summary>
		/// <param name="s">The string to test whether it is all white space.</param>
		/// <returns>
		/// 	<c>true</c> if the string is all white space; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x060005E8 RID: 1512 RVA: 0x000194F8 File Offset: 0x000176F8
		public static bool IsWhiteSpace(string s)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (s.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < s.Length; i++)
			{
				if (!char.IsWhiteSpace(s[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060005E9 RID: 1513 RVA: 0x0001953F File Offset: 0x0001773F
		public static StringWriter CreateStringWriter(int capacity)
		{
			return new StringWriter(new StringBuilder(capacity), CultureInfo.InvariantCulture);
		}

		// Token: 0x060005EA RID: 1514 RVA: 0x00019554 File Offset: 0x00017754
		public static void ToCharAsUnicode(char c, char[] buffer)
		{
			buffer[0] = '\\';
			buffer[1] = 'u';
			buffer[2] = MathUtils.IntToHex((int)((c >> 12) & '\u000f'));
			buffer[3] = MathUtils.IntToHex((int)((c >> 8) & '\u000f'));
			buffer[4] = MathUtils.IntToHex((int)((c >> 4) & '\u000f'));
			buffer[5] = MathUtils.IntToHex((int)(c & '\u000f'));
		}

		// Token: 0x060005EB RID: 1515 RVA: 0x000195A4 File Offset: 0x000177A4
		[return: Nullable(2)]
		public static TSource ForgivingCaseSensitiveFind<[Nullable(2)] TSource>(this IEnumerable<TSource> source, Func<TSource, string> valueSelector, string testValue)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (valueSelector == null)
			{
				throw new ArgumentNullException("valueSelector");
			}
			IEnumerable<TSource> source2 = from s in source
				where string.Equals(valueSelector(s), testValue, StringComparison.OrdinalIgnoreCase)
				select s;
			if (source2.Count<TSource>() <= 1)
			{
				return source2.SingleOrDefault<TSource>();
			}
			return (from s in source
				where string.Equals(valueSelector(s), testValue, StringComparison.Ordinal)
				select s).SingleOrDefault<TSource>();
		}

		// Token: 0x060005EC RID: 1516 RVA: 0x00019620 File Offset: 0x00017820
		public static string ToCamelCase(string s)
		{
			if (StringUtils.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
			{
				return s;
			}
			char[] array = s.ToCharArray();
			int num = 0;
			while (num < array.Length && (num != 1 || char.IsUpper(array[num])))
			{
				bool flag = num + 1 < array.Length;
				if (num > 0 && flag && !char.IsUpper(array[num + 1]))
				{
					if (char.IsSeparator(array[num + 1]))
					{
						array[num] = StringUtils.ToLower(array[num]);
						break;
					}
					break;
				}
				else
				{
					array[num] = StringUtils.ToLower(array[num]);
					num++;
				}
			}
			return new string(array);
		}

		// Token: 0x060005ED RID: 1517 RVA: 0x000196AF File Offset: 0x000178AF
		private static char ToLower(char c)
		{
			c = char.ToLower(c, CultureInfo.InvariantCulture);
			return c;
		}

		// Token: 0x060005EE RID: 1518 RVA: 0x000196BF File Offset: 0x000178BF
		public static string ToSnakeCase(string s)
		{
			return StringUtils.ToSeparatedCase(s, '_');
		}

		// Token: 0x060005EF RID: 1519 RVA: 0x000196C9 File Offset: 0x000178C9
		public static string ToKebabCase(string s)
		{
			return StringUtils.ToSeparatedCase(s, '-');
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x000196D4 File Offset: 0x000178D4
		private static string ToSeparatedCase(string s, char separator)
		{
			if (StringUtils.IsNullOrEmpty(s))
			{
				return s;
			}
			StringBuilder stringBuilder = new StringBuilder();
			StringUtils.SeparatedCaseState separatedCaseState = StringUtils.SeparatedCaseState.Start;
			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] == ' ')
				{
					if (separatedCaseState != StringUtils.SeparatedCaseState.Start)
					{
						separatedCaseState = StringUtils.SeparatedCaseState.NewWord;
					}
				}
				else if (char.IsUpper(s[i]))
				{
					switch (separatedCaseState)
					{
					case StringUtils.SeparatedCaseState.Lower:
					case StringUtils.SeparatedCaseState.NewWord:
						stringBuilder.Append(separator);
						break;
					case StringUtils.SeparatedCaseState.Upper:
					{
						bool flag = i + 1 < s.Length;
						if (i > 0 && flag)
						{
							char c = s[i + 1];
							if (!char.IsUpper(c) && c != separator)
							{
								stringBuilder.Append(separator);
							}
						}
						break;
					}
					}
					char value = char.ToLower(s[i], CultureInfo.InvariantCulture);
					stringBuilder.Append(value);
					separatedCaseState = StringUtils.SeparatedCaseState.Upper;
				}
				else if (s[i] == separator)
				{
					stringBuilder.Append(separator);
					separatedCaseState = StringUtils.SeparatedCaseState.Start;
				}
				else
				{
					if (separatedCaseState == StringUtils.SeparatedCaseState.NewWord)
					{
						stringBuilder.Append(separator);
					}
					stringBuilder.Append(s[i]);
					separatedCaseState = StringUtils.SeparatedCaseState.Lower;
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060005F1 RID: 1521 RVA: 0x000197DD File Offset: 0x000179DD
		public static bool IsHighSurrogate(char c)
		{
			return char.IsHighSurrogate(c);
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x000197E5 File Offset: 0x000179E5
		public static bool IsLowSurrogate(char c)
		{
			return char.IsLowSurrogate(c);
		}

		// Token: 0x060005F3 RID: 1523 RVA: 0x000197ED File Offset: 0x000179ED
		public static int IndexOf(string s, char c)
		{
			return s.IndexOf(c);
		}

		// Token: 0x060005F4 RID: 1524 RVA: 0x000197F6 File Offset: 0x000179F6
		public static string Replace(string s, string oldValue, string newValue)
		{
			return s.Replace(oldValue, newValue);
		}

		// Token: 0x060005F5 RID: 1525 RVA: 0x00019800 File Offset: 0x00017A00
		public static bool StartsWith(this string source, char value)
		{
			return source.Length > 0 && source[0] == value;
		}

		// Token: 0x060005F6 RID: 1526 RVA: 0x00019817 File Offset: 0x00017A17
		public static bool EndsWith(this string source, char value)
		{
			return source.Length > 0 && source[source.Length - 1] == value;
		}

		// Token: 0x060005F7 RID: 1527 RVA: 0x00019838 File Offset: 0x00017A38
		public static string Trim(this string s, int start, int length)
		{
			if (s == null)
			{
				throw new ArgumentNullException();
			}
			if (start < 0)
			{
				throw new ArgumentOutOfRangeException("start");
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			int num = start + length - 1;
			if (num >= s.Length)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			while (start < num)
			{
				if (!char.IsWhiteSpace(s[start]))
				{
					IL_6C:
					while (num >= start && char.IsWhiteSpace(s[num]))
					{
						num--;
					}
					return s.Substring(start, num - start + 1);
				}
				start++;
			}
			goto IL_6C;
		}

		// Token: 0x04000221 RID: 545
		public const string CarriageReturnLineFeed = "\r\n";

		// Token: 0x04000222 RID: 546
		public const string Empty = "";

		// Token: 0x04000223 RID: 547
		public const char CarriageReturn = '\r';

		// Token: 0x04000224 RID: 548
		public const char LineFeed = '\n';

		// Token: 0x04000225 RID: 549
		public const char Tab = '\t';

		// Token: 0x0200019A RID: 410
		[NullableContext(0)]
		private enum SeparatedCaseState
		{
			// Token: 0x0400071F RID: 1823
			Start,
			// Token: 0x04000720 RID: 1824
			Lower,
			// Token: 0x04000721 RID: 1825
			Upper,
			// Token: 0x04000722 RID: 1826
			NewWord
		}
	}
}
