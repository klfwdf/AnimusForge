using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class ConversationExceptionGuard
{
	private const string LogSource = "ConversationSafety";

	private static readonly Dictionary<string, int> _suppressedCounts = new Dictionary<string, int>(StringComparer.Ordinal);

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
