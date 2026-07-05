using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class ConversationExceptionGuard
{
	private const string LogSource = "ConversationSafety";

	private static readonly Dictionary<string, int> _suppressedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
	private static readonly Dictionary<string, int> _cleanupFailureCounts = new Dictionary<string, int>(StringComparer.Ordinal);
	private static readonly object _pendingLock = new object();
	private static object _pendingStaleConversationManager;
	private static string _pendingStaleConversationReason = "";
	private static int _pendingStaleConversationUntilTick;

	internal static void MarkCurrentConversationStale(string reason)
	{
		object manager = null;
		try
		{
			manager = Campaign.Current?.ConversationManager;
		}
		catch
		{
			manager = null;
		}
		if (manager == null)
		{
			return;
		}
		lock (_pendingLock)
		{
			_pendingStaleConversationManager = manager;
			_pendingStaleConversationReason = string.IsNullOrWhiteSpace(reason) ? "stale_conversation" : reason.Trim();
			_pendingStaleConversationUntilTick = unchecked(Environment.TickCount + 15000);
		}
		Logger.Log(LogSource, "Marked stale conversation. reason=" + _pendingStaleConversationReason);
	}

	internal static bool TryPreemptStaleConversation(object conversationManager, string context, MethodBase originalMethod)
	{
		try
		{
			if (!IsPendingStaleConversation(conversationManager, out string reason))
			{
				return false;
			}
			LogSuppressed(context, originalMethod, "preempt_" + reason, new NullReferenceException("stale conversation preempted"));
			TryEndStaleConversation(conversationManager, "preempt_" + reason);
			ClearPendingStaleConversation(conversationManager);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static Exception Filter(Exception exception, object conversationManager, string context, MethodBase originalMethod)
	{
		if (exception == null)
		{
			return null;
		}
		if (!(exception is NullReferenceException))
		{
			return exception;
		}
		try
		{
			if (!ShouldSuppressNullReference(conversationManager, context, out string reason))
			{
				return exception;
			}
			LogSuppressed(context, originalMethod, reason, exception);
			TryEndStaleConversation(conversationManager, reason);
			ClearPendingStaleConversation(conversationManager);
			return null;
		}
		catch
		{
			return exception;
		}
	}

	private static bool ShouldSuppressNullReference(object manager, string context, out string reason)
	{
		reason = "";
		if (manager == null)
		{
			reason = "manager_null";
			return true;
		}
		Campaign campaign = Campaign.Current;
		if (campaign == null)
		{
			reason = "campaign_null";
			return true;
		}
		object currentManager = SafeGetProperty(campaign, "ConversationManager");
		if (currentManager == null)
		{
			reason = "current_manager_null";
			return true;
		}
		if (!ReferenceEquals(currentManager, manager))
		{
			reason = "stale_manager_instance";
			return true;
		}
		if (IsPendingStaleConversation(manager, out string pendingReason))
		{
			reason = "pending_stale_" + pendingReason;
			return true;
		}
		if (IsEncounterLeavePendingForConversation(context, out reason))
		{
			return true;
		}
		if (IsMissionInvalidForConversation(out reason))
		{
			return true;
		}
		if (TryGetBoolProperty(manager, "IsConversationInProgress", out bool inProgress) && !inProgress)
		{
			reason = "conversation_not_in_progress";
			return true;
		}
		if (IsConversationAgentsInvalid(manager, out reason))
		{
			return true;
		}
		if (IsContextStateInvalid(manager, context, out reason))
		{
			return true;
		}
		reason = "";
		return false;
	}

	private static bool IsPendingStaleConversation(object manager, out string reason)
	{
		reason = "";
		lock (_pendingLock)
		{
			if (_pendingStaleConversationManager == null)
			{
				return false;
			}
			if (unchecked(Environment.TickCount - _pendingStaleConversationUntilTick) > 0)
			{
				_pendingStaleConversationManager = null;
				_pendingStaleConversationReason = "";
				_pendingStaleConversationUntilTick = 0;
				return false;
			}
			if (manager != null && !ReferenceEquals(_pendingStaleConversationManager, manager))
			{
				return false;
			}
			reason = string.IsNullOrWhiteSpace(_pendingStaleConversationReason) ? "stale_conversation" : _pendingStaleConversationReason;
			return true;
		}
	}

	private static void ClearPendingStaleConversation(object manager)
	{
		lock (_pendingLock)
		{
			if (_pendingStaleConversationManager == null)
			{
				return;
			}
			if (manager == null || ReferenceEquals(_pendingStaleConversationManager, manager))
			{
				_pendingStaleConversationManager = null;
				_pendingStaleConversationReason = "";
				_pendingStaleConversationUntilTick = 0;
			}
		}
	}

	private static bool IsEncounterLeavePendingForConversation(string context, out string reason)
	{
		reason = "";
		if (!IsConversationFlowContext(context))
		{
			return false;
		}
		try
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.LeaveEncounter)
			{
				reason = "encounter_leave_pending";
				return true;
			}
		}
		catch (NullReferenceException)
		{
			reason = "encounter_leave_state_nullref";
			return true;
		}
		catch
		{
		}
		return false;
	}

	private static bool IsConversationFlowContext(string context)
	{
		return string.Equals(context, "ContinueConversation", StringComparison.Ordinal)
			|| string.Equals(context, "ProcessPartnerSentence", StringComparison.Ordinal)
			|| string.Equals(context, "ProcessSentence", StringComparison.Ordinal)
			|| string.Equals(context, "UpdateSpeakerAndListenerAgents", StringComparison.Ordinal);
	}

	private static bool IsMissionInvalidForConversation(out string reason)
	{
		reason = "";
		try
		{
			Mission mission = Mission.Current;
			if (mission == null)
			{
				return false;
			}
			if (mission.MissionEnded)
			{
				reason = "mission_ended";
				return true;
			}
			if (mission.Scene == null)
			{
				reason = "mission_scene_null";
				return true;
			}
		}
		catch (NullReferenceException)
		{
			reason = "mission_state_nullref";
			return true;
		}
		catch
		{
		}
		return false;
	}

	private static bool IsConversationAgentsInvalid(object manager, out string reason)
	{
		reason = "";
		object agents = SafeGetProperty(manager, "ConversationAgents") ?? SafeGetField(manager, "_conversationAgents");
		if (agents == null)
		{
			reason = "conversation_agents_null";
			return true;
		}
		if (TryGetCount(agents, out int count) && count <= 0)
		{
			reason = "conversation_agents_empty";
			return true;
		}
		return false;
	}

	private static bool IsContextStateInvalid(object manager, string context, out string reason)
	{
		reason = "";
		if (string.Equals(context, "UpdateSpeakerAndListenerAgents", StringComparison.Ordinal))
		{
			object mainAgent = SafeGetField(manager, "_mainAgent");
			if (IsAgentReferenceInvalid(mainAgent, requireCharacter: false))
			{
				reason = "main_agent_invalid";
				return true;
			}
			return false;
		}
		if (string.Equals(context, "ContinueConversation", StringComparison.Ordinal))
		{
			if (IsCurOptionsInvalid(manager))
			{
				reason = "cur_options_invalid";
				return true;
			}
			if (IsAgentReferenceInvalid(SafeGetProperty(manager, "ListenerAgent"), requireCharacter: true))
			{
				reason = "listener_agent_invalid";
				return true;
			}
			return false;
		}
		if (string.Equals(context, "ProcessSentence", StringComparison.Ordinal))
		{
			if (IsAgentReferenceInvalid(SafeGetProperty(manager, "SpeakerAgent"), requireCharacter: true))
			{
				reason = "speaker_agent_invalid";
				return true;
			}
			if (IsAgentReferenceInvalid(SafeGetProperty(manager, "ListenerAgent"), requireCharacter: true))
			{
				reason = "listener_agent_invalid";
				return true;
			}
			return false;
		}
		if (string.Equals(context, "ProcessPartnerSentence", StringComparison.Ordinal))
		{
			object mainAgent = SafeGetField(manager, "_mainAgent");
			if (IsAgentReferenceInvalid(mainAgent, requireCharacter: false))
			{
				reason = "main_agent_invalid";
				return true;
			}
		}
		return false;
	}

	private static bool IsCurOptionsInvalid(object manager)
	{
		object curOptions = SafeGetProperty(manager, "CurOptions");
		if (curOptions == null)
		{
			return true;
		}
		return TryGetCount(curOptions, out int count) && count < 0;
	}

	private static bool IsAgentReferenceInvalid(object agent, bool requireCharacter)
	{
		if (agent == null)
		{
			return true;
		}
		if (!requireCharacter)
		{
			return false;
		}
		object character = SafeGetProperty(agent, "Character");
		return character == null;
	}

	private static void TryEndStaleConversation(object manager, string reason)
	{
		if (manager == null)
		{
			return;
		}
		try
		{
			if (!TryGetBoolProperty(manager, "IsConversationInProgress", out bool inProgress) || inProgress)
			{
				MethodInfo endConversation = manager.GetType().GetMethod("EndConversation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (endConversation != null)
				{
					endConversation.Invoke(manager, null);
					return;
				}
			}
		}
		catch (Exception ex)
		{
			LogCleanupFailure("end_conversation", reason, ex);
		}
		TryForceDeactivateConversation(manager, reason);
	}

	private static void TryForceDeactivateConversation(object manager, string reason)
	{
		try
		{
			SafeSetAutoPropertyBackingField(manager, "IsConversationInProgress", false);
			SafeSetField(manager, "ActiveToken", 4);
			SafeSetField(manager, "_currentSentence", -1);
			SafeSetField(manager, "_currentSentenceText", null);
			SafeSetField(manager, "_conversationParty", null);
			SafeSetField(manager, "_speakerAgent", null);
			SafeSetField(manager, "_listenerAgent", null);
			SafeSetField(manager, "_mainAgent", null);
			SafeClearList(SafeGetProperty(manager, "CurOptions"));
			SafeClearList(SafeGetField(manager, "_conversationAgents"));
			SafeClearList(SafeGetField(manager, "_dialogRepeatObjects"));
			SafeClearList(SafeGetField(manager, "_dialogRepeatLines"));
			Logger.Log(LogSource, "Force deactivated stale conversation. reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			LogCleanupFailure("force_deactivate", reason, ex);
		}
	}

	private static object SafeGetProperty(object target, string propertyName)
	{
		if (target == null || string.IsNullOrWhiteSpace(propertyName))
		{
			return null;
		}
		try
		{
			PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return property?.GetValue(target, null);
		}
		catch
		{
			return null;
		}
	}

	private static object SafeGetField(object target, string fieldName)
	{
		if (target == null || string.IsNullOrWhiteSpace(fieldName))
		{
			return null;
		}
		try
		{
			FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return field?.GetValue(target);
		}
		catch
		{
			return null;
		}
	}

	private static bool SafeSetAutoPropertyBackingField(object target, string propertyName, object value)
	{
		return SafeSetField(target, "<" + propertyName + ">k__BackingField", value);
	}

	private static bool SafeSetField(object target, string fieldName, object value)
	{
		if (target == null || string.IsNullOrWhiteSpace(fieldName))
		{
			return false;
		}
		try
		{
			FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
			{
				return false;
			}
			field.SetValue(target, value);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void SafeClearList(object value)
	{
		try
		{
			if (value is IList list)
			{
				list.Clear();
			}
		}
		catch
		{
		}
	}

	private static bool TryGetBoolProperty(object target, string propertyName, out bool value)
	{
		value = false;
		try
		{
			object raw = SafeGetProperty(target, propertyName);
			if (raw is bool boolValue)
			{
				value = boolValue;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool TryGetCount(object value, out int count)
	{
		count = -1;
		try
		{
			if (value is ICollection collection)
			{
				count = collection.Count;
				return true;
			}
			object raw = SafeGetProperty(value, "Count");
			if (raw is int intValue)
			{
				count = intValue;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void LogCleanupFailure(string phase, string reason, Exception exception)
	{
		string key = (phase ?? "") + ":" + (reason ?? "");
		int count;
		lock (_cleanupFailureCounts)
		{
			_cleanupFailureCounts.TryGetValue(key, out count);
			count++;
			_cleanupFailureCounts[key] = count;
		}
		if (count > 3)
		{
			return;
		}
		Exception reported = exception is TargetInvocationException targetInvocationException && targetInvocationException.InnerException != null
			? targetInvocationException.InnerException
			: exception;
		Logger.Log(LogSource, "Stale conversation cleanup failed. phase=" + (phase ?? "") +
			", reason=" + (reason ?? "") +
			", count=" + count +
			", exception=" + reported.GetType().Name + ": " + reported.Message);
	}

	private static void LogSuppressed(string context, MethodBase originalMethod, string reason, Exception exception)
	{
		string key = context ?? "";
		int count;
		lock (_suppressedCounts)
		{
			_suppressedCounts.TryGetValue(key, out count);
			count++;
			_suppressedCounts[key] = count;
		}
		if (count > 3)
		{
			return;
		}
		Logger.Log(LogSource, "Suppressed conversation null reference. context=" + key +
			", count=" + count +
			", method=" + (originalMethod?.Name ?? "null") +
			", reason=" + (reason ?? "") +
			", exception=" + exception.GetType().Name + ": " + exception.Message);
	}
}
