using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class FreezeWatchdog
{
	internal sealed class ScopeState
	{
		public string Name;
		public long StartTimestamp;
		public int ThreadId;
		public bool MainThreadScope;
		public ScopeState Parent;
	}

	internal readonly struct ScopeToken : IDisposable
	{
		private readonly ScopeState _state;

		internal ScopeToken(ScopeState state)
		{
			_state = state;
		}

		public void Dispose()
		{
			CompleteScope(_state);
		}
	}

	private const string LogSource = "FreezeWatchdog";
	private const string SnapshotFileName = "FreezeWatchdog_LastCheckpoint.txt";
	private const string TimelineFileName = "FreezeWatchdog_Timeline.txt";
	private const int RecentEventLimit = 256;
	private const double MainThreadSlowScopeMs = 250.0;
	private const double BackgroundSlowScopeMs = 1000.0;
	private const double FrameGapReportMs = 1000.0;
	private const double MonitorHangReportMs = 2500.0;
	private const double HangDumpCaptureMs = 5000.0;
	private const double MonitorRepeatMs = 5000.0;
	private const int MonitorIntervalMs = 1000;
	private static readonly long RuntimeContextRefreshTicks = TimeSpan.FromMilliseconds(250.0).Ticks;

	private static readonly object SyncRoot = new object();
	private static readonly object FileWriteRoot = new object();
	private static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
	private static readonly string[] RecentEvents = new string[RecentEventLimit];
	private static int _recentEventNext;
	private static int _recentEventCount;
	private static long _frameIndex;
	private static int _mainThreadId;
	private static long _lastMainHeartbeatTimestamp;
	private static long _lastMainHeartbeatUtcTicks;
	private static string _mainThreadActiveScope = "";
	private static long _mainThreadActiveScopeStartTimestamp;
	private static string _lastCompletedMainScope = "";
	private static long _lastCompletedMainScopeUtcTicks;
	private static long _lastMonitorReportTimestamp;
	private static string _cachedRuntimeContext = "state=unknown mission=False conversation=False menu=";
	private static long _cachedRuntimeContextUtcTicks;
	private static long _nextRuntimeContextRefreshUtcTicks;
	private static long _skippedFileWriteCount;
	private static long _fileWriteSequence;
	private static long _lastSnapshotWrittenSequence;
	private static Thread _monitorThread;
	private static long _monitorHeartbeatUtcTicks;
	private static int _monitorStarted;
	private static int _hangDumpEnabled = 1;
	private static int _hangDumpCapturedForCurrentStall;
	private static int _hangDumpInFlight;

	[Flags]
	private enum MiniDumpType : uint
	{
		MiniDumpNormal = 0u,
		MiniDumpWithUnloadedModules = 0x20u,
		MiniDumpWithIndirectlyReferencedMemory = 0x40u,
		MiniDumpWithThreadInfo = 0x1000u
	}

	[DllImport("Dbghelp.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool MiniDumpWriteDump(IntPtr processHandle, uint processId, SafeFileHandle fileHandle, MiniDumpType dumpType, IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

	[ThreadStatic]
	private static ScopeState _currentScope;

	internal static ScopeToken Scope(string name)
	{
		try
		{
			if (!IsEnabled() || string.IsNullOrWhiteSpace(name))
			{
				return default;
			}
			EnsureMonitorStarted();
			int threadId = Thread.CurrentThread.ManagedThreadId;
			bool isMainThread = IsKnownMainThread(threadId);
			long startTimestamp = Stopwatch.GetTimestamp();
			ScopeState state = new ScopeState
			{
				Name = Sanitize(name, 180),
				StartTimestamp = startTimestamp,
				ThreadId = threadId,
				MainThreadScope = isMainThread,
				Parent = _currentScope
			};
			_currentScope = state;
			if (isMainThread)
			{
				TouchMainHeartbeat(startTimestamp);
				lock (SyncRoot)
				{
					_mainThreadActiveScope = state.Name;
					_mainThreadActiveScopeStartTimestamp = startTimestamp;
				}
			}
			RecordEvent("begin", state.Name, "", threadId);
			return new ScopeToken(state);
		}
		catch
		{
			return default;
		}
	}

	internal static void BeginFrame(float dt)
	{
		try
		{
			if (!IsEnabled())
			{
				return;
			}
			RefreshHangDumpEnabledOnMainThread();
			EnsureMonitorStarted();
			int threadId = Thread.CurrentThread.ManagedThreadId;
			if (_mainThreadId == 0)
			{
				Interlocked.CompareExchange(ref _mainThreadId, threadId, 0);
			}
			long now = Stopwatch.GetTimestamp();
			long previous = Interlocked.Exchange(ref _lastMainHeartbeatTimestamp, now);
			Interlocked.Exchange(ref _lastMainHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
			Interlocked.Exchange(ref _hangDumpCapturedForCurrentStall, 0);
			long frame = Interlocked.Increment(ref _frameIndex);
			double dtMs = Math.Max(0.0, dt * 1000.0);
			RecordEvent("frame_begin", "SubModule.OnApplicationTick", "dtMs=" + dtMs.ToString("0.00") + " frame=" + frame, threadId);
			if (DateTime.UtcNow.Ticks >= Interlocked.Read(ref _nextRuntimeContextRefreshUtcTicks))
			{
				using (Scope("FreezeWatchdog.CaptureRuntimeContext"))
				{
					CaptureRuntimeContextOnMainThread();
				}
			}
			if (previous > 0L)
			{
				double gapMs = TimestampDeltaMs(previous, now);
				if (gapMs >= FrameGapReportMs)
				{
					WriteImmediate("[FREEZE_GAP] no_frame_ms=" + gapMs.ToString("0.00") + " dtMs=" + dtMs.ToString("0.00") + " " + BuildStateSummary(includeRecent: true), writeSnapshot: true);
				}
			}
		}
		catch
		{
		}
	}

	internal static void EndFrame()
	{
		try
		{
			if (!IsEnabled())
			{
				return;
			}
			long now = Stopwatch.GetTimestamp();
			TouchMainHeartbeat(now);
			lock (SyncRoot)
			{
				if (_currentScope == null)
				{
					_mainThreadActiveScope = "";
					_mainThreadActiveScopeStartTimestamp = 0L;
				}
				_lastCompletedMainScope = "SubModule.OnApplicationTick.frame_end";
				_lastCompletedMainScopeUtcTicks = DateTime.UtcNow.Ticks;
			}
			RecordEvent("frame_end", "SubModule.OnApplicationTick", "frame=" + Interlocked.Read(ref _frameIndex), Thread.CurrentThread.ManagedThreadId);
		}
		catch
		{
		}
	}

	internal static void Mark(string name, string detail = null, bool immediate = false)
	{
		try
		{
			if (!IsEnabled() || string.IsNullOrWhiteSpace(name))
			{
				return;
			}
			EnsureMonitorStarted();
			string safeName = Sanitize(name, 180);
			string safeDetail = Sanitize(detail, 300);
			int threadId = Thread.CurrentThread.ManagedThreadId;
			RecordEvent("mark", safeName, safeDetail, threadId);
			if (immediate)
			{
				WriteImmediate("[MARK] name=" + safeName + (string.IsNullOrWhiteSpace(safeDetail) ? "" : " detail=" + safeDetail) + " " + BuildStateSummary(includeRecent: false), writeSnapshot: true);
			}
		}
		catch
		{
		}
	}

	private static void CompleteScope(ScopeState state)
	{
		try
		{
			if (state == null || state.StartTimestamp <= 0L)
			{
				return;
			}
			long now = Stopwatch.GetTimestamp();
			double elapsedMs = TimestampDeltaMs(state.StartTimestamp, now);
			if (ReferenceEquals(_currentScope, state))
			{
				_currentScope = state.Parent;
			}
			else
			{
				_currentScope = state.Parent;
			}
			if (state.MainThreadScope)
			{
				TouchMainHeartbeat(now);
				lock (SyncRoot)
				{
					if (state.Parent != null && state.Parent.MainThreadScope)
					{
						_mainThreadActiveScope = state.Parent.Name;
						_mainThreadActiveScopeStartTimestamp = state.Parent.StartTimestamp;
					}
					else
					{
						_mainThreadActiveScope = "";
						_mainThreadActiveScopeStartTimestamp = 0L;
					}
					_lastCompletedMainScope = state.Name + " elapsedMs=" + elapsedMs.ToString("0.00");
					_lastCompletedMainScopeUtcTicks = DateTime.UtcNow.Ticks;
				}
			}
			RecordEvent("end", state.Name, "elapsedMs=" + elapsedMs.ToString("0.00"), state.ThreadId);
			double threshold = state.MainThreadScope ? MainThreadSlowScopeMs : BackgroundSlowScopeMs;
			if (elapsedMs >= threshold)
			{
				WriteImmediate("[FREEZE_SLOW_SCOPE] name=" + state.Name + " elapsedMs=" + elapsedMs.ToString("0.00") + " thread=" + state.ThreadId + " main=" + state.MainThreadScope + " " + BuildStateSummary(includeRecent: true), writeSnapshot: true);
			}
		}
		catch
		{
		}
	}

	private static void EnsureMonitorStarted()
	{
		try
		{
			if (Interlocked.CompareExchange(ref _monitorStarted, 1, 0) != 0)
			{
				return;
			}
			_monitorThread = new Thread(MonitorLoop)
			{
				IsBackground = true,
				Name = "AnimusForge.FreezeWatchdog",
				Priority = ThreadPriority.BelowNormal
			};
			_monitorThread.Start();
			WriteImmediate("[WATCHDOG_START] schema=3 writer=dedicated_thread runtimeContext=main_thread_cached recentLimit=" + RecentEventLimit + " mainThread=" + Interlocked.CompareExchange(ref _mainThreadId, 0, 0), writeSnapshot: true);
		}
		catch
		{
		}
	}

	private static void MonitorLoop()
	{
		while (true)
		{
			try
			{
				Thread.Sleep(MonitorIntervalMs);
				Interlocked.Exchange(ref _monitorHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
				MonitorOnce();
			}
			catch
			{
			}
		}
	}

	private static void MonitorOnce()
	{
		try
		{
			if (!IsEnabled())
			{
				return;
			}
			long lastHeartbeat = Interlocked.Read(ref _lastMainHeartbeatTimestamp);
			if (lastHeartbeat <= 0L)
			{
				return;
			}
			long now = Stopwatch.GetTimestamp();
			double noHeartbeatMs = TimestampDeltaMs(lastHeartbeat, now);
			if (noHeartbeatMs < MonitorHangReportMs)
			{
				return;
			}
			TryCaptureHangDump(noHeartbeatMs);
			long lastReport = Interlocked.Read(ref _lastMonitorReportTimestamp);
			if (lastReport > 0L && TimestampDeltaMs(lastReport, now) < MonitorRepeatMs)
			{
				return;
			}
			Interlocked.Exchange(ref _lastMonitorReportTimestamp, now);
			WriteImmediate("[FREEZE_MONITOR] no_main_heartbeat_ms=" + noHeartbeatMs.ToString("0.00") + " " + BuildStateSummary(includeRecent: true), writeSnapshot: true);
		}
		catch
		{
		}
	}

	private static void RefreshHangDumpEnabledOnMainThread()
	{
		try
		{
			bool enabled = DuelSettings.GetSettings()?.EnableFreezeDumpCapture ?? true;
			Interlocked.Exchange(ref _hangDumpEnabled, enabled ? 1 : 0);
		}
		catch
		{
			Interlocked.Exchange(ref _hangDumpEnabled, 1);
		}
	}

	private static void TryCaptureHangDump(double noHeartbeatMs)
	{
		if (noHeartbeatMs < HangDumpCaptureMs || Volatile.Read(ref _hangDumpEnabled) == 0)
		{
			return;
		}
		if (Interlocked.CompareExchange(ref _hangDumpCapturedForCurrentStall, 1, 0) != 0 || Interlocked.CompareExchange(ref _hangDumpInFlight, 1, 0) != 0)
		{
			return;
		}
		try
		{
			Thread dumpThread = new Thread((ThreadStart)delegate
			{
				try
				{
					WriteHangDump(noHeartbeatMs);
				}
				finally
				{
					Interlocked.Exchange(ref _hangDumpInFlight, 0);
				}
			})
			{
				IsBackground = true,
				Name = "AnimusForge.FreezeDump",
				Priority = ThreadPriority.BelowNormal
			};
			dumpThread.Start();
		}
		catch
		{
			Interlocked.Exchange(ref _hangDumpCapturedForCurrentStall, 0);
			Interlocked.Exchange(ref _hangDumpInFlight, 0);
		}
	}

	private static void WriteHangDump(double noHeartbeatMs)
	{
		string dumpPath = "";
		try
		{
			string dumpDirectory = Path.Combine(AnimusForgeModulePaths.GetLogsDirectory(), "FreezeDumps");
			Directory.CreateDirectory(dumpDirectory);
			using Process process = Process.GetCurrentProcess();
			dumpPath = Path.Combine(dumpDirectory, "AnimusForge_Freeze_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_pid" + process.Id + ".dmp");
			WriteImmediate("[FREEZE_DUMP_START] no_main_heartbeat_ms=" + noHeartbeatMs.ToString("0.00") + " path=" + dumpPath + " " + BuildStateSummary(includeRecent: false), writeSnapshot: true);
			using FileStream stream = new FileStream(dumpPath, FileMode.Create, FileAccess.Write, FileShare.Read);
			MiniDumpType dumpType = MiniDumpType.MiniDumpWithThreadInfo | MiniDumpType.MiniDumpWithUnloadedModules | MiniDumpType.MiniDumpWithIndirectlyReferencedMemory;
			if (!MiniDumpWriteDump(process.Handle, (uint)process.Id, stream.SafeFileHandle, dumpType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
			{
				int error = Marshal.GetLastWin32Error();
				throw new InvalidOperationException("MiniDumpWriteDump failed win32=" + error);
			}
			stream.Flush();
			long bytes = 0L;
			try
			{
				bytes = new FileInfo(dumpPath).Length;
			}
			catch
			{
			}
			WriteImmediate("[FREEZE_DUMP_DONE] no_main_heartbeat_ms=" + noHeartbeatMs.ToString("0.00") + " path=" + dumpPath + " bytes=" + bytes, writeSnapshot: true);
		}
		catch (Exception ex)
		{
			WriteImmediate("[FREEZE_DUMP_FAILED] no_main_heartbeat_ms=" + noHeartbeatMs.ToString("0.00") + " path=" + dumpPath + " error=" + ex.GetType().Name + ": " + ex.Message, writeSnapshot: true);
		}
	}

	private static void TouchMainHeartbeat(long timestamp)
	{
		try
		{
			Interlocked.Exchange(ref _lastMainHeartbeatTimestamp, timestamp);
			Interlocked.Exchange(ref _lastMainHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
	}

	private static bool IsKnownMainThread(int threadId)
	{
		try
		{
			int known = Interlocked.CompareExchange(ref _mainThreadId, 0, 0);
			if (known == 0)
			{
				return false;
			}
			return known == threadId;
		}
		catch
		{
			return false;
		}
	}

	private static void RecordEvent(string kind, string name, string detail, int threadId)
	{
		try
		{
			string line = DateTime.Now.ToString("HH:mm:ss.fff")
				+ " frame=" + Interlocked.Read(ref _frameIndex)
				+ " tid=" + threadId
				+ " " + Sanitize(kind, 32)
				+ " " + Sanitize(name, 180)
				+ (string.IsNullOrWhiteSpace(detail) ? "" : " " + Sanitize(detail, 300));
			lock (SyncRoot)
			{
				RecentEvents[_recentEventNext] = line;
				_recentEventNext = (_recentEventNext + 1) % RecentEventLimit;
				if (_recentEventCount < RecentEventLimit)
				{
					_recentEventCount++;
				}
			}
		}
		catch
		{
		}
	}

	private static void WriteImmediate(string message, bool writeSnapshot)
	{
		long sequence = Interlocked.Increment(ref _fileWriteSequence);
		if (IsKnownMainThread(Thread.CurrentThread.ManagedThreadId))
		{
			try
			{
				string queuedMessage = message ?? "";
				ThreadPool.QueueUserWorkItem(_ => WriteImmediateCore(queuedMessage, writeSnapshot, sequence));
			}
			catch
			{
			}
			return;
		}
		WriteImmediateCore(message, writeSnapshot, sequence);
	}

	private static void WriteImmediateCore(string message, bool writeSnapshot, long sequence)
	{
		try
		{
			if (!Monitor.TryEnter(FileWriteRoot, 100))
			{
				Interlocked.Increment(ref _skippedFileWriteCount);
				return;
			}
			try
			{
				string timelinePath = AnimusForgeModulePaths.GetLogFilePath(TimelineFileName);
				string snapshotPath = AnimusForgeModulePaths.GetLogFilePath(SnapshotFileName);
				EnsureParentDirectory(timelinePath);
				EnsureParentDirectory(snapshotPath);
				File.AppendAllText(timelinePath, "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [" + LogSource + "] seq=" + sequence + " " + (message ?? "") + Environment.NewLine, Utf8WithBom);
				if (writeSnapshot && sequence >= _lastSnapshotWrittenSequence)
				{
					_lastSnapshotWrittenSequence = sequence;
					File.WriteAllText(snapshotPath, BuildSnapshot("seq=" + sequence + " " + (message ?? "")), Utf8WithBom);
				}
			}
			finally
			{
				Monitor.Exit(FileWriteRoot);
			}
		}
		catch
		{
		}
	}

	private static string BuildStateSummary(bool includeRecent)
	{
		string activeScope;
		long activeStart;
		string lastCompleted;
		long lastCompletedUtc;
		long lastHeartbeatUtc;
		lock (SyncRoot)
		{
			activeScope = _mainThreadActiveScope ?? "";
			activeStart = _mainThreadActiveScopeStartTimestamp;
			lastCompleted = _lastCompletedMainScope ?? "";
			lastCompletedUtc = _lastCompletedMainScopeUtcTicks;
			lastHeartbeatUtc = _lastMainHeartbeatUtcTicks;
		}
		double activeMs = activeStart > 0L ? TimestampDeltaMs(activeStart, Stopwatch.GetTimestamp()) : 0.0;
		string summary = "frame=" + Interlocked.Read(ref _frameIndex)
			+ " mainThread=" + Interlocked.CompareExchange(ref _mainThreadId, 0, 0)
			+ " active=" + (string.IsNullOrWhiteSpace(activeScope) ? "(none)" : activeScope)
			+ " activeMs=" + activeMs.ToString("0.00")
			+ " lastCompleted=" + (string.IsNullOrWhiteSpace(lastCompleted) ? "(none)" : Sanitize(lastCompleted, 180))
			+ " lastCompletedAgeMs=" + AgeMsFromUtcTicks(lastCompletedUtc).ToString("0.00")
			+ " heartbeatAgeMs=" + AgeMsFromUtcTicks(lastHeartbeatUtc).ToString("0.00")
			+ " context={" + (Volatile.Read(ref _cachedRuntimeContext) ?? "state=unknown") + "}"
			+ " contextAgeMs=" + AgeMsFromUtcTicks(Interlocked.Read(ref _cachedRuntimeContextUtcTicks)).ToString("0.00")
			+ " monitorAgeMs=" + AgeMsFromUtcTicks(Interlocked.Read(ref _monitorHeartbeatUtcTicks)).ToString("0.00")
			+ " skippedWrites=" + Interlocked.Read(ref _skippedFileWriteCount)
			+ " process={" + BuildProcessDiagnostics() + "}"
			+ " diagnostics={" + Logger.GetFreezeWatchdogDiagnosticSnapshot() + " " + ShoutBehavior.GetFreezeWatchdogDiagnosticSnapshot() + "}";
		if (includeRecent)
		{
			summary += " recent={" + BuildRecentEventsOneLine(12) + "}";
		}
		return summary;
	}

	private static string BuildSnapshot(string trigger)
	{
		try
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("AnimusForge FreezeWatchdog last checkpoint");
			sb.AppendLine("time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			sb.AppendLine("trigger=" + Sanitize(trigger, 500));
			sb.AppendLine("state=" + BuildStateSummary(includeRecent: false));
			sb.AppendLine("recent:");
			foreach (string line in BuildRecentEventsSnapshot())
			{
				sb.AppendLine(line);
			}
			sb.AppendLine("performance:");
			sb.AppendLine(PerfProbe.BuildCurrentSnapshotForFreeze());
			return sb.ToString();
		}
		catch
		{
			return "AnimusForge FreezeWatchdog snapshot failed.";
		}
	}

	private static List<string> BuildRecentEventsSnapshot()
	{
		List<string> result = new List<string>();
		try
		{
			lock (SyncRoot)
			{
				int start = (_recentEventNext - _recentEventCount + RecentEventLimit) % RecentEventLimit;
				for (int i = 0; i < _recentEventCount; i++)
				{
					string line = RecentEvents[(start + i) % RecentEventLimit];
					if (!string.IsNullOrWhiteSpace(line))
					{
						result.Add(line);
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static string BuildRecentEventsOneLine(int maxCount)
	{
		try
		{
			List<string> snapshot = BuildRecentEventsSnapshot();
			if (snapshot.Count == 0)
			{
				return "";
			}
			int start = Math.Max(0, snapshot.Count - Math.Max(1, maxCount));
			StringBuilder sb = new StringBuilder();
			for (int i = start; i < snapshot.Count; i++)
			{
				if (sb.Length > 0)
				{
					sb.Append(" | ");
				}
				sb.Append(Sanitize(snapshot[i], 180));
			}
			return sb.ToString();
		}
		catch
		{
			return "";
		}
	}

	private static void CaptureRuntimeContextOnMainThread()
	{
		try
		{
			long nowTicks = DateTime.UtcNow.Ticks;
			Interlocked.Exchange(ref _nextRuntimeContextRefreshUtcTicks, nowTicks + RuntimeContextRefreshTicks);
			string activeState = "";
			try
			{
				activeState = Game.Current?.GameStateManager?.ActiveState?.GetType()?.Name ?? "";
			}
			catch
			{
				activeState = "";
			}
			string menu = "";
			try
			{
				menu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "";
			}
			catch
			{
				menu = "";
			}
			bool conversation = false;
			try
			{
				conversation = Campaign.Current?.ConversationManager?.IsConversationInProgress == true;
			}
			catch
			{
				conversation = false;
			}
			bool mission = false;
			try
			{
				mission = Mission.Current != null;
			}
			catch
			{
				mission = false;
			}
			Volatile.Write(ref _cachedRuntimeContext, "state=" + activeState + " mission=" + mission + " conversation=" + conversation + " menu=" + menu);
			Interlocked.Exchange(ref _cachedRuntimeContextUtcTicks, DateTime.UtcNow.Ticks);
		}
		catch
		{
			Volatile.Write(ref _cachedRuntimeContext, "state=unknown");
			Interlocked.Exchange(ref _cachedRuntimeContextUtcTicks, DateTime.UtcNow.Ticks);
		}
	}

	internal static string GetCachedRuntimeContextForDiagnostics()
	{
		try
		{
			return Volatile.Read(ref _cachedRuntimeContext) ?? "state=unknown";
		}
		catch
		{
			return "state=unknown";
		}
	}

	private static string BuildProcessDiagnostics()
	{
		try
		{
			ThreadPool.GetAvailableThreads(out int workerAvailable, out int ioAvailable);
			ThreadPool.GetMaxThreads(out int workerMax, out int ioMax);
			using Process process = Process.GetCurrentProcess();
			return "workingSetMB=" + (process.WorkingSet64 / 1048576L)
				+ " privateMB=" + (process.PrivateMemorySize64 / 1048576L)
				+ " cpuMs=" + process.TotalProcessorTime.TotalMilliseconds.ToString("0")
				+ " osThreads=" + process.Threads.Count
				+ " managedTid=" + Thread.CurrentThread.ManagedThreadId
				+ " poolWorker=" + workerAvailable + "/" + workerMax
				+ " poolIo=" + ioAvailable + "/" + ioMax
				+ " gcMB=" + (GC.GetTotalMemory(forceFullCollection: false) / 1048576L)
				+ " gc=" + GC.CollectionCount(0) + "/" + GC.CollectionCount(1) + "/" + GC.CollectionCount(2)
				+ " conversationLock={" + ConversationHelper.GetPendingLockDiagnosticSnapshot() + "}";
		}
		catch
		{
			return "unavailable";
		}
	}

	private static void EnsureParentDirectory(string path)
	{
		try
		{
			string directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
		catch
		{
		}
	}

	private static bool IsEnabled()
	{
		return true;
	}

	private static double TimestampDeltaMs(long startTimestamp, long endTimestamp)
	{
		try
		{
			return Math.Max(0.0, (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency);
		}
		catch
		{
			return 0.0;
		}
	}

	private static double AgeMsFromUtcTicks(long utcTicks)
	{
		try
		{
			if (utcTicks <= 0L)
			{
				return -1.0;
			}
			return Math.Max(0.0, TimeSpan.FromTicks(DateTime.UtcNow.Ticks - utcTicks).TotalMilliseconds);
		}
		catch
		{
			return -1.0;
		}
	}

	private static string Sanitize(string value, int maxLength)
	{
		try
		{
			string text = (value ?? "").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", " ").Trim();
			if (maxLength > 0 && text.Length > maxLength)
			{
				text = text.Substring(0, maxLength) + "...";
			}
			return text;
		}
		catch
		{
			return "";
		}
	}
}
