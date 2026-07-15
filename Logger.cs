#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MCM.Abstractions.Base.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Engine;

namespace AnimusForge;

public static class Logger
{
	private sealed class TraceScopeState
	{
		public string TraceId;

		public string Channel;

		public string HeroId;

		public string NpcName;

		public long StartUtcTicks;

		public TraceScopeState Parent;
	}

	private sealed class TraceScope : IDisposable
	{
		private readonly TraceScopeState _prev;

		private readonly TraceScopeState _current;

		private bool _disposed;

		public TraceScope(TraceScopeState prev, TraceScopeState current)
		{
			_prev = prev;
			_current = current;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			try
			{
				long ticks = DateTime.UtcNow.Ticks;
				double value = 0.0;
				if (_current != null && _current.StartUtcTicks > 0)
				{
					value = Math.Max(0.0, TimeSpan.FromTicks(ticks - _current.StartUtcTicks).TotalMilliseconds);
				}
				if (_current != null)
				{
					Obs("Trace", "end", new Dictionary<string, object> { ["elapsedMs"] = Math.Round(value, 2) });
				}
			}
			catch
			{
			}
			_traceState.Value = _prev;
		}
	}

	private sealed class MetricBucket
	{
		public long Count;

		public long Ok;

		public long Err;

		public double SumMs;

		public double MaxMs;
	}

	private sealed class HitRateBucket
	{
		public long Total;

		public long Hit;
	}

	private sealed class TokenStatsWorkItem
	{
		public bool IsMessageDump;

		public int InputTokens;

		public int OutputTokens;

		public List<object> Messages;

		public string OutputContent;

		public string Mode;

		public string RequestBody;

		public string Title;

		public string TimeText;

		public string TraceId;
	}

	private sealed class LogWriteWorkItem
	{
		public string Path;

		public string Content;

		public bool IsVerbose;
	}

	private static string _modLogPath;

	private static string _gameTracePath;

	private static string _obsLogPath;

	private static string _hitRatePath;

	private static string _tokenStatsPath;

	private static string _eventLogsPath;

	private static string _compatibilityAuditPath;

	private static readonly object _fileLock;

	private static readonly AsyncLocal<TraceScopeState> _traceState;

	private static readonly object _metricsLock;

	private static readonly Dictionary<string, MetricBucket> _metrics;

	private static readonly object _hitRateLock;

	private static readonly object _verboseLogThrottleLock;

	private static readonly UTF8Encoding _utf8WithBom;

	private static readonly Dictionary<string, HitRateBucket> _hitRate;

	private static readonly Dictionary<string, long> _hitRateScopeSeq;

	private static readonly Dictionary<string, long> _hitRateActiveQuery;

	private static readonly Dictionary<string, long> _verboseLogNextAllowedTicks;

	private static readonly ConcurrentQueue<TokenStatsWorkItem> _tokenStatsWriteQueue;

	private static readonly ConcurrentQueue<LogWriteWorkItem> _logWriteQueue;

	private static DateTime _metricsWindowStartUtc;

	private static DateTime _nextMetricsFlushUtc;

	private static long _traceSeed;

	private static long _hitRateEventSeed;

	private static long _droppedVerboseLogCount;

	private static long _droppedNormalLogCount;

	private static long _lastDroppedLogSummaryUtcTicks;

	private const int MetricsFlushIntervalSeconds = 180;

	private const int LogCleanupCheckIntervalSeconds = 5;

	private const int MaxLogWriteQueueItems = 4096;

	private const int HardMaxLogWriteQueueItems = 8192;

	private const int LogBatchFlushItemCount = 256;

	private const int DroppedLogSummaryIntervalSeconds = 10;

	private static string _lastLogCleanupSelection;

	private static DateTime _nextLogCleanupUtc;

	private static DateTime _nextLogCleanupCheckUtc;

	private static bool _startupLogCleanupDone;

	private static int _tokenStatsWorkerRunning;

	private static int _logWriterRunning;

	private static int _logWriteQueueCount;

	public static string CurrentTraceId => _traceState.Value?.TraceId ?? "";

	public static string CurrentChannel => _traceState.Value?.Channel ?? "";

	public static bool IsModLogicEnabled => IsPathEnabled(_modLogPath);

	internal static string GetFreezeWatchdogDiagnosticSnapshot()
	{
		try
		{
			return "logQueue=" + Volatile.Read(ref _logWriteQueueCount)
				+ " tokenQueue=" + _tokenStatsWriteQueue.Count
				+ " logWriter=" + Volatile.Read(ref _logWriterRunning)
				+ " tokenWriter=" + Volatile.Read(ref _tokenStatsWorkerRunning)
				+ " droppedVerbose=" + Interlocked.Read(ref _droppedVerboseLogCount)
				+ " droppedNormal=" + Interlocked.Read(ref _droppedNormalLogCount);
		}
		catch
		{
			return "logger=unavailable";
		}
	}

	public static bool IsVerboseModLogicEnabled
	{
		get
		{
			try
			{
				if (!IsModLogicEnabled)
				{
					return false;
				}
				return TryGetSettings()?.EnableVerboseModLogicLog == true;
			}
			catch
			{
				return false;
			}
		}
	}

