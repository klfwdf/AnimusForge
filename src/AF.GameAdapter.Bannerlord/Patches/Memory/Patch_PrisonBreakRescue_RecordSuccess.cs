using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

[HarmonyPatch(typeof(EndCaptivityAction), nameof(EndCaptivityAction.ApplyByEscape), new Type[] { typeof(Hero), typeof(Hero), typeof(bool) })]
public static class Patch_PrisonBreakRescue_RecordSuccess
{
	public static void Postfix(Hero character, Hero facilitator, bool showNotification)
	{
		try
		{
			if (character == null || character == Hero.MainHero || facilitator != Hero.MainHero)
			{
				return;
			}
			MyBehavior.RecordPlayerPrisonBreakRescueForExternal(character);
			PlayerNotorietyBehavior.RecordPlayerPrisonBreakRescueForExternal(character);
		}
		catch (Exception ex)
		{
			Logger.Log("PrisonBreakRescue", "record success failed: " + ex.Message);
		}
	}
}
