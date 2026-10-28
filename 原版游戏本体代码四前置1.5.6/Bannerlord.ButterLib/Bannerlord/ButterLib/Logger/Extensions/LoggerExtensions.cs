using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using TaleWorlds.Library;

namespace Bannerlord.ButterLib.Logger.Extensions;

public static class LoggerExtensions
{
	private class LogValuesFormatter
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
			array[array.Length - 1] = new KeyValuePair<string, object>("{OriginalFormat}", OriginalFormat);
			return array;
		}
	}

	private class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object>>, IReadOnlyCollection<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable
	{
		internal const int MaxCachedFormatters = 1024;

		private const string NullFormat = "[null]";

		private static int _count;

		private static ConcurrentDictionary<string, LogValuesFormatter> _formatters = new ConcurrentDictionary<string, LogValuesFormatter>();

		private readonly LogValuesFormatter? _formatter;

		private readonly object[]? _values;

		private readonly string _originalMessage;

		internal LogValuesFormatter? Formatter => _formatter;

		public KeyValuePair<string, object> this[int index]
		{
			get
			{
				if (index < 0 || index >= Count)
				{
					throw new IndexOutOfRangeException("index");
				}
				if (index != Count - 1)
				{
					return _formatter.GetValue(_values, index);
				}
				return new KeyValuePair<string, object>("{OriginalFormat}", _originalMessage);
			}
		}

		public int Count
		{
			get
			{
				if (_formatter != null)
				{
					return _formatter.ValueNames.Count + 1;
				}
				return 1;
			}
		}

		public FormattedLogValues(string? format, params object[]? values)
		{
			if ((values == null || values.Length != 0) && format != null)
			{
				if (_count >= 1024)
				{
					if (!_formatters.TryGetValue(format, out _formatter))
					{
						_formatter = new LogValuesFormatter(format);
					}
				}
				else
				{
					_formatter = _formatters.GetOrAdd(format, delegate(string f)
					{
						Interlocked.Increment(ref _count);
						return new LogValuesFormatter(f);
					});
				}
			}
			_originalMessage = format ?? "[null]";
			_values = values;
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			int i = 0;
			while (i < Count)
			{
				yield return this[i];
				int num = i + 1;
				i = num;
			}
		}

		public override string ToString()
		{
			if (_formatter != null)
			{
				return _formatter.Format(_values);
			}
			return _originalMessage;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	private static string MessageFormatter(object state, Exception error)
	{
		return state.ToString() ?? string.Empty;
	}

	public static void LogAndDisplay(this ILogger logger, LogLevel logLevel, string message, params object[] args)
	{
		logger.LogAndDisplay(logLevel, null, message, args);
	}

	public static void LogAndDisplay(this ILogger logger, LogLevel logLevel, Exception exception, string message, params object[] args)
	{
		FormattedLogValues formattedLogValues = new FormattedLogValues(message, args);
		logger.Log(logLevel, 0, formattedLogValues, exception, MessageFormatter);
		string information = $"{logLevel}: {formattedLogValues}";
		InformationManager.DisplayMessage(new InformationMessage(information, logLevel switch
		{
			LogLevel.Trace => Color.FromUint(16777215u), 
			LogLevel.Debug => Color.FromUint(8421504u), 
			LogLevel.Information => Color.FromUint(32768u), 
			LogLevel.Warning => Color.FromUint(16744448u), 
			LogLevel.Error => Color.FromUint(16711680u), 
			LogLevel.Critical => Color.FromUint(9109504u), 
			LogLevel.None => Color.White, 
			_ => throw new ArgumentOutOfRangeException("logLevel", logLevel, null), 
		}));
	}

	public static void LogTraceAndDisplay(this ILogger logger, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Trace, null, message, args);
	}

	public static void LogDebugAndDisplay(this ILogger logger, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Debug, null, message, args);
	}

	public static void LogInformationAndDisplay(this ILogger logger, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Information, null, message, args);
	}

	public static void LogWarningAndDisplay(this ILogger logger, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Warning, null, message, args);
	}

	public static void LogErrorAndDisplay(this ILogger logger, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Error, null, message, args);
	}

	public static void LogCriticalAndDisplay(this ILogger logger, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Critical, null, message, args);
	}

	public static void LogTraceAndDisplay(this ILogger logger, Exception exception, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Trace, exception, message, args);
	}

	public static void LogDebugAndDisplay(this ILogger logger, Exception exception, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Debug, exception, message, args);
	}

	public static void LogInformationAndDisplay(this ILogger logger, Exception exception, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Information, exception, message, args);
	}

	public static void LogWarningAndDisplay(this ILogger logger, Exception exception, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Warning, exception, message, args);
	}

	public static void LogErrorAndDisplay(this ILogger logger, Exception exception, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Error, exception, message, args);
	}

	public static void LogCriticalAndDisplay(this ILogger logger, Exception exception, string message, params object[] args)
	{
		logger.LogAndDisplay(LogLevel.Critical, exception, message, args);
	}
}
