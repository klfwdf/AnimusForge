using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MCM.UI.ButterLib;

internal class LogValuesFormatter
{
	private const string NullValue = "(null)";

	private static readonly object[] EmptyArray = Array.Empty<object>();

	private readonly string _format;

	private readonly List<string> _valueNames = new List<string>();

	public string OriginalFormat { get; private set; }

	public List<string> ValueNames => _valueNames;

	public LogValuesFormatter(string format)
	{
		OriginalFormat = format;
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		int length = format.Length;
		while (num < length)
		{
			int num2 = FindBraceIndex(format, '{', num, length);
			int num3 = FindBraceIndex(format, '}', num2, length);
			int num4 = FindIndexOf(format, ',', num2, num3);
			if (num4 == num3)
			{
				num4 = FindIndexOf(format, ':', num2, num3);
			}
			if (num3 == length)
			{
				stringBuilder.Append(format, num, length - num);
				num = length;
				continue;
			}
			stringBuilder.Append(format, num, num2 - num + 1);
			stringBuilder.Append(_valueNames.Count.ToString(CultureInfo.InvariantCulture));
			_valueNames.Add(format.Substring(num2 + 1, num4 - num2 - 1));
			stringBuilder.Append(format, num4, num3 - num4 + 1);
			num = num3 + 1;
		}
		_format = stringBuilder.ToString();
	}

	private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
	{
		int result = endIndex;
		int i = startIndex;
		int num = 0;
		for (; i < endIndex; i++)
		{
			if (num > 0 && format[i] != brace)
			{
				if (num % 2 != 0)
				{
					break;
				}
				num = 0;
				result = endIndex;
			}
			else
			{
				if (format[i] != brace)
				{
					continue;
				}
				if (brace == '}')
				{
					if (num == 0)
					{
						result = i;
					}
				}
				else
				{
					result = i;
				}
				num++;
			}
		}
		return result;
	}

	private static int FindIndexOf(string format, char ch, int startIndex, int endIndex)
	{
		int num = format.IndexOf(ch, startIndex, endIndex - startIndex);
		if (num == -1)
		{
			return endIndex;
		}
		return num;
	}

	public string Format(object[]? values)
	{
		if (values != null)
		{
			for (int i = 0; i < values.Length; i++)
			{
				object obj = values[i];
				if (obj != null)
				{
					if (obj is IEnumerable source)
					{
						values[i] = string.Join(", ", from object o in source
							select o ?? "(null)");
					}
				}
				else
				{
					values[i] = "(null)";
				}
			}
		}
		return string.Format(CultureInfo.InvariantCulture, _format, values ?? EmptyArray);
	}

	public KeyValuePair<string, object> GetValue(object[] values, int index)
	{
		if (index < 0 || index > _valueNames.Count)
		{
			throw new IndexOutOfRangeException("index");
		}
		if (_valueNames.Count <= index)
		{
			return new KeyValuePair<string, object>("{OriginalFormat}", OriginalFormat);
		}
		return new KeyValuePair<string, object>(_valueNames[index], values[index]);
	}

	public IEnumerable<KeyValuePair<string, object>> GetValues(object[] values)
	{
		KeyValuePair<string, object>[] array = new KeyValuePair<string, object>[values.Length + 1];
		for (int i = 0; i != _valueNames.Count; i++)
		{
			array[i] = new KeyValuePair<string, object>(_valueNames[i], values[i]);
		}
		array[^1] = new KeyValuePair<string, object>("{OriginalFormat}", OriginalFormat);
		return array;
	}
}
