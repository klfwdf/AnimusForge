using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;

namespace AnimusForge;

internal static class PlayerEncounterPropertySafePatch
{
	private static bool _patched;

	internal static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			MethodInfo methodInfo = AccessTools.PropertyGetter(typeof(PlayerEncounter), "EncounteredBattle");
			MethodInfo methodInfo2 = AccessTools.PropertyGetter(typeof(PlayerEncounter), "CampaignBattleResult");
			MethodInfo methodInfo3 = AccessTools.PropertyGetter(typeof(PlayerEncounter), "Battle");
			MethodInfo methodInfo4 = AccessTools.Method(typeof(PlayerEncounterPropertySafePatch), "EncounteredBattlePrefix");
			MethodInfo methodInfo5 = AccessTools.Method(typeof(PlayerEncounterPropertySafePatch), "CampaignBattleResultPrefix");
			MethodInfo methodInfo6 = AccessTools.Method(typeof(PlayerEncounterPropertySafePatch), "BattlePrefix");
			Harmony harmony = new Harmony("AnimusForge.playerencounter.property.safe");
			if (methodInfo != null && methodInfo4 != null)
			{
				harmony.Patch(methodInfo, new HarmonyMethod(methodInfo4));
				Logger.LogTrace("System", "PlayerEncounter.EncounteredBattle safe prefix registered.");
			}
			else
			{
				Logger.LogTrace("System", "PlayerEncounter.EncounteredBattle getter not found; safe prefix skipped.");
			}
			if (methodInfo2 != null && methodInfo5 != null)
			{
				harmony.Patch(methodInfo2, new HarmonyMethod(methodInfo5));
				Logger.LogTrace("System", "PlayerEncounter.CampaignBattleResult safe prefix registered.");
			}
			else
			{
				Logger.LogTrace("System", "PlayerEncounter.CampaignBattleResult getter not found; safe prefix skipped.");
			}
			if (methodInfo3 != null && methodInfo6 != null)
			{
				harmony.Patch(methodInfo3, new HarmonyMethod(methodInfo6));
				Logger.LogTrace("System", "PlayerEncounter.Battle safe prefix registered.");
			}
			else
			{
				Logger.LogTrace("System", "PlayerEncounter.Battle getter not found; safe prefix skipped.");
			}
			_patched = true;
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ PlayerEncounterPropertySafePatch 注册失败: " + ex.Message);
		}
	}

	public static bool EncounteredBattlePrefix(ref MapEvent __result)
	{
		__result = PlayerEncounterCompat.GetEncounteredBattleSafe();
		return false;
	}

	public static bool CampaignBattleResultPrefix(ref CampaignBattleResult __result)
	{
		__result = PlayerEncounterCompat.GetCampaignBattleResultSafe();
		return false;
	}

	public static bool BattlePrefix(ref MapEvent __result)
	{
		__result = PlayerEncounterCompat.GetBattleSafe();
		return false;
	}
}