	static Logger()
	{
		_fileLock = new object();
		_traceState = new AsyncLocal<TraceScopeState>();
		_metricsLock = new object();
		_metrics = new Dictionary<string, MetricBucket>(StringComparer.Ordinal);
		_hitRateLock = new object();
		_verboseLogThrottleLock = new object();
		_utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
		_hitRate = new Dictionary<string, HitRateBucket>(StringComparer.OrdinalIgnoreCase);
		_hitRateScopeSeq = new Dictionary<string, long>(StringComparer.Ordinal);
		_hitRateActiveQuery = new Dictionary<string, long>(StringComparer.Ordinal);
		_verboseLogNextAllowedTicks = new Dictionary<string, long>(StringComparer.Ordinal);
		_tokenStatsWriteQueue = new ConcurrentQueue<TokenStatsWorkItem>();
		_logWriteQueue = new ConcurrentQueue<LogWriteWorkItem>();
		_metricsWindowStartUtc = DateTime.UtcNow;
		_nextMetricsFlushUtc = DateTime.UtcNow.AddSeconds(180.0);
		_traceSeed = 0L;
		_hitRateEventSeed = 0L;
		try
		{
			string text = AnimusForgeModulePaths.GetLogsDirectory();
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			_modLogPath = System.IO.Path.Combine(text, "Mod_Logic.txt");
			_gameTracePath = System.IO.Path.Combine(text, "Game_Trace.txt");
			_obsLogPath = System.IO.Path.Combine(text, "Observability.jsonl");
			_hitRatePath = System.IO.Path.Combine(text, "HitRate_Stats.txt");
			_tokenStatsPath = System.IO.Path.Combine(text, "Token_Stats.txt");
			_eventLogsPath = System.IO.Path.Combine(text, "Event_Logs.txt");
			_compatibilityAuditPath = System.IO.Path.Combine(text, "Compatibility_Audit.txt");
			EnsureUtf8Bom(_modLogPath);
			EnsureUtf8Bom(_gameTracePath);
			EnsureUtf8Bom(_obsLogPath);
			EnsureUtf8Bom(_hitRatePath);
			EnsureUtf8Bom(_tokenStatsPath);
			EnsureUtf8Bom(_eventLogsPath);
			EnsureUtf8Bom(_compatibilityAuditPath);
			string contents = $"\n\n====== 游戏启动 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ======\n";
			if (IsPathEnabled(_modLogPath))
			{
				AppendUtf8(_modLogPath, contents);
			}
			if (IsPathEnabled(_gameTracePath))
			{
				AppendUtf8(_gameTracePath, contents);
			}
			if (IsPathEnabled(_hitRatePath))
			{
				AppendUtf8(_hitRatePath, contents);
			}
			if (IsPathEnabled(_tokenStatsPath))
			{
				AppendUtf8(_tokenStatsPath, contents);
			}
			if (IsPathEnabled(_eventLogsPath))
			{
				AppendUtf8(_eventLogsPath, contents);
			}
			if (IsPathEnabled(_compatibilityAuditPath))
			{
				AppendUtf8(_compatibilityAuditPath, contents);
			}
			if (IsPathEnabled(_obsLogPath))
			{
				AppendUtf8(_obsLogPath, JsonConvert.SerializeObject(new Dictionary<string, object>
				{
					["ts"] = DateTime.UtcNow.ToString("o"),
					["type"] = "boot",
					["message"] = "AnimusForge logger initialized"
				}) + "\n");
			}
		}
		catch (Exception ex)
		{
			try
			{
				Debug.Print("[Logger Error] " + ex.Message);
			}
			catch
			{
			}
		}
	}

	public static IDisposable BeginTrace(string channel, string heroId = null, string npcName = null, string traceId = null)
	{
		TraceScopeState value = _traceState.Value;
		string text = (traceId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ((value == null || string.IsNullOrWhiteSpace(value.TraceId)) ? NewTraceId() : value.TraceId);
		}
		TraceScopeState traceScopeState = new TraceScopeState
		{
			TraceId = text,
			Channel = (channel ?? value?.Channel ?? "").Trim(),
			HeroId = (heroId ?? value?.HeroId ?? "").Trim(),
			NpcName = (npcName ?? value?.NpcName ?? "").Trim(),
			StartUtcTicks = DateTime.UtcNow.Ticks,
			Parent = value
		};
		_traceState.Value = traceScopeState;
		Obs("Trace", "start", new Dictionary<string, object>
		{
			["channel"] = traceScopeState.Channel,
			["heroId"] = traceScopeState.HeroId,
			["npcName"] = traceScopeState.NpcName
		});
		return new TraceScope(value, traceScopeState);
	}

	public static void Log(string source, string message)
	{
		if (ShouldRouteModLogicLogToVerbose(source, message))
		{
			if (!IsVerboseModLogicEnabled)
			{
				return;
			}
			WriteHumanLine(_modLogPath, source, message, isVerbose: true);
			return;
		}
		WriteHumanLine(_modLogPath, source, message);
	}

	public static void LogImmediate(string source, string message)
	{
		try
		{
			if (ShouldRouteModLogicLogToVerbose(source, message) && !IsVerboseModLogicEnabled)
			{
				return;
			}
			if (string.IsNullOrWhiteSpace(_modLogPath) || !IsPathEnabled(_modLogPath))
			{
				return;
			}
			string text = DateTime.Now.ToString("HH:mm:ss");
			string currentTraceId = CurrentTraceId;
			string text2 = string.IsNullOrWhiteSpace(currentTraceId) ? "" : (" [trace=" + currentTraceId + "]");
			lock (_fileLock)
			{
				AppendUtf8(_modLogPath, "[" + text + "] [" + source + "]" + text2 + " " + (message ?? "") + "\n");
			}
		}
		catch
		{
		}
	}

	public static void WriteLogSnapshotImmediate(string fileName, string content)
	{
		try
		{
			string safeFileName = System.IO.Path.GetFileName((fileName ?? "").Trim());
			if (string.IsNullOrWhiteSpace(safeFileName))
			{
				safeFileName = "AnimusForge_LastCheckpoint.txt";
			}
			string path = AnimusForgeModulePaths.GetLogFilePath(safeFileName);
			string directory = System.IO.Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			lock (_fileLock)
			{
				File.WriteAllText(path, content ?? "", _utf8WithBom);
			}
		}
		catch
		{
		}
	}

	public static void LogLazy(string source, Func<string> messageFactory)
	{
		try
		{
			if (!IsModLogicEnabled)
			{
				return;
			}
			WriteHumanLine(_modLogPath, source, messageFactory?.Invoke() ?? "");
		}
		catch
		{
		}
	}

