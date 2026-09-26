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
	internal readonly struct ScopeToken : IDisposable
	{
		private readonly FreezeWatchState.ScopeState _state;

		internal ScopeToken(FreezeWatchState.ScopeState state)
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
	private const double MainThreadSlowScopeMs = 250.0;
	private const double BackgroundSlowScopeMs = 1000.0;
	private const double FrameGapReportMs = 1000.0;
	private const double MonitorHangReportMs = 2500.0;
	private const double HangDumpCaptureMs = 5000.0;
	private const double MonitorRepeatMs = 5000.0;
	private const int MonitorIntervalMs = 1000;
	private static readonly long RuntimeContextRefreshTicks = TimeSpan.FromMilliseconds(250.0).Ticks;
	private static readonly long RuntimeActivationRefreshTicks = TimeSpan.FromMilliseconds(250.0).Ticks;

	private static readonly FreezeWatchState State = new FreezeWatchState();
	private static readonly object FileWriteRoot = new object();
	private static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
	private static long _lastMonitorReportTimestamp;
	private static long _nextRuntimeContextRefreshUtcTicks;
	private static long _nextActivationRefreshUtcTicks;
	private static long _skippedFileWriteCount;
	private static long _fileWriteSequence;
	private static long _lastSnapshotWrittenSequence;
	private static Thread _monitorThread;
	private static int _hangDumpEnabled = 1;
	private static int _saveInProgress;
	private static int _aiInteractionActive;
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
			FreezeWatchState.ScopeState state = State.EnterScope(name, threadId, isMainThread, startTimestamp);
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
			long nowUtcTicks = DateTime.UtcNow.Ticks;
			RefreshActivationStateOnMainThread(nowUtcTicks);
			if (!IsEnabled())
			{
				return;
			}
			EnsureMonitorStarted();
			int threadId = Thread.CurrentThread.ManagedThreadId;
			State.EnsureMainThread(threadId);
			long now = Stopwatch.GetTimestamp();
			long previous = State.BeginFrameHeartbeat(now, DateTime.UtcNow.Ticks);
			Interlocked.Exchange(ref _hangDumpCapturedForCurrentStall, 0);
			long frame = State.AdvanceFrame();
			double dtMs = Math.Max(0.0, dt * 1000.0);
			RecordEvent("frame_begin", "SubModule.OnApplicationTick", "dtMs=" + dtMs.ToString("0.00") + " frame=" + frame, threadId);
			if (nowUtcTicks >= Interlocked.Read(ref _nextRuntimeContextRefreshUtcTicks))
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
			State.EndFrame(now);
			RecordEvent("frame_end", "SubModule.OnApplicationTick", "frame=" + State.FrameIndex, Thread.CurrentThread.ManagedThreadId);
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

	private static void CompleteScope(FreezeWatchState.ScopeState state)
	{
		try
		{
			if (state == null || state.StartTimestamp <= 0L)
			{
				return;
			}
			long now = Stopwatch.GetTimestamp();
			double elapsedMs = TimestampDeltaMs(state.StartTimestamp, now);
			State.CompleteScope(state, now, elapsedMs);
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
			if (!State.TryStartMonitor())
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
			WriteImmediate("[WATCHDOG_START] schema=3 writer=dedicated_thread runtimeContext=main_thread_cached recentLimit=" + FreezeWatchState.RecentEventLimit + " mainThread=" + State.MainThreadId, writeSnapshot: true);
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
				State.SetMonitorHeartbeat(DateTime.UtcNow.Ticks);
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
			long lastHeartbeat = State.LastMainHeartbeatTimestamp;
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

	internal static bool IsScopeRecordingActive()
	{
		return IsEnabled();
	}

	// The watchdog exists for stalled AnimusForge AI interactions. Outside that narrow state,
	// recording every frame/scope only adds allocation and locking pressure to map simulation.
	private static void RefreshActivationStateOnMainThread(long nowUtcTicks)
	{
		try
		{
			long nextAllowedTicks = Interlocked.Read(ref _nextActivationRefreshUtcTicks);
			if (nowUtcTicks < nextAllowedTicks)
			{
				return;
			}
			Interlocked.Exchange(ref _nextActivationRefreshUtcTicks, nowUtcTicks + RuntimeActivationRefreshTicks);
			RefreshHangDumpEnabledOnMainThread();
			RefreshSaveStateOnMainThread();
			RefreshAiInteractionStateOnMainThread();
		}
		catch
		{
		}
	}

	private static void RefreshSaveStateOnMainThread()
	{
		try
		{
			bool isSaving = Campaign.Current?.SaveHandler?.IsSaving ?? false;
			Interlocked.Exchange(ref _saveInProgress, isSaving ? 1 : 0);
		}
		catch
		{
			Interlocked.Exchange(ref _saveInProgress, 0);
		}
	}

	private static void RefreshAiInteractionStateOnMainThread()
	{
		try
		{
			bool isAiInteractionActive = ShoutBehavior.IsNativeConversationInputOpenForExternal()
				|| ShoutBehavior.IsSceneShoutInputActiveForExternal();
			Interlocked.Exchange(ref _aiInteractionActive, isAiInteractionActive ? 1 : 0);
		}
		catch
		{
			Interlocked.Exchange(ref _aiInteractionActive, 0);
		}
	}

	private static void TryCaptureHangDump(double noHeartbeatMs)
	{
		// Vanilla saving intentionally stops the normal frame heartbeat while it serializes
		// campaign data. Capturing a dump from inside the same process at that point can
		// suspend/scan the process and turn a slow save into an apparent permanent hang.
		// Dumps are useful for AnimusForge AI interaction hangs only. Keep ordinary dialogue,
		// menus, map play and every save path on the lightweight log-only watchdog path.
		if (noHeartbeatMs < HangDumpCaptureMs
			|| Volatile.Read(ref _hangDumpEnabled) == 0
			|| Volatile.Read(ref _saveInProgress) != 0
			|| Volatile.Read(ref _aiInteractionActive) == 0)
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
			MiniDumpType dumpType = MiniDumpType.MiniDumpWithThreadInfo | MiniDumpType.MiniDumpWithUnloadedModules;
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

	private static bool IsKnownMainThread(int threadId)
	{
		try
		{
			return State.IsKnownMainThread(threadId);
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
			State.RecordEvent(kind, name, detail, threadId);
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
		FreezeWatchState.Snapshot state = State.Capture();
		double activeMs = state.ActiveStart > 0L ? TimestampDeltaMs(state.ActiveStart, Stopwatch.GetTimestamp()) : 0.0;
		string summary = "frame=" + state.FrameIndex
			+ " mainThread=" + state.MainThreadId
			+ " saveInProgress=" + Volatile.Read(ref _saveInProgress)
			+ " aiInteractionActive=" + Volatile.Read(ref _aiInteractionActive)
			+ " active=" + (string.IsNullOrWhiteSpace(state.ActiveScope) ? "(none)" : state.ActiveScope)
			+ " activeMs=" + activeMs.ToString("0.00")
			+ " lastCompleted=" + (string.IsNullOrWhiteSpace(state.LastCompleted) ? "(none)" : Sanitize(state.LastCompleted, 180))
			+ " lastCompletedAgeMs=" + AgeMsFromUtcTicks(state.LastCompletedUtc).ToString("0.00")
			+ " heartbeatAgeMs=" + AgeMsFromUtcTicks(state.LastHeartbeatUtc).ToString("0.00")
			+ " context={" + state.RuntimeContext + "}"
			+ " contextAgeMs=" + AgeMsFromUtcTicks(state.RuntimeContextUtc).ToString("0.00")
			+ " monitorAgeMs=" + AgeMsFromUtcTicks(state.MonitorHeartbeatUtc).ToString("0.00")
			+ " skippedWrites=" + Interlocked.Read(ref _skippedFileWriteCount)
			+ " process={" + BuildProcessDiagnostics() + "}"
			+ " diagnostics={" + Logger.GetFreezeWatchdogDiagnosticSnapshot() + " " + ShoutBehavior.GetFreezeWatchdogDiagnosticSnapshot() + "}";
		if (includeRecent)
		{
			summary += " recent={" + State.RecentEventsOneLine(12) + "}";
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
			foreach (string line in State.RecentEventsSnapshot())
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
			State.SetRuntimeContext("state=" + activeState + " mission=" + mission + " conversation=" + conversation + " menu=" + menu, DateTime.UtcNow.Ticks);
		}
		catch
		{
			State.SetRuntimeContext("state=unknown", DateTime.UtcNow.Ticks);
		}
	}

	internal static string GetCachedRuntimeContextForDiagnostics()
	{
		try
		{
			return State.CachedRuntimeContext;
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
		return Volatile.Read(ref _hangDumpEnabled) != 0
			&& Volatile.Read(ref _saveInProgress) == 0
			&& Volatile.Read(ref _aiInteractionActive) != 0;
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
			return FreezeWatchState.Sanitize(value, maxLength);
		}
		catch
		{
			return "";
		}
	}
}
