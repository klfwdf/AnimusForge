using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

public static class ContinueConversationSafePatch
{
	private static readonly HashSet<string> PatchedEntries = new HashSet<string>();
	private static readonly HashSet<string> ReportedUnavailableEntries = new HashSet<string>();

	public static void EnsurePatched()
	{
		// The map VM invokes a view callback; mouse and keyboard then converge on the mission VM.
		// Block before the map callback or mission command runs, with a final guard at the manager.
		if (PatchedEntries.Count == 3) return;
		PatchEntry("TaleWorlds.CampaignSystem.Conversation.ConversationManager", "ContinueConversation", nameof(Prefix), true);
		PatchEntry("TaleWorlds.CampaignSystem.ViewModelCollection.Conversation.MissionConversationVM", "ExecuteContinue", nameof(UiContinuePrefix), false);
		PatchEntry("TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapConversation.MapConversationVM", "ExecuteContinue", nameof(UiContinuePrefix), false);
	}

	private static void PatchEntry(string typeName, string methodName, string prefixName, bool withFinalizer)
	{
		if (PatchedEntries.Contains(typeName)) return;
		try
		{
			MethodInfo method = AccessTools.Method(AccessTools.TypeByName(typeName), methodName, Type.EmptyTypes);
			if (method == null)
			{
				if (ReportedUnavailableEntries.Add(typeName))
					Logger.Log("NativeConversationUI", "[WARN] Continue guard entry unavailable: " + typeName + "." + methodName);
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.continueconversation.safety");
			HarmonyMethod prefix = new HarmonyMethod(typeof(ContinueConversationSafePatch), prefixName);
			HarmonyMethod finalizer = withFinalizer ? new HarmonyMethod(typeof(ContinueConversationSafePatch), nameof(Finalizer)) : null;
			harmony.Patch(method, prefix, null, null, finalizer);
			PatchedEntries.Add(typeName);
			Logger.Log("NativeConversationUI", "Continue guard installed: " + typeName + "." + methodName);
		}
		catch (Exception ex)
		{
			if (ReportedUnavailableEntries.Add(typeName))
				Logger.Log("NativeConversationUI", "[WARN] Continue guard installation failed: " + typeName + "." + methodName + ": " + ex.Message);
		}
	}

	public static bool Prefix(object __instance, MethodBase __originalMethod)
	{
		if (ShouldBlockContinue(__originalMethod)) return false;
		return !ConversationExceptionGuard.TryPreemptStaleConversation(__instance, "ContinueConversation", __originalMethod);
	}

	public static bool UiContinuePrefix(MethodBase __originalMethod) => !ShouldBlockContinue(__originalMethod);

	private static bool ShouldBlockContinue(MethodBase source)
	{
		// AI ownership lasts until a mode switch/close, including idle, streaming, audio and auxiliary UI.
		// Keep the pre-existing backend guard for NPC openings before the overlay has entered AI mode.
		try
		{
			if (AnimusForgeNativeConversationOverlay.IsAiModeBlockingNativeContinue()
				|| ShoutBehavior.IsNativeConversationBackendBusy())
			{
				try
				{
					Logger.LogVerbose("NativeConversationUI", "ai_native_continue_blocked",
						() => "Blocked native continue while AI owns conversation: " + source?.DeclaringType?.Name + "." + source?.Name, 1.0);
				}
				catch { } // Diagnostic failure must never release a blocked click.
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public static Exception Finalizer(Exception __exception, object __instance, MethodBase __originalMethod)
	{
		return ConversationExceptionGuard.Filter(__exception, __instance, "ContinueConversation", __originalMethod);
	}
}
