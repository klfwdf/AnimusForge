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
		if (!_initialized)
		{
			_initialized = true;
			try
			{
				Harmony harmony = new Harmony("my.dynamic.patcher.runtime");
				DynamicPatcher.DoMassiveHook(harmony);
			}
			catch (Exception ex)
			{
				Logger.LogTrace("System", "拦截器异常: " + ex.Message);
			}
		}
		NameMarkerSafePatch.EnsurePatched();
		MissionScreenSafePatch.EnsurePatched();
		MissionUiInterruptionPatch.EnsurePatched();
		EndMissionInternalSafePatch.EnsurePatched();
		ConversationCameraSafePatch.EnsurePatched();
		InteractionComponentSafePatch.EnsurePatched();
		MainAgentControllerSafePatch.EnsurePatched();
		PassageUsePointSafePatch.EnsurePatched();
		ConversationManagerSafePatch.EnsurePatched();
		ProcessSentenceSafePatch.EnsurePatched();
		ProcessPartnerSentenceSafePatch.EnsurePatched();
		ContinueConversationSafePatch.EnsurePatched();
		ConversationVMCapturePatch.EnsurePatched();
		MeetingTargetWieldBlockPatch.EnsurePatched();
		MeetingDuelBattleAgentLogicSafePatch.EnsurePatched();
		LipSyncFacialAnimSuppressPatch.EnsurePatched();
		try
		{
			ConversationHelper.Tick();
		}
		catch
		{
		}
		try
		{
			DuelBehavior.GlobalArenaLeaveTick();
		}
		catch
		{
		}
		try
		{
			DuelBehavior.GlobalSourceMissionLeaveTick();
		}
		catch
		{
		}
		try
		{
			DuelBehavior.GlobalDuelStarterTick();
		}
		catch
		{
		}
		try
		{
			DuelBehavior.GlobalPendingMainHeroDeathTick();
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

	private static void ForceDumpAllAgents()
	{
		if (Mission.Current == null || Mission.Current.Agents == null)
		{
			Logger.LogTrace("System", "⚠\ufe0f 当前不在场景中，无法获取动作。");
			return;
		}
		int num = 0;
		Logger.LogTrace("System", "\ud83d\udcf8 ================= [F10] 全场动作点名 ================= \ud83d\udcf8");
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent != null)
			{
				string text = "Unknown";
				if (agent.Character != null)
				{
					text = agent.Character.Name.ToString();
				}
				else if (!string.IsNullOrEmpty(agent.Name.ToString()))
				{
					text = agent.Name.ToString();
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
		}
		Logger.LogTrace("System", $"\ud83d\udcf8 ================= 点名结束 (共 {num} 人) ================= \ud83d\udcf8");
	}
}
