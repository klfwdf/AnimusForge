using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x0200005F RID: 95
	[NullableContext(1)]
	[Nullable(0)]
	internal static class JavaScriptUtils
	{
		// Token: 0x06000554 RID: 1364 RVA: 0x00016E3C File Offset: 0x0001503C
		static JavaScriptUtils()
		{
			IList<char> list = new List<char> { '\n', '\r', '\t', '\\', '\f', '\b' };
			for (int i = 0; i < 32; i++)
			{
				list.Add((char)i);
			}
			foreach (char c in list.Union(new char[] { '\'' }))
			{
				JavaScriptUtils.SingleQuoteCharEscapeFlags[(int)c] = true;
			}
			foreach (char c2 in list.Union(new char[] { '"' }))
			{
				JavaScriptUtils.DoubleQuoteCharEscapeFlags[(int)c2] = true;
			}
			foreach (char c3 in list.Union(new char[] { '"', '\'', '<', '>', '&' }))
			{
				JavaScriptUtils.HtmlCharEscapeFlags[(int)c3] = true;
			}
		}

		// Token: 0x06000555 RID: 1365 RVA: 0x00016FA8 File Offset: 0x000151A8
		public static bool[] GetCharEscapeFlags(StringEscapeHandling stringEscapeHandling, char quoteChar)
		{
			if (stringEscapeHandling == StringEscapeHandling.EscapeHtml)
			{
				return JavaScriptUtils.HtmlCharEscapeFlags;
			}
			if (quoteChar == '"')
			{
				return JavaScriptUtils.DoubleQuoteCharEscapeFlags;
			}
			return JavaScriptUtils.SingleQuoteCharEscapeFlags;
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x00016FC4 File Offset: 0x000151C4
		public static bool ShouldEscapeJavaScriptString([Nullable(2)] string s, bool[] charEscapeFlags)
		{
			if (s == null)
			{
				return false;
			}
			foreach (char c in s)
			{
				if ((int)c >= charEscapeFlags.Length || charEscapeFlags[(int)c])
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x00017000 File Offset: 0x00015200
		[NullableContext(2)]
		public static void WriteEscapedJavaScriptString([Nullable(1)] TextWriter writer, string s, char delimiter, bool appendDelimiters, [Nullable(1)] bool[] charEscapeFlags, StringEscapeHandling stringEscapeHandling, IArrayPool<char> bufferPool, ref char[] writeBuffer)
		{
			if (appendDelimiters)
			{
				writer.Write(delimiter);
			}
			if (!StringUtils.IsNullOrEmpty(s))
			{
				int num = JavaScriptUtils.FirstCharToEscape(s, charEscapeFlags, stringEscapeHandling);
				if (num == -1)
				{
					writer.Write(s);
				}
				else
				{
					if (num != 0)
					{
						if (writeBuffer == null || writeBuffer.Length < num)
						{
							writeBuffer = BufferUtils.EnsureBufferSize(bufferPool, num, writeBuffer);
						}
						s.CopyTo(0, writeBuffer, 0, num);
						writer.Write(writeBuffer, 0, num);
					}
					int num2;
					for (int i = num; i < s.Length; i++)
					{
						char c = s[i];
						if ((int)c >= charEscapeFlags.Length || charEscapeFlags[(int)c])
						{
							string text;
							if (c <= '\\')
							{
								switch (c)
								{
								case '\b':
									text = "\\b";
									break;
								case '\t':
									text = "\\t";
									break;
								case '\n':
									text = "\\n";
									break;
								case '\v':
									goto IL_12A;
								case '\f':
									text = "\\f";
									break;
								case '\r':
									text = "\\r";
									break;
								default:
									if (c != '\\')
									{
										goto IL_12A;
									}
									text = "\\\\";
									break;
								}
							}
							else if (c != '\u0085')
							{
								if (c != '\u2028')
								{
									if (c != '\u2029')
									{
										goto IL_12A;
									}
									text = "\\u2029";
								}
								else
								{
									text = "\\u2028";
								}
							}
							else
							{
								text = "\\u0085";
							}
							IL_18C:
							if (text == null)
							{
								goto IL_22E;
							}
							bool flag = string.Equals(text, "!", StringComparison.Ordinal);
							if (i > num)
							{
								num2 = i - num + (flag ? 6 : 0);
								int num3 = (flag ? 6 : 0);
								if (writeBuffer == null || writeBuffer.Length < num2)
								{
									char[] array = BufferUtils.RentBuffer(bufferPool, num2);
									if (flag)
									{
										Array.Copy(writeBuffer, array, 6);
									}
									BufferUtils.ReturnBuffer(bufferPool, writeBuffer);
									writeBuffer = array;
								}
								s.CopyTo(num, writeBuffer, num3, num2 - num3);
								writer.Write(writeBuffer, num3, num2 - num3);
							}
							num = i + 1;
							if (!flag)
							{
								writer.Write(text);
								goto IL_22E;
							}
							writer.Write(writeBuffer, 0, 6);
							goto IL_22E;
							IL_12A:
							if ((int)c >= charEscapeFlags.Length && stringEscapeHandling != StringEscapeHandling.EscapeNonAscii)
							{
								text = null;
								goto IL_18C;
							}
							if (c == '\'' && stringEscapeHandling != StringEscapeHandling.EscapeHtml)
							{
								text = "\\'";
								goto IL_18C;
							}
							if (c == '"' && stringEscapeHandling != StringEscapeHandling.EscapeHtml)
							{
								text = "\\\"";
								goto IL_18C;
							}
							if (writeBuffer == null || writeBuffer.Length < 6)
							{
								writeBuffer = BufferUtils.EnsureBufferSize(bufferPool, 6, writeBuffer);
							}
							StringUtils.ToCharAsUnicode(c, writeBuffer);
							text = "!";
							goto IL_18C;
						}
						IL_22E:;
					}
					num2 = s.Length - num;
					if (num2 > 0)
					{
						if (writeBuffer == null || writeBuffer.Length < num2)
						{
							writeBuffer = BufferUtils.EnsureBufferSize(bufferPool, num2, writeBuffer);
						}
						s.CopyTo(num, writeBuffer, 0, num2);
						writer.Write(writeBuffer, 0, num2);
					}
				}
			}
			if (appendDelimiters)
			{
				writer.Write(delimiter);
			}
		}

		// Token: 0x06000558 RID: 1368 RVA: 0x00017294 File Offset: 0x00015494
		public static string ToEscapedJavaScriptString([Nullable(2)] string value, char delimiter, bool appendDelimiters, StringEscapeHandling stringEscapeHandling)
		{
			bool[] charEscapeFlags = JavaScriptUtils.GetCharEscapeFlags(stringEscapeHandling, delimiter);
			string result;
			using (StringWriter stringWriter = StringUtils.CreateStringWriter((value != null) ? value.Length : 16))
			{
				char[] array = null;
				JavaScriptUtils.WriteEscapedJavaScriptString(stringWriter, value, delimiter, appendDelimiters, charEscapeFlags, stringEscapeHandling, null, ref array);
				result = stringWriter.ToString();
			}
			return result;
		}

		// Token: 0x06000559 RID: 1369 RVA: 0x000172F0 File Offset: 0x000154F0
		private static int FirstCharToEscape(string s, bool[] charEscapeFlags, StringEscapeHandling stringEscapeHandling)
		{
			for (int num = 0; num != s.Length; num++)
			{
				char c = s[num];
				if ((int)c < charEscapeFlags.Length)
				{
					if (charEscapeFlags[(int)c])
					{
						return num;
					}
				}
				else
				{
					if (stringEscapeHandling == StringEscapeHandling.EscapeNonAscii)
					{
						return num;
					}
					if (c == '\u0085' || c == '\u2028' || c == '\u2029')
					{
						return num;
					}
				}
			}
			return -1;
		}

		// Token: 0x0600055A RID: 1370 RVA: 0x00017344 File Offset: 0x00015544
		public static Task WriteEscapedJavaScriptStringAsync(TextWriter writer, string s, char delimiter, bool appendDelimiters, bool[] charEscapeFlags, StringEscapeHandling stringEscapeHandling, JsonTextWriter client, char[] writeBuffer, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return cancellationToken.FromCanceled();
			}
			if (appendDelimiters)
			{
				return JavaScriptUtils.WriteEscapedJavaScriptStringWithDelimitersAsync(writer, s, delimiter, charEscapeFlags, stringEscapeHandling, client, writeBuffer, cancellationToken);
			}
			if (StringUtils.IsNullOrEmpty(s))
			{
				return cancellationToken.CancelIfRequestedAsync() ?? AsyncUtils.CompletedTask;
			}
			return JavaScriptUtils.WriteEscapedJavaScriptStringWithoutDelimitersAsync(writer, s, charEscapeFlags, stringEscapeHandling, client, writeBuffer, cancellationToken);
		}

		// Token: 0x0600055B RID: 1371 RVA: 0x000173A4 File Offset: 0x000155A4
		private static Task WriteEscapedJavaScriptStringWithDelimitersAsync(TextWriter writer, string s, char delimiter, bool[] charEscapeFlags, StringEscapeHandling stringEscapeHandling, JsonTextWriter client, char[] writeBuffer, CancellationToken cancellationToken)
		{
			Task task = writer.WriteAsync(delimiter, cancellationToken);
			if (!task.IsCompletedSuccessfully())
			{
				return JavaScriptUtils.WriteEscapedJavaScriptStringWithDelimitersAsync(task, writer, s, delimiter, charEscapeFlags, stringEscapeHandling, client, writeBuffer, cancellationToken);
			}
			if (!StringUtils.IsNullOrEmpty(s))
			{
				task = JavaScriptUtils.WriteEscapedJavaScriptStringWithoutDelimitersAsync(writer, s, charEscapeFlags, stringEscapeHandling, client, writeBuffer, cancellationToken);
				if (task.IsCompletedSuccessfully())
				{
					return writer.WriteAsync(delimiter, cancellationToken);
				}
			}
			return JavaScriptUtils.WriteCharAsync(task, writer, delimiter, cancellationToken);
		}

		// Token: 0x0600055C RID: 1372 RVA: 0x0001740C File Offset: 0x0001560C
		private static async Task WriteEscapedJavaScriptStringWithDelimitersAsync(Task task, TextWriter writer, string s, char delimiter, bool[] charEscapeFlags, StringEscapeHandling stringEscapeHandling, JsonTextWriter client, char[] writeBuffer, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			if (!StringUtils.IsNullOrEmpty(s))
			{
				await JavaScriptUtils.WriteEscapedJavaScriptStringWithoutDelimitersAsync(writer, s, charEscapeFlags, stringEscapeHandling, client, writeBuffer, cancellationToken).ConfigureAwait(false);
			}
			await writer.WriteAsync(delimiter).ConfigureAwait(false);
		}

		// Token: 0x0600055D RID: 1373 RVA: 0x00017494 File Offset: 0x00015694
		public static async Task WriteCharAsync(Task task, TextWriter writer, char c, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await writer.WriteAsync(c, cancellationToken).ConfigureAwait(false);
		}

		// Token: 0x0600055E RID: 1374 RVA: 0x000174F0 File Offset: 0x000156F0
		private static Task WriteEscapedJavaScriptStringWithoutDelimitersAsync(TextWriter writer, string s, bool[] charEscapeFlags, StringEscapeHandling stringEscapeHandling, JsonTextWriter client, char[] writeBuffer, CancellationToken cancellationToken)
		{
			int num = JavaScriptUtils.FirstCharToEscape(s, charEscapeFlags, stringEscapeHandling);
			if (num != -1)
			{
				return JavaScriptUtils.WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync(writer, s, num, charEscapeFlags, stringEscapeHandling, client, writeBuffer, cancellationToken);
			}
			return writer.WriteAsync(s, cancellationToken);
		}

		// Token: 0x0600055F RID: 1375 RVA: 0x00017524 File Offset: 0x00015724
		private static Task WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync(TextWriter writer, string s, int lastWritePosition, bool[] charEscapeFlags, StringEscapeHandling stringEscapeHandling, JsonTextWriter client, char[] writeBuffer, CancellationToken cancellationToken)
		{
			JavaScriptUtils.<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__16 <WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.writer = writer;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.s = s;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.lastWritePosition = lastWritePosition;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.charEscapeFlags = charEscapeFlags;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.stringEscapeHandling = stringEscapeHandling;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.client = client;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.writeBuffer = writeBuffer;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.cancellationToken = cancellationToken;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.<>1__state = -1;
			<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.<>t__builder.Start<JavaScriptUtils.<WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__16>(ref <WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__);
			return <WriteDefinitelyEscapedJavaScriptStringWithoutDelimitersAsync>d__.<>t__builder.Task;
		}

		// Token: 0x06000560 RID: 1376 RVA: 0x000175A4 File Offset: 0x000157A4
		public static bool TryGetDateFromConstructorJson(JsonReader reader, out DateTime dateTime, [Nullable(2)] [NotNullWhen(false)] out string errorMessage)
		{
			dateTime = default(DateTime);
			errorMessage = null;
			long? num;
			if (!JavaScriptUtils.TryGetDateConstructorValue(reader, out num, out errorMessage) || num == null)
			{
				errorMessage = errorMessage ?? "Date constructor has no arguments.";
				return false;
			}
			long? num2;
			if (!JavaScriptUtils.TryGetDateConstructorValue(reader, out num2, out errorMessage))
			{
				return false;
			}
			if (num2 != null)
			{
				List<long> list = new List<long> { num.Value, num2.Value };
				long? num3;
				while (JavaScriptUtils.TryGetDateConstructorValue(reader, out num3, out errorMessage))
				{
					if (num3 != null)
					{
						list.Add(num3.Value);
					}
					else
					{
						if (list.Count > 7)
						{
							errorMessage = "Unexpected number of arguments when reading date constructor.";
							return false;
						}
						while (list.Count < 7)
						{
							list.Add(0L);
						}
						dateTime = new DateTime((int)list[0], (int)list[1] + 1, (list[2] == 0L) ? 1 : ((int)list[2]), (int)list[3], (int)list[4], (int)list[5], (int)list[6]);
						return true;
					}
				}
				return false;
			}
			dateTime = DateTimeUtils.ConvertJavaScriptTicksToDateTime(num.Value);
			return true;
		}

		// Token: 0x06000561 RID: 1377 RVA: 0x000176C8 File Offset: 0x000158C8
		private static bool TryGetDateConstructorValue(JsonReader reader, out long? integer, [Nullable(2)] [NotNullWhen(false)] out string errorMessage)
		{
			integer = null;
			errorMessage = null;
			if (!reader.Read())
			{
				errorMessage = "Unexpected end when reading date constructor.";
				return false;
			}
			if (reader.TokenType == JsonToken.EndConstructor)
			{
				return true;
			}
			if (reader.TokenType != JsonToken.Integer)
			{
				errorMessage = "Unexpected token when reading date constructor. Expected Integer, got " + reader.TokenType.ToString();
				return false;
			}
			integer = new long?((long)reader.Value);
			return true;
		}

		// Token: 0x04000210 RID: 528
		internal static readonly bool[] SingleQuoteCharEscapeFlags = new bool[128];

		// Token: 0x04000211 RID: 529
		internal static readonly bool[] DoubleQuoteCharEscapeFlags = new bool[128];

		// Token: 0x04000212 RID: 530
		internal static readonly bool[] HtmlCharEscapeFlags = new bool[128];

		// Token: 0x04000213 RID: 531
		private const int UnicodeTextLength = 6;

		// Token: 0x04000214 RID: 532
		private const string EscapedUnicodeText = "!";
	}
}
