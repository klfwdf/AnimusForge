using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace System.Diagnostics.Logger;

internal class LoggerTraceListener : TraceListener
{
	private record ParseResult
	{
		public required string Process { get; init; }

		public required TraceEventType Level { get; init; }

		public required int EventId { get; init; }

		public required string Message { get; init; }

		[CompilerGenerated]
		[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
		protected ParseResult(ParseResult original)
		{
			Process = original.Process;
			Level = original.Level;
			EventId = original.EventId;
			Message = original.Message;
		}
	}

	private readonly ILogger _logger;

	public override bool IsThreadSafe => true;

	public override string Name { get; set; } = "ILogger Tracer";

	private static ParseResult? Parse(ReadOnlySpan<char> str)
	{
		try
		{
			int num = str.IndexOf(':');
			if (num == -1)
			{
				return null;
			}
			ReadOnlySpan<char> span = str.Slice(0, num);
			ReadOnlySpan<char> span2 = str.Slice(num + 1);
			int num2 = span.LastIndexOf(' ');
			if (num2 == -1)
			{
				return null;
			}
			ReadOnlySpan<char> readOnlySpan = span.Slice(0, num2);
			if (!Enum.TryParse<TraceEventType>(span.Slice(num2 + 1).ToString(), out var result))
			{
				return null;
			}
			int num3 = span2.IndexOf(':');
			if (num3 == -1)
			{
				return null;
			}
			if (!int.TryParse(span2.Slice(0, num3).ToString(), out var result2))
			{
				return null;
			}
			ReadOnlySpan<char> readOnlySpan2 = span2.Slice(num3 + 2);
			return new ParseResult
			{
				Process = readOnlySpan.ToString(),
				Level = result,
				EventId = result2,
				Message = readOnlySpan2.ToString()
			};
		}
		catch (Exception arg)
		{
			return new ParseResult
			{
				Process = "UNKNOWN",
				Level = TraceEventType.Error,
				EventId = 0,
				Message = $"Failed to parse log message: {str.ToString()}! Please report this issue to the ButterLib developers! Exception: {arg}"
			};
		}
	}

	public LoggerTraceListener(ILogger logger)
	{
		_logger = logger;
	}

	private void Log(ParseResult result)
	{
		TraceEventType level = result.Level;
		if (level <= TraceEventType.Start)
		{
			if (level <= TraceEventType.Information)
			{
				switch (level)
				{
				case TraceEventType.Critical:
					_logger.LogCritical("{Message}", result.Message);
					break;
				case TraceEventType.Error:
					_logger.LogError("{Message}", result.Message);
					break;
				case TraceEventType.Warning:
					_logger.LogWarning("{Message}", result.Message);
					break;
				case TraceEventType.Information:
					_logger.LogInformation("{Message}", result.Message);
					break;
				}
			}
			else if (level != TraceEventType.Verbose)
			{
				_ = 256;
			}
			else
			{
				_logger.LogTrace("{Message}", result.Message);
			}
		}
		else if (level <= TraceEventType.Suspend)
		{
			if (level != TraceEventType.Stop)
			{
				_ = 1024;
			}
		}
		else if (level != TraceEventType.Resume)
		{
			_ = 4096;
		}
	}

	public override void Write(string? message)
	{
		ParseResult parseResult = Parse(message.AsSpan());
		if ((object)parseResult != null)
		{
			Log(parseResult);
			return;
		}
		_logger.LogInformation("{Message}", message);
	}

	public override void WriteLine(string? message)
	{
		ParseResult parseResult = Parse(message.AsSpan());
		if ((object)parseResult != null)
		{
			Log(parseResult);
			return;
		}
		_logger.LogInformation("{Message}", message);
	}
}
