using System;
using System.Reflection;
using AnimusForge.SiegeAftermathIntervention;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class GcczVolunteerRecruitmentRatePatch
{
	private const string HarmonyId = "AnimusForge.gccz.volunteer.recruitment.rate";

	private static bool _patched;

	public static void EnsurePatched()
	{
		if (_patched || Campaign.Current?.Models?.VolunteerModel == null)
		{
			return;
		}
		try
		{
			object volunteerModel = Campaign.Current.Models.VolunteerModel;
			MethodInfo target = AccessTools.Method(
				volunteerModel.GetType(),
				"GetDailyVolunteerProductionProbability",
				new[] { typeof(Hero), typeof(int), typeof(Settlement) });
			if (target == null)
			{
				Logger.Log("SiegeAiIntervention", "GCCZ recruitment-rate patch target was not found.");
				return;
			}
			HarmonyMethod postfix = new HarmonyMethod(typeof(GcczVolunteerRecruitmentRatePatch), nameof(Postfix))
			{
				priority = Priority.Last,
				after = new[] { "com.AnimusForge.custompolicy.settlementmodels" }
			};
			new Harmony(HarmonyId).Patch(target, postfix: postfix);
			_patched = true;
			Logger.Log("SiegeAiIntervention", "GCCZ recruitment-rate patch enabled: " + target.DeclaringType?.FullName);
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "GCCZ recruitment-rate patch failed: " + ex.Message);
		}
	}

	private static void Postfix(Hero hero, int index, Settlement settlement, ref float __result)
	{
		try
		{
			if (hero?.VolunteerTypes == null
				|| index < 0
				|| index >= hero.VolunteerTypes.Length
				|| hero.VolunteerTypes[index] != null
				|| !SiegeAiInterventionBehavior.TryGetActiveRecruitmentRateMultiplier(settlement, out float multiplier))
			{
				return;
			}
			__result = MathF.Max(0f, MathF.Min(1f, __result * multiplier));
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "GCCZ recruitment-rate postfix failed: " + ex.Message);
		}
	}
}
