using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class ConversationCameraSafePatch
{
	private static bool _patched;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("SandBox.View.Missions.MissionConversationCameraView");
			if (type == null)
			{
				Logger.LogTrace("System", "❌ ConversationCameraSafePatch: 找不到 MissionConversationCameraView 类型。");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.conversationcamera.safety");
			HarmonyMethod prefix = new HarmonyMethod(typeof(ConversationCameraSafePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
			HarmonyMethod finalizer = new HarmonyMethod(typeof(ConversationCameraSafePatch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.Public));
			string[] methodNames =
			{
				"UpdateAgentLooksForConversation",
				"SetFocusedObjectForCameraFocus",
				"SetConversationLookToPointOfInterest",
				"MakeAgentLookToSpeaker",
				"MakeSpeakerLookToListener"
			};
			int patchedCount = 0;
			foreach (string methodName in methodNames)
			{
				MethodInfo methodInfo = AccessTools.Method(type, methodName);
				if (methodInfo != null)
				{
					harmony.Patch(methodInfo, prefix, null, null, finalizer);
					patchedCount++;
				}
			}
			_patched = true;
			Logger.LogTrace("System", "✅ ConversationCameraSafePatch 已对 MissionConversationCameraView 相关方法打补丁，count=" + patchedCount + "。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ ConversationCameraSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(object __instance, MethodBase __originalMethod)
	{
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (ConversationExceptionGuard.TryPreemptStaleConversation(conversationManager, "MissionConversationCameraView." + (__originalMethod?.Name ?? ""), __originalMethod))
			{
				return false;
			}
			if (!IsConversationCameraStateSafe(conversationManager, out var reason))
			{
				LogSkip(__originalMethod, reason);
				return false;
			}
		}
		catch (Exception ex)
		{
			LogSkip(__originalMethod, "prefix_exception_" + ex.GetType().Name);
			return false;
		}
		return true;
	}

	public static Exception Finalizer(Exception __exception, object __instance, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		if (!(__exception is NullReferenceException))
		{
			return __exception;
		}
		try
		{
			ConversationManager conversationManager = Campaign.Current?.ConversationManager;
			if (ConversationExceptionGuard.TryPreemptStaleConversation(conversationManager, "MissionConversationCameraView." + (__originalMethod?.Name ?? ""), __originalMethod))
			{
				return null;
			}
			if (!IsConversationCameraStateSafe(conversationManager, out var reason))
			{
				LogSuppressed(__originalMethod, reason, __exception);
				TryEndConversation(conversationManager, "camera_nullref_" + reason);
				return null;
			}
		}
		catch (Exception ex)
		{
			LogSkip(__originalMethod, "finalizer_exception_" + ex.GetType().Name);
		}
		return __exception;
	}

	private static bool IsConversationCameraStateSafe(ConversationManager conversationManager, out string reason)
	{
		reason = "";
		Mission current = Mission.Current;
		if (current == null)
		{
			reason = "mission_null";
			return false;
		}
		if (current.MissionEnded)
		{
			reason = "mission_ended";
			return false;
		}
		if (current.Scene == null || current.Agents == null)
		{
			reason = "mission_scene_or_agents_null";
			return false;
		}
		if (conversationManager == null)
		{
			reason = "conversation_manager_null";
			return false;
		}
		if (!conversationManager.IsConversationInProgress)
		{
			reason = "conversation_not_in_progress";
			return false;
		}
		if (conversationManager.OneToOneConversationAgent == null)
		{
			reason = "one_to_one_agent_null";
			return false;
		}
		Agent mainAgent = Agent.Main;
		if (!IsCameraAgentUsable(mainAgent, requireVisuals: true))
		{
			reason = "main_agent_invalid";
			return false;
		}
		Agent speaker = conversationManager.SpeakerAgent as Agent;
		if (!IsCameraAgentUsable(speaker, requireVisuals: true))
		{
			reason = "speaker_agent_invalid";
			return false;
		}
		Agent listener = conversationManager.ListenerAgent as Agent;
		if (!IsCameraAgentUsable(listener, requireVisuals: true))
		{
			reason = "listener_agent_invalid";
			return false;
		}
		var conversationAgents = conversationManager.ConversationAgents;
		if (conversationAgents == null || conversationAgents.Count <= 0)
		{
			reason = "conversation_agents_empty";
			return false;
		}
		foreach (IAgent conversationAgent in conversationAgents)
		{
			if (!IsCameraAgentUsable(conversationAgent as Agent, requireVisuals: true))
			{
				reason = "conversation_agent_invalid";
				return false;
			}
		}
		return true;
	}

	private static bool IsCameraAgentUsable(Agent agent, bool requireVisuals)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return false;
			}
			if (!requireVisuals)
			{
				return true;
			}
			if (agent.AgentVisuals == null)
			{
				return false;
			}
			return agent.AgentVisuals.GetSkeleton() != null;
		}
		catch
		{
			return false;
		}
	}

	private static void TryEndConversation(ConversationManager conversationManager, string reason)
	{
		try
		{
			if (conversationManager != null && conversationManager.IsConversationInProgress)
			{
				conversationManager.EndConversation();
				Logger.Log("ConversationSafety", "Ended unsafe mission conversation camera state. reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ConversationSafety", "Failed to end unsafe mission conversation camera state. reason=" + (reason ?? "") + " error=" + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void LogSkip(MethodBase method, string reason)
	{
		Logger.LogTrace("ConversationSafety", "Skipped MissionConversationCameraView." + (method?.Name ?? "") + " reason=" + (reason ?? ""));
	}

	private static void LogSuppressed(MethodBase method, string reason, Exception exception)
	{
		Logger.Log("ConversationSafety", "Suppressed mission conversation camera null reference. method=" + (method?.Name ?? "") + " reason=" + (reason ?? "") + " exception=" + exception.GetType().Name + ": " + exception.Message);
	}
}
