using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AnimusForge;

// Uses the existing composition queue, never a second pending-operation registry.
internal sealed class ConversationMainThreadActionDrain
{
private readonly ConcurrentQueue<Action> _mainThreadActions;
private long _nextSceneMainThreadActionBudgetLogTicks;
internal ConversationMainThreadActionDrain(ConcurrentQueue<Action> queue) { _mainThreadActions = queue ?? throw new ArgumentNullException(nameof(queue)); }
internal void ResetQueue() { while (_mainThreadActions.TryDequeue(out _)) { } }
	private const int SceneMainThreadActionMaxPerTick13 = 2;
	private const double SceneMainThreadActionBudgetMs13 = 6.0;
	internal const double SceneMainThreadActionSlowMs = 40.0;
internal void DrainMainThreadActionsForMissionTick()
	{
		LlmRetryPrompt.CaptureMainThreadContext();
#if BANNERLORD_1_4_OR_GREATER
		Action action;
		while (_mainThreadActions.TryDequeue(out action))
		{
			ExecuteMainThreadAction(action);
		}
#else
		int pendingAtStart = _mainThreadActions.Count;
		if (pendingAtStart <= 0)
		{
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		int processed = 0;
		int maxActions = Math.Min(SceneMainThreadActionMaxPerTick13, pendingAtStart);
		while (processed < maxActions && _mainThreadActions.TryDequeue(out var action))
		{
			processed++;
			ExecuteMainThreadAction(action);
			if (stopwatch.Elapsed.TotalMilliseconds >= SceneMainThreadActionBudgetMs13)
			{
				break;
			}
		}
		stopwatch.Stop();
		int remaining = _mainThreadActions.Count;
		if (remaining > 0 || stopwatch.Elapsed.TotalMilliseconds >= SceneMainThreadActionSlowMs)
		{
			LogMainThreadActionBudget13(pendingAtStart, processed, remaining, stopwatch.Elapsed.TotalMilliseconds);
		}
#endif
	}
internal void ExecuteMainThreadAction(Action action)
	{
		if (action == null)
		{
			return;
		}
		string actionScopeName = "ShoutBehavior.ExecuteMainThreadAction";
		try
		{
			string methodName = action.Method?.Name;
			if (!string.IsNullOrWhiteSpace(methodName))
			{
				actionScopeName += "." + methodName;
			}
		}
		catch
		{
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			using (FreezeWatchdog.Scope(actionScopeName))
			{
				FreezeWatchdog.Mark("ShoutBehavior.main_thread_action.begin", "method=" + actionScopeName + " remaining=" + _mainThreadActions.Count);
				action();
				FreezeWatchdog.Mark("ShoutBehavior.main_thread_action.end", "method=" + actionScopeName + " remaining=" + _mainThreadActions.Count);
			}
		}
		catch (Exception ex)
		{
			FreezeWatchdog.Mark("ShoutBehavior.main_thread_action.exception", ex.GetType().Name + ": " + ex.Message, immediate: true);
			BannerlordExceptionSentinel.ReportObservedException("LipSync.MainThreadAction", ex, "behavior=ShoutMissionBehavior");
		}
		finally
		{
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalMilliseconds >= SceneMainThreadActionSlowMs)
			{
				Logger.Log("ShoutBehavior", "[MainThreadActions] slow_action elapsedMs=" + Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2) + " remaining=" + _mainThreadActions.Count);
			}
		}
	}
internal void LogMainThreadActionBudget13(int pendingAtStart, int processed, int remaining, double elapsedMs)
	{
		try
		{
			long now = DateTime.UtcNow.Ticks;
			if (remaining > 0 && now < _nextSceneMainThreadActionBudgetLogTicks && elapsedMs < SceneMainThreadActionSlowMs)
			{
				return;
			}
			_nextSceneMainThreadActionBudgetLogTicks = now + TimeSpan.FromSeconds(1.0).Ticks;
			Logger.Log("ShoutBehavior", "[MainThreadActions][1.3_budget] pendingAtStart=" + pendingAtStart + " processed=" + processed + " remaining=" + remaining + " elapsedMs=" + Math.Round(elapsedMs, 2));
		}
		catch
		{
		}
	}
}
