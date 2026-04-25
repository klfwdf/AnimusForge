#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
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

	private static string _modLogPath;

	private static string _gameTracePath;

	private static string _obsLogPath;

	private static string _hitRatePath;

	private static string _tokenStatsPath;

	private static string _eventLogsPath;

	private static readonly object _fileLock;

	private static readonly AsyncLocal<TraceScopeState> _traceState;

	private static readonly object _metricsLock;

	private static readonly Dictionary<string, MetricBucket> _metrics;

	private static readonly object _hitRateLock;

	private static readonly UTF8Encoding _utf8WithBom;

	private static readonly Dictionary<string, HitRateBucket> _hitRate;

	private static readonly Dictionary<string, long> _hitRateScopeSeq;

	private static readonly Dictionary<string, long> _hitRateActiveQuery;

	private static DateTime _metricsWindowStartUtc;

	private static DateTime _nextMetricsFlushUtc;

	private static long _traceSeed;

	private static long _hitRateEventSeed;

	private const int MetricsFlushIntervalSeconds = 180;

	public static string CurrentTraceId => _traceState.Value?.TraceId ?? "";

	public static string CurrentChannel => _traceState.Value?.Channel ?? "";

	static Logger()
	{
		_fileLock = new object();
		_traceState = new AsyncLocal<TraceScopeState>();
		_metricsLock = new object();
		_metrics = new Dictionary<string, MetricBucket>(StringComparer.Ordinal);
		_hitRateLock = new object();
		_utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
		_hitRate = new Dictionary<string, HitRateBucket>(StringComparer.OrdinalIgnoreCase);
		_hitRateScopeSeq = new Dictionary<string, long>(StringComparer.Ordinal);
		_hitRateActiveQuery = new Dictionary<string, long>(StringComparer.Ordinal);
		_metricsWindowStartUtc = DateTime.UtcNow;
		_nextMetricsFlushUtc = DateTime.UtcNow.AddSeconds(180.0);
		_traceSeed = 0L;
		_hitRateEventSeed = 0L;
		try
		{
			string basePath = Utilities.GetBasePath();
			string text = System.IO.Path.Combine(basePath, "Modules", "AnimusForge", "Logs");
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
			EnsureUtf8Bom(_modLogPath);
			EnsureUtf8Bom(_gameTracePath);
			EnsureUtf8Bom(_obsLogPath);
			EnsureUtf8Bom(_hitRatePath);
			EnsureUtf8Bom(_tokenStatsPath);
			EnsureUtf8Bom(_eventLogsPath);
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
		WriteHumanLine(_modLogPath, source, message);
	}

	public static void LogTrace(string source, string message)
	{
		WriteHumanLine(_gameTracePath, source, message);
	}

	public static void LogEvent(string source, string message)
	{
		WriteHumanLine(_eventLogsPath, source, message);
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
			lock (_fileLock)
			{
				AppendUtf8(_eventLogsPath, stringBuilder.ToString());
			}
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

	public static void RecordTokenStats(int inputTokens, int outputTokens, IEnumerable<object> messages = null, string outputContent = null, string mode = null)
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
			string text = DateTime.Now.ToString("HH:mm:ss");
			string currentTraceId = CurrentTraceId;
			string text2 = (string.IsNullOrWhiteSpace(currentTraceId) ? "" : (" trace=" + currentTraceId));
			string text3 = (string.IsNullOrWhiteSpace(mode) ? "" : (" mode=" + mode.Trim()));
			string value = $"[{text}] in={inputTokens} out={outputTokens}{text2}{text3}";
			string value2 = BuildMessagesDump(messages);
			string value3 = NormalizeTokenContent(outputContent);
			lock (_fileLock)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(value);
				if (!string.IsNullOrWhiteSpace(value2))
				{
					stringBuilder.AppendLine("INPUT:");
					stringBuilder.AppendLine(value2);
				}
				if (!string.IsNullOrWhiteSpace(value3))
				{
					stringBuilder.AppendLine("OUTPUT:");
					stringBuilder.AppendLine(value3);
				}
				stringBuilder.AppendLine("----");
				AppendUtf8(_tokenStatsPath, stringBuilder.ToString());
			}
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
			string text = BuildMessagesDump(messages);
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string text2 = DateTime.Now.ToString("HH:mm:ss");
			string currentTraceId = CurrentTraceId;
			string text3 = string.IsNullOrWhiteSpace(currentTraceId) ? "" : (" trace=" + currentTraceId);
			string text4 = string.IsNullOrWhiteSpace(mode) ? "" : (" mode=" + mode.Trim());
			string text5 = string.IsNullOrWhiteSpace(title) ? "" : (" title=" + title.Trim());
			lock (_fileLock)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"[{text2}] STRICT_MESSAGES{text3}{text4}{text5}");
				stringBuilder.AppendLine(text);
				stringBuilder.AppendLine("----");
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

	private static void WriteHumanLine(string path, string source, string message)
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
			lock (_fileLock)
			{
				AppendUtf8(path, "[" + text + "] [" + source + "]" + text2 + " " + message + "\n");
			}
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
			lock (_fileLock)
			{
				AppendUtf8(path, line + "\n");
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
