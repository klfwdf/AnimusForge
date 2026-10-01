using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class CompatibilityAudit
{
	private sealed class AuditCounters
	{
		public int CriticalOk;

		public int CriticalFail;

		public int OptionalOk;

		public int OptionalFail;
	}

	private static bool _hasRun;

	internal static void RunStartupAudit()
	{
		if (_hasRun)
		{
			return;
		}
		_hasRun = true;
		AuditCounters auditCounters = new AuditCounters();
		Logger.LogCompatibilityAudit("CompatAudit", "====== Startup compatibility audit begin ======");
		LogEnvironment();
		RunSection(auditCounters, "Encounter & Conversation", AuditEncounterAndConversation);
		RunSection(auditCounters, "Battle & Deployment", AuditBattleAndDeployment);
		RunSection(auditCounters, "Stealth Hideout & Scene Flow", AuditStealthHideoutAndSceneFlow);
		RunSection(auditCounters, "Quest & Issue Bridge", AuditQuestAndIssueBridge);
		RunSection(auditCounters, "UI & Input", AuditUiAndInput);
		RunSection(auditCounters, "Optional Expansion Signals", AuditOptionalExpansionSignals);
		Logger.LogCompatibilityAudit("CompatAudit", $"Summary CriticalOK={auditCounters.CriticalOk} CriticalFail={auditCounters.CriticalFail} OptionalOK={auditCounters.OptionalOk} OptionalFail={auditCounters.OptionalFail}");
		Logger.LogCompatibilityAudit("CompatAudit", "====== Startup compatibility audit end ======");
	}

	private static void LogEnvironment()
	{
		try
		{
			Assembly assembly = typeof(SubModule).Assembly;
			string text = "";
			try
			{
				text = assembly.Location;
			}
			catch
			{
				text = "";
			}
			Logger.LogCompatibilityAudit("CompatAudit", $"AnimusForgeAssembly={assembly.GetName().Name} Version={assembly.GetName().Version} Path={text}");
			LogAssemblyVersionIfLoaded("TaleWorlds.CampaignSystem");
			LogAssemblyVersionIfLoaded("TaleWorlds.MountAndBlade");
			LogAssemblyVersionIfLoaded("TaleWorlds.MountAndBlade.View");
			LogAssemblyVersionIfLoaded("SandBox");
			LogAssemblyVersionIfLoaded("SandBox.View");
			LogAssemblyVersionIfLoaded("TaleWorlds.InputSystem");
		}
		catch (Exception ex)
		{
			Logger.LogCompatibilityAudit("CompatAudit", "Environment logging failed: " + ex.Message);
		}
	}

	private static void LogAssemblyVersionIfLoaded(string assemblyName)
	{
		try
		{
			Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
			if (assembly == null)
			{
				Logger.LogCompatibilityAudit("CompatAudit", $"Assembly {assemblyName}: not loaded yet");
				return;
			}
			Logger.LogCompatibilityAudit("CompatAudit", $"Assembly {assemblyName}: loaded version={assembly.GetName().Version}");
		}
		catch
		{
		}
	}

	private static void RunSection(AuditCounters counters, string sectionName, Action<AuditCounters> action)
	{
		int criticalOk = counters.CriticalOk;
		int criticalFail = counters.CriticalFail;
		int optionalOk = counters.OptionalOk;
		int optionalFail = counters.OptionalFail;
		Logger.LogCompatibilityAudit("CompatAudit", "--- " + sectionName + " ---");
		try
		{
			action(counters);
		}
		catch (Exception ex)
		{
			LogCheck(counters, "CompatAudit", ok: false, sectionName + " audit execution", critical: true, ex.GetType().Name + ": " + ex.Message);
		}
		Logger.LogCompatibilityAudit("CompatAudit", $"SectionSummary {sectionName}: CriticalOK={counters.CriticalOk - criticalOk} CriticalFail={counters.CriticalFail - criticalFail} OptionalOK={counters.OptionalOk - optionalOk} OptionalFail={counters.OptionalFail - optionalFail}");
	}

	private static void AuditEncounterAndConversation(AuditCounters counters)
	{
		CheckType(counters, "EncounterConversation", typeof(ConversationManager), "ConversationManager", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "OpenMapConversation", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "SetupAndStartMapConversation", critical: true, parameterCount: 3);
		CheckConversationStartEntry(counters);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "BeginConversation", critical: true, parameterCount: 0);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "StartNew", critical: true, parameterCount: 2);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "SetupAndStartMissionConversation", critical: true, parameterCount: 3);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "ContinueConversation", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "ProcessSentence", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "ProcessPartnerSentence", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(ConversationManager), "UpdateSpeakerAndListenerAgents", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(CampaignGameStarter), "AddPlayerLine", critical: true, parameterTypes: new Type[9]
		{
			typeof(string),
			typeof(string),
			typeof(string),
			typeof(string),
			typeof(ConversationSentence.OnConditionDelegate),
			typeof(ConversationSentence.OnConsequenceDelegate),
			typeof(int),
			typeof(ConversationSentence.OnClickableConditionDelegate),
			typeof(ConversationSentence.OnPersuasionOptionDelegate)
		});
		CheckMethod(counters, "EncounterConversation", typeof(GameMenu), "ActivateGameMenu", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(PlayerEncounter), "Start", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(PlayerEncounter), "StartBattle", critical: true);
		CheckProperty(counters, "EncounterConversation", typeof(PlayerEncounter), "EncounterState", critical: true, requireSetter: true);
		CheckProperty(counters, "EncounterConversation", typeof(PlayerEncounter), "EncounteredBattle", critical: true);
		CheckProperty(counters, "EncounterConversation", typeof(PlayerEncounter), "EncounteredParty", critical: true);
		CheckProperty(counters, "EncounterConversation", typeof(PlayerEncounter), "Battle", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(ChangeRelationAction), "ApplyInternal", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(DeclareWarAction), "ApplyInternal", critical: true);
		CheckMethod(counters, "EncounterConversation", typeof(BeHostileAction), "ApplyEncounterHostileAction", critical: true);
	}

	private static void AuditBattleAndDeployment(AuditCounters counters)
	{
		CheckType(counters, "BattleDeployment", typeof(Mission), "Mission", critical: true);
		CheckMethod(counters, "BattleDeployment", typeof(Mission), "EndMissionInternal", critical: true);
		CheckGenericZeroArgMethod(counters, "BattleDeployment", typeof(Mission), "GetMissionBehavior", critical: true);
		CheckMethod(counters, "BattleDeployment", typeof(DeploymentHandler), "FinishDeployment", critical: true);
		CheckRuntimeTypeAndMethod(counters, "BattleDeployment", "TaleWorlds.MountAndBlade.View.MissionViews.MissionMainAgentInteractionComponent", "FocusTick", critical: true);
		CheckRuntimeTypeAndMethod(counters, "BattleDeployment", "TaleWorlds.MountAndBlade.View.MissionViews.MissionMainAgentController", "OnPreMissionTick", critical: true);
		CheckRuntimeTypeAndMethod(counters, "BattleDeployment", "SandBox.View.Missions.NameMarkers.DefaultMissionNameMarkerHandler", "OnTick", critical: true);
		CheckRuntimeTypeAndMethod(counters, "BattleDeployment", "SandBox.Missions.MissionLogics.BattleAgentLogic", "OnMissionTick", critical: false);
		CheckRuntimeTypeAndMethod(counters, "BattleDeployment", "SandBox.View.Missions.MissionConversationCameraView", "UpdateAgentLooksForConversation", critical: true);
		CheckRuntimeTypeAndMethod(counters, "BattleDeployment", "SandBox.View.Missions.MissionConversationCameraView", "SetFocusedObjectForCameraFocus", critical: true);
		CheckMissionScreenBeforeTick(counters);
		CheckFieldByTypeName(counters, "BattleDeployment", "SandBox.Missions.MissionLogics.MissionFightHandler", "_playerSideAgentsOldTeamData", critical: true);
		CheckFieldByTypeName(counters, "BattleDeployment", "SandBox.Missions.MissionLogics.MissionFightHandler", "_opponentSideAgentsOldTeamData", critical: true);
		CheckFieldByTypeName(counters, "BattleDeployment", "SandBox.Missions.MissionLogics.MissionFightHandler", "_opponentSideAgents", critical: true);
		CheckFieldByTypeName(counters, "BattleDeployment", "SandBox.Missions.MissionLogics.MissionFightHandler", "_finishTimer", critical: true);
	}

	private static void AuditStealthHideoutAndSceneFlow(AuditCounters counters)
	{
		CheckRuntimeTypeAndMethod(counters, "StealthHideout", "SandBox.Conversation.MissionLogics.MissionConversationLogic", "StartConversation", critical: true, parameterTypes: new Type[3]
		{
			typeof(Agent),
			typeof(bool),
			typeof(bool)
		});
		CheckRuntimeTypeAndMethod(counters, "StealthHideout", "SandBox.Missions.MissionLogics.MissionAlleyHandler", "CheckAndTriggerConversationWithRivalThug", critical: true);
		CheckRuntimeTypeAndMethod(counters, "StealthHideout", "SandBox.Missions.MissionLogics.MissionAlleyHandler", "StartCommonAreaBattle", critical: true);
		CheckRuntimeTypeAndMethod(counters, "StealthHideout", "TaleWorlds.MountAndBlade.BasicLeaveMissionLogic", "OnEndMissionRequest", critical: true);
		CheckRuntimeTypeAndMethod(counters, "StealthHideout", "SandBox.Missions.MissionLogics.LeaveMissionLogic", "OnEndMissionRequest", critical: true);
		CheckRuntimeTypeAndMethod(counters, "StealthHideout", "SandBox.Objects.PassageUsePoint", "AfterMissionStart", critical: true);
		CheckRuntimeTypeAndMethod(counters, "StealthHideout", "SandBox.Objects.PassageUsePoint", "OnUse", critical: true, parameterTypes: new Type[2]
		{
			typeof(Agent),
			typeof(sbyte)
		});
		CheckRuntimeTypeAndMethod(counters, "StealthHideout", "SandBox.Missions.MissionLogics.MissionFacialAnimationHandler", "OnMissionTick", critical: false);
	}

	private static void AuditQuestAndIssueBridge(AuditCounters counters)
	{
		CheckMethod(counters, "QuestIssue", typeof(IssueBase), "CheckPreconditions", critical: true);
		CheckProperty(counters, "QuestIssue", typeof(IssueBase), "RewardGold", critical: false);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_sentences", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "stateMap", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_numberOfStateIndices", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_autoId", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_autoToken", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_usedIndices", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_currentSentence", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_currentSentenceText", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_lastSelectedDialogObject", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_currentRepeatedDialogSetIndex", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_currentRepeatIndex", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_dialogRepeatObjects", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_dialogRepeatLines", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_isActive", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_mainAgent", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_speakerAgent", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_listenerAgent", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_conversationAgents", critical: true);
		CheckField(counters, "QuestIssue", typeof(ConversationManager), "_conversationParty", critical: true);
		CheckProperty(counters, "QuestIssue", typeof(ConversationManager), "CurOptions", critical: true);
		CheckMethod(counters, "QuestIssue", typeof(ConversationManager), "ResetRepeatedDialogSystem", critical: true);
		CheckField(counters, "QuestIssue", typeof(QuestBase), "DiscussDialogFlow", critical: true);
	}

	private static void AuditUiAndInput(AuditCounters counters)
	{
		CheckType(counters, "UiInput", typeof(Input), "Input", critical: true);
		CheckMethod(counters, "UiInput", typeof(Input), "GetClipboardText", critical: true);
		CheckMethod(counters, "UiInput", typeof(Input), "SetClipboardText", critical: true, parameterTypes: new Type[1] { typeof(string) });
		CheckType(counters, "UiInput", typeof(ButtonWidget), "ButtonWidget", critical: true);
		CheckMethod(counters, "UiInput", typeof(ButtonWidget), "HandleClick", critical: true);
		CheckRuntimeType(counters, "UiInput", "TaleWorlds.MountAndBlade.View.Screens.MissionScreen", critical: true);
		CheckRuntimeType(counters, "UiInput", "SandBox.View.Missions.NameMarkers.DefaultMissionNameMarkerHandler", critical: true);
		CheckRuntimeType(counters, "UiInput", "SandBox.View.Missions.MissionConversationCameraView", critical: true);
	}

	private static void AuditOptionalExpansionSignals(AuditCounters counters)
	{
		Logger.LogCompatibilityAudit("CompatAudit", "OptionalNote Current audit has no direct War Sails-specific hooks; expansion impact is inferred through encounter, battle, mission, and UI systems.");
		bool flag = AccessTools.TypeByName("TaleWorlds.MountAndBlade.View.MissionViews.MissionCameraFadeView") != null;
		bool flag2 = AccessTools.TypeByName("SandBox.View.Missions.MissionCameraFadeView") != null;
		LogCheck(counters, "ExpansionSignal", flag || flag2, "MissionCameraFadeView old/new path", critical: false, "oldPath=" + flag + " newPath=" + flag2);
	}

	private static void CheckMissionScreenBeforeTick(AuditCounters counters)
	{
		Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.View.Screens.MissionScreen");
		if (type == null)
		{
			LogCheck(counters, "BattleDeployment", ok: false, "MissionScreen BeforeMissionTick-compatible hook", critical: true, "MissionScreen type missing");
			return;
		}
		MethodInfo methodInfo = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(delegate(MethodInfo m)
		{
			if (!m.Name.Contains("BeforeMissionTick"))
			{
				return false;
			}
			ParameterInfo[] parameters = m.GetParameters();
			return parameters.Length == 2 && parameters[0].ParameterType.FullName == "TaleWorlds.MountAndBlade.Mission" && parameters[1].ParameterType == typeof(float);
		});
		LogCheck(counters, "BattleDeployment", methodInfo != null, "MissionScreen BeforeMissionTick-compatible hook", critical: true, methodInfo?.Name ?? "not found");
	}

	private static void CheckConversationStartEntry(AuditCounters counters)
	{
		MethodInfo methodInfo = FindMethod(typeof(ConversationManager), "StartConversation");
		MethodInfo methodInfo2 = FindMethod(typeof(ConversationManager), "BeginConversation", null, 0);
		MethodInfo methodInfo3 = FindMethod(typeof(ConversationManager), "StartNew", null, 2);
		MethodInfo methodInfo4 = FindMethod(typeof(ConversationManager), "SetupAndStartMissionConversation", null, 3);
		bool flag = methodInfo != null || (methodInfo2 != null && methodInfo3 != null && methodInfo4 != null);
		string text = $"legacyStart={(methodInfo != null)} begin={(methodInfo2 != null)} startNew={(methodInfo3 != null)} missionStart={(methodInfo4 != null)}";
		LogCheck(counters, "EncounterConversation", flag, "ConversationManager conversation start entry", critical: true, text);
	}

	private static void CheckType(AuditCounters counters, string source, Type type, string label, bool critical)
	{
		LogCheck(counters, source, type != null, label, critical);
	}

	private static void CheckRuntimeType(AuditCounters counters, string source, string typeName, bool critical)
	{
		Type typeByName = AccessTools.TypeByName(typeName);
		LogCheck(counters, source, typeByName != null, typeName, critical);
	}

	private static void CheckMethod(AuditCounters counters, string source, Type type, string methodName, bool critical, Type[] parameterTypes = null, int? parameterCount = null)
	{
		MethodInfo methodInfo = FindMethod(type, methodName, parameterTypes, parameterCount);
		string text = (methodInfo == null) ? "not found" : DescribeMethodSignature(methodInfo);
		LogCheck(counters, source, methodInfo != null, ((type != null) ? type.Name : "null") + "." + methodName, critical, text);
	}

	private static void CheckRuntimeTypeAndMethod(AuditCounters counters, string source, string typeName, string methodName, bool critical, Type[] parameterTypes = null, int? parameterCount = null)
	{
		Type typeByName = AccessTools.TypeByName(typeName);
		if (typeByName == null)
		{
			LogCheck(counters, source, ok: false, typeName + "." + methodName, critical, "type missing");
			return;
		}
		MethodInfo methodInfo = FindMethod(typeByName, methodName, parameterTypes, parameterCount);
		string text = (methodInfo == null) ? "not found" : DescribeMethodSignature(methodInfo);
		LogCheck(counters, source, methodInfo != null, typeName + "." + methodName, critical, text);
	}

	private static void CheckGenericZeroArgMethod(AuditCounters counters, string source, Type type, string methodName, bool critical)
	{
		MethodInfo methodInfo = null;
		try
		{
			methodInfo = type?.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo m) => m.Name == methodName && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
		}
		catch
		{
			methodInfo = null;
		}
		string text = (methodInfo == null) ? "not found" : DescribeMethodSignature(methodInfo);
		LogCheck(counters, source, methodInfo != null, ((type != null) ? type.Name : "null") + "." + methodName + "<T>", critical, text);
	}

	private static void CheckProperty(AuditCounters counters, string source, Type type, string propertyName, bool critical, bool requireSetter = false)
	{
		PropertyInfo propertyInfo = null;
		try
		{
			propertyInfo = AccessTools.Property(type, propertyName);
		}
		catch
		{
			propertyInfo = null;
		}
		bool flag = propertyInfo != null && (!requireSetter || propertyInfo.GetSetMethod(nonPublic: true) != null);
		string text = "not found";
		if (propertyInfo != null)
		{
			text = propertyInfo.PropertyType.Name + (requireSetter ? (" setter=" + (propertyInfo.GetSetMethod(nonPublic: true) != null)) : "");
		}
		LogCheck(counters, source, flag, ((type != null) ? type.Name : "null") + "." + propertyName, critical, text);
	}

	private static void CheckField(AuditCounters counters, string source, Type type, string fieldName, bool critical)
	{
		FieldInfo fieldInfo = null;
		try
		{
			fieldInfo = AccessTools.Field(type, fieldName);
		}
		catch
		{
			fieldInfo = null;
		}
		string text = (fieldInfo == null) ? "not found" : fieldInfo.FieldType.Name;
		LogCheck(counters, source, fieldInfo != null, ((type != null) ? type.Name : "null") + "." + fieldName, critical, text);
	}

	private static void CheckFieldByTypeName(AuditCounters counters, string source, string typeName, string fieldName, bool critical)
	{
		Type typeByName = AccessTools.TypeByName(typeName);
		if (typeByName == null)
		{
			LogCheck(counters, source, ok: false, typeName + "." + fieldName, critical, "type missing");
			return;
		}
		CheckField(counters, source, typeByName, fieldName, critical);
	}

	private static MethodInfo FindMethod(Type type, string methodName, Type[] parameterTypes = null, int? parameterCount = null)
	{
		try
		{
			if (type == null || string.IsNullOrWhiteSpace(methodName))
			{
				return null;
			}
			if (parameterTypes != null)
			{
				return AccessTools.Method(type, methodName, parameterTypes);
			}
			IEnumerable<MethodInfo> source = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where((MethodInfo m) => m.Name == methodName);
			if (parameterCount.HasValue)
			{
				source = source.Where((MethodInfo m) => m.GetParameters().Length == parameterCount.Value);
			}
			return source.FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	private static string DescribeMethodSignature(MethodInfo method)
	{
		try
		{
			if (method == null)
			{
				return "null";
			}
			string text = string.Join(", ", method.GetParameters().Select((ParameterInfo p) => p.ParameterType.Name));
			return method.ReturnType.Name + " " + method.Name + "(" + text + ")";
		}
		catch
		{
			return method?.Name ?? "unknown";
		}
	}

	private static void LogCheck(AuditCounters counters, string source, bool ok, string target, bool critical, string detail = null)
	{
		string text = ok ? "OK" : "FAIL";
		string text2 = critical ? "CRITICAL" : "OPTIONAL";
		if (critical)
		{
			if (ok)
			{
				counters.CriticalOk++;
			}
			else
			{
				counters.CriticalFail++;
			}
		}
		else if (ok)
		{
			counters.OptionalOk++;
		}
		else
		{
			counters.OptionalFail++;
		}
		string text3 = string.IsNullOrWhiteSpace(detail) ? "" : (" | " + detail);
		Logger.LogCompatibilityAudit(source, $"[{text2}] [{text}] {target}{text3}");
	}
}
