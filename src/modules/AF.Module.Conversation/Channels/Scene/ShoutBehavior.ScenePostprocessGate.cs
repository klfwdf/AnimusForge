using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.SceneMovementController;
using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

internal sealed partial class SceneConversationSessionRuntime
{
	private readonly object _scenePostprocessGateLock = new object();

	private int _pendingScenePostprocessActionCount = 0;

	private TaskCompletionSource<bool> _scenePostprocessIdleTcs = null;

	private bool _isWaitingForScenePostprocessGate = false;

	private long _scenePostprocessWaitOwnerSequence;

	private Task _scenePostprocessWaitGate;

	private bool _scenePostprocessWaitBorrowedProcessingFlag;

	private long _scenePostprocessWaitProcessingSequence;

	internal void RegisterScenePostprocessGateTask(Task task)
	{
		if (task == null || task.IsCompleted)
		{
			return;
		}
		TaskCompletionSource<bool> registeredGate;
		lock (_scenePostprocessGateLock)
		{
			if (_pendingScenePostprocessActionCount <= 0 || _scenePostprocessIdleTcs == null || _scenePostprocessIdleTcs.Task.IsCompleted)
			{
				_pendingScenePostprocessActionCount = 0;
				_scenePostprocessIdleTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			}
			_pendingScenePostprocessActionCount++;
			registeredGate = _scenePostprocessIdleTcs;
		}
		task.ContinueWith(delegate(Task completedTask)
		{
			try
			{
				if (completedTask != null && completedTask.IsFaulted)
				{
					Logger.Log("ShoutBehavior", "[ERROR] Scene postprocess gate task faulted: " + (completedTask.Exception?.GetBaseException()?.Message ?? "unknown"));
				}
			}
			catch
			{
			}
			TaskCompletionSource<bool> taskCompletionSource = null;
			lock (_scenePostprocessGateLock)
			{
				// Force-clear or scene reset may already have installed another request's gate.
				if (!ReferenceEquals(_scenePostprocessIdleTcs, registeredGate))
				{
					return;
				}
				_pendingScenePostprocessActionCount--;
				if (_pendingScenePostprocessActionCount <= 0)
				{
					_pendingScenePostprocessActionCount = 0;
					taskCompletionSource = _scenePostprocessIdleTcs;
					_scenePostprocessIdleTcs = null;
				}
			}
			taskCompletionSource?.TrySetResult(true);
		}, TaskScheduler.Default);
	}

	internal Task GetScenePostprocessGateTask()
	{
		lock (_scenePostprocessGateLock)
		{
			if (_pendingScenePostprocessActionCount > 0 && _scenePostprocessIdleTcs != null)
			{
				return _scenePostprocessIdleTcs.Task;
			}
		}
		return Task.CompletedTask;
	}

	internal void ForceClearScenePostprocessGate(string reason, Task expectedGate = null)
	{
		TaskCompletionSource<bool> taskCompletionSource = null;
		lock (_scenePostprocessGateLock)
		{
			if (expectedGate != null && !ReferenceEquals(_scenePostprocessIdleTcs?.Task, expectedGate))
			{
				return;
			}
			Interlocked.Increment(ref _scenePostprocessWaitOwnerSequence);
			_isWaitingForScenePostprocessGate = false;
			_scenePostprocessWaitGate = null;
			_scenePostprocessWaitBorrowedProcessingFlag = false;
			if (_pendingScenePostprocessActionCount <= 0 && _scenePostprocessIdleTcs == null)
			{
				return;
			}
			Logger.Log("ShoutBehavior", "[WARN] ScenePostprocessGate force_clear reason=" + (reason ?? "") + " pending=" + _pendingScenePostprocessActionCount);
			_pendingScenePostprocessActionCount = 0;
			taskCompletionSource = _scenePostprocessIdleTcs;
			_scenePostprocessIdleTcs = null;
		}
		taskCompletionSource?.TrySetResult(true);
	}