	public static void LogVerbose(string source, string key, Func<string> messageFactory, double minIntervalSeconds = 0.0)
	{
		try
		{
			if (!IsVerboseModLogicEnabled)
			{
				return;
			}
			if (!CanWriteVerboseLog(source, key, minIntervalSeconds))
			{
				return;
			}
			WriteHumanLine(_modLogPath, source, messageFactory?.Invoke() ?? "", isVerbose: true);
		}
		catch
		{
		}
	}

	public static void LogTrace(string source, string message)
	{
		WriteHumanLine(_gameTracePath, source, message);
	}

	public static void LogCompatibilityAudit(string source, string message)
	{
		WriteHumanLine(_compatibilityAuditPath, source, message);
	}

	public static void LogEvent(string source, string message)
	{
		WriteHumanLine(_eventLogsPath, source, message);
	}

	public static void LogToFile(string fileName, string content, bool isVerbose = false)
	{
		try
		{
			string safeFileName = System.IO.Path.GetFileName((fileName ?? "").Trim());
			if (string.IsNullOrWhiteSpace(safeFileName) || content == null)
			{
				return;
			}
			string path = AnimusForgeModulePaths.GetLogFilePath(safeFileName);
			if (string.IsNullOrWhiteSpace(path) || !IsPathEnabled(path))
			{
				return;
			}
			EnqueueLogWrite(path, content, isVerbose);
		}
		catch
		{
		}
	}

