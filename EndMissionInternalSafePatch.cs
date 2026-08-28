using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class EndMissionInternalSafePatch
{
	private enum PatchState
	{
		NotAttempted,
		Installing,
		Ready,
		FailedClosed
	}

	internal enum ProtectedMissionKind
	{
		NativeHostileCastleMeeting,
		AnimusForgeMeeting,
		AnimusForgeDuel
	}

	private sealed class MissionReferenceComparer : IEqualityComparer<Mission>
	{
		internal static readonly MissionReferenceComparer Instance = new MissionReferenceComparer();

		public bool Equals(Mission x, Mission y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(Mission obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}

	private const int ExpectedCleanupReplacementCount = 10;

	private static readonly object PatchSync = new object();

	private static readonly object ProtectionSync = new object();

	private static readonly Dictionary<Mission, ProtectedMissionKind> ProtectedMissions = new Dictionary<Mission, ProtectedMissionKind>(MissionReferenceComparer.Instance);

	private static volatile PatchState _patchState;

	private static volatile bool _transpilerShapeValidated;

	private static Action<Mission> _stopSoundEventsAction;

	private static Action<Mission> _freeResourcesAction;

	private static Action<Mission> _finalizeMissionAction;

	private static Action<Agent> _agentOnRemoveAction;

	private static Action<Agent> _agentOnDeleteAction;

	private static Action<Agent> _agentClearAction;

	internal static bool IsNativeMeetingProtectionReady => _patchState == PatchState.Ready;

	public static void EnsurePatched()
	{
		if (_patchState != PatchState.NotAttempted)
		{
			return;
		}
		lock (PatchSync)
		{
			if (_patchState != PatchState.NotAttempted)
			{
				return;
			}
			_patchState = PatchState.Installing;
			_transpilerShapeValidated = false;
			try
			{
				MethodInfo target = AccessTools.Method(typeof(Mission), "EndMissionInternal", Type.EmptyTypes);
				MethodInfo stopSoundEvents = AccessTools.Method(typeof(Mission), "StopSoundEvents", Type.EmptyTypes);
				MethodInfo freeResources = AccessTools.Method(typeof(Mission), "FreeResources", Type.EmptyTypes);
				MethodInfo finalizeMission = AccessTools.Method(typeof(Mission), "FinalizeMission", Type.EmptyTypes);
				MethodInfo agentOnRemove = AccessTools.Method(typeof(Agent), "OnRemove", Type.EmptyTypes);
				MethodInfo agentOnDelete = AccessTools.Method(typeof(Agent), "OnDelete", Type.EmptyTypes);
				MethodInfo agentClear = AccessTools.Method(typeof(Agent), "Clear", Type.EmptyTypes);
				if (!HasSupportedMissionMethodSignature(target))
				{
					Logger.LogTrace("System", "EndMissionInternalSafePatch failed closed because Mission.EndMissionInternal has an unsupported signature.");
					return;
				}
				if (!TryCreateInstanceAction(stopSoundEvents, out _stopSoundEventsAction)
					|| !TryCreateInstanceAction(freeResources, out _freeResourcesAction)
					|| !TryCreateInstanceAction(finalizeMission, out _finalizeMissionAction)
					|| !TryCreateInstanceAction(agentOnRemove, out _agentOnRemoveAction)
					|| !TryCreateInstanceAction(agentOnDelete, out _agentOnDeleteAction)
					|| !TryCreateInstanceAction(agentClear, out _agentClearAction))
				{
					Logger.LogTrace("System", "EndMissionInternalSafePatch failed closed because a private cleanup delegate could not be created.");
					return;
				}
				Harmony harmony = new Harmony("AnimusForge.mission.endmissioninternal.protected-cleanup");
				HarmonyMethod transpiler = new HarmonyMethod(typeof(EndMissionInternalSafePatch).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.Public));
				HarmonyMethod finalizer = new HarmonyMethod(typeof(EndMissionInternalSafePatch).GetMethod(nameof(RegistrationFinalizer), BindingFlags.Static | BindingFlags.Public));
				harmony.Patch(target, null, null, transpiler, finalizer);
				_patchState = _transpilerShapeValidated ? PatchState.Ready : PatchState.FailedClosed;
				Logger.LogTrace("System", IsNativeMeetingProtectionReady
					? "EndMissionInternalSafePatch installed with ten validated protected cleanup wrappers."
					: "EndMissionInternalSafePatch failed closed because the cleanup IL shape did not match the validated contract.");
			}
			catch (Exception ex)
			{
				_patchState = PatchState.FailedClosed;
				Logger.LogTrace("System", "EndMissionInternalSafePatch failed closed: " + ex.Message);
			}
			finally
			{
				if (_patchState == PatchState.Installing)
				{
					_patchState = PatchState.FailedClosed;
				}
			}
		}
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> original = new List<CodeInstruction>(instructions);
		Dictionary<int, MethodInfo> replacements = new Dictionary<int, MethodInfo>();
		Dictionary<string, int> matchCounts = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < original.Count; i++)
		{
			if (!(original[i].operand is MethodInfo calledMethod) || !TryResolveCleanupWrapper(calledMethod, out MethodInfo wrapper, out string key))
			{
				continue;
			}
			replacements[i] = wrapper;
			matchCounts.TryGetValue(key, out int count);
			matchCounts[key] = count + 1;
		}
		if (replacements.Count != ExpectedCleanupReplacementCount || matchCounts.Count != ExpectedCleanupReplacementCount)
		{
			PublishTranspilerShapeResult(valid: false);
			return original;
		}
		foreach (KeyValuePair<string, int> match in matchCounts)
		{
			if (match.Value != 1)
			{
				PublishTranspilerShapeResult(valid: false);
				return original;
			}
		}
		List<CodeInstruction> rewritten = new List<CodeInstruction>(original.Count + ExpectedCleanupReplacementCount);
		for (int i = 0; i < original.Count; i++)
		{
			CodeInstruction instruction = original[i];
			if (!replacements.TryGetValue(i, out MethodInfo wrapper))
			{
				rewritten.Add(instruction);
				continue;
			}
			if (wrapper.GetParameters().Length == 2)
			{
				CodeInstruction loadMission = new CodeInstruction(OpCodes.Ldarg_0);
				loadMission.labels.AddRange(instruction.labels);
				instruction.labels.Clear();
				loadMission.blocks.AddRange(instruction.blocks);
				instruction.blocks.Clear();
				rewritten.Add(loadMission);
			}
			instruction.opcode = OpCodes.Call;
			instruction.operand = wrapper;
			rewritten.Add(instruction);
		}
		PublishTranspilerShapeResult(valid: true);
		return rewritten;
	}

	private static void PublishTranspilerShapeResult(bool valid)
	{
		lock (PatchSync)
		{
			_transpilerShapeValidated = valid;
			if (!valid && _patchState == PatchState.Ready)
			{
				_patchState = PatchState.FailedClosed;
			}
		}
	}

	internal static bool TryRegisterProtectedMission(Mission mission, ProtectedMissionKind kind, string reason)
	{
		if (mission == null || !IsNativeMeetingProtectionReady)
		{
			return false;
		}
		lock (ProtectionSync)
		{
			ProtectedMissions[mission] = kind;
		}
		Logger.LogTrace("MissionCleanup", $"Registered protected mission cleanup. Kind={kind}, Reason={reason ?? "N/A"}");
		return true;
	}

	internal static void UnregisterProtectedMission(Mission mission, string reason)
	{
		if (mission == null)
		{
			return;
		}
		bool removed;
		lock (ProtectionSync)
		{
			removed = ProtectedMissions.Remove(mission);
		}
		if (removed)
		{
			Logger.LogTrace("MissionCleanup", "Unregistered protected mission cleanup. Reason=" + (reason ?? "N/A"));
		}
	}

	public static void SafeMissionListenerOnEndMission(IMissionListener listener, Mission mission)
	{
		if (!TryGetProtection(mission, out ProtectedMissionKind kind))
		{
			listener.OnEndMission();
			return;
		}
		try
		{
			listener?.OnEndMission();
		}
		catch (NullReferenceException ex)
		{
			LogProtectedCleanupFailure(kind, "listener_on_end_mission", ex);
		}
	}

	public static void SafeStopSoundEvents(Mission mission)
	{
		ExecuteMissionCleanup(mission, "stop_sound_events", _stopSoundEventsAction);
	}

	public static void SafeMissionBehaviorOnEndMissionInternal(MissionBehavior behavior, Mission mission)
	{
		if (!TryGetProtection(mission, out ProtectedMissionKind kind))
		{
			behavior.OnEndMissionInternal();
			return;
		}
		try
		{
			behavior?.OnEndMissionInternal();
		}
		catch (NullReferenceException ex)
		{
			LogProtectedCleanupFailure(kind, "mission_behavior_on_end_internal", ex);
		}
	}

	public static void SafeAgentOnRemove(Agent agent, Mission mission)
	{
		ExecuteAgentCleanup(agent, mission, "agent_on_remove", _agentOnRemoveAction);
	}

	public static void SafeAgentOnDelete(Agent agent, Mission mission)
	{
		ExecuteAgentCleanup(agent, mission, "agent_on_delete", _agentOnDeleteAction);
	}

	public static void SafeAgentClear(Agent agent, Mission mission)
	{
		ExecuteAgentCleanup(agent, mission, "agent_clear", _agentClearAction);
	}

	public static void SafeFocusableObjectInformationProviderOnFinalize(MissionFocusableObjectInformationProvider provider, Mission mission)
	{
		if (!TryGetProtection(mission, out ProtectedMissionKind kind))
		{
			provider.OnFinalize();
			return;
		}
		try
		{
			provider?.OnFinalize();
		}
		catch (NullReferenceException ex)
		{
			LogProtectedCleanupFailure(kind, "focusable_provider_finalize", ex);
		}
	}

	public static void SafeMissionObjectOnEndMission(MissionObject missionObject, Mission mission)
	{
		if (!TryGetProtection(mission, out ProtectedMissionKind kind))
		{
			missionObject.OnEndMission();
			return;
		}
		try
		{
			missionObject?.OnEndMission();
		}
		catch (NullReferenceException ex)
		{
			LogProtectedCleanupFailure(kind, "mission_object_on_end_mission", ex);
		}
	}

	public static void SafeFreeResources(Mission mission)
	{
		ExecuteMissionCleanup(mission, "free_resources", _freeResourcesAction);
	}

	public static void SafeFinalizeMission(Mission mission)
	{
		bool protectedMission = TryGetProtection(mission, out _);
		try
		{
			ExecuteMissionCleanup(mission, "finalize_mission", _finalizeMissionAction);
		}
		finally
		{
			try
			{
				if (protectedMission)
				{
					LordEncounterRedirectGuard.SuppressForSeconds(1f);
				}
			}
			finally
			{
				UnregisterProtectedMission(mission, "finalize_mission");
			}
		}
	}

	public static Exception RegistrationFinalizer(Mission __instance, Exception __exception)
	{
		try
		{
			UnregisterProtectedMission(__instance, __exception == null ? "end_mission_internal_completed" : "end_mission_internal_exception");
		}
		catch
		{
		}
		return __exception;
	}

	private static bool HasSupportedMissionMethodSignature(MethodInfo method)
	{
		return method != null && !method.IsStatic && method.ReturnType == typeof(void) && method.GetParameters().Length == 0;
	}

	private static bool TryCreateInstanceAction<T>(MethodInfo method, out Action<T> action)
	{
		action = null;
		if (!HasSupportedMissionMethodSignature(method))
		{
			return false;
		}
		try
		{
			action = (Action<T>)method.CreateDelegate(typeof(Action<T>));
			return action != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryResolveCleanupWrapper(MethodInfo calledMethod, out MethodInfo wrapper, out string key)
	{
		wrapper = null;
		key = null;
		if (MethodMatches(calledMethod, typeof(IMissionListener), "OnEndMission"))
		{
			key = "listener_on_end_mission";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeMissionListenerOnEndMission));
		}
		else if (MethodMatches(calledMethod, typeof(Mission), "StopSoundEvents"))
		{
			key = "stop_sound_events";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeStopSoundEvents));
		}
		else if (MethodMatches(calledMethod, typeof(MissionBehavior), "OnEndMissionInternal"))
		{
			key = "mission_behavior_on_end_internal";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeMissionBehaviorOnEndMissionInternal));
		}
		else if (MethodMatches(calledMethod, typeof(Agent), "OnRemove"))
		{
			key = "agent_on_remove";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeAgentOnRemove));
		}
		else if (MethodMatches(calledMethod, typeof(Agent), "OnDelete"))
		{
			key = "agent_on_delete";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeAgentOnDelete));
		}
		else if (MethodMatches(calledMethod, typeof(Agent), "Clear"))
		{
			key = "agent_clear";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeAgentClear));
		}
		else if (MethodMatches(calledMethod, typeof(MissionFocusableObjectInformationProvider), "OnFinalize"))
		{
			key = "focusable_provider_finalize";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeFocusableObjectInformationProviderOnFinalize));
		}
		else if (MethodMatches(calledMethod, typeof(MissionObject), "OnEndMission"))
		{
			key = "mission_object_on_end_mission";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeMissionObjectOnEndMission));
		}
		else if (MethodMatches(calledMethod, typeof(Mission), "FreeResources"))
		{
			key = "free_resources";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeFreeResources));
		}
		else if (MethodMatches(calledMethod, typeof(Mission), "FinalizeMission"))
		{
			key = "finalize_mission";
			wrapper = AccessTools.Method(typeof(EndMissionInternalSafePatch), nameof(SafeFinalizeMission));
		}
		return wrapper != null;
	}

	private static bool MethodMatches(MethodInfo method, Type declaringType, string name)
	{
		return method != null
			&& method.DeclaringType == declaringType
			&& string.Equals(method.Name, name, StringComparison.Ordinal)
			&& method.ReturnType == typeof(void)
			&& method.GetParameters().Length == 0;
	}

	private static bool TryGetProtection(Mission mission, out ProtectedMissionKind kind)
	{
		kind = default;
		if (mission == null)
		{
			return false;
		}
		lock (ProtectionSync)
		{
			if (ProtectedMissions.TryGetValue(mission, out kind))
			{
				return true;
			}
		}
		if (!IsNativeMeetingProtectionReady || !TryRecognizeAnimusForgeMission(mission, out kind))
		{
			return false;
		}
		return TryRegisterProtectedMission(mission, kind, "mission_behavior_fallback");
	}

	private static bool TryRecognizeAnimusForgeMission(Mission mission, out ProtectedMissionKind kind)
	{
		kind = default;
		try
		{
			if (mission.GetMissionBehavior<MeetingBattleLockMissionBehavior>() != null)
			{
				kind = ProtectedMissionKind.AnimusForgeMeeting;
				return true;
			}
			if (DuelBehavior.IsAnimusForgeIndependentDuelMission(mission))
			{
				kind = ProtectedMissionKind.AnimusForgeDuel;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void ExecuteMissionCleanup(Mission mission, string step, Action<Mission> cleanup)
	{
		if (!TryGetProtection(mission, out ProtectedMissionKind kind))
		{
			cleanup(mission);
			return;
		}
		try
		{
			cleanup(mission);
		}
		catch (NullReferenceException ex)
		{
			LogProtectedCleanupFailure(kind, step, ex);
		}
	}

	private static void ExecuteAgentCleanup(Agent agent, Mission mission, string step, Action<Agent> cleanup)
	{
		if (!TryGetProtection(mission, out ProtectedMissionKind kind))
		{
			cleanup(agent);
			return;
		}
		try
		{
			if (agent != null)
			{
				cleanup(agent);
			}
		}
		catch (NullReferenceException ex)
		{
			LogProtectedCleanupFailure(kind, step, ex);
		}
	}

	private static void LogProtectedCleanupFailure(ProtectedMissionKind kind, string step, NullReferenceException exception)
	{
		Logger.LogTrace("MissionCleanup", $"Protected cleanup step ignored a null reference and continued. Kind={kind}, Step={step}, Error={exception.Message}");
	}
}
