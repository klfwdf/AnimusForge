using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MCM.UI.ButterLib
{
	// Token: 0x02000032 RID: 50
	[NullableContext(1)]
	[Nullable(0)]
	internal class LogValuesFormatter
	{
		// Token: 0x060001B3 RID: 435 RVA: 0x00007A94 File Offset: 0x00005C94
		public LogValuesFormatter(string format)
		{
			this.OriginalFormat = format;
			StringBuilder stringBuilder = new StringBuilder();
			int startIndex = 0;
			int length = format.Length;
			while (startIndex < length)
			{
				int braceIndex = LogValuesFormatter.FindBraceIndex(format, '{', startIndex, length);
				int braceIndex2 = LogValuesFormatter.FindBraceIndex(format, '}', braceIndex, length);
				int indexOf = LogValuesFormatter.FindIndexOf(format, ',', braceIndex, braceIndex2);
				if (indexOf == braceIndex2)
				{
					indexOf = LogValuesFormatter.FindIndexOf(format, ':', braceIndex, braceIndex2);
				}
				if (braceIndex2 == length)
				{
					stringBuilder.Append(format, startIndex, length - startIndex);
					startIndex = length;
				}
				else
				{
					stringBuilder.Append(format, startIndex, braceIndex - startIndex + 1);
					stringBuilder.Append(this._valueNames.Count.ToString(CultureInfo.InvariantCulture));
					this._valueNames.Add(format.Substring(braceIndex + 1, indexOf - braceIndex - 1));
					stringBuilder.Append(format, indexOf, braceIndex2 - indexOf + 1);
					startIndex = braceIndex2 + 1;
				}
			}
			this._format = stringBuilder.ToString();
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060001B4 RID: 436 RVA: 0x00007B8A File Offset: 0x00005D8A
		// (set) Token: 0x060001B5 RID: 437 RVA: 0x00007B92 File Offset: 0x00005D92
		public string OriginalFormat { get; private set; }

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060001B6 RID: 438 RVA: 0x00007B9B File Offset: 0x00005D9B
		public List<string> ValueNames
		{
			get
			{
				return this._valueNames;
			}
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x00007BA4 File Offset: 0x00005DA4
		private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
		{
			int braceIndex = endIndex;
			int index = startIndex;
			int num = 0;
			while (index < endIndex)
			{
				if (num > 0 && format[index] != brace)
				{
					if (num % 2 != 0)
					{
						break;
					}
					num = 0;
					braceIndex = endIndex;
				}
				else if (format[index] == brace)
				{
					if (brace == '}')
					{
						if (num == 0)
						{
							braceIndex = index;
						}
					}
					else
					{
						braceIndex = index;
					}
					num++;
				}
				index++;
			}
			return braceIndex;
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x00007BF8 File Offset: 0x00005DF8
		private static int FindIndexOf(string format, char ch, int startIndex, int endIndex)
		{
			int num = format.IndexOf(ch, startIndex, endIndex - startIndex);
			if (num == -1)
			{
				return endIndex;
			}
			return num;
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x00007C18 File Offset: 0x00005E18
		public string Format([Nullable(new byte[] { 2, 1 })] object[] values)
		{
			if (values != null)
			{
				for (int index = 0; index < values.Length; index++)
				{
					object obj = values[index];
					if (obj != null)
					{
						IEnumerable source = obj as IEnumerable;
						if (source != null)
						{
							values[index] = string.Join<object>(", ", from object o in source
								select o ?? "(null)");
						}
					}
					else
					{
						values[index] = "(null)";
					}
				}
			}
			return string.Format(CultureInfo.InvariantCulture, this._format, values ?? LogValuesFormatter.EmptyArray);
		}

		// Token: 0x060001BA RID: 442 RVA: 0x00007CA4 File Offset: 0x00005EA4
		[return: Nullable(new byte[] { 0, 1, 1 })]
		public KeyValuePair<string, object> GetValue(object[] values, int index)
		{
			if (index < 0 || index > this._valueNames.Count)
			{
				throw new IndexOutOfRangeException("index");
			}
			if (this._valueNames.Count <= index)
			{
				return new KeyValuePair<string, object>("{OriginalFormat}", this.OriginalFormat);
			}
			return new KeyValuePair<string, object>(this._valueNames[index], values[index]);
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00007D04 File Offset: 0x00005F04
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		public IEnumerable<KeyValuePair<string, object>> GetValues(object[] values)
		{
			KeyValuePair<string, object>[] values2 = new KeyValuePair<string, object>[values.Length + 1];
			for (int index = 0; index != this._valueNames.Count; index++)
			{
				values2[index] = new KeyValuePair<string, object>(this._valueNames[index], values[index]);
			}
			values2[values2.Length - 1] = new KeyValuePair<string, object>("{OriginalFormat}", this.OriginalFormat);
			return values2;
		}

		// Token: 0x0400006F RID: 111
		private const string NullValue = "(null)";

		// Token: 0x04000070 RID: 112
		private static readonly object[] EmptyArray = Array.Empty<object>();

		// Token: 0x04000071 RID: 113
		private readonly string _format;

		// Token: 0x04000072 RID: 114
		private readonly List<string> _valueNames = new List<string>();
	}
}
