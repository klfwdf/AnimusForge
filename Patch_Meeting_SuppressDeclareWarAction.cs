using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

[HarmonyPatch(typeof(DeclareWarAction), "ApplyInternal")]
public static class Patch_Meeting_SuppressDeclareWarAction
{
	private static bool Prefix(
		IFaction faction1,
		IFaction faction2,
		DeclareWarAction.DeclareWarDetail declareWarDetail)
	{
		bool bypassMeetingGuard = false;
		if (VassalageBehavior.IsApplyingVassalageDiplomacy && VassalageBehavior.CanApplyVassalageDiplomacyNowForExternal)
		{
			VassalageDiagnosticLog.Event("meeting_suppress.declare_war.allow_vassalage_diplomacy", new Dictionary<string, object>
			{
				["faction1"] = VassalageDiagnosticLog.DescribeKingdom(faction1 as Kingdom),
				["faction2"] = VassalageDiagnosticLog.DescribeKingdom(faction2 as Kingdom),
				["detail"] = declareWarDetail
			});
			bypassMeetingGuard = true;
		}
		if (!bypassMeetingGuard
			&& DiplomacyRecentPeaceGuard.ShouldBlockDeclareWar(faction1, faction2, declareWarDetail, "DeclareWarAction.ApplyInternal"))
		{
			return false;
		}
		if (!bypassMeetingGuard && MeetingBattleRuntime.ShouldBlockDiplomaticSideEffects)
		{
			Logger.Log("MeetingBattle", "Blocked DeclareWarAction.ApplyInternal during meeting-safe phase.");
			return false;
		}
		return PermanentAllianceGuard.ShouldAllowDeclareWar(
			faction1,
			faction2,
			declareWarDetail);
	}
}