	public static void LogEventPromptExchange(string targetLabel, string requestText, string replyText)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_eventLogsPath) || !IsPathEnabled(_eventLogsPath))
			{
				return;
			}
			string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			string currentTraceId = CurrentTraceId;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("==================================================");
			stringBuilder.AppendLine("[时间] " + text);
			if (!string.IsNullOrWhiteSpace(currentTraceId))
			{
				stringBuilder.AppendLine("[Trace] " + currentTraceId);
			}
			if (!string.IsNullOrWhiteSpace(targetLabel))
			{
				stringBuilder.AppendLine("[目标] " + targetLabel.Trim());
			}
			stringBuilder.AppendLine("[请求]");
			stringBuilder.AppendLine((requestText ?? "").Trim());
			stringBuilder.AppendLine("[回复]");
			stringBuilder.AppendLine((replyText ?? "").Trim());
			stringBuilder.AppendLine();
			EnqueueLogWrite(_eventLogsPath, stringBuilder.ToString());
		}
		catch
		{
		}
	}

	public static void Obs(string source, string stage, Dictionary<string, object> fields = null)
	{
		try
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.Ordinal)
			{
				["ts"] = DateTime.UtcNow.ToString("o"),
				["source"] = source ?? "",
				["stage"] = stage ?? ""
			};
			TraceScopeState value = _traceState.Value;
			if (value != null)
			{
				if (!string.IsNullOrWhiteSpace(value.TraceId))
				{
					dictionary["traceId"] = value.TraceId;
				}
				if (!string.IsNullOrWhiteSpace(value.Channel))
				{
					dictionary["channel"] = value.Channel;
				}
				if (!string.IsNullOrWhiteSpace(value.HeroId))
				{
					dictionary["heroId"] = value.HeroId;
				}
				if (!string.IsNullOrWhiteSpace(value.NpcName))
				{
					dictionary["npcName"] = value.NpcName;
				}
			}
			if (fields != null)
			{
				foreach (KeyValuePair<string, object> field in fields)
				{
					if (!string.IsNullOrWhiteSpace(field.Key))
					{
						dictionary[field.Key] = field.Value;
					}
				}
			}
			string line = JsonConvert.SerializeObject(dictionary);
			WriteRawLine(_obsLogPath, line);
			MaybeFlushMetrics();
		}
		catch
		{
		}
	}

	public static void Metric(string metric, bool ok = true, double latencyMs = -1.0)
	{
		try
		{
			string text = (metric ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			lock (_metricsLock)
			{
				if (!_metrics.TryGetValue(text, out var value) || value == null)
				{
					value = new MetricBucket();
					_metrics[text] = value;
				}
				value.Count++;
				if (ok)
				{
					value.Ok++;
				}
				else
				{
					value.Err++;
				}
				if (latencyMs >= 0.0)
				{
					value.SumMs += latencyMs;
					if (latencyMs > value.MaxMs)
					{
						value.MaxMs = latencyMs;
					}
				}
			}
			MaybeFlushMetrics();
		}
		catch
		{
		}
	}

	public static void RecordHitRate(string domain, string tag, bool hit, string detail = null, string inputText = null)
	{
		try
		{
			string text = (domain ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "unknown";
			}
			string text2 = (tag ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "__unknown__";
			}
			string text3 = CurrentTraceId;
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "__notrace__";
			}
			string key = text + "|" + text2;
			string key2 = text + "|__all__";
			string text4 = inputText ?? "";
			string text5 = text3 + "|" + text;
			string key3 = text5 + "|" + text4;
			long total;
			long hit2;
			double num;
			long total2;
			long hit3;
			double num2;
			long value3;
			lock (_hitRateLock)
			{
				if (!_hitRate.TryGetValue(key, out var value) || value == null)
				{
					value = new HitRateBucket();
					_hitRate[key] = value;
				}
				value.Total++;
				if (hit)
				{
					value.Hit++;
				}
				total = value.Total;
				hit2 = value.Hit;
				num = ((total > 0) ? ((double)hit2 / (double)total * 100.0) : 0.0);
				if (!_hitRate.TryGetValue(key2, out var value2) || value2 == null)
				{
					value2 = new HitRateBucket();
					_hitRate[key2] = value2;
				}
				value2.Total++;
				if (hit)
				{
					value2.Hit++;
				}
				total2 = value2.Total;
				hit3 = value2.Hit;
				num2 = ((total2 > 0) ? ((double)hit3 / (double)total2 * 100.0) : 0.0);
				if (!_hitRateActiveQuery.TryGetValue(key3, out value3) || value3 <= 0)
				{
					long value4 = 0L;
					_hitRateScopeSeq.TryGetValue(text5, out value4);
					value3 = value4 + 1;
					_hitRateScopeSeq[text5] = value3;
					_hitRateActiveQuery[key3] = value3;
				}
				if (string.Equals(text2, "__query__", StringComparison.OrdinalIgnoreCase))
				{
					_hitRateActiveQuery.Remove(key3);
				}
			}
			long num3 = Interlocked.Increment(ref _hitRateEventSeed);
			string arg = text3 + "/" + text + "/" + value3;
			string text6 = $"eventId={num3} queryNo={value3} queryId={arg} " + $"domain={text} tag={text2} hit={hit}";
			if (!string.IsNullOrWhiteSpace(detail))
			{
				text6 = text6 + " " + detail.Trim();
			}
			if (!string.IsNullOrWhiteSpace(inputText))
			{
				text6 = text6 + " input=" + JsonConvert.ToString(inputText);
			}
			text6 = text6 + $" total={total} hits={hit2} rate={num:0.00}%" + $" domainTotal={total2} domainHits={hit3} domainRate={num2:0.00}%";
			WriteHumanLine(_hitRatePath, "HITRATE", text6);
		}
		catch
		{
		}
	}

	public static int EstimateTokens(string text)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return 0;
			}
			int num = 0;
			int num2 = 0;
			foreach (char c in text)
			{
				if (char.IsWhiteSpace(c))
				{
					if (num2 > 0)
					{
						num += (num2 + 3) / 4;
						num2 = 0;
					}
				}
				else if (IsCjk(c))
				{
					if (num2 > 0)
					{
						num += (num2 + 3) / 4;
						num2 = 0;
					}
					num++;
				}
				else if (c <= '\u007f' && char.IsLetterOrDigit(c))
				{
					num2++;
				}
				else
				{
					if (num2 > 0)
					{
						num += (num2 + 3) / 4;
						num2 = 0;
					}
					num++;
				}
			}
			if (num2 > 0)
			{
				num += (num2 + 3) / 4;
			}
			return Math.Max(0, num);
		}
		catch
		{
			return 0;
		}
	}

	public static int EstimateTokensFromMessages(IEnumerable<object> messages)
	{
		try
		{
			if (messages == null)
			{
				return 0;
			}
			int num = 0;
			foreach (object message in messages)
			{
				if (TryGetMessageRoleAndContent(message, out var role, out var content))
				{
					num += 4;
					num += EstimateTokens(role);
					num += EstimateTokens(content);
				}
			}
			num += 2;
			return Math.Max(0, num);
		}
		catch
		{
			return 0;
		}
	}

	public static void RecordTokenStats(int inputTokens, int outputTokens, IEnumerable<object> messages = null, string outputContent = null, string mode = null, string requestBody = null)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_tokenStatsPath) || !IsPathEnabled(_tokenStatsPath))
			{
				return;
			}
			if (inputTokens < 0)
			{
				inputTokens = 0;
			}
			if (outputTokens < 0)
			{
				outputTokens = 0;
			}
			EnqueueTokenStatsWrite(new TokenStatsWorkItem
			{
				IsMessageDump = false,
				InputTokens = inputTokens,
				OutputTokens = outputTokens,
				Messages = CopyMessagesForTokenStats(messages),
				OutputContent = outputContent,
				Mode = mode,
				RequestBody = requestBody,
				TimeText = DateTime.Now.ToString("HH:mm:ss"),
				TraceId = CurrentTraceId
			});
		}
		catch
		{
		}
	}

	public static void RecordMessageDump(string title, IEnumerable<object> messages, string mode = null)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_tokenStatsPath) || !IsPathEnabled(_tokenStatsPath))
			{
				return;
			}
			EnqueueTokenStatsWrite(new TokenStatsWorkItem
			{
				IsMessageDump = true,
				Messages = CopyMessagesForTokenStats(messages),
				Mode = mode,
				Title = title,
				TimeText = DateTime.Now.ToString("HH:mm:ss"),
				TraceId = CurrentTraceId
			});
		}
		catch
		{
		}
	}

	private static List<object> CopyMessagesForTokenStats(IEnumerable<object> messages)
	{
		try
		{
			return messages == null ? null : new List<object>(messages);
		}
		catch
		{
			return null;
		}
	}

	private static void EnqueueTokenStatsWrite(TokenStatsWorkItem item)
	{
		if (item == null)
		{
			return;
		}
		_tokenStatsWriteQueue.Enqueue(item);
		TryStartTokenStatsWriter();
	}

	private static void TryStartTokenStatsWriter()
	{
		if (Interlocked.CompareExchange(ref _tokenStatsWorkerRunning, 1, 0) != 0)
		{
			return;
		}
		Task.Run(ProcessTokenStatsWriteQueue);
	}

	private static void ProcessTokenStatsWriteQueue()
	{
		try
		{
			while (true)
			{
				while (_tokenStatsWriteQueue.TryDequeue(out var item))
				{
					WriteTokenStatsWorkItem(item);
				}
				Interlocked.Exchange(ref _tokenStatsWorkerRunning, 0);
				if (_tokenStatsWriteQueue.IsEmpty || Interlocked.CompareExchange(ref _tokenStatsWorkerRunning, 1, 0) != 0)
				{
					break;
				}
			}
		}
		catch
		{
			Interlocked.Exchange(ref _tokenStatsWorkerRunning, 0);
			if (!_tokenStatsWriteQueue.IsEmpty)
			{
				TryStartTokenStatsWriter();
			}
		}
	}

	private static void WriteTokenStatsWorkItem(TokenStatsWorkItem item)
	{
		try
		{
			if (item == null || string.IsNullOrWhiteSpace(_tokenStatsPath) || !IsPathEnabled(_tokenStatsPath))
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (item.IsMessageDump)
			{
				string text = BuildMessagesDump(item.Messages);
				if (string.IsNullOrWhiteSpace(text))
				{
					return;
				}
				string traceText = string.IsNullOrWhiteSpace(item.TraceId) ? "" : (" trace=" + item.TraceId);
				string modeText = string.IsNullOrWhiteSpace(item.Mode) ? "" : (" mode=" + item.Mode.Trim());
				string titleText = string.IsNullOrWhiteSpace(item.Title) ? "" : (" title=" + item.Title.Trim());
				stringBuilder.AppendLine($"[{item.TimeText}] STRICT_MESSAGES{traceText}{modeText}{titleText}");
				stringBuilder.AppendLine(text);
				stringBuilder.AppendLine("----");
			}
			else
			{
				string traceText2 = string.IsNullOrWhiteSpace(item.TraceId) ? "" : (" trace=" + item.TraceId);
				string modeText2 = string.IsNullOrWhiteSpace(item.Mode) ? "" : (" mode=" + item.Mode.Trim());
				string value = $"[{item.TimeText}] in={item.InputTokens} out={item.OutputTokens}{traceText2}{modeText2}";
				string requestBodyText = NormalizeTokenContent(item.RequestBody);
				string messagesText = BuildMessagesDump(item.Messages);
				string outputText = NormalizeTokenContent(item.OutputContent);
				stringBuilder.AppendLine(value);
				if (!string.IsNullOrWhiteSpace(requestBodyText))
				{
					stringBuilder.AppendLine("REQUEST_BODY:");
					stringBuilder.AppendLine(requestBodyText);
				}
				if (!string.IsNullOrWhiteSpace(messagesText))
				{
					stringBuilder.AppendLine("INPUT:");
					stringBuilder.AppendLine(messagesText);
				}
				if (!string.IsNullOrWhiteSpace(outputText))
				{
					stringBuilder.AppendLine("OUTPUT:");
					stringBuilder.AppendLine(outputText);
				}
				stringBuilder.AppendLine("----");
			}
			lock (_fileLock)
			{
				AppendUtf8(_tokenStatsPath, stringBuilder.ToString());
			}
		}
		catch
		{
		}
	}

	private static string BuildMessagesDump(IEnumerable<object> messages)
	{
		try
		{
			if (messages == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			int num = 0;
			foreach (object message in messages)
			{
				num++;
				if (TryGetMessageRoleAndContent(message, out var role, out var content))
				{
					stringBuilder.Append("#").Append(num).Append(" role=")
						.Append(string.IsNullOrWhiteSpace(role) ? "unknown" : role.Trim())
						.AppendLine();
					stringBuilder.AppendLine(NormalizeTokenContent(content));
				}
				else
				{
					stringBuilder.Append("#").Append(num).Append(" role=unknown")
						.AppendLine();
					stringBuilder.AppendLine(NormalizeTokenContent(message?.ToString() ?? ""));
				}
			}
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return "";
		}
	}

	private static string NormalizeTokenContent(string text)
	{
		try
		{
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			return text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		}
		catch
		{
			return text ?? "";
		}
	}

	private static bool IsCjk(char c)
	{
		return (c >= '\u4E00' && c <= '\u9FFF') || (c >= '\u3400' && c <= '\u4DBF') || (c >= '\uF900' && c <= '\uFAFF');
	}

	private static bool TryGetMessageRoleAndContent(object message, out string role, out string content)
	{
		role = "";
		content = "";
		if (message == null)
		{
			return false;
		}
		try
		{
			if (message is JObject jObject)
			{
				role = (string)jObject["role"] ?? "";
				content = (string)jObject["content"] ?? "";
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (message is IDictionary<string, object> dictionary)
			{
				if (dictionary.TryGetValue("role", out var value) && value != null)
				{
					role = value.ToString();
				}
				if (dictionary.TryGetValue("content", out var value2) && value2 != null)
				{
					content = value2.ToString();
				}
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Type type = message.GetType();
			PropertyInfo propertyInfo = type.GetProperty("role") ?? type.GetProperty("Role");
			PropertyInfo propertyInfo2 = type.GetProperty("content") ?? type.GetProperty("Content");
			if (propertyInfo != null)
			{
				object value3 = propertyInfo.GetValue(message, null);
				if (value3 != null)
				{
					role = value3.ToString();
				}
			}
			if (propertyInfo2 != null)
			{
				object value4 = propertyInfo2.GetValue(message, null);
				if (value4 != null)
				{
					content = value4.ToString();
				}
			}
			return propertyInfo != null || propertyInfo2 != null;
		}
		catch
		{
			return false;
		}
	}

	private static void MaybeFlushMetrics()
	{
		try
		{
			DateTime utcNow = DateTime.UtcNow;
			if (utcNow < _nextMetricsFlushUtc)
			{
				return;
			}
			DateTime metricsWindowStartUtc;
			List<KeyValuePair<string, MetricBucket>> list;
			lock (_metricsLock)
			{
				if (utcNow < _nextMetricsFlushUtc)
				{
					return;
				}
				metricsWindowStartUtc = _metricsWindowStartUtc;
				list = new List<KeyValuePair<string, MetricBucket>>(_metrics);
				_metrics.Clear();
				_metricsWindowStartUtc = utcNow;
				_nextMetricsFlushUtc = utcNow.AddSeconds(180.0);
			}
			double value = Math.Max(1.0, (utcNow - metricsWindowStartUtc).TotalSeconds);
			if (list.Count <= 0)
			{
				Obs("Metrics", "rollup_empty", new Dictionary<string, object> { ["windowSec"] = Math.Round(value, 1) });
				return;
			}
			WriteHumanLine(_modLogPath, "OBS-SUMMARY", $"window={Math.Round(value, 1)}s metrics={list.Count}");
			foreach (KeyValuePair<string, MetricBucket> item in list)
			{
				string key = item.Key;
				MetricBucket metricBucket = item.Value ?? new MetricBucket();
				double value2 = ((metricBucket.Count > 0) ? (metricBucket.SumMs / (double)metricBucket.Count) : 0.0);
				WriteHumanLine(_modLogPath, "OBS-SUMMARY", $"{key}: count={metricBucket.Count} ok={metricBucket.Ok} err={metricBucket.Err} avgMs={Math.Round(value2, 2)} maxMs={Math.Round(metricBucket.MaxMs, 2)}");
				Obs("Metrics", "rollup", new Dictionary<string, object>
				{
					["metric"] = key,
					["windowSec"] = Math.Round(value, 1),
					["count"] = metricBucket.Count,
					["ok"] = metricBucket.Ok,
					["err"] = metricBucket.Err,
					["avgMs"] = Math.Round(value2, 2),
					["maxMs"] = Math.Round(metricBucket.MaxMs, 2)
				});
			}
		}
		catch
		{
		}
	}

	public static void OnApplicationTick()
	{
		try
		{
			DateTime utcNow = DateTime.UtcNow;
			if (utcNow < _nextLogCleanupCheckUtc)
			{
				return;
			}
			_nextLogCleanupCheckUtc = utcNow.AddSeconds(LogCleanupCheckIntervalSeconds);
			DuelSettings settings = TryGetSettings();
			if (settings == null)
			{
				return;
			}
			string selection = DuelSettings.NormalizeLogCleanupIntervalSelection(settings.GetLogCleanupIntervalSelection());
			if (!string.Equals(selection, _lastLogCleanupSelection, StringComparison.Ordinal))
			{
				_lastLogCleanupSelection = selection;
				_nextLogCleanupUtc = DateTime.MinValue;
				if (!string.Equals(selection, DuelSettings.LogCleanupOnStartup, StringComparison.Ordinal))
				{
					_startupLogCleanupDone = false;
				}
			}
			if (string.Equals(selection, DuelSettings.LogCleanupOff, StringComparison.Ordinal))
			{
				return;
			}
			if (string.Equals(selection, DuelSettings.LogCleanupOnStartup, StringComparison.Ordinal))
			{
				if (_startupLogCleanupDone)
				{
					return;
				}
				_startupLogCleanupDone = true;
				ClearAllLogFiles("startup");
				return;
			}
			TimeSpan interval = ResolveLogCleanupInterval(selection);
			if (interval <= TimeSpan.Zero)
			{
				return;
			}
			if (_nextLogCleanupUtc == DateTime.MinValue)
			{
				_nextLogCleanupUtc = utcNow.Add(interval);
				return;
			}
			if (utcNow < _nextLogCleanupUtc)
			{
				return;
			}
			ClearAllLogFiles(selection);
			_nextLogCleanupUtc = utcNow.Add(interval);
		}
		catch
		{
		}
	}

	private static TimeSpan ResolveLogCleanupInterval(string selection)
	{
		string text = DuelSettings.NormalizeLogCleanupIntervalSelection(selection);
		if (string.Equals(text, DuelSettings.LogCleanupEvery30Minutes, StringComparison.Ordinal))
		{
			return TimeSpan.FromMinutes(30.0);
		}
		if (string.Equals(text, DuelSettings.LogCleanupEveryHour, StringComparison.Ordinal))
		{
			return TimeSpan.FromHours(1.0);
		}
		if (string.Equals(text, DuelSettings.LogCleanupEvery6Hours, StringComparison.Ordinal))
		{
			return TimeSpan.FromHours(6.0);
		}
		if (string.Equals(text, DuelSettings.LogCleanupEveryDay, StringComparison.Ordinal))
		{
			return TimeSpan.FromDays(1.0);
		}
		if (string.Equals(text, DuelSettings.LogCleanupEvery3Days, StringComparison.Ordinal))
		{
			return TimeSpan.FromDays(3.0);
		}
		if (string.Equals(text, DuelSettings.LogCleanupEveryWeek, StringComparison.Ordinal))
		{
			return TimeSpan.FromDays(7.0);
		}
		return TimeSpan.Zero;
	}

	private static void ClearAllLogFiles(string reason)
	{
		try
		{
			DrainQueuedLogWrites();
			DrainQueuedTokenStatsWrites();
			string[] paths = new string[]
			{
				_modLogPath,
				_gameTracePath,
				_obsLogPath,
				_hitRatePath,
				_tokenStatsPath,
				_eventLogsPath,
				_compatibilityAuditPath
			};
			string marker = $"\n====== AnimusForge logs cleared {DateTime.Now:yyyy-MM-dd HH:mm:ss} reason={reason ?? ""} ======\n";
			lock (_fileLock)
			{
				foreach (string path in paths)
				{
					if (string.IsNullOrWhiteSpace(path))
					{
						continue;
					}
					string directoryName = System.IO.Path.GetDirectoryName(path);
					if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
					{
						Directory.CreateDirectory(directoryName);
					}
					File.WriteAllBytes(path, _utf8WithBom.GetPreamble());
					AppendUtf8(path, marker);
				}
			}
		}
		catch (Exception ex)
		{
			try
			{
				Debug.Print("[Logger Cleanup Error] " + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static string NewTraceId()
	{
		long num = Interlocked.Increment(ref _traceSeed);
		string text = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
		return "vf-" + text + "-" + num.ToString("x");
	}

	private static DuelSettings TryGetSettings()
	{
		try
		{
			return GlobalSettings<DuelSettings>.Instance;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsPathEnabled(string path)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return false;
			}
			DuelSettings duelSettings = TryGetSettings();
			if (duelSettings == null)
			{
				return !string.Equals(path, _gameTracePath, StringComparison.OrdinalIgnoreCase);
			}
			if (string.Equals(path, _gameTracePath, StringComparison.OrdinalIgnoreCase))
			{
				return duelSettings.EnableDeepTrace;
			}
			if (string.Equals(path, _modLogPath, StringComparison.OrdinalIgnoreCase))
			{
				return duelSettings.EnableModLogicLog;
			}
			if (string.Equals(path, _obsLogPath, StringComparison.OrdinalIgnoreCase))
			{
				return duelSettings.EnableObservabilityLog;
			}
			if (string.Equals(path, _hitRatePath, StringComparison.OrdinalIgnoreCase))
			{
				return duelSettings.EnableHitRateStatsLog;
			}
			if (string.Equals(path, _tokenStatsPath, StringComparison.OrdinalIgnoreCase))
			{
				return duelSettings.EnableTokenStatsLog;
			}
			if (string.Equals(path, _eventLogsPath, StringComparison.OrdinalIgnoreCase))
			{
				return duelSettings.EnableEventLogs;
			}
			return true;
		}
		catch
		{
			return true;
		}
	}

	private static bool CanWriteVerboseLog(string source, string key, double minIntervalSeconds)
	{
		try
		{
			if (minIntervalSeconds <= 0.0)
			{
				return true;
			}
			string throttleKey = ((source ?? "").Trim() + "|" + (key ?? "").Trim()).Trim('|');
			if (string.IsNullOrWhiteSpace(throttleKey))
			{
				throttleKey = "__verbose__";
			}
			long now = DateTime.UtcNow.Ticks;
			long next = now + TimeSpan.FromSeconds(Math.Max(0.1, minIntervalSeconds)).Ticks;
			lock (_verboseLogThrottleLock)
			{
				if (_verboseLogNextAllowedTicks.TryGetValue(throttleKey, out long allowed) && now < allowed)
				{
					return false;
				}
				if (_verboseLogNextAllowedTicks.Count > 512)
				{
					_verboseLogNextAllowedTicks.Clear();
				}
				_verboseLogNextAllowedTicks[throttleKey] = next;
			}
			return true;
		}
		catch
		{
			return true;
		}
	}

	private static bool ShouldRouteModLogicLogToVerbose(string source, string message)
	{
		try
		{
			string text = (source ?? "").Trim();
			string msg = (message ?? "").TrimStart();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			if (string.Equals(text, "SceneTauntPerf", StringComparison.Ordinal)
				|| string.Equals(text, "SceneGoldDiag", StringComparison.Ordinal)
				|| string.Equals(text, "TTSReport", StringComparison.Ordinal)
				|| string.Equals(text, "LipSyncProbe", StringComparison.Ordinal)
				|| string.Equals(text, "ShoutStrict", StringComparison.Ordinal))
			{
				return true;
			}
			if (string.Equals(text, "LoreMatch", StringComparison.Ordinal))
			{
				return true;
			}
			if (string.Equals(text, "GuardrailSemantic", StringComparison.Ordinal))
			{
				return !LooksLikeWarningOrError(msg);
			}
			if (string.Equals(text, "DialogueHistory", StringComparison.Ordinal))
			{
				return msg.StartsWith("candidate_pool", StringComparison.OrdinalIgnoreCase)
					|| msg.StartsWith("context ", StringComparison.OrdinalIgnoreCase)
					|| msg.StartsWith("semantic_accept", StringComparison.OrdinalIgnoreCase);
			}
			if (string.Equals(text, "Logic", StringComparison.Ordinal))
			{
				return msg.StartsWith("[SemanticTrigger-Shout]", StringComparison.Ordinal)
					|| msg.StartsWith("[RuleInjectionDebug]", StringComparison.Ordinal)
					|| msg.StartsWith("[Context]", StringComparison.Ordinal);
			}
			if (string.Equals(text, "ShoutBehavior", StringComparison.Ordinal))
			{
				return msg.StartsWith("[Hotkey]", StringComparison.Ordinal);
			}
			if (string.Equals(text, "ShoutNetwork", StringComparison.Ordinal))
			{
				return msg.StartsWith("[PrimaryChatRaw]", StringComparison.Ordinal);
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool LooksLikeWarningOrError(string message)
	{
		try
		{
			string text = message ?? "";
			return text.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.IndexOf("WARN", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.Contains("错误")
				|| text.Contains("失败")
				|| text.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static void WriteHumanLine(string path, string source, string message, bool isVerbose = false)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path) || !IsPathEnabled(path))
			{
				return;
			}
			string text = DateTime.Now.ToString("HH:mm:ss");
			string currentTraceId = CurrentTraceId;
			string text2 = (string.IsNullOrWhiteSpace(currentTraceId) ? "" : (" [trace=" + currentTraceId + "]"));
			EnqueueLogWrite(path, "[" + text + "] [" + source + "]" + text2 + " " + message + "\n", isVerbose);
		}
		catch
		{
		}
	}

	private static void WriteRawLine(string path, string line)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path) || line == null || !IsPathEnabled(path))
			{
				return;
			}
			EnqueueLogWrite(path, line + "\n");
		}
		catch
		{
		}
	}

	private static void EnqueueLogWrite(string path, string content, bool isVerbose = false, bool bypassBackpressure = false)
	{
		if (string.IsNullOrWhiteSpace(path) || content == null)
		{
			return;
		}
		if (!bypassBackpressure)
		{
			int queued = Volatile.Read(ref _logWriteQueueCount);
			if (queued >= MaxLogWriteQueueItems && isVerbose)
			{
				Interlocked.Increment(ref _droppedVerboseLogCount);
				TryEnqueueDroppedLogSummary();
				return;
			}
			if (queued >= HardMaxLogWriteQueueItems)
			{
				if (isVerbose)
				{
					Interlocked.Increment(ref _droppedVerboseLogCount);
				}
				else
				{
					Interlocked.Increment(ref _droppedNormalLogCount);
				}
				TryEnqueueDroppedLogSummary();
				return;
			}
		}
		Interlocked.Increment(ref _logWriteQueueCount);
		_logWriteQueue.Enqueue(new LogWriteWorkItem
		{
			Path = path,
			Content = content,
			IsVerbose = isVerbose
		});
		TryStartLogWriter();
	}

	private static void TryEnqueueDroppedLogSummary()
	{
		try
		{
			if (!IsModLogicEnabled)
			{
				return;
			}
			long now = DateTime.UtcNow.Ticks;
			long last = Interlocked.Read(ref _lastDroppedLogSummaryUtcTicks);
			if (now - last < TimeSpan.FromSeconds(DroppedLogSummaryIntervalSeconds).Ticks)
			{
				return;
			}
			if (Interlocked.CompareExchange(ref _lastDroppedLogSummaryUtcTicks, now, last) != last)
			{
				return;
			}
			long verbose = Interlocked.Exchange(ref _droppedVerboseLogCount, 0L);
			long normal = Interlocked.Exchange(ref _droppedNormalLogCount, 0L);
			if (verbose <= 0 && normal <= 0)
			{
				return;
			}
			string text = DateTime.Now.ToString("HH:mm:ss");
			string line = $"[{text}] [Logger] dropped verbose={verbose} normal={normal} queued={Volatile.Read(ref _logWriteQueueCount)}\n";
			EnqueueLogWrite(_modLogPath, line, isVerbose: false, bypassBackpressure: true);
		}
		catch
		{
		}
	}

	private static void TryStartLogWriter()
	{
		if (Interlocked.CompareExchange(ref _logWriterRunning, 1, 0) != 0)
		{
			return;
		}
		Task.Run(ProcessLogWriteQueue);
	}

	private static void ProcessLogWriteQueue()
	{
		try
		{
			while (true)
			{
				Dictionary<string, StringBuilder> batches = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
				int batchCount = 0;
				while (_logWriteQueue.TryDequeue(out var item))
				{
					Interlocked.Decrement(ref _logWriteQueueCount);
					AppendLogWorkItemToBatch(item, batches, ref batchCount);
					if (batchCount >= LogBatchFlushItemCount)
					{
						FlushLogBatches(batches);
						batches.Clear();
						batchCount = 0;
					}
				}
				FlushLogBatches(batches);
				Interlocked.Exchange(ref _logWriterRunning, 0);
				if (_logWriteQueue.IsEmpty || Interlocked.CompareExchange(ref _logWriterRunning, 1, 0) != 0)
				{
					break;
				}
			}
		}
		catch
		{
			Interlocked.Exchange(ref _logWriterRunning, 0);
			if (!_logWriteQueue.IsEmpty)
			{
				TryStartLogWriter();
			}
		}
	}

	private static void AppendLogWorkItemToBatch(LogWriteWorkItem item, Dictionary<string, StringBuilder> batches, ref int batchCount)
	{
		try
		{
			if (item == null || string.IsNullOrWhiteSpace(item.Path) || item.Content == null || !IsPathEnabled(item.Path) || batches == null)
			{
				return;
			}
			if (!batches.TryGetValue(item.Path, out var stringBuilder) || stringBuilder == null)
			{
				stringBuilder = new StringBuilder();
				batches[item.Path] = stringBuilder;
			}
			stringBuilder.Append(item.Content);
			batchCount++;
		}
		catch
		{
		}
	}

	private static void FlushLogBatches(Dictionary<string, StringBuilder> batches)
	{
		try
		{
			if (batches == null || batches.Count == 0)
			{
				return;
			}
			lock (_fileLock)
			{
				foreach (KeyValuePair<string, StringBuilder> item in batches)
				{
					if (!string.IsNullOrWhiteSpace(item.Key) && item.Value != null && item.Value.Length > 0 && IsPathEnabled(item.Key))
					{
						AppendUtf8(item.Key, item.Value.ToString());
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void WriteLogWorkItem(LogWriteWorkItem item)
	{
		try
		{
			if (item == null || string.IsNullOrWhiteSpace(item.Path) || item.Content == null || !IsPathEnabled(item.Path))
			{
				return;
			}
			lock (_fileLock)
			{
				AppendUtf8(item.Path, item.Content);
			}
		}
		catch
		{
		}
	}

	private static void DrainQueuedLogWrites()
	{
		try
		{
			while (_logWriteQueue.TryDequeue(out var item))
			{
				Interlocked.Decrement(ref _logWriteQueueCount);
				WriteLogWorkItem(item);
			}
		}
		catch
		{
		}
	}

	private static void DrainQueuedTokenStatsWrites()
	{
		try
		{
			while (_tokenStatsWriteQueue.TryDequeue(out var item))
			{
				WriteTokenStatsWorkItem(item);
			}
		}
		catch
		{
		}
	}

	private static void AppendUtf8(string path, string content)
	{
		if (!string.IsNullOrWhiteSpace(path) && content != null)
		{
			File.AppendAllText(path, content, _utf8WithBom);
		}
	}

	private static void EnsureUtf8Bom(string path)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return;
			}
			byte[] preamble = _utf8WithBom.GetPreamble();
			if (preamble == null || preamble.Length == 0)
			{
				return;
			}
			if (!File.Exists(path))
			{
				File.WriteAllBytes(path, preamble);
				return;
			}
			byte[] array = File.ReadAllBytes(path);
			if (array.Length >= preamble.Length)
			{
				bool flag = true;
				for (int i = 0; i < preamble.Length; i++)
				{
					if (array[i] != preamble[i])
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					return;
				}
			}
			byte[] array2 = new byte[preamble.Length + array.Length];
			Buffer.BlockCopy(preamble, 0, array2, 0, preamble.Length);
			if (array.Length > 0)
			{
				Buffer.BlockCopy(array, 0, array2, preamble.Length, array.Length);
			}
			File.WriteAllBytes(path, array2);
		}
		catch
		{
		}
	}
}
