using System;
using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

[HarmonyPatch(typeof(Module), "OnApplicationTick")]
public static class Patch_TriggerMassiveHook
{
	private static bool _initialized;

	private static float _lastF10Time;

	public static void Postfix(float dt)
	{
		using (PerfProbe.Scope("Patch_TriggerMassiveHook.Postfix"))
		{
		if (!_initialized && TraceHelper.IsEnabled)
		{
			_initialized = true;
			try
			{
				using (PerfProbe.Scope("Patch_TriggerMassiveHook.DynamicPatcher.DoMassiveHook"))
				{
					Harmony harmony = new Harmony("my.dynamic.patcher.runtime");
					DynamicPatcher.DoMassiveHook(harmony);
				}
			}
			catch (Exception ex)
			{
				Logger.LogTrace("System", "拦截器异常: " + ex.Message);
			}
		}
		using (PerfProbe.Scope("Patch_TriggerMassiveHook.EnsurePatchedGroup"))
		{
			NameMarkerSafePatch.EnsurePatched();
			MissionScreenSafePatch.EnsurePatched();
			MissionUiInterruptionPatch.EnsurePatched();
			CriticalUiLipSyncTeardownPatch.EnsurePatched();
			EndMissionInternalSafePatch.EnsurePatched();
			ConversationCameraSafePatch.EnsurePatched();
			InteractionComponentSafePatch.EnsurePatched();
			MainAgentControllerSafePatch.EnsurePatched();
			PassageUsePointSafePatch.EnsurePatched();
			ConversationManagerSafePatch.EnsurePatched();
			ProcessSentenceSafePatch.EnsurePatched();
			ProcessPartnerSentenceSafePatch.EnsurePatched();
			ContinueConversationSafePatch.EnsurePatched();
			MeetingTargetWieldBlockPatch.EnsurePatched();
			SceneTauntWieldBlockPatch.EnsurePatched();
			MeetingDuelBattleAgentLogicSafePatch.EnsurePatched();
			AgentVictoryRetreatNullTeamSafePatch.EnsurePatched();
			LipSyncFacialAnimSuppressPatch.EnsurePatched();
		}
		try
		{
			using (PerfProbe.Scope("Patch_TriggerMassiveHook.DuelBehavior.GlobalArenaLeaveTick"))
			{
				DuelBehavior.GlobalArenaLeaveTick();
			}
		}
		catch
		{
		}
		try
		{
			using (PerfProbe.Scope("Patch_TriggerMassiveHook.DuelBehavior.GlobalSourceMissionLeaveTick"))
			{
				DuelBehavior.GlobalSourceMissionLeaveTick();
			}
		}
		catch
		{
		}
		try
		{
			using (PerfProbe.Scope("Patch_TriggerMassiveHook.DuelBehavior.GlobalDuelStarterTick"))
			{
				DuelBehavior.GlobalDuelStarterTick();
			}
		}
		catch
		{
		}
		try
		{
			using (PerfProbe.Scope("Patch_TriggerMassiveHook.DuelBehavior.GlobalWildernessDuelEncounterMenuGuardTick"))
			{
				DuelBehavior.GlobalWildernessDuelEncounterMenuGuardTick();
			}
		}
		catch
		{
		}
		try
		{
			using (PerfProbe.Scope("Patch_TriggerMassiveHook.DuelBehavior.GlobalPendingMainHeroDeathTick"))
			{
				DuelBehavior.GlobalPendingMainHeroDeathTick();
			}
		}
		catch
		{
		}
		if (TraceHelper.IsEnabled && Input.IsKeyPressed(InputKey.F10) && Time.ApplicationTime - _lastF10Time > 0.5f)
		{
			_lastF10Time = Time.ApplicationTime;
			ForceDumpAllAgents();
		}
		}
	}

	private static void ForceDumpAllAgents()
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (mission == null || agents == null)
		{
			Logger.LogTrace("System", "⚠\ufe0f 当前不在场景中，无法获取动作。");
			return;
		}
		int num = 0;
		Logger.LogTrace("System", "\ud83d\udcf8 ================= [F10] 全场动作点名 ================= \ud83d\udcf8");
		foreach (Agent agent in agents)
		{
			if (agent == null)
			{
				continue;
			}
			try
			{
				string text = "Unknown";
				if (agent.Character != null)
				{
					text = agent.Character.Name.ToString();
				}
				else
				{
					string agentName = agent.Name?.ToString();
					if (!string.IsNullOrEmpty(agentName))
					{
						text = agentName;
					}
				}
				string text2 = agent.Character?.StringId ?? "No_ID";
				ActionIndexCache currentAction = agent.GetCurrentAction(0);
				string text3 = currentAction.ToString();
				if (string.IsNullOrEmpty(text3) || text3.Contains("ActionIndexCache"))
				{
					text3 = $"Index_{currentAction.Index}";
				}
				float currentActionProgress = agent.GetCurrentActionProgress(0);
				Logger.LogTrace("Snapshot", $"\ud83d\udc49 [点名] {text} (ID:{text2}) | 动作: {text3} | 进度: {currentActionProgress:P0}");
				num++;
			}
			catch (Exception ex)
			{
				Logger.LogTrace("Snapshot", "[F10] skipped agent dump. index=" + agent.Index + ", error=" + ex.Message);
			}
		}
		Logger.LogTrace("System", $"\ud83d\udcf8 ================= 点名结束 (共 {num} 人) ================= \ud83d\udcf8");
	}
}