	internal async Task WaitForScenePostprocessGateAsync(string reason)
	{
		long waitGeneration = SaveRuntimeGuard.CaptureGeneration();
		int waitSceneSessionId = _ports.SceneSessionId();
		int waitConversationEpoch = Volatile.Read(ref _sceneConversationEpoch);
		Task task = GetScenePostprocessGateTask();
		if (task == null || task.IsCompleted)
		{
			return;
		}
		bool IsWaitContextCurrent()
		{
			return SaveRuntimeGuard.IsCurrentGeneration(waitGeneration)
				&& waitSceneSessionId == _ports.SceneSessionId()
				&& waitConversationEpoch == Volatile.Read(ref _sceneConversationEpoch);
		}
		long waitOwner;
		long processingSequence;
		bool restoreProcessingFlag;
		lock (_scenePostprocessGateLock)
		{
			if (task.IsCompleted || !IsWaitContextCurrent())
			{
				return;
			}
			waitOwner = Interlocked.Increment(ref _scenePostprocessWaitOwnerSequence);
			// A newer waiter inherits the same gate's flag, but never an already closed UI's busy state.
			processingSequence = _ports.ProcessingSequence();
			restoreProcessingFlag = IsProcessingShout
				|| (ReferenceEquals(_scenePostprocessWaitGate, task) && _scenePostprocessWaitBorrowedProcessingFlag
					&& _scenePostprocessWaitProcessingSequence == processingSequence);
			_scenePostprocessWaitProcessingSequence = processingSequence;
			_scenePostprocessWaitGate = task;
			_scenePostprocessWaitBorrowedProcessingFlag = restoreProcessingFlag;
			_isWaitingForScenePostprocessGate = true;
			if (restoreProcessingFlag)
			{
				IsProcessingShout = false;
			}
		}
		bool IsCurrentWaitOwner()
		{
			return IsWaitContextCurrent()
				&& waitOwner == Interlocked.Read(ref _scenePostprocessWaitOwnerSequence);
		}
		void QueueWaitMessage(string message, Color color, long messageOwner)
		{
			_ports.PostMainThread(() =>
			{
				if (IsWaitContextCurrent()
					&& messageOwner == Interlocked.Read(ref _scenePostprocessWaitOwnerSequence))
				{
					InformationManager.DisplayMessage(new InformationMessage(message, color));
				}
			});
		}
		try
		{
			Logger.Log("ShoutBehavior", "[ScenePostprocessGate] waiting reason=" + (reason ?? ""));
			QueueWaitMessage("正在等待上一轮NPC行为处理完成...", new Color(1f, 0.95f, 0.25f), waitOwner);
			Task completedTask = await Task.WhenAny(task, Task.Delay(ScenePostprocessGateWaitTimeoutMilliseconds));
			if (completedTask == task)
			{
				await task;
				Logger.Log("ShoutBehavior", "[ScenePostprocessGate] wait_done reason=" + (reason ?? ""));
			}
			else
			{
				long timeoutMessageOwner = 0L;
				lock (_scenePostprocessGateLock)
				{
					if (IsCurrentWaitOwner() && ReferenceEquals(_scenePostprocessIdleTcs?.Task, task))
					{
						// Restore only the flag borrowed by this waiter before force-clear retires it.
						if (restoreProcessingFlag && processingSequence == _ports.ProcessingSequence())
						{
							IsProcessingShout = true;
						}
						ForceClearScenePostprocessGate("wait_timeout:" + (reason ?? ""), task);
						timeoutMessageOwner = Interlocked.Read(ref _scenePostprocessWaitOwnerSequence);
					}
				}
				if (timeoutMessageOwner > 0L)
				{
					QueueWaitMessage("[场景喊话] 上一轮NPC行为处理超时，已自动重置。", new Color(1f, 0.8f, 0.2f), timeoutMessageOwner);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] ScenePostprocessGate wait failed: " + ex.Message);
		}
		finally
		{
			lock (_scenePostprocessGateLock)
			{
				if (IsCurrentWaitOwner())
				{
					if (restoreProcessingFlag && processingSequence == _ports.ProcessingSequence())
					{
						IsProcessingShout = true;
					}
					_isWaitingForScenePostprocessGate = false;
					_scenePostprocessWaitGate = null;
					_scenePostprocessWaitBorrowedProcessingFlag = false;
					Interlocked.Increment(ref _scenePostprocessWaitOwnerSequence);
				}
			}
		}
	}

}
